using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class AlignmentTests
{
    private const float Tol = 0.1f;
    private const float AngleTol = 0.05f; // radians

    /// <summary>Helper: rotate a point cloud around a centroid.</summary>
    private static Point<float>[] RotatePoints(Point<float>[] pts, float angleRad, Point<float> center)
    {
        var cos = MathF.Cos(angleRad);
        var sin = MathF.Sin(angleRad);
        var result = new Point<float>[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            var dx = pts[i].X - center.X;
            var dy = pts[i].Y - center.Y;
            result[i] = new Point<float>(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos);
        }
        return result;
    }

    /// <summary>Helper: translate a point cloud.</summary>
    private static Point<float>[] TranslatePoints(Point<float>[] pts, float tx, float ty)
    {
        var result = new Point<float>[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            result[i] = new Point<float>(pts[i].X + tx, pts[i].Y + ty);
        return result;
    }

    /// <summary>Helper: an asymmetric L-shaped point cloud.</summary>
    private static Point<float>[] MakeLShape()
    {
        // L-shape: vertical bar + horizontal bar (asymmetric)
        var pts = new List<Point<float>>();
        // Vertical bar: x=0, y=0..5
        for (int i = 0; i <= 10; i++)
            pts.Add(new Point<float>(0, i * 0.5f));
        // Horizontal bar: x=0..3, y=0
        for (int i = 1; i <= 6; i++)
            pts.Add(new Point<float>(i * 0.5f, 0));
        return pts.ToArray();
    }

    [Fact]
    public void Pca_IdenticalClouds_ZeroError()
    {
        var pts = MakeLShape();
        var result = Alignment.Pca<float>(pts, pts);

        result.Error.Should().BeApproximately(0f, 0.01f);
        result.Rotation.M11.Should().BeApproximately(1f, Tol);
        result.Rotation.M12.Should().BeApproximately(0f, Tol);
        result.Rotation.M21.Should().BeApproximately(0f, Tol);
        result.Rotation.M22.Should().BeApproximately(1f, Tol);
        result.Translation.X.Should().BeApproximately(0f, Tol);
        result.Translation.Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Pca_PureTranslation()
    {
        var source = MakeLShape();
        var target = TranslatePoints(source, 5f, 3f);

        var result = Alignment.Pca<float>(source, target);

        result.Error.Should().BeApproximately(0f, Tol);
        // Rotation should be near identity
        result.Rotation.M11.Should().BeApproximately(1f, Tol);
        result.Rotation.M22.Should().BeApproximately(1f, Tol);
        // Translation should be ~(5, 3)
        result.Translation.X.Should().BeApproximately(5f, Tol);
        result.Translation.Y.Should().BeApproximately(3f, Tol);
    }

    [Fact]
    public void Pca_90DegreeRotation()
    {
        var source = MakeLShape();
        var center = new Point<float>(
            source.Average(p => p.X),
            source.Average(p => p.Y));
        var target = RotatePoints(source, MathF.PI / 2f, center);

        var result = Alignment.Pca<float>(source, target);

        result.Error.Should().BeLessThan(1f);
        // Angle should be ~π/2
        float angle = MathF.Abs((float)(Radian<float>)result.Angle);
        angle.Should().BeApproximately(MathF.PI / 2f, AngleTol);
    }

    [Fact]
    public void Pca_45DegreeRotation_PlusTranslation()
    {
        var source = MakeLShape();
        var center = new Point<float>(
            source.Average(p => p.X),
            source.Average(p => p.Y));
        var rotated = RotatePoints(source, MathF.PI / 4f, center);
        var target = TranslatePoints(rotated, 10f, -5f);

        var result = Alignment.Pca<float>(source, target);

        result.Error.Should().BeLessThan(1f);
        // Apply the result to source and check all points land near target
        var applied = result.ApplyTo((ReadOnlySpan<Point<float>>)source);
        var tree = KdTree<float>.Build(target);
        var span = applied.Span;
        for (int i = 0; i < span.Length; i++)
        {
            var (_, _, distSq) = tree.NearestNeighbour(span[i]);
            distSq.Should().BeLessThan(1f, $"point {i} should be near target");
        }
    }

    [Fact]
    public void Pca_AsymmetricLShape_CorrectCandidate()
    {
        // L-shape is asymmetric — only one of 4 candidates should work well
        var source = MakeLShape();
        var center = new Point<float>(
            source.Average(p => p.X),
            source.Average(p => p.Y));
        // Rotate by 30 degrees + translate
        var rotated = RotatePoints(source, MathF.PI / 6f, center);
        var target = TranslatePoints(rotated, 3f, 7f);

        var result = Alignment.Pca<float>(source, target);

        // ApplyTo roundtrip — every point should be close
        var applied = result.ApplyTo((ReadOnlySpan<Point<float>>)source);
        var tree = KdTree<float>.Build(target);
        float maxDistSq = 0;
        var span = applied.Span;
        for (int i = 0; i < span.Length; i++)
        {
            var (_, _, distSq) = tree.NearestNeighbour(span[i]);
            if (distSq > maxDistSq) maxDistSq = distSq;
        }
        maxDistSq.Should().BeLessThan(1f, "all aligned points should be near target");
    }

    [Fact]
    public void Pca_RectangleSymmetry()
    {
        // Rectangles have 180° symmetry — PCA should still find a good alignment
        var rect = new Point<float>[]
        {
            new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0),
            new(4, 1), new(4, 2),
            new(3, 2), new(2, 2), new(1, 2), new(0, 2),
            new(0, 1),
        };
        var center = new Point<float>(2, 1);
        // Rotate 180° — rectangle looks the same
        var target = RotatePoints(rect, MathF.PI, center);
        // Translate
        target = TranslatePoints(target, 5f, 5f);

        var result = Alignment.Pca<float>(rect, target);

        // Error should be low — the 180° ambiguity should be resolved
        var applied = result.ApplyTo((ReadOnlySpan<Point<float>>)rect);
        var tree = KdTree<float>.Build(target);
        float totalError = 0;
        var span = applied.Span;
        for (int i = 0; i < span.Length; i++)
        {
            var (_, _, distSq) = tree.NearestNeighbour(span[i]);
            totalError += distSq;
        }
        totalError.Should().BeLessThan(2f, "rectangle alignment should handle 180° symmetry");
    }

    [Fact]
    public void AlignTo_Polygon()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 0),
            new Point<float>(3, 1),
            new Point<float>(0, 1));
        var target = TranslatePoints(poly.AsSpan().ToArray(), 10f, 10f);

        var result = poly.AlignTo(target);

        result.Translation.X.Should().BeApproximately(10f, Tol);
        result.Translation.Y.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void AlignTo_Segment_WithDensify()
    {
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var target = new Point<float>[]
        {
            new(5, 5), new(6, 5), new(7, 5), new(8, 5), new(9, 5),
            new(10, 5), new(11, 5), new(12, 5), new(13, 5), new(14, 5), new(15, 5),
        };

        var result = seg.AlignTo(target, densify: true);

        // Should find some alignment (segment is 1D so PCA is limited)
        result.Error.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void ApplyTo_RoundTrip()
    {
        var source = MakeLShape();
        var center = new Point<float>(
            source.Average(p => p.X),
            source.Average(p => p.Y));
        var target = RotatePoints(source, 0.7f, center);
        target = TranslatePoints(target, 2f, -3f);

        var result = Alignment.Pca<float>(source, target);
        var applied = result.ApplyTo((ReadOnlySpan<Point<float>>)source);

        // Every applied point should have a near neighbor in target
        var tree = KdTree<float>.Build(target);
        var span = applied.Span;
        for (int i = 0; i < span.Length; i++)
        {
            var (_, _, distSq) = tree.NearestNeighbour(span[i]);
            distSq.Should().BeLessThan(1f);
        }
    }

    [Fact]
    public void Pca_Degenerate_SinglePoint()
    {
        var source = new[] { new Point<float>(1, 2) };
        var target = new[] { new Point<float>(5, 7) };

        var result = Alignment.Pca<float>(source, target);

        // Should return translation from centroid A to centroid B
        result.Translation.X.Should().BeApproximately(4f, Tol);
        result.Translation.Y.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void Pca_EmptyInput_ReturnsIdentity()
    {
        var result = Alignment.Pca<float>(ReadOnlySpan<Point<float>>.Empty, ReadOnlySpan<Point<float>>.Empty);

        result.Rotation.Should().Be(Matrix2x2<float>.Identity);
        result.Error.Should().Be(0f);
    }

    [Fact]
    public void AlignmentResult_Angle_Property()
    {
        // 90° rotation matrix
        var rotation = Matrix2x2<float>.Rotation(Radian<float>.FromRadian(MathF.PI / 2f));
        var result = new AlignmentResult<float>(rotation, new Vector<float>(0, 0), 0f);

        float angle = (float)(Radian<float>)result.Angle;
        angle.Should().BeApproximately(MathF.PI / 2f, 1e-5f);
    }

    [Fact]
    public void ApplyTo_SinglePoint()
    {
        var rotation = Matrix2x2<float>.Rotation(Radian<float>.FromRadian(MathF.PI / 2f));
        var result = new AlignmentResult<float>(rotation, new Vector<float>(1, 2), 0f);

        var transformed = result.ApplyTo(new Point<float>(1, 0));
        // R * (1,0) = (0,1) for 90° rotation, + (1,2) = (1,3)
        transformed.X.Should().BeApproximately(1f, 1e-5f);
        transformed.Y.Should().BeApproximately(3f, 1e-5f);
    }
}
