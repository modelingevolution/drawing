using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Result of a rigid alignment (rotation + translation) between two 2D point clouds.
/// </summary>
public readonly record struct AlignmentResult<T>(
    Matrix2x2<T> Rotation,
    Vector<T> Translation,
    T Error)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// The rotation angle extracted from the rotation matrix.
    /// </summary>
    public Radian<T> Angle => Radian<T>.FromRadian(T.Atan2(Rotation.M21, Rotation.M11));

    /// <summary>
    /// Transforms a single point by this alignment: R · p + t.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point<T> ApplyTo(Point<T> point) =>
        Rotation.Transform(point) + Translation;

    /// <summary>
    /// Transforms a span of points, returning pool-backed memory.
    /// </summary>
    public ReadOnlyMemory<Point<T>> ApplyTo(ReadOnlySpan<Point<T>> points)
    {
        var mem = Alloc.Memory<Point<T>>(points.Length);
        var dst = mem.Span;
        for (int i = 0; i < points.Length; i++)
            dst[i] = Rotation.Transform(points[i]) + Translation;
        return mem;
    }
}

/// <summary>
/// Static methods for rigid alignment between 2D point clouds.
/// </summary>
public static class Alignment
{
    /// <summary>
    /// Computes a PCA-based rigid alignment (rotation + translation) that best maps
    /// <paramref name="source"/> onto <paramref name="target"/>, minimizing sum of squared
    /// nearest-neighbor distances. Handles symmetry by testing 4 candidate rotations.
    /// </summary>
    public static AlignmentResult<T> Pca<T>(
        ReadOnlySpan<Point<T>> source,
        ReadOnlySpan<Point<T>> target)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (source.Length == 0 || target.Length == 0)
            return new AlignmentResult<T>(Matrix2x2<T>.Identity, new Vector<T>(T.Zero, T.Zero), T.Zero);

        // 1. Centroids
        var centroidA = ComputeCentroid(source);
        var centroidB = ComputeCentroid(target);

        // 2. Covariance matrices
        var covA = Matrix2x2<T>.CovarianceMatrix(source, centroidA);
        var covB = Matrix2x2<T>.CovarianceMatrix(target, centroidB);

        // Degenerate guard: if variance is near-zero, return pure translation
        var eps = T.CreateTruncating(1e-12);
        if (T.Abs(covA.Determinant) < eps || T.Abs(covB.Determinant) < eps)
        {
            var t = centroidB - centroidA;
            return new AlignmentResult<T>(Matrix2x2<T>.Identity, t, ComputeTranslationError(source, t, target));
        }

        // 3. Eigendecompose
        var (_, vA1, _, vA2) = covA.Eigen();
        var (_, vB1, _, vB2) = covB.Eigen();

        // 4. Build KD-tree from target for error measurement
        var tree = KdTree<T>.Build(target);

        // 5. Test 4 candidate rotations (sign-flipped eigenvectors)
        var bestRotation = Matrix2x2<T>.Identity;
        var bestTranslation = new Vector<T>(T.Zero, T.Zero);
        var bestError = T.MaxValue;

        var frameB = FromColumns(vB1, vB2);

        Span<int> signs = stackalloc int[] { 1, -1 };
        foreach (var s1 in signs)
        foreach (var s2 in signs)
        {
            var flippedA1 = s1 == 1 ? vA1 : new Vector<T>(-vA1.X, -vA1.Y);
            var flippedA2 = s2 == 1 ? vA2 : new Vector<T>(-vA2.X, -vA2.Y);
            var frameA = FromColumns(flippedA1, flippedA2);
            var rotation = frameB * frameA.Transpose();

            // Skip reflections (det < 0)
            if (rotation.Determinant < T.Zero)
                continue;

            var translation = centroidB - rotation.Transform(centroidA);
            var error = ComputeError(source, rotation, translation, tree);

            if (error < bestError)
            {
                bestError = error;
                bestRotation = rotation;
                bestTranslation = translation;
            }
        }

        return new AlignmentResult<T>(bestRotation, bestTranslation, bestError);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Point<T> ComputeCentroid<T>(ReadOnlySpan<Point<T>> points)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var cx = T.Zero;
        var cy = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            cx += points[i].X;
            cy += points[i].Y;
        }
        var n = T.CreateTruncating(points.Length);
        return new Point<T>(cx / n, cy / n);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix2x2<T> FromColumns<T>(Vector<T> c1, Vector<T> c2)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
        => new(c1.X, c2.X, c1.Y, c2.Y);

    private static T ComputeError<T>(
        ReadOnlySpan<Point<T>> source,
        Matrix2x2<T> rotation,
        Vector<T> translation,
        KdTree<T> targetTree)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var error = T.Zero;
        for (int i = 0; i < source.Length; i++)
        {
            var transformed = rotation.Transform(source[i]) + translation;
            var (_, _, distSq) = targetTree.NearestNeighbour(transformed);
            error += distSq;
        }
        return error;
    }

    private static T ComputeTranslationError<T>(
        ReadOnlySpan<Point<T>> source,
        Vector<T> translation,
        ReadOnlySpan<Point<T>> target)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var tree = KdTree<T>.Build(target);
        var error = T.Zero;
        for (int i = 0; i < source.Length; i++)
        {
            var translated = source[i] + translation;
            var (_, _, distSq) = tree.NearestNeighbour(translated);
            error += distSq;
        }
        return error;
    }
}
