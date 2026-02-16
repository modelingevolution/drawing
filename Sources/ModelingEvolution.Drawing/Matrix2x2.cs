using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A 2×2 matrix stored as four fields. Supports basic linear algebra
/// operations including eigendecomposition of symmetric matrices (closed-form).
/// </summary>
[Matrix2x2JsonConverter]
public readonly record struct Matrix2x2<T>(T M11, T M12, T M21, T M22)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Four = T.CreateTruncating(4);
    private static readonly T Half = T.One / Two;

    /// <summary>The identity matrix.</summary>
    public static Matrix2x2<T> Identity => new(T.One, T.Zero, T.Zero, T.One);

    /// <summary>The zero matrix.</summary>
    public static Matrix2x2<T> Zero => new(T.Zero, T.Zero, T.Zero, T.Zero);

    /// <summary>The determinant: M11*M22 - M12*M21.</summary>
    public T Determinant
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => M11 * M22 - M12 * M21;
    }

    /// <summary>The trace: M11 + M22.</summary>
    public T Trace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => M11 + M22;
    }

    /// <summary>Returns the transpose of this matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix2x2<T> Transpose() => new(M11, M21, M12, M22);

    /// <summary>
    /// Returns the inverse of this matrix.
    /// Throws if the matrix is singular (determinant ≈ 0).
    /// </summary>
    public Matrix2x2<T> Inverse()
    {
        var det = Determinant;
        if (T.Abs(det) < T.CreateTruncating(1e-15))
            throw new InvalidOperationException("Matrix is singular.");
        var invDet = T.One / det;
        return new(M22 * invDet, -M12 * invDet, -M21 * invDet, M11 * invDet);
    }

    /// <summary>Matrix addition.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> operator +(in Matrix2x2<T> a, in Matrix2x2<T> b) =>
        new(a.M11 + b.M11, a.M12 + b.M12, a.M21 + b.M21, a.M22 + b.M22);

    /// <summary>Matrix subtraction.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> operator -(in Matrix2x2<T> a, in Matrix2x2<T> b) =>
        new(a.M11 - b.M11, a.M12 - b.M12, a.M21 - b.M21, a.M22 - b.M22);

    /// <summary>Matrix multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> operator *(in Matrix2x2<T> a, in Matrix2x2<T> b) =>
        new(a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22);

    /// <summary>Scalar multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> operator *(in Matrix2x2<T> m, T s) =>
        new(m.M11 * s, m.M12 * s, m.M21 * s, m.M22 * s);

    /// <summary>Scalar multiplication (commutative).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> operator *(T s, in Matrix2x2<T> m) => m * s;

    /// <summary>Transforms a vector by this matrix: result = M · v.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector<T> Transform(Vector<T> v) =>
        new(M11 * v.X + M12 * v.Y, M21 * v.X + M22 * v.Y);

    /// <summary>Transforms a point by this matrix: result = M · p.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point<T> Transform(Point<T> p) =>
        new(M11 * p.X + M12 * p.Y, M21 * p.X + M22 * p.Y);

    /// <summary>Matrix-vector multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<T> operator *(in Matrix2x2<T> m, Vector<T> v) => m.Transform(v);

    /// <summary>
    /// Creates a 2D rotation matrix for the given angle.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> Rotation(Radian<T> angle)
    {
        var c = Radian.Cos(angle);
        var s = Radian.Sin(angle);
        return new(c, -s, s, c);
    }

    /// <summary>
    /// Creates a 2D scaling matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2<T> Scale(T sx, T sy) => new(sx, T.Zero, T.Zero, sy);

    // ── Eigendecomposition (symmetric 2×2) ──

    /// <summary>
    /// Computes the eigenvalues of a symmetric 2×2 matrix using the closed-form quadratic.
    /// Returns (λ1, λ2) where λ1 ≥ λ2.
    /// For non-symmetric matrices the result is undefined.
    /// </summary>
    public (T Lambda1, T Lambda2) Eigenvalues()
    {
        // Characteristic equation: λ² - trace·λ + det = 0
        var tr = Trace;
        var det = Determinant;
        var disc = tr * tr - Four * det;
        if (disc < T.Zero) disc = T.Zero; // clamp numerical noise for symmetric matrices
        var sqrtDisc = T.Sqrt(disc);
        var l1 = (tr + sqrtDisc) * Half;
        var l2 = (tr - sqrtDisc) * Half;
        return (l1, l2);
    }

    /// <summary>
    /// Computes the eigenvectors of a symmetric 2×2 matrix (closed-form).
    /// Returns two unit eigenvectors corresponding to the eigenvalues from <see cref="Eigenvalues"/>.
    /// The eigenvectors are orthogonal for symmetric matrices.
    /// </summary>
    public (Vector<T> V1, Vector<T> V2) Eigenvectors()
    {
        var (l1, l2) = Eigenvalues();
        return (EigenvectorFor(l1), EigenvectorFor(l2));
    }

    /// <summary>
    /// Full eigendecomposition: eigenvalues and their corresponding eigenvectors.
    /// </summary>
    public (T Lambda1, Vector<T> V1, T Lambda2, Vector<T> V2) Eigen()
    {
        var (l1, l2) = Eigenvalues();
        return (l1, EigenvectorFor(l1), l2, EigenvectorFor(l2));
    }

    private Vector<T> EigenvectorFor(T lambda)
    {
        // (A - λI)v = 0 → use the row that has larger magnitude to avoid numerical issues
        var a = M11 - lambda;
        var b = M12;
        var c = M21;
        var d = M22 - lambda;

        // Pick the row with larger norm for numerical stability
        var row1Sq = a * a + b * b;
        var row2Sq = c * c + d * d;

        T vx, vy;
        if (row1Sq >= row2Sq)
        {
            if (row1Sq < T.CreateTruncating(1e-30))
                return new Vector<T>(T.One, T.Zero); // degenerate — identity eigenvalue
            // null-space of [a, b]: v = (-b, a) normalized
            vx = -b;
            vy = a;
        }
        else
        {
            // null-space of [c, d]: v = (-d, c) normalized
            vx = -d;
            vy = c;
        }

        var len = T.Sqrt(vx * vx + vy * vy);
        if (len < T.CreateTruncating(1e-30))
            return new Vector<T>(T.One, T.Zero);
        return new Vector<T>(vx / len, vy / len);
    }

    // ── Covariance ──

    /// <summary>
    /// Computes the 2×2 covariance matrix of a set of points (centered around their centroid).
    /// </summary>
    public static Matrix2x2<T> CovarianceMatrix(ReadOnlySpan<Point<T>> points)
    {
        if (points.Length == 0) return Zero;

        // Compute centroid
        var cx = T.Zero;
        var cy = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            cx += points[i].X;
            cy += points[i].Y;
        }
        var n = T.CreateTruncating(points.Length);
        cx /= n;
        cy /= n;

        // Compute covariance entries
        var cxx = T.Zero;
        var cxy = T.Zero;
        var cyy = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            var dx = points[i].X - cx;
            var dy = points[i].Y - cy;
            cxx += dx * dx;
            cxy += dx * dy;
            cyy += dy * dy;
        }
        var invN = T.One / n;
        return new Matrix2x2<T>(cxx * invN, cxy * invN, cxy * invN, cyy * invN);
    }

    /// <summary>
    /// Computes the 2×2 covariance matrix of a set of points around a known centroid.
    /// </summary>
    public static Matrix2x2<T> CovarianceMatrix(ReadOnlySpan<Point<T>> points, Point<T> centroid)
    {
        if (points.Length == 0) return Zero;

        var cxx = T.Zero;
        var cxy = T.Zero;
        var cyy = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            var dx = points[i].X - centroid.X;
            var dy = points[i].Y - centroid.Y;
            cxx += dx * dx;
            cxy += dx * dy;
            cyy += dy * dy;
        }
        var invN = T.One / T.CreateTruncating(points.Length);
        return new Matrix2x2<T>(cxx * invN, cxy * invN, cxy * invN, cyy * invN);
    }

    public override string ToString() =>
        $"[{M11}, {M12}; {M21}, {M22}]";
}
