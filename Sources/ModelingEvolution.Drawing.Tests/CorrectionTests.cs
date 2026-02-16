using System.Globalization;
using System.Text;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class CorrectionTests
{
    private readonly ITestOutputHelper _output;
    private static readonly float Eps = 0.5f;

    // ── Shared test parameters (identical across all rounded-rect tests) ──
    private const float NoiseMag = 1.0f;
    private const float DropoutFraction = 0.10f;
    private const float Tx = 15f;
    private const float Ty = -10f;
    private const float AngleDeg = 12f;

    private record struct NoiseEntry(float Offset, bool Drop);

    private static readonly ComplexCurve<float> SharedTemplate = BuildRoundedRectangle(100, 70, 10);
    private static readonly NoiseEntry[] SharedNoise = GenerateNoise(2000, NoiseMag, DropoutFraction, new Random(77));
    private static readonly ComplexCurve<float> PerturbedTpl = PerturbBezierControlPoints(SharedTemplate, 5f, new Random(42));
    private static readonly ComplexCurve<float> ShiftedTpl = ShiftPolylineEdges(SharedTemplate, 3f, new Random(200));
    private static readonly ComplexCurve<float> ShiftedPerturbedTpl = PerturbBezierControlPoints(ShiftedTpl, 4f, new Random(42));
    private static readonly ComplexCurve<float> DensifiedTpl = BuildPolylineCurve(SharedTemplate.Densify(0.5f).AsSpan().ToArray());

    private static NoiseEntry[] GenerateNoise(int count, float noiseMag, float dropoutFraction, Random rng)
    {
        var entries = new NoiseEntry[count];
        for (int i = 0; i < count; i++)
            entries[i] = new NoiseEntry(
                (float)(rng.NextDouble() * 2 - 1) * noiseMag,
                rng.NextDouble() < dropoutFraction);
        return entries;
    }

    public CorrectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

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

    // ═══════════════════════════════════════════════════════
    // Rounded Rectangle Integration Tests
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Builds a rounded rectangle as a ComplexCurve with 8 segments:
    /// 4 polyline edges + 4 Bezier quarter-circle corners.
    /// Uses κ = 0.5522847498 for circular arc approximation.
    /// </summary>
    private static ComplexCurve<float> BuildRoundedRectangle(float w, float h, float r)
    {
        const float k = 0.5522847498f; // Bezier approximation of quarter circle
        var kr = k * r;
        var builder = new ComplexCurveBuilder<float>();

        // Bottom edge: (r,0) → (w-r,0)
        builder.AddPoints(new Point<float>[] { new(r, 0), new(w - r, 0) });

        // BR corner: (w-r,0) → (w,r)
        builder.AddBezier(new BezierCurve<float>(
            new(w - r, 0), new(w - r + kr, 0), new(w, r - kr), new(w, r)));

        // Right edge: (w,r) → (w,h-r)
        builder.AddPoints(new Point<float>[] { new(w, r), new(w, h - r) });

        // TR corner: (w,h-r) → (w-r,h)
        builder.AddBezier(new BezierCurve<float>(
            new(w, h - r), new(w, h - r + kr), new(w - r + kr, h), new(w - r, h)));

        // Top edge: (w-r,h) → (r,h)
        builder.AddPoints(new Point<float>[] { new(w - r, h), new(r, h) });

        // TL corner: (r,h) → (0,h-r)
        builder.AddBezier(new BezierCurve<float>(
            new(r, h), new(r - kr, h), new(0, h - r + kr), new(0, h - r)));

        // Left edge: (0,h-r) → (0,r)
        builder.AddPoints(new Point<float>[] { new(0, h - r), new(0, r) });

        // BL corner: (0,r) → (r,0)
        builder.AddBezier(new BezierCurve<float>(
            new(0, r), new(0, r - kr), new(r - kr, 0), new(r, 0)));

        return builder.Build();
    }

    /// <summary>
    /// Densifies a ComplexCurve, adds pre-generated noise perpendicular to local direction,
    /// drops out points per the noise pattern, then rotates and translates.
    /// Uses shared NoiseEntry[] so all tests get identical noise.
    /// </summary>
    private static Polyline<float> DensifyAndPerturb(
        ComplexCurve<float> curve,
        NoiseEntry[] noise,
        float tx, float ty, float angleDeg)
    {
        var polyline = curve.Densify();
        var span = polyline.AsSpan();
        var pts = new List<Point<float>>(span.Length);

        for (int i = 0; i < span.Length; i++)
        {
            var entry = noise[i % noise.Length];

            // Keep first and last points always (anchor the shape)
            if (i > 0 && i < span.Length - 1 && entry.Drop)
                continue;

            // Local direction via central/forward/backward difference
            float dx, dy;
            if (i == 0)
            { dx = span[1].X - span[0].X; dy = span[1].Y - span[0].Y; }
            else if (i == span.Length - 1)
            { dx = span[i].X - span[i - 1].X; dy = span[i].Y - span[i - 1].Y; }
            else
            { dx = span[i + 1].X - span[i - 1].X; dy = span[i + 1].Y - span[i - 1].Y; }

            var len = MathF.Sqrt(dx * dx + dy * dy);
            if (len > 1e-6f)
            {
                var ndx = -dy / len;
                var ndy = dx / len;
                pts.Add(new Point<float>(span[i].X + entry.Offset * ndx, span[i].Y + entry.Offset * ndy));
            }
            else
            {
                pts.Add(new Point<float>(span[i].X + entry.Offset, span[i].Y));
            }
        }

        // Apply rotation then translation
        var angleRad = angleDeg * MathF.PI / 180f;
        var cos = MathF.Cos(angleRad);
        var sin = MathF.Sin(angleRad);
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            var rx = p.X * cos - p.Y * sin + tx;
            var ry = p.X * sin + p.Y * cos + ty;
            pts[i] = new Point<float>(rx, ry);
        }

        return new Polyline<float>(pts.ToArray());
    }

    /// <summary>
    /// Applies a simple moving-average smoothing to a polyline.
    /// Each interior point is replaced by the average of its window neighbors.
    /// First and last points are preserved.
    /// </summary>
    private static Polyline<float> SmoothPolyline(Polyline<float> polyline, int windowRadius = 3)
    {
        var span = polyline.AsSpan();
        if (span.Length < 3) return polyline;

        var smoothed = new Point<float>[span.Length];
        smoothed[0] = span[0];
        smoothed[span.Length - 1] = span[span.Length - 1];

        for (int i = 1; i < span.Length - 1; i++)
        {
            int lo = Math.Max(0, i - windowRadius);
            int hi = Math.Min(span.Length - 1, i + windowRadius);
            float sx = 0, sy = 0;
            int count = 0;
            for (int j = lo; j <= hi; j++)
            {
                sx += span[j].X;
                sy += span[j].Y;
                count++;
            }
            smoothed[i] = new Point<float>(sx / count, sy / count);
        }

        return new Polyline<float>(smoothed);
    }

    [Fact]
    public void RoundedRect_Unmodified_NearZeroCorrection()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(template, SharedNoise, Tx, Ty, AngleDeg);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        RenderCorrectionSvg("unmodified", template, measured, result);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(5f,
            "recovered rotation should be close to applied angle");

        result.CorrectedCurve.SegmentCount.Should().Be(8);

        int segIdx = 0;
        foreach (var seg in result.CorrectedCurve)
        {
            bool shouldBePolyline = segIdx % 2 == 0;
            if (shouldBePolyline)
                seg.IsPolyline.Should().BeTrue($"segment {segIdx} should be polyline");
            else
                seg.IsBezier.Should().BeTrue($"segment {segIdx} should be bezier");
            segIdx++;
        }

        var correctedInTemplateSpace = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        var maxShapeDev = MaxShapeDeviation(template, correctedInTemplateSpace);
        _output.WriteLine($"Max shape deviation (template space): {maxShapeDev:F4}");
        maxShapeDev.Should().BeLessThan(5f,
            "corrected curve shape should be near original template");
    }

    [Fact]
    public void RoundedRect_PerturbedKeyPoints_NonZeroCorrection()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(PerturbedTpl, SharedNoise, Tx, Ty, AngleDeg);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var perturbedInMeasured = PerturbedTpl
            + Degree<float>.Create(AngleDeg)
            + new Vector<float>(Tx, Ty);
        RenderCorrectionSvg("perturbed", template, measured, result, perturbedInMeasured, PerturbedTpl);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(15f,
            "recovered rotation should be close to applied angle (relaxed for large perturbation)");

        result.CorrectedCurve.SegmentCount.Should().Be(8);
    }

    [Fact]
    public void RoundedRect_PolylineShift_Unmodified()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(ShiftedTpl, SharedNoise, Tx, Ty, AngleDeg);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var shiftedInMeasured = ShiftedTpl
            + Degree<float>.Create(AngleDeg)
            + new Vector<float>(Tx, Ty);
        RenderCorrectionSvg("polyshift-unmodified", template, measured, result, shiftedInMeasured, ShiftedTpl);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(5f,
            "recovered rotation should be close to applied angle");

        result.CorrectedCurve.SegmentCount.Should().Be(8);

        var correctedInTemplateSpace = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        var maxShapeDev = MaxShapeDeviation(template, correctedInTemplateSpace);
        _output.WriteLine($"Max shape deviation (template space): {maxShapeDev:F4}");
        maxShapeDev.Should().BeLessThan(8f,
            "corrected curve shape should be near original template");
    }

    [Fact]
    public void RoundedRect_PolylineShift_PerturbedCorners()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(ShiftedPerturbedTpl, SharedNoise, Tx, Ty, AngleDeg);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var perturbedInMeasured = ShiftedPerturbedTpl
            + Degree<float>.Create(AngleDeg)
            + new Vector<float>(Tx, Ty);
        RenderCorrectionSvg("polyshift-perturbed", template, measured, result, perturbedInMeasured, ShiftedPerturbedTpl);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(15f,
            "recovered rotation should be close to applied angle (relaxed for perturbation)");

        result.CorrectedCurve.SegmentCount.Should().Be(8);

        var shapeDeviationFromTemplate = MaxShapeDeviation(template, result.CorrectedCurve);
        _output.WriteLine($"Shape deviation from original template: {shapeDeviationFromTemplate:F4}");
        shapeDeviationFromTemplate.Should().BeGreaterThan(0.5f,
            "correction should produce non-zero changes since shape was shifted and perturbed");
    }

    [Fact]
    public void RoundedRect_Smoothed_Unmodified()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(template, SharedNoise, Tx, Ty, AngleDeg);
        measured = SmoothPolyline(measured);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        RenderCorrectionSvg("smoothed-unmodified", template, measured, result);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(5f,
            "recovered rotation should be close to applied angle");

        result.CorrectedCurve.SegmentCount.Should().Be(8);

        var correctedInTemplateSpace = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        var maxShapeDev = MaxShapeDeviation(template, correctedInTemplateSpace);
        _output.WriteLine($"Max shape deviation (template space): {maxShapeDev:F4}");
        maxShapeDev.Should().BeLessThan(5f,
            "smoothed corrected curve should be near original template");
    }

    [Fact]
    public void RoundedRect_Smoothed_PerturbedCorners()
    {
        var template = SharedTemplate;
        var measured = DensifyAndPerturb(PerturbedTpl, SharedNoise, Tx, Ty, AngleDeg);
        measured = SmoothPolyline(measured);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var perturbedInMeasured = PerturbedTpl
            + Degree<float>.Create(AngleDeg)
            + new Vector<float>(Tx, Ty);
        RenderCorrectionSvg("smoothed-perturbed", template, measured, result, perturbedInMeasured, PerturbedTpl);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(15f,
            "recovered rotation should be close to applied angle (relaxed for perturbation)");

        result.CorrectedCurve.SegmentCount.Should().Be(8);

        var shapeDeviationFromTemplate = MaxShapeDeviation(template, result.CorrectedCurve);
        _output.WriteLine($"Shape deviation from original template: {shapeDeviationFromTemplate:F4}");
        shapeDeviationFromTemplate.Should().BeGreaterThan(0.5f,
            "correction should produce non-zero changes since shape was perturbed");
    }

    [Fact]
    public void RoundedRect_DensifiedPolyline_NoNoise()
    {
        var template = DensifiedTpl;
        // Zero noise, no dropout — just rotation + translation
        var zeroNoise = new NoiseEntry[2000];
        Array.Fill(zeroNoise, new NoiseEntry(0f, false));
        var measured = DensifyAndPerturb(SharedTemplate, zeroNoise, Tx, Ty, AngleDeg);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var correctedInTemplateSpace = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        var maxShapeDev = MaxShapeDeviation(template, correctedInTemplateSpace);
        _output.WriteLine($"Max shape deviation (template space): {maxShapeDev:F4}");
        // With zero noise, this should be near zero
        maxShapeDev.Should().BeLessThan(1f,
            "zero-noise correction should produce near-zero deviation");
    }

    [Fact]
    public void RoundedRect_DensifiedPolyline_Unmodified()
    {
        var template = DensifiedTpl;
        var measured = DensifyAndPerturb(SharedTemplate, SharedNoise, Tx, Ty, AngleDeg);
        measured = SmoothPolyline(measured);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        RenderCorrectionSvg("densified-unmodified", template, measured, result);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(5f,
            "recovered rotation should be close to applied angle");

        // Pure polyline template → single polyline segment in result
        result.CorrectedCurve.SegmentCount.Should().Be(1);

        var correctedInTemplateSpace = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        var maxShapeDev = MaxShapeDeviation(template, correctedInTemplateSpace);
        _output.WriteLine($"Max shape deviation (template space): {maxShapeDev:F4}");
        maxShapeDev.Should().BeLessThan(5f,
            "corrected curve should be near original template");
    }

    [Fact]
    public void RoundedRect_DensifiedPolyline_Perturbed()
    {
        var template = DensifiedTpl;
        var measured = DensifyAndPerturb(PerturbedTpl, SharedNoise, Tx, Ty, AngleDeg);
        measured = SmoothPolyline(measured);

        var result = template.ComputeCorrections(measured);

        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        _output.WriteLine($"Applied angle: {AngleDeg}deg, Recovered: {recoveredDeg:F2}deg");
        _output.WriteLine($"Translation: ({result.Alignment.Translation.X:F2}, {result.Alignment.Translation.Y:F2})");
        _output.WriteLine($"Total residual: {result.TotalResidualError:F4}");

        var perturbedInMeasured = PerturbedTpl
            + Degree<float>.Create(AngleDeg)
            + new Vector<float>(Tx, Ty);
        RenderCorrectionSvg("densified-perturbed", template, measured, result, perturbedInMeasured, PerturbedTpl);

        var angleDiff = MathF.Abs(recoveredDeg - AngleDeg) % 180f;
        if (angleDiff > 90f) angleDiff = 180f - angleDiff;
        angleDiff.Should().BeLessThan(15f,
            "recovered rotation should be close to applied angle (relaxed for perturbation)");

        result.CorrectedCurve.SegmentCount.Should().Be(1);
    }

    /// <summary>
    /// Perturbs Bezier corners while maintaining C1 continuity.
    /// Moving an endpoint also moves its anchored control point and the shared
    /// point in the adjacent polyline segment.
    /// </summary>
    private static ComplexCurve<float> PerturbBezierControlPoints(
        ComplexCurve<float> curve, float magnitude, Random rng)
    {
        // Collect all segments as mutable data
        var segments = new List<object>();
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
                segments.Add(seg.AsBezier());
            else
                segments.Add(seg.AsPoints().ToArray());
        }

        int bezierIdx = 0;
        for (int s = 0; s < segments.Count; s++)
        {
            if (segments[s] is not BezierCurve<float> b) continue;

            // Perturb 1st and 3rd Bezier corners (BR and TL)
            if (bezierIdx == 0 || bezierIdx == 2)
            {
                // Displacement for Start endpoint — C0 is anchored to Start
                var dxS = (float)(rng.NextDouble() * 2 - 1) * magnitude;
                var dyS = (float)(rng.NextDouble() * 2 - 1) * magnitude;
                // Displacement for End endpoint — C1 is anchored to End
                var dxE = (float)(rng.NextDouble() * 2 - 1) * magnitude;
                var dyE = (float)(rng.NextDouble() * 2 - 1) * magnitude;

                var newStart = new Point<float>(b.Start.X + dxS, b.Start.Y + dyS);
                var newC0 = new Point<float>(b.C0.X + dxS, b.C0.Y + dyS);
                var newC1 = new Point<float>(b.C1.X + dxE, b.C1.Y + dyE);
                var newEnd = new Point<float>(b.End.X + dxE, b.End.Y + dyE);

                segments[s] = new BezierCurve<float>(newStart, newC0, newC1, newEnd);

                // Update previous segment's last point (shared with Start)
                if (s > 0 && segments[s - 1] is Point<float>[] prevPoly)
                    prevPoly[prevPoly.Length - 1] = newStart;

                // Update next segment's first point (shared with End)
                if (s + 1 < segments.Count && segments[s + 1] is Point<float>[] nextPoly)
                    nextPoly[0] = newEnd;
            }
            bezierIdx++;
        }

        var builder = new ComplexCurveBuilder<float>();
        foreach (var seg in segments)
        {
            if (seg is BezierCurve<float> bz)
                builder.AddBezier(bz);
            else if (seg is Point<float>[] pts)
                builder.AddPoints(pts);
        }
        return builder.Build();
    }

    /// <summary>
    /// Shifts each polyline edge perpendicular to its direction by a random amount,
    /// then reconstructs adjacent Bezier corners using the κ recipe:
    /// C0 = Start + κ*(corner - Start), C1 = End + κ*(corner - End),
    /// where corner is the intersection of the tangent lines from Start and End.
    /// </summary>
    private static ComplexCurve<float> ShiftPolylineEdges(
        ComplexCurve<float> curve, float magnitude, Random rng)
    {
        const float k = 0.5522847498f;

        // Collect all segments as mutable data
        var segments = new List<object>();
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
                segments.Add(seg.AsBezier());
            else
                segments.Add(seg.AsPoints().ToArray());
        }

        // Shift each polyline edge perpendicular to its direction
        for (int s = 0; s < segments.Count; s++)
        {
            if (segments[s] is not Point<float>[] pts || pts.Length < 2) continue;

            var dx = pts[pts.Length - 1].X - pts[0].X;
            var dy = pts[pts.Length - 1].Y - pts[0].Y;
            var len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6f) continue;

            // Perpendicular direction (rotated 90° CCW)
            var nx = -dy / len;
            var ny = dx / len;
            var shift = (float)(rng.NextDouble() * 2 - 1) * magnitude;

            for (int i = 0; i < pts.Length; i++)
                pts[i] = new Point<float>(pts[i].X + shift * nx, pts[i].Y + shift * ny);
        }

        // Reconstruct each Bezier corner from adjacent polyline endpoints
        for (int s = 0; s < segments.Count; s++)
        {
            if (segments[s] is not BezierCurve<float>) continue;

            int prevIdx = (s - 1 + segments.Count) % segments.Count;
            int nextIdx = (s + 1) % segments.Count;

            var prevPoly = (Point<float>[])segments[prevIdx];
            var nextPoly = (Point<float>[])segments[nextIdx];

            var start = prevPoly[prevPoly.Length - 1];
            var end = nextPoly[0];

            // Tangent direction at Start = direction of previous polyline
            var tdx1 = prevPoly[prevPoly.Length - 1].X - prevPoly[0].X;
            var tdy1 = prevPoly[prevPoly.Length - 1].Y - prevPoly[0].Y;
            var tlen1 = MathF.Sqrt(tdx1 * tdx1 + tdy1 * tdy1);

            // Tangent direction at End = direction of next polyline
            var tdx2 = nextPoly[nextPoly.Length - 1].X - nextPoly[0].X;
            var tdy2 = nextPoly[nextPoly.Length - 1].Y - nextPoly[0].Y;
            var tlen2 = MathF.Sqrt(tdx2 * tdx2 + tdy2 * tdy2);

            if (tlen1 < 1e-6f || tlen2 < 1e-6f) continue;

            var tx1 = tdx1 / tlen1; var ty1 = tdy1 / tlen1;
            var tx2 = tdx2 / tlen2; var ty2 = tdy2 / tlen2;

            // Virtual corner: intersection of tangent lines
            // Line 1: Start + t1*(tx1,ty1)
            // Line 2: End - t2*(tx2,ty2)
            var ex = end.X - start.X;
            var ey = end.Y - start.Y;
            var det = tx1 * ty2 - ty1 * tx2;

            if (MathF.Abs(det) < 1e-6f) continue;

            var t1 = (ex * ty2 - ey * tx2) / det;
            var cornerX = start.X + t1 * tx1;
            var cornerY = start.Y + t1 * ty1;

            var c0 = new Point<float>(
                start.X + k * (cornerX - start.X),
                start.Y + k * (cornerY - start.Y));
            var c1 = new Point<float>(
                end.X + k * (cornerX - end.X),
                end.Y + k * (cornerY - end.Y));

            segments[s] = new BezierCurve<float>(start, c0, c1, end);
        }

        var builder = new ComplexCurveBuilder<float>();
        foreach (var seg in segments)
        {
            if (seg is BezierCurve<float> bz)
                builder.AddBezier(bz);
            else if (seg is Point<float>[] pts)
                builder.AddPoints(pts);
        }
        return builder.Build();
    }

    /// <summary>
    /// Computes max deviation between endpoint/vertex positions of two ComplexCurves.
    /// Only compares polyline vertices and Bezier start/end (NOT interior control points,
    /// which can vary significantly for similar curve shapes).
    /// </summary>
    private static float MaxEndpointDeviation(ComplexCurve<float> a, ComplexCurve<float> b)
    {
        var ptsA = ExtractEndpoints(a);
        var ptsB = ExtractEndpoints(b);
        int n = Math.Min(ptsA.Count, ptsB.Count);
        float maxDev = 0;
        for (int i = 0; i < n; i++)
        {
            var dx = ptsA[i].X - ptsB[i].X;
            var dy = ptsA[i].Y - ptsB[i].Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > maxDev) maxDev = dist;
        }
        return maxDev;
    }

    /// <summary>
    /// Computes the max Hausdorff-like distance between densified curves.
    /// This measures shape similarity regardless of control point placement.
    /// </summary>
    private static float MaxShapeDeviation(ComplexCurve<float> a, ComplexCurve<float> b)
    {
        var denseA = a.Densify().AsSpan();
        var denseB = b.Densify().AsSpan();
        float maxDist = 0;

        // For each point in A, find closest in B
        for (int i = 0; i < denseA.Length; i++)
        {
            float bestDist = float.MaxValue;
            for (int j = 0; j < denseB.Length; j++)
            {
                var dx = denseA[i].X - denseB[j].X;
                var dy = denseA[i].Y - denseB[j].Y;
                var d = dx * dx + dy * dy;
                if (d < bestDist) bestDist = d;
            }
            var dist = MathF.Sqrt(bestDist);
            if (dist > maxDist) maxDist = dist;
        }
        return maxDist;
    }

    private static List<Point<float>> ExtractEndpoints(ComplexCurve<float> curve)
    {
        var pts = new List<Point<float>>();
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                pts.Add(b.Start);
                pts.Add(b.End);
            }
            else
            {
                var span = seg.AsPoints();
                for (int i = 0; i < span.Length; i++)
                    pts.Add(span[i]);
            }
        }
        return pts;
    }

    // ═══════════════════════════════════════════════════════
    // SVG Rendering
    // ═══════════════════════════════════════════════════════

    private void RenderCorrectionSvg(
        string name,
        ComplexCurve<float> template,
        Polyline<float> measured,
        CorrectionResult<float> result,
        ComplexCurve<float>? perturbedTemplate = null,
        ComplexCurve<float>? sourceTemplate = null)
    {
        var outputDir = Path.Combine(
            Path.GetDirectoryName(typeof(CorrectionTests).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "correction-svg");
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var svg = RenderCorrectionCase(template, measured, result.CorrectedCurve, perturbedTemplate);
        File.WriteAllText(Path.Combine(outputDir, $"{name}.svg"), svg);

        var statsHtml = ComputeStatsHtml(template, result, sourceTemplate ?? template);
        File.WriteAllText(Path.Combine(outputDir, $"{name}.stats.html"), statsHtml);

        GenerateCorrectionHtml(outputDir);
        _output.WriteLine($"SVGs written to: {outputDir}");
    }

    /// <summary>
    /// Computes correction stats and returns an HTML fragment with a table.
    /// </summary>
    private static string ComputeStatsHtml(ComplexCurve<float> template, CorrectionResult<float> result,
        ComplexCurve<float> sourceTemplate)
    {
        var recoveredDeg = (float)(double)(Degree<float>)result.Alignment.Angle;
        var tx = result.Alignment.Translation.X;
        var ty = result.Alignment.Translation.Y;

        // Inverse-transform corrected curve back to template space
        var correctedInTemplate = result.CorrectedCurve
            - result.Alignment.Translation
            - result.Alignment.Angle;

        // Extract all key points (endpoints + control points for Bezier)
        var templatePts = ExtractAllKeyPoints(template);
        var correctedPts = ExtractAllKeyPoints(correctedInTemplate);

        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th colspan=\"7\" style=\"text-align:left;padding:8px 0 4px\">Alignment</th></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Rotation</td><td colspan=\"4\"><b>{recoveredDeg:F2}&deg;</b></td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Translation</td><td colspan=\"4\"><b>({tx:F2}, {ty:F2})</b></td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Residual (sum sq.)</td><td colspan=\"4\"><b>{result.TotalResidualError:F4}</b></td></tr>");

        sb.AppendLine("<tr><th colspan=\"7\" style=\"text-align:left;padding:12px 0 4px\">Correction Vectors (template space)</th></tr>");
        sb.AppendLine("<tr><th>#</th><th>Segment</th><th>Point</th><th>Template</th><th>Corrected</th><th>dx, dy</th><th>|v|</th></tr>");

        int n = Math.Min(templatePts.Count, correctedPts.Count);
        float sumVecX = 0, sumVecY = 0;
        float sumLengths = 0;
        float maxDisplacement = 0;
        string maxDisplacementLabel = "";

        for (int i = 0; i < n; i++)
        {
            var (segLabel, ptLabel, tp) = templatePts[i];
            var (_, _, cp) = correctedPts[i];
            var dx = cp.X - tp.X;
            var dy = cp.Y - tp.Y;
            var mag = MathF.Sqrt(dx * dx + dy * dy);
            sumVecX += dx;
            sumVecY += dy;
            sumLengths += mag;
            if (mag > maxDisplacement)
            {
                maxDisplacement = mag;
                maxDisplacementLabel = $"{segLabel} {ptLabel}";
            }

            sb.AppendLine($"<tr><td>{i}</td><td>{segLabel}</td><td>{ptLabel}</td>" +
                $"<td>({tp.X:F1}, {tp.Y:F1})</td>" +
                $"<td>({cp.X:F1}, {cp.Y:F1})</td>" +
                $"<td>({dx:F2}, {dy:F2})</td>" +
                $"<td>{mag:F2}</td></tr>");
        }

        // Max point displacement: densify both curves, for each corrected point
        // find min distance to template, return max of those.
        var maxPointDisplacement = MaxShapeDeviation(correctedInTemplate, template);

        // Correction accuracy: how close is the corrected curve to the source
        // template (the one that generated the measured data).
        var correctionAccuracy = MaxShapeDeviation(correctedInTemplate, sourceTemplate);

        var sumVecLen = MathF.Sqrt(sumVecX * sumVecX + sumVecY * sumVecY);
        sb.AppendLine("<tr><th colspan=\"7\" style=\"text-align:left;padding:12px 0 4px\">Summary</th></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Max key point displacement</td><td colspan=\"4\"><b>{maxDisplacement:F2}</b> at {maxDisplacementLabel}</td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Max point displacement</td><td colspan=\"4\"><b>{maxPointDisplacement:F2}</b></td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Correction accuracy</td><td colspan=\"4\"><b>{correctionAccuracy:F2}</b></td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Sum of correction lengths</td><td colspan=\"4\"><b>{sumLengths:F2}</b></td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\">Sum vector</td><td colspan=\"4\"><b>({sumVecX:F2}, {sumVecY:F2})</b> |v| = <b>{sumVecLen:F2}</b></td></tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Extracts all key points with labels: polyline vertices + Bezier start/C0/C1/end.
    /// </summary>
    private static List<(string SegLabel, string PtLabel, Point<float> Pt)> ExtractAllKeyPoints(ComplexCurve<float> curve)
    {
        var pts = new List<(string, string, Point<float>)>();
        int segIdx = 0;
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                pts.Add(($"Seg{segIdx} Bezier", "Start", b.Start));
                pts.Add(($"Seg{segIdx} Bezier", "C0", b.C0));
                pts.Add(($"Seg{segIdx} Bezier", "C1", b.C1));
                pts.Add(($"Seg{segIdx} Bezier", "End", b.End));
            }
            else
            {
                var span = seg.AsPoints();
                for (int i = 0; i < span.Length; i++)
                    pts.Add(($"Seg{segIdx} Poly", $"P{i}", span[i]));
            }
            segIdx++;
        }
        return pts;
    }

    private static string RenderCorrectionCase(
        ComplexCurve<float> template,
        Polyline<float> measured,
        ComplexCurve<float> corrected,
        ComplexCurve<float>? perturbedTemplate = null)
    {
        // Compute bounding box from all elements
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        void Expand(float x, float y)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        void ExpandCurve(ComplexCurve<float> c)
        {
            foreach (var seg in c)
            {
                if (seg.IsBezier)
                {
                    var b = seg.AsBezier();
                    Expand(b.Start.X, b.Start.Y); Expand(b.C0.X, b.C0.Y);
                    Expand(b.C1.X, b.C1.Y); Expand(b.End.X, b.End.Y);
                }
                else
                {
                    var pts = seg.AsPoints();
                    for (int i = 0; i < pts.Length; i++) Expand(pts[i].X, pts[i].Y);
                }
            }
        }

        ExpandCurve(template);
        ExpandCurve(corrected);
        if (perturbedTemplate.HasValue) ExpandCurve(perturbedTemplate.Value);

        var measSpan = measured.AsSpan();
        for (int i = 0; i < measSpan.Length; i++) Expand(measSpan[i].X, measSpan[i].Y);

        float margin = 10f;
        float vbX = minX - margin;
        float vbY = minY - margin;
        float vbW = (maxX - minX) + margin * 2;
        float vbH = (maxY - minY) + margin * 2;

        int svgW = Math.Max(400, (int)(vbW * 3));
        int svgH = Math.Max(300, (int)(vbH * 3));

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgW}\" height=\"{svgH}\" viewBox=\"{F(vbX)} {F(-maxY - margin)} {F(vbW)} {F(vbH)}\">");
        sb.AppendLine($"<rect x=\"{F(vbX)}\" y=\"{F(-maxY - margin)}\" width=\"{F(vbW)}\" height=\"{F(vbH)}\" fill=\"white\"/>");
        sb.AppendLine("<g transform=\"scale(1,-1)\">");

        // 1. Template (blue dashed)
        RenderComplexCurve(sb, template, "#4466aa", 0.8f, "4,2");

        // 2. Perturbed template if provided (green dashed)
        if (perturbedTemplate.HasValue)
            RenderComplexCurve(sb, perturbedTemplate.Value, "#33aa33", 0.8f, "6,3");

        // 3. Measured polyline (gray dots)
        sb.Append("<polyline points=\"");
        for (int i = 0; i < measSpan.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}",
                (double)measSpan[i].X, (double)measSpan[i].Y);
        }
        sb.AppendLine("\" fill=\"none\" stroke=\"#999999\" stroke-width=\"0.3\" stroke-dasharray=\"1,1\"/>");

        float pointR = Math.Max(0.4f, vbW / 400f);
        for (int i = 0; i < measSpan.Length; i += 3) // every 3rd point for readability
            sb.AppendLine(Circle(measSpan[i], pointR, "#aaaaaa"));

        // 4. Corrected (red solid)
        RenderComplexCurve(sb, corrected, "#dd3333", 0.8f, null);

        // 5. Filter rectangles for Bezier segments (orange semi-transparent)
        RenderFilterRectangles(sb, corrected);

        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void RenderFilterRectangles(StringBuilder sb, ComplexCurve<float> corrected)
    {
        foreach (var seg in corrected)
        {
            if (!seg.IsBezier) continue;
            var b = seg.AsBezier();
            var p0 = b.Start;
            var p3 = b.End;

            var dx = p3.X - p0.X;
            var dy = p3.Y - p0.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 1e-6f) continue;

            var ux = dx / dist;
            var uy = dy / dist;
            var nx = -uy;
            var ny = ux;

            var cx = (p0.X + p3.X) / 2;
            var cy = (p0.Y + p3.Y) / 2;
            var halfAlong = dist / 2;   // height/2 = dist/2 (along chord)
            var halfPerp = dist;        // width/2 = dist (perpendicular)

            // 4 corners of oriented rectangle
            float c0x = cx + halfAlong * ux + halfPerp * nx, c0y = cy + halfAlong * uy + halfPerp * ny;
            float c1x = cx + halfAlong * ux - halfPerp * nx, c1y = cy + halfAlong * uy - halfPerp * ny;
            float c2x = cx - halfAlong * ux - halfPerp * nx, c2y = cy - halfAlong * uy - halfPerp * ny;
            float c3x = cx - halfAlong * ux + halfPerp * nx, c3y = cy - halfAlong * uy + halfPerp * ny;

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "<polygon points=\"{0},{1} {2},{3} {4},{5} {6},{7}\" fill=\"#ff8800\" fill-opacity=\"0.12\" stroke=\"#ff8800\" stroke-width=\"0.3\" stroke-dasharray=\"2,2\"/>",
                (double)c0x, (double)c0y, (double)c1x, (double)c1y,
                (double)c2x, (double)c2y, (double)c3x, (double)c3y));
        }
    }

    private static void RenderComplexCurve(StringBuilder sb, ComplexCurve<float> curve,
        string color, float width, string? dashArray)
    {
        var dash = dashArray != null ? $" stroke-dasharray=\"{dashArray}\"" : "";
        foreach (var seg in curve)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "<path d=\"M{0},{1} C{2},{3} {4},{5} {6},{7}\" fill=\"none\" stroke=\"{8}\" stroke-width=\"{9}\"{10}/>",
                    (double)b.Start.X, (double)b.Start.Y,
                    (double)b.C0.X, (double)b.C0.Y,
                    (double)b.C1.X, (double)b.C1.Y,
                    (double)b.End.X, (double)b.End.Y,
                    color, F(width), dash));
            }
            else
            {
                var pts = seg.AsPoints();
                if (pts.Length < 2) continue;
                sb.Append("<polyline points=\"");
                for (int i = 0; i < pts.Length; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}",
                        (double)pts[i].X, (double)pts[i].Y);
                }
                sb.AppendLine($"\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{F(width)}\"{dash}/>");
            }
        }
    }

    private static void GenerateCorrectionHtml(string outputDir)
    {
        // Read stats fragments if available
        string ReadStats(string name)
        {
            var path = Path.Combine(outputDir, $"{name}.stats.html");
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }

        var unmodifiedStats = ReadStats("unmodified");
        var perturbedStats = ReadStats("perturbed");
        var polyshiftUnmodifiedStats = ReadStats("polyshift-unmodified");
        var polyshiftPerturbedStats = ReadStats("polyshift-perturbed");
        var smoothedUnmodifiedStats = ReadStats("smoothed-unmodified");
        var smoothedPerturbedStats = ReadStats("smoothed-perturbed");
        var densifiedUnmodifiedStats = ReadStats("densified-unmodified");
        var densifiedPerturbedStats = ReadStats("densified-perturbed");

        var sb = new StringBuilder();
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en"><head>
<meta charset="UTF-8">
<title>ComputeCorrections() — ModelingEvolution.Drawing</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #f0f2f5; color: #1d1d1f; }

.hero { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%); color: #fff; padding: 40px 32px 32px; }
.hero h1 { font-size: 28px; font-weight: 700; margin-bottom: 6px; }
.hero p { color: #a0b0d0; font-size: 15px; }

.container { max-width: 1400px; margin: 0 auto; padding: 24px; }

.legend { display: flex; gap: 24px; margin-bottom: 24px; justify-content: center; flex-wrap: wrap; }
.legend .item { display: flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 500; }
.legend .swatch { width: 24px; height: 3px; border-radius: 2px; }

.cards { display: grid; grid-template-columns: 1fr; gap: 16px; }
.card { background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
.card-header { padding: 10px 16px; font-size: 14px; font-weight: 600; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f0f0f0; }
.card-header .meta { color: #999; font-weight: 400; font-size: 12px; }
.card-body { padding: 16px; display: flex; align-items: center; justify-content: center; background: #fafbfc; cursor: pointer; }
.card-body img { width: 100%; max-width: 800px; height: auto; object-fit: contain; }
.card-stats { padding: 16px; border-top: 1px solid #f0f0f0; font-size: 13px; overflow-x: auto; }
.card-stats table { border-collapse: collapse; width: 100%; font-family: "SF Mono", "Consolas", monospace; font-size: 12px; }
.card-stats th, .card-stats td { padding: 3px 8px; text-align: left; border-bottom: 1px solid #eee; white-space: nowrap; }
.card-stats th { font-weight: 600; color: #555; }
.card-stats tr:hover td { background: #f5f7fa; }

.overlay { display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.85); z-index:1000; cursor:pointer; align-items:center; justify-content:center; }
.overlay.active { display:flex; }
.overlay img { width:90vw; height:90vh; object-fit:contain; background:#fff; border-radius:8px; padding:20px; }
</style>
</head><body>

<div class="hero">
    <h1>ComplexCurve&lt;T&gt;.ComputeCorrections()</h1>
    <p>PCA alignment + per-segment correction — Rounded Rectangle 100x70 r=10</p>
</div>

<div class="overlay" id="overlay" onclick="this.classList.remove('active')"><img id="overlay-img"></div>

<div class="container">
    <div class="legend">
        <div class="item"><div class="swatch" style="background:#4466aa"></div> Template (blue dashed)</div>
        <div class="item"><div class="swatch" style="background:#33aa33"></div> Perturbed template (green dashed)</div>
        <div class="item"><div class="swatch" style="background:#999999"></div> Measured polyline (gray)</div>
        <div class="item"><div class="swatch" style="background:#dd3333"></div> Corrected (red solid)</div>
        <div class="item"><div class="swatch" style="background:#ff8800;opacity:0.5"></div> Filter rect (orange)</div>
    </div>
    <div class="cards">
        <div class="card">
            <div class="card-header">
                <span>Unmodified Template</span>
                <span class="meta">noise=1.0 | dropout=10% | rot=12deg | tx=15,ty=-10</span>
            </div>
            <div class="card-body"><img src="unmodified.svg" alt="Unmodified"></div>
""");
        if (!string.IsNullOrEmpty(unmodifiedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{unmodifiedStats}</div>");
        sb.AppendLine("""
        </div>
        <div class="card">
            <div class="card-header">
                <span>Perturbed Key Points</span>
                <span class="meta">2 corners perturbed +-5 | noise=1.0 | dropout=10% | rot=12deg</span>
            </div>
            <div class="card-body"><img src="perturbed.svg" alt="Perturbed"></div>
""");
        if (!string.IsNullOrEmpty(perturbedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{perturbedStats}</div>");
        sb.AppendLine("""
        </div>
""");
        // Polyline-shift cards
        sb.AppendLine("""
        <div class="card">
            <div class="card-header">
                <span>Polyline Shift — Unmodified Corners</span>
                <span class="meta">edges shifted +-3 | corners reconstructed via κ | noise=1.0 | rot=12deg</span>
            </div>
            <div class="card-body"><img src="polyshift-unmodified.svg" alt="Polyline Shift Unmodified"></div>
""");
        if (!string.IsNullOrEmpty(polyshiftUnmodifiedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{polyshiftUnmodifiedStats}</div>");
        sb.AppendLine("""
        </div>
        <div class="card">
            <div class="card-header">
                <span>Polyline Shift — Perturbed Corners</span>
                <span class="meta">edges shifted +-3 | corners perturbed +-4 | noise=1.0 | rot=12deg</span>
            </div>
            <div class="card-body"><img src="polyshift-perturbed.svg" alt="Polyline Shift Perturbed"></div>
""");
        if (!string.IsNullOrEmpty(polyshiftPerturbedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{polyshiftPerturbedStats}</div>");
        sb.AppendLine("""
        </div>
""");
        // Smoothed cards
        sb.AppendLine("""
        <div class="card">
            <div class="card-header">
                <span>Smoothed — Unmodified Template</span>
                <span class="meta">noise=1.0 | smoothed (window=3) | dropout=10% | rot=12deg | tx=15,ty=-10</span>
            </div>
            <div class="card-body"><img src="smoothed-unmodified.svg" alt="Smoothed Unmodified"></div>
""");
        if (!string.IsNullOrEmpty(smoothedUnmodifiedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{smoothedUnmodifiedStats}</div>");
        sb.AppendLine("""
        </div>
        <div class="card">
            <div class="card-header">
                <span>Smoothed — Perturbed Key Points</span>
                <span class="meta">2 corners perturbed +-5 | noise=1.0 | smoothed (window=3) | rot=12deg</span>
            </div>
            <div class="card-body"><img src="smoothed-perturbed.svg" alt="Smoothed Perturbed"></div>
""");
        if (!string.IsNullOrEmpty(smoothedPerturbedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{smoothedPerturbedStats}</div>");
        sb.AppendLine("""
        </div>
""");
        // Densified polyline cards
        sb.AppendLine("""
        <div class="card">
            <div class="card-header">
                <span>Densified Polyline — Unmodified</span>
                <span class="meta">template densified (unit=3) | smoothed (window=3) | noise=1.0 | rot=12deg</span>
            </div>
            <div class="card-body"><img src="densified-unmodified.svg" alt="Densified Unmodified"></div>
""");
        if (!string.IsNullOrEmpty(densifiedUnmodifiedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{densifiedUnmodifiedStats}</div>");
        sb.AppendLine("""
        </div>
        <div class="card">
            <div class="card-header">
                <span>Densified Polyline — Perturbed Key Points</span>
                <span class="meta">template densified (unit=3) | 2 corners perturbed +-5 | smoothed (window=3) | noise=1.0 | rot=12deg</span>
            </div>
            <div class="card-body"><img src="densified-perturbed.svg" alt="Densified Perturbed"></div>
""");
        if (!string.IsNullOrEmpty(densifiedPerturbedStats))
            sb.AppendLine($"            <div class=\"card-stats\">{densifiedPerturbedStats}</div>");
        sb.AppendLine("""
        </div>
    </div>
</div>
<script>
document.querySelectorAll('.card-body img').forEach(img => {
    img.addEventListener('click', () => {
        document.getElementById('overlay-img').src = img.src;
        document.getElementById('overlay').classList.add('active');
    });
});
</script>
</body></html>
""");

        File.WriteAllText(Path.Combine(outputDir, "corrections.html"), sb.ToString());
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

    private static string Circle(Point<float> p, float r, string color)
        => string.Format(CultureInfo.InvariantCulture,
            "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\"/>",
            (double)p.X, (double)p.Y, F(r), color);

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
