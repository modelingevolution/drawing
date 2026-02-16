using System.Globalization;
using System.Text;
using FluentAssertions;
using ModelingEvolution.Drawing;
using ModelingEvolution.Drawing.Svg;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class DensifyTests
{
    private readonly ITestOutputHelper _output;

    public DensifyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private const float Tol = 1e-3f;

    private static float MaxEdgeLength(ReadOnlySpan<Point<float>> pts, bool closed)
    {
        float max = 0;
        int n = pts.Length;
        int edges = closed ? n : n - 1;
        for (int i = 0; i < edges; i++)
        {
            var len = pts[i].DistanceTo(pts[(i + 1) % n]);
            if (len > max) max = len;
        }
        return max;
    }

    // ── Polygon ──

    [Fact]
    public void Polygon_Densify_NoEdgeExceedsOneUnit()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(10, 5),
            new Point<float>(0, 5));

        var dense = poly.Densify();

        dense.Count.Should().BeGreaterThan(poly.Count);
        MaxEdgeLength(dense.AsSpan(), closed: true).Should().BeLessThanOrEqualTo(1f + Tol);
    }

    [Fact]
    public void Polygon_Densify_SmallPolygon_Unchanged()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(0.5f, 0),
            new Point<float>(0.25f, 0.4f));

        var dense = poly.Densify();

        dense.Count.Should().Be(poly.Count);
    }

    [Fact]
    public void Polygon_Densify_PreservesFirstAndLastVertices()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(5, 0),
            new Point<float>(5, 5));

        var dense = poly.Densify();
        var span = dense.AsSpan();

        span[0].X.Should().BeApproximately(0, Tol);
        span[0].Y.Should().BeApproximately(0, Tol);
    }

    // ── Polyline ──

    [Fact]
    public void Polyline_Densify_NoEdgeExceedsOneUnit()
    {
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(7, 0),
            new Point<float>(7, 3));

        var dense = pl.Densify();

        dense.Count.Should().BeGreaterThan(pl.Count);
        MaxEdgeLength(dense.AsSpan(), closed: false).Should().BeLessThanOrEqualTo(1f + Tol);
    }

    [Fact]
    public void Polyline_Densify_PreservesEndpoints()
    {
        var pl = new Polyline<float>(
            new Point<float>(1, 2),
            new Point<float>(11, 2));

        var dense = pl.Densify();
        var span = dense.AsSpan();

        span[0].X.Should().BeApproximately(1, Tol);
        span[0].Y.Should().BeApproximately(2, Tol);
        span[^1].X.Should().BeApproximately(11, Tol);
        span[^1].Y.Should().BeApproximately(2, Tol);
    }

    // ── Segment ──

    [Fact]
    public void Segment_Densify_ReturnsPolylineWithCorrectSpacing()
    {
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(5, 0));

        var dense = seg.Densify();

        dense.Count.Should().Be(6); // 5 steps + 1
        MaxEdgeLength(dense.AsSpan(), closed: false).Should().BeLessThanOrEqualTo(1f + Tol);
        dense.AsSpan()[0].X.Should().BeApproximately(0, Tol);
        dense.AsSpan()[^1].X.Should().BeApproximately(5, Tol);
    }

    [Fact]
    public void Segment_Densify_ShortSegment_ReturnsTwoPoints()
    {
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(0.5f, 0));

        var dense = seg.Densify();

        dense.Count.Should().Be(2);
    }

    // ── Circle ──

    [Fact]
    public void Circle_Densify_NoEdgeExceedsOneUnit()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);

        var dense = circle.Densify();

        dense.Count.Should().BeGreaterThanOrEqualTo(31); // circumference ~31.4
        MaxEdgeLength(dense.AsSpan(), closed: true).Should().BeLessThanOrEqualTo(1f + Tol);
    }

    [Fact]
    public void Circle_Densify_SmallCircle_MinimumPoints()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 0.1f);

        var dense = circle.Densify();

        dense.Count.Should().BeGreaterThanOrEqualTo(8); // minimum
    }

    // ── Triangle ──

    [Fact]
    public void Triangle_Densify_NoEdgeExceedsOneUnit()
    {
        var tri = new Triangle<float>(
            new Point<float>(0, 0),
            new Point<float>(5, 0),
            new Point<float>(2.5f, 4));

        var dense = tri.Densify();

        dense.Count.Should().BeGreaterThan(3);
        MaxEdgeLength(dense.AsSpan(), closed: true).Should().BeLessThanOrEqualTo(1f + Tol);
    }

    // ── Rectangle ──

    [Fact]
    public void Rectangle_Densify_NoEdgeExceedsOneUnit()
    {
        var rect = new Rectangle<float>(0, 0, 4, 3);

        var dense = rect.Densify();

        dense.Count.Should().BeGreaterThan(4);
        MaxEdgeLength(dense.AsSpan(), closed: true).Should().BeLessThanOrEqualTo(1f + Tol);
    }

    [Fact]
    public void Rectangle_Densify_CorrectTotalPoints()
    {
        // Perimeter = 2*(4+3) = 14, so ~14 points
        var rect = new Rectangle<float>(0, 0, 4, 3);
        var dense = rect.Densify();
        dense.Count.Should().Be(14);
    }

    // ── Path (Bezier) ──

    [Fact]
    public void Path_Densify_ReturnsPolylineWithDensePoints()
    {
        var curve = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 10),
            new Point<float>(7, 10),
            new Point<float>(10, 0));
        var path = new Path<float>(new[] { curve });

        var dense = path.Densify();

        dense.Count.Should().BeGreaterThan(2);
        MaxEdgeLength(dense.AsSpan(), closed: false).Should().BeLessThanOrEqualTo(2f);
    }

    [Fact]
    public void Path_Densify_EmptyPath_ReturnsEmptyPolyline()
    {
        var path = new Path<float>();

        var dense = path.Densify();

        dense.Count.Should().Be(0);
    }

    // ── SVG Rendering ──

    [Fact]
    public void RenderDensify_ToSvg()
    {
        var outputDir = Path.Combine(
            Path.GetDirectoryName(typeof(DensifyTests).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "densify-svg");
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var shapes = new (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine)[]
        {
            MakePolygonCase(),
            MakeTriangleCase(),
            MakeRectangleCase(),
            MakeCircleCase(),
            MakeSegmentCase(),
            MakePolylineCase(),
            MakePathCase(),
        };

        var scale = 8f; // scale up small shapes for visibility

        foreach (var (name, original, densePoly, denseLine) in shapes)
        {
            var denseSpan = densePoly.HasValue ? densePoly.Value.AsSpan() : denseLine!.Value.AsSpan();
            var isClosed = densePoly.HasValue;

            // Compute bounding box from dense points
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < denseSpan.Length; i++)
            {
                if (denseSpan[i].X < minX) minX = denseSpan[i].X;
                if (denseSpan[i].Y < minY) minY = denseSpan[i].Y;
                if (denseSpan[i].X > maxX) maxX = denseSpan[i].X;
                if (denseSpan[i].Y > maxY) maxY = denseSpan[i].Y;
            }

            float margin = 2f;
            float vbW = (maxX - minX) + margin * 2;
            float vbH = (maxY - minY) + margin * 2;
            float vbX = minX - margin;
            float vbY = minY - margin;
            int svgW = (int)(vbW * scale);
            int svgH = (int)(vbH * scale);

            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgW}\" height=\"{svgH}\" viewBox=\"{F(vbX)} {F(vbY)} {F(vbW)} {F(vbH)}\">");
            sb.AppendLine($"<rect x=\"{F(vbX)}\" y=\"{F(vbY)}\" width=\"{F(vbW)}\" height=\"{F(vbH)}\" fill=\"white\"/>");

            // 1. Draw original shape using SvgExporter
            var origPaint = new SvgPaint(Color.Parse("#e8edff"), Color.Parse("#4466aa"), 0.15f);
            var origExporter = GetExporter(original.GetType());
            sb.AppendLine(origExporter.Export(original, origPaint));

            // 2. Draw densified outline (edges between points)
            if (isClosed)
            {
                var polyExporter = new PolygonSvgExporter<float>();
                var outlinePaint = new SvgPaint(Colors.Transparent, Color.Parse("#dd3333"), 0.08f);
                sb.AppendLine(polyExporter.Export(densePoly!.Value, outlinePaint));
            }
            else
            {
                var plExporter = new PolylineSvgExporter<float>();
                var outlinePaint = new SvgPaint(Colors.Transparent, Color.Parse("#dd3333"), 0.08f);
                sb.AppendLine(plExporter.Export(denseLine!.Value, outlinePaint));
            }

            // 3. Draw densified points as small circles
            float pointR = 0.15f;
            for (int i = 0; i < denseSpan.Length; i++)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"#dd3333\"/>",
                    Convert.ToDouble(denseSpan[i].X),
                    Convert.ToDouble(denseSpan[i].Y),
                    pointR);
                sb.AppendLine();
            }

            sb.AppendLine("</svg>");

            var slug = name.ToLower().Replace(' ', '-').Replace("(", "").Replace(")", "");
            var fileName = $"{slug}.svg";
            File.WriteAllText(Path.Combine(outputDir, fileName), sb.ToString());
            _output.WriteLine($"Written: {fileName} ({denseSpan.Length} points)");
        }

        GenerateHtmlGallery(shapes, outputDir);
        _output.WriteLine($"\nAll SVGs written to: {outputDir}");
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakePolygonCase()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(10, 5),
            new Point<float>(0, 5));
        return ("Polygon", poly, poly.Densify(), null);
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakeTriangleCase()
    {
        var tri = new Triangle<float>(
            new Point<float>(0, 0),
            new Point<float>(8, 0),
            new Point<float>(4, 6));
        return ("Triangle", tri, tri.Densify(), null);
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakeRectangleCase()
    {
        var rect = new Rectangle<float>(0, 0, 12, 5);
        return ("Rectangle", rect, rect.Densify(), null);
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakeCircleCase()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 5f);
        return ("Circle", circle, circle.Densify(), null);
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakeSegmentCase()
    {
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 3));
        return ("Segment", seg, null, seg.Densify());
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakePolylineCase()
    {
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(5, 4),
            new Point<float>(10, 0),
            new Point<float>(15, 4));
        return ("Polyline", pl, null, pl.Densify());
    }

    private static (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine) MakePathCase()
    {
        var curve = new BezierCurve<float>(
            new Point<float>(0, 5),
            new Point<float>(3, -5),
            new Point<float>(7, 15),
            new Point<float>(10, 5));
        var path = new Path<float>(new[] { curve });
        return ("Path (Bezier)", path, null, path.Densify());
    }

    private static ISvgExporter GetExporter(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SvgExporterAttribute), false)
            .Cast<SvgExporterAttribute>()
            .FirstOrDefault();
        if (attr == null)
            throw new InvalidOperationException($"No SvgExporter for {type.Name}");
        var instance = Activator.CreateInstance(attr.Exporter);
        return instance switch
        {
            ISvgExporterFactory factory => factory.Create(type),
            ISvgExporter exporter => exporter,
            _ => throw new InvalidOperationException()
        };
    }

    private void GenerateHtmlGallery(
        (string name, object original, Polygon<float>? densePoly, Polyline<float>? denseLine)[] shapes,
        string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en"><head>
<meta charset="UTF-8">
<title>Densify — ModelingEvolution.Drawing</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #f0f2f5; color: #1d1d1f; }

.hero { background: linear-gradient(135deg, #1a2e1a 0%, #163e16 50%, #0f600f 100%); color: #fff; padding: 40px 32px 32px; }
.hero h1 { font-size: 28px; font-weight: 700; margin-bottom: 6px; }
.hero p { color: #a0d0a0; font-size: 15px; }

.container { max-width: 1200px; margin: 0 auto; padding: 24px; }

.legend { display: flex; gap: 24px; margin-bottom: 24px; justify-content: center; flex-wrap: wrap; }
.legend .item { display: flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 500; }
.legend .dot { width: 12px; height: 12px; border-radius: 50%; }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 16px; }
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
    <h1>Densify()</h1>
    <p>ModelingEvolution.Drawing &mdash; Places points along shape edges at most 1 unit apart</p>
</div>

<div class="overlay" id="overlay" onclick="this.classList.remove('active')"><img id="overlay-img"></div>

<div class="container">
    <div class="legend">
        <div class="item"><div class="dot" style="background:#e8edff; border:1px solid #4466aa"></div> Original shape (blue)</div>
        <div class="item"><div class="dot" style="background:#dd3333"></div> Densified points (red)</div>
    </div>
    <div class="cards">
""");

        foreach (var (name, _, densePoly, denseLine) in shapes)
        {
            var slug = name.ToLower().Replace(' ', '-').Replace("(", "").Replace(")", "");
            var pointCount = densePoly.HasValue ? densePoly.Value.Count : denseLine!.Value.Count;
            sb.AppendLine($"        <div class=\"card\">");
            sb.AppendLine($"            <div class=\"card-header\">");
            sb.AppendLine($"                <span>{name}</span>");
            sb.AppendLine($"                <span class=\"meta\">{pointCount} points</span>");
            sb.AppendLine($"            </div>");
            sb.AppendLine($"            <div class=\"card-body\"><img src=\"{slug}.svg\" alt=\"{name}\"></div>");
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

        File.WriteAllText(Path.Combine(outputDir, "densify.html"), sb.ToString());
        _output.WriteLine($"Written HTML gallery: {Path.Combine(outputDir, "densify.html")}");
    }

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
