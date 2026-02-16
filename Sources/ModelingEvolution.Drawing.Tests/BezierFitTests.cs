using System.Globalization;
using System.Text;
using FluentAssertions;
using ModelingEvolution.Drawing.Svg;
using Xunit;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class BezierFitTests
{
    private readonly ITestOutputHelper _output;

    public BezierFitTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Sample a known Bezier at N uniformly-spaced t values.
    /// </summary>
    private static Point<float>[] SampleBezier(BezierCurve<float> b, int n)
    {
        var pts = new Point<float>[n];
        for (int i = 0; i < n; i++)
        {
            var t = (float)i / (n - 1);
            pts[i] = b.Evaluate(t);
        }
        return pts;
    }

    /// <summary>
    /// Max distance between the fitted curve and a set of reference points.
    /// </summary>
    private static float MaxError(BezierCurve<float> fitted, Point<float>[] refPoints)
    {
        float maxErr = 0;
        for (int i = 0; i < refPoints.Length; i++)
        {
            var t = (float)i / (refPoints.Length - 1);
            var eval = fitted.Evaluate(t);
            var dx = eval.X - refPoints[i].X;
            var dy = eval.Y - refPoints[i].Y;
            var err = MathF.Sqrt(dx * dx + dy * dy);
            if (err > maxErr) maxErr = err;
        }
        return maxErr;
    }

    [Fact]
    public void Fit_TwoPoints_Returns1ThirdRuleBezier()
    {
        var pts = new Point<float>[]
        {
            new(0, 0),
            new(9, 0),
        };

        var fitted = BezierCurve<float>.Fit(pts);

        fitted.Start.Should().Be(pts[0]);
        fitted.End.Should().Be(pts[1]);
        // 1/3-rule: C0 = (3,0), C1 = (6,0)
        fitted.C0.X.Should().BeApproximately(3f, 0.01f);
        fitted.C0.Y.Should().BeApproximately(0f, 0.01f);
        fitted.C1.X.Should().BeApproximately(6f, 0.01f);
        fitted.C1.Y.Should().BeApproximately(0f, 0.01f);
    }

    [Fact]
    public void Fit_StraightLine_ProducesLinearBezier()
    {
        // Points along a straight line from (0,0) to (10,0)
        var pts = new Point<float>[11];
        for (int i = 0; i <= 10; i++)
            pts[i] = new Point<float>(i, 0);

        var fitted = BezierCurve<float>.Fit(pts);

        fitted.Start.Should().Be(new Point<float>(0, 0));
        fitted.End.Should().Be(new Point<float>(10, 0));
        // Control points should lie on the line (y ≈ 0)
        fitted.C0.Y.Should().BeApproximately(0f, 0.01f);
        fitted.C1.Y.Should().BeApproximately(0f, 0.01f);
        // And be between start and end
        fitted.C0.X.Should().BeInRange(0f, 10f);
        fitted.C1.X.Should().BeInRange(0f, 10f);
    }

    [Fact]
    public void Fit_RecoverKnownBezier_SymmetricArch()
    {
        // Known Bezier: symmetric arch
        var original = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 10),
            new Point<float>(7, 10),
            new Point<float>(10, 0));

        // Sample 50 points from it
        var pts = SampleBezier(original, 50);

        var fitted = BezierCurve<float>.Fit(pts);

        // Endpoints must match exactly
        fitted.Start.Should().Be(original.Start);
        fitted.End.Should().Be(original.End);

        // Control points should be close to originals
        fitted.C0.X.Should().BeApproximately(original.C0.X, 0.5f);
        fitted.C0.Y.Should().BeApproximately(original.C0.Y, 0.5f);
        fitted.C1.X.Should().BeApproximately(original.C1.X, 0.5f);
        fitted.C1.Y.Should().BeApproximately(original.C1.Y, 0.5f);
    }

    [Fact]
    public void Fit_RecoverKnownBezier_Asymmetric()
    {
        // Asymmetric S-curve — extreme velocity variation means control points
        // are not uniquely recoverable; shape accuracy is verified by MaxErrorSmall test.
        var original = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(2, 15),
            new Point<float>(8, -5),
            new Point<float>(10, 0));

        var pts = SampleBezier(original, 100);
        var fitted = BezierCurve<float>.Fit(pts);

        fitted.C0.X.Should().BeApproximately(original.C0.X, 2f);
        fitted.C0.Y.Should().BeApproximately(original.C0.Y, 2f);
        fitted.C1.X.Should().BeApproximately(original.C1.X, 2f);
        fitted.C1.Y.Should().BeApproximately(original.C1.Y, 2f);

        // Shape should still be accurate
        var refPts = SampleBezier(original, 1000);
        MaxError(fitted, refPts).Should().BeLessThan(1f);
    }

    [Fact]
    public void Fit_RecoverKnownBezier_MaxErrorSmall()
    {
        var original = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(5, 20),
            new Point<float>(15, -10),
            new Point<float>(20, 5));

        var pts = SampleBezier(original, 200);
        var fitted = BezierCurve<float>.Fit(pts);

        // Evaluate fitted at 1000 points and check max deviation
        var refPts = SampleBezier(original, 1000);
        var maxErr = MaxError(fitted, refPts);
        maxErr.Should().BeLessThan(0.5f);
    }

    [Fact]
    public void Fit_FewPoints_StillReasonable()
    {
        // Only 4 points (minimum for meaningful fit: 2 interior)
        var original = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 10),
            new Point<float>(7, 10),
            new Point<float>(10, 0));

        var pts = SampleBezier(original, 4);
        var fitted = BezierCurve<float>.Fit(pts);

        // Should at least pass through endpoints
        fitted.Start.Should().Be(original.Start);
        fitted.End.Should().Be(original.End);

        // With only 2 interior points the fit may not be perfect, but should be reasonable
        var refPts = SampleBezier(original, 100);
        var maxErr = MaxError(fitted, refPts);
        maxErr.Should().BeLessThan(3f);
    }

    [Fact]
    public void Fit_ThreePoints_OneInterior()
    {
        // 3 points: start, one interior, end
        var pts = new Point<float>[]
        {
            new(0, 0),
            new(5, 10),
            new(10, 0),
        };

        var fitted = BezierCurve<float>.Fit(pts);

        fitted.Start.Should().Be(pts[0]);
        fitted.End.Should().Be(pts[2]);
        // The curve should pass near (5, 10) at roughly t=0.5
        var mid = fitted.Evaluate(0.5f);
        mid.Y.Should().BeGreaterThan(5f); // should bulge upward toward (5,10)
    }

    [Fact]
    public void Fit_DensifiedBezier_HighPrecision()
    {
        // Use Densify() to get arc-length spaced points (realistic input)
        var original = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(4, 12),
            new Point<float>(8, 12),
            new Point<float>(12, 0));

        var densified = original.Densify();
        var pts = densified.Span.ToArray();

        var fitted = BezierCurve<float>.Fit(pts);

        // Control points should be very close
        fitted.C0.X.Should().BeApproximately(original.C0.X, 1f);
        fitted.C0.Y.Should().BeApproximately(original.C0.Y, 1f);
        fitted.C1.X.Should().BeApproximately(original.C1.X, 1f);
        fitted.C1.Y.Should().BeApproximately(original.C1.Y, 1f);
    }

    [Fact]
    public void Fit_Semicircle_ReasonableApproximation()
    {
        // Points along a semicircle (not exactly a Bezier, so fit is approximate)
        var n = 50;
        var pts = new Point<float>[n];
        for (int i = 0; i < n; i++)
        {
            var angle = MathF.PI * i / (n - 1);
            pts[i] = new Point<float>(MathF.Cos(angle) * 10, MathF.Sin(angle) * 10);
        }

        var fitted = BezierCurve<float>.Fit(pts);

        fitted.Start.X.Should().BeApproximately(10f, 0.01f);
        fitted.End.X.Should().BeApproximately(-10f, 0.01f);

        // The peak should be near y=10 (top of semicircle)
        var peak = fitted.Evaluate(0.5f);
        peak.Y.Should().BeGreaterThan(8f);
    }

    [Fact]
    public void Fit_SinglePoint_Throws()
    {
        var act = () => BezierCurve<float>.Fit(new Point<float>[] { new(0, 0) });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fit_EmptySpan_Throws()
    {
        var act = () => BezierCurve<float>.Fit(ReadOnlySpan<Point<float>>.Empty);
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════
    // SVG visualization
    // ═══════════════════════════════════

    [Fact]
    public void RenderFit_ToSvg()
    {
        var outputDir = Path.Combine(
            Path.GetDirectoryName(typeof(BezierFitTests).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "bezier-fit-svg");
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var cases = new List<FitCase>
        {
            MakeSymmetricArch(),
            MakeAsymmetricSCurve(),
            MakeMaxErrorCurve(),
            MakeFewPoints(),
            MakeThreePoints(),
            MakeDensifiedBezier(),
            MakeSemicircle(),
            MakeStraightLine(),
            MakeTwoPoints(),
        };

        foreach (var c in cases)
        {
            var svg = RenderFitCase(c);
            var slug = c.Name.ToLower().Replace(' ', '-').Replace("(", "").Replace(")", "");
            var fileName = $"{slug}.svg";
            File.WriteAllText(Path.Combine(outputDir, fileName), svg);
            _output.WriteLine($"Written: {fileName} ({c.Points.Length} sample points)");
        }

        GenerateHtml(cases, outputDir);
        _output.WriteLine($"\nAll SVGs written to: {outputDir}");
    }

    private record FitCase(
        string Name,
        BezierCurve<float>? Original,
        Point<float>[] Points,
        BezierCurve<float> Fitted);

    private static FitCase MakeSymmetricArch()
    {
        var original = new BezierCurve<float>(
            new(0, 0), new(3, 10), new(7, 10), new(10, 0));
        var pts = SampleBezier(original, 50);
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Symmetric Arch", original, pts, fitted);
    }

    private static FitCase MakeAsymmetricSCurve()
    {
        var original = new BezierCurve<float>(
            new(0, 0), new(2, 15), new(8, -5), new(10, 0));
        var pts = SampleBezier(original, 100);
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Asymmetric S-Curve", original, pts, fitted);
    }

    private static FitCase MakeMaxErrorCurve()
    {
        var original = new BezierCurve<float>(
            new(0, 0), new(5, 20), new(15, -10), new(20, 5));
        var pts = SampleBezier(original, 200);
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Max Error Curve", original, pts, fitted);
    }

    private static FitCase MakeFewPoints()
    {
        var original = new BezierCurve<float>(
            new(0, 0), new(3, 10), new(7, 10), new(10, 0));
        var pts = SampleBezier(original, 5);
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Few Points (5)", original, pts, fitted);
    }

    private static FitCase MakeThreePoints()
    {
        var pts = new Point<float>[] { new(0, 0), new(5, 10), new(10, 0) };
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Three Points", null, pts, fitted);
    }

    private static FitCase MakeDensifiedBezier()
    {
        var original = new BezierCurve<float>(
            new(0, 0), new(4, 12), new(8, 12), new(12, 0));
        var densified = original.Densify();
        var pts = densified.Span.ToArray();
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Densified Bezier", original, pts, fitted);
    }

    private static FitCase MakeSemicircle()
    {
        int n = 50;
        var pts = new Point<float>[n];
        for (int i = 0; i < n; i++)
        {
            var angle = MathF.PI * i / (n - 1);
            pts[i] = new(MathF.Cos(angle) * 10, MathF.Sin(angle) * 10);
        }
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Semicircle", null, pts, fitted);
    }

    private static FitCase MakeStraightLine()
    {
        var pts = new Point<float>[11];
        for (int i = 0; i <= 10; i++)
            pts[i] = new Point<float>(i, 0);
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Straight Line", null, pts, fitted);
    }

    private static FitCase MakeTwoPoints()
    {
        var pts = new Point<float>[] { new(0, 0), new(9, 0) };
        var fitted = BezierCurve<float>.Fit(pts);
        return new("Two Points", null, pts, fitted);
    }

    private static string RenderFitCase(FitCase c)
    {
        // Compute bounding box from all points
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        void Expand(float x, float y)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        foreach (var p in c.Points) Expand(p.X, p.Y);

        // Expand for original control points
        if (c.Original.HasValue)
        {
            var o = c.Original.Value;
            Expand(o.C0.X, o.C0.Y);
            Expand(o.C1.X, o.C1.Y);
        }
        // Expand for fitted control points
        Expand(c.Fitted.C0.X, c.Fitted.C0.Y);
        Expand(c.Fitted.C1.X, c.Fitted.C1.Y);

        // Also sample fitted curve for BB
        for (int i = 0; i <= 20; i++)
        {
            var p = c.Fitted.Evaluate(i / 20f);
            Expand(p.X, p.Y);
        }
        if (c.Original.HasValue)
        {
            for (int i = 0; i <= 20; i++)
            {
                var p = c.Original.Value.Evaluate(i / 20f);
                Expand(p.X, p.Y);
            }
        }

        float margin = 2f;
        float vbX = minX - margin;
        float vbY = minY - margin;
        float vbW = (maxX - minX) + margin * 2;
        float vbH = (maxY - minY) + margin * 2;
        // Flip Y: SVG Y grows downward, math Y grows upward
        float scale = 10f;
        int svgW = Math.Max(200, (int)(vbW * scale));
        int svgH = Math.Max(120, (int)(vbH * scale));

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgW}\" height=\"{svgH}\" viewBox=\"{F(vbX)} {F(-maxY - margin)} {F(vbW)} {F(vbH)}\">");
        sb.AppendLine($"<rect x=\"{F(vbX)}\" y=\"{F(-maxY - margin)}\" width=\"{F(vbW)}\" height=\"{F(vbH)}\" fill=\"white\"/>");
        // Flip Y axis: apply transform to negate Y
        sb.AppendLine("<g transform=\"scale(1,-1)\">");

        // 1. Original Bezier (blue dashed, if available)
        if (c.Original.HasValue)
        {
            var o = c.Original.Value;
            sb.AppendLine(BezierPath(o, "#4466aa", 0.15f, "4,2"));
            // Original control point handles (thin blue lines)
            sb.AppendLine(ControlHandle(o.Start, o.C0, "#4466aa", 0.06f));
            sb.AppendLine(ControlHandle(o.End, o.C1, "#4466aa", 0.06f));
            sb.AppendLine(Diamond(o.C0, 0.3f, "#4466aa"));
            sb.AppendLine(Diamond(o.C1, 0.3f, "#4466aa"));
        }

        // 2. Sample points (gray dots connected by thin gray polyline)
        sb.Append("<polyline points=\"");
        for (int i = 0; i < c.Points.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}",
                (double)c.Points[i].X, (double)c.Points[i].Y);
        }
        sb.AppendLine("\" fill=\"none\" stroke=\"#999999\" stroke-width=\"0.06\" stroke-dasharray=\"2,1\"/>");

        float pointR = Math.Max(0.12f, vbW / 200f);
        foreach (var p in c.Points)
            sb.AppendLine(Circle(p, pointR, "#888888"));

        // 3. Fitted Bezier (red solid)
        sb.AppendLine(BezierPath(c.Fitted, "#dd3333", 0.15f, null));
        // Fitted control point handles
        sb.AppendLine(ControlHandle(c.Fitted.Start, c.Fitted.C0, "#dd3333", 0.06f));
        sb.AppendLine(ControlHandle(c.Fitted.End, c.Fitted.C1, "#dd3333", 0.06f));
        sb.AppendLine(Diamond(c.Fitted.C0, 0.3f, "#dd3333"));
        sb.AppendLine(Diamond(c.Fitted.C1, 0.3f, "#dd3333"));

        // 4. Endpoints (black dots)
        sb.AppendLine(Circle(c.Fitted.Start, pointR * 1.5f, "#000000"));
        sb.AppendLine(Circle(c.Fitted.End, pointR * 1.5f, "#000000"));

        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string BezierPath(BezierCurve<float> b, string color, float width, string? dashArray)
    {
        var dash = dashArray != null ? $" stroke-dasharray=\"{dashArray}\"" : "";
        return string.Format(CultureInfo.InvariantCulture,
            "<path d=\"M{0},{1} C{2},{3} {4},{5} {6},{7}\" fill=\"none\" stroke=\"{8}\" stroke-width=\"{9}\"{10}/>",
            (double)b.Start.X, (double)b.Start.Y,
            (double)b.C0.X, (double)b.C0.Y,
            (double)b.C1.X, (double)b.C1.Y,
            (double)b.End.X, (double)b.End.Y,
            color, F(width), dash);
    }

    private static string ControlHandle(Point<float> anchor, Point<float> cp, string color, float width)
        => string.Format(CultureInfo.InvariantCulture,
            "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\"/>",
            (double)anchor.X, (double)anchor.Y,
            (double)cp.X, (double)cp.Y,
            color, F(width));

    private static string Diamond(Point<float> p, float size, string color)
        => string.Format(CultureInfo.InvariantCulture,
            "<polygon points=\"{0},{1} {2},{3} {4},{5} {6},{7}\" fill=\"{8}\"/>",
            (double)p.X, (double)(p.Y - size),
            (double)(p.X + size), (double)p.Y,
            (double)p.X, (double)(p.Y + size),
            (double)(p.X - size), (double)p.Y,
            color);

    private static string Circle(Point<float> p, float r, string color)
        => string.Format(CultureInfo.InvariantCulture,
            "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\"/>",
            (double)p.X, (double)p.Y, F(r), color);

    private void GenerateHtml(List<FitCase> cases, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en"><head>
<meta charset="UTF-8">
<title>BezierCurve.Fit() — ModelingEvolution.Drawing</title>
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

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(400px, 1fr)); gap: 16px; }
.card { background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); transition: box-shadow 0.2s, transform 0.2s; }
.card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.15); transform: translateY(-2px); }
.card-header { padding: 10px 16px; font-size: 14px; font-weight: 600; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f0f0f0; }
.card-header .meta { color: #999; font-weight: 400; font-size: 12px; }
.card-body { padding: 16px; display: flex; align-items: center; justify-content: center; background: #fafbfc; cursor: pointer; }
.card-body img { width: 100%; height: auto; object-fit: contain; }

.overlay { display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.85); z-index:1000; cursor:pointer; align-items:center; justify-content:center; }
.overlay.active { display:flex; }
.overlay img { width:90vw; height:90vh; object-fit:contain; background:#fff; border-radius:8px; padding:20px; }
</style>
</head><body>

<div class="hero">
    <h1>BezierCurve&lt;T&gt;.Fit()</h1>
    <p>Schneider iterative least-squares fitting &mdash; ModelingEvolution.Drawing</p>
</div>

<div class="overlay" id="overlay" onclick="this.classList.remove('active')"><img id="overlay-img"></div>

<div class="container">
    <div class="legend">
        <div class="item"><div class="swatch" style="background:#4466aa"></div> Original Bezier (blue dashed)</div>
        <div class="item"><div class="swatch" style="background:#888888"></div> Sample points (gray)</div>
        <div class="item"><div class="swatch" style="background:#dd3333"></div> Fitted Bezier (red solid)</div>
        <div class="item"><div class="swatch" style="background:#000000; width:8px; height:8px; border-radius:50%"></div> Endpoints</div>
        <div class="item">&#9670; Control points</div>
    </div>
    <div class="cards">
""");

        foreach (var c in cases)
        {
            var slug = c.Name.ToLower().Replace(' ', '-').Replace("(", "").Replace(")", "");
            var maxErr = c.Original.HasValue
                ? MaxError(c.Fitted, SampleBezier(c.Original.Value, 200))
                : 0f;
            var meta = c.Original.HasValue
                ? $"{c.Points.Length} pts | maxErr={maxErr:F2}"
                : $"{c.Points.Length} pts";

            sb.AppendLine($"        <div class=\"card\">");
            sb.AppendLine($"            <div class=\"card-header\">");
            sb.AppendLine($"                <span>{c.Name}</span>");
            sb.AppendLine($"                <span class=\"meta\">{meta}</span>");
            sb.AppendLine($"            </div>");
            sb.AppendLine($"            <div class=\"card-body\"><img src=\"{slug}.svg\" alt=\"{c.Name}\"></div>");
            sb.AppendLine($"        </div>");
        }

        sb.AppendLine("""
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

        File.WriteAllText(Path.Combine(outputDir, "bezier-fit.html"), sb.ToString());
        _output.WriteLine($"Written HTML gallery: {Path.Combine(outputDir, "bezier-fit.html")}");
    }

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
