using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class CorrectionTests
{
    private static readonly float Eps = 0.5f;

    /// <summary>
    /// Helper: builds a ComplexCurve from a single polyline segment.
    /// </summary>
    private static ComplexCurve<float> BuildPolylineCurve(params Point<float>[] points)
    {
        var builder = new ComplexCurveBuilder<float>();
        builder.AddPoints(points);
        return builder.Build();
    }

    /// <summary>
    /// Helper: builds a ComplexCurve from a single Bezier segment.
    /// </summary>
    private static ComplexCurve<float> BuildBezierCurve(BezierCurve<float> bezier)
    {
        var builder = new ComplexCurveBuilder<float>();
        builder.AddBezier(bezier);
        return builder.Build();
    }

    /// <summary>
    /// Helper: builds a Polyline from points.
    /// </summary>
    private static Polyline<float> BuildTarget(params Point<float>[] points)
        => new(points);

    [Fact]
    public void Empty_Curve_ReturnsItself()
    {
        var curve = new ComplexCurve<float>();
        var target = BuildTarget(
            new Point<float>(0, 0),
            new Point<float>(10, 0));

        var result = curve.ComputeCorrections(target);

        result.CorrectedCurve.IsEmpty.Should().BeTrue();
        result.TotalResidualError.Should().Be(0f);
    }

    [Fact]
    public void Short_Target_ReturnsItself()
    {
        var curve = BuildPolylineCurve(
            new Point<float>(0, 0),
            new Point<float>(5, 0));
        var target = new Polyline<float>(new Point<float>(0, 0));

        var result = curve.ComputeCorrections(target);

        result.CorrectedCurve.SegmentCount.Should().Be(curve.SegmentCount);
    }

    [Fact]
    public void Polyline_Identity_ResidualNearZero()
    {
        // Curve and target are the same straight line
        var points = new[]
        {
            new Point<float>(0, 0),
            new Point<float>(5, 0),
            new Point<float>(10, 0),
        };
        var curve = BuildPolylineCurve(points);
        var target = BuildTarget(points);

        var result = curve.ComputeCorrections(target);

        result.TotalResidualError.Should().BeLessThan(1f);
        result.CorrectedCurve.SegmentCount.Should().Be(1);
    }

    [Fact]
    public void Polyline_Translation_SnapsToTarget()
    {
        // Curve is a horizontal line at y=0, target is at y=5
        var curve = BuildPolylineCurve(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(20, 0));

        var target = BuildTarget(
            new Point<float>(0, 5),
            new Point<float>(10, 5),
            new Point<float>(20, 5));

        var result = curve.ComputeCorrections(target);

        // After correction, points should be near y=5
        result.CorrectedCurve.SegmentCount.Should().Be(1);
        var correctedPts = GetPolylinePoints(result.CorrectedCurve);
        foreach (var pt in correctedPts)
            pt.Y.Should().BeApproximately(5f, Eps);
    }

    [Fact]
    public void Bezier_Fit_FollowsTargetArc()
    {
        // Template: a Bezier that curves upward
        var bezier = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 10),
            new Point<float>(7, 10),
            new Point<float>(10, 0));
        var curve = BuildBezierCurve(bezier);

        // Target: densify the same Bezier (should get near-perfect fit)
        var densified = bezier.Densify();
        var target = new Polyline<float>(densified);

        var result = curve.ComputeCorrections(target);

        result.CorrectedCurve.SegmentCount.Should().Be(1);
        // Residual should be small since target was generated from the same curve.
        // The residual is the sum of squared chord-length-parameterized distances,
        // which differs from zero because the Bezier's native t ≠ chord-length t.
        result.TotalResidualError.Should().BeLessThan(25f);
    }

    [Fact]
    public void Bezier_Translation_CorrectedEndpointsNearTarget()
    {
        // Template Bezier at y=0..10
        var bezier = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 5),
            new Point<float>(7, 5),
            new Point<float>(10, 0));
        var curve = BuildBezierCurve(bezier);

        // Target: same shape but shifted up by 3
        var shiftedBezier = new BezierCurve<float>(
            new Point<float>(0, 3),
            new Point<float>(3, 8),
            new Point<float>(7, 8),
            new Point<float>(10, 3));
        var target = new Polyline<float>(shiftedBezier.Densify());

        var result = curve.ComputeCorrections(target);

        // After correction, endpoints should be near the shifted target endpoints
        var corrected = GetFirstBezier(result.CorrectedCurve);
        corrected.Start.Y.Should().BeApproximately(3f, 1.5f);
        corrected.End.Y.Should().BeApproximately(3f, 1.5f);
    }

    [Fact]
    public void Mixed_Curve_PreservesTopology()
    {
        // Mixed: polyline + bezier
        var builder = new ComplexCurveBuilder<float>();
        builder.AddPoints(new Point<float>[]
        {
            new(0, 0), new(5, 0), new(10, 0)
        });
        builder.AddBezier(new BezierCurve<float>(
            new Point<float>(10, 0),
            new Point<float>(13, 5),
            new Point<float>(17, 5),
            new Point<float>(20, 0)));
        var curve = builder.Build();

        // Target: a straight line across the same x-range
        var targetPts = new List<Point<float>>();
        for (int x = 0; x <= 20; x++)
            targetPts.Add(new Point<float>(x, 1));
        var target = new Polyline<float>(targetPts.ToArray());

        var result = curve.ComputeCorrections(target);

        // Topology preserved: 2 segments (polyline + bezier)
        result.CorrectedCurve.SegmentCount.Should().Be(2);

        int segIndex = 0;
        foreach (var seg in result.CorrectedCurve)
        {
            if (segIndex == 0) seg.IsPolyline.Should().BeTrue();
            if (segIndex == 1) seg.IsBezier.Should().BeTrue();
            segIndex++;
        }
    }

    [Fact]
    public void Alignment_IsPopulated()
    {
        var curve = BuildPolylineCurve(
            new Point<float>(0, 0),
            new Point<float>(10, 0));
        var target = BuildTarget(
            new Point<float>(0, 5),
            new Point<float>(10, 5));

        var result = curve.ComputeCorrections(target);

        // Alignment should have non-zero translation for the y-offset
        var translationMagnitude = result.Alignment.Translation.Length;
        translationMagnitude.Should().BeGreaterThan(0.1f);
    }

    [Fact]
    public void Polyline_DiagonalTarget_PointsSnapToLine()
    {
        // Curve: horizontal line
        var curve = BuildPolylineCurve(
            new Point<float>(0, 0),
            new Point<float>(5, 0),
            new Point<float>(10, 0));

        // Target: diagonal line y = x (densified)
        var targetPts = new List<Point<float>>();
        for (int i = 0; i <= 20; i++)
            targetPts.Add(new Point<float>(i * 0.5f, i * 0.5f));
        var target = new Polyline<float>(targetPts.ToArray());

        var result = curve.ComputeCorrections(target);

        // After correction, each point should be near the diagonal
        var correctedPts = GetPolylinePoints(result.CorrectedCurve);
        foreach (var pt in correctedPts)
        {
            // Distance from y=x line: |x-y|/sqrt(2)
            var distToLine = MathF.Abs(pt.X - pt.Y) / MathF.Sqrt(2);
            distToLine.Should().BeLessThan(2f);
        }
    }

    // ═══════════════════════════════════
    // Helper methods
    // ═══════════════════════════════════

    private static Point<float>[] GetPolylinePoints(ComplexCurve<float> curve)
    {
        foreach (var seg in curve)
        {
            if (seg.IsPolyline)
                return seg.AsPoints().ToArray();
        }
        return Array.Empty<Point<float>>();
    }

    private static BezierCurve<float> GetFirstBezier(ComplexCurve<float> curve)
    {
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
                return seg.AsBezier();
        }
        throw new InvalidOperationException("No Bezier segment found");
    }
}
