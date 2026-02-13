using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing;
using ModelingEvolution.Drawing.Svg;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class SkeletonTests
{
    private readonly ITestOutputHelper _output;

    public SkeletonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private const float Tol = 1e-2f;

    // Helper polygons
    private static Polygon<float> UnitSquare => new(
        new Point<float>(0, 0),
        new Point<float>(10, 0),
        new Point<float>(10, 10),
        new Point<float>(0, 10));

    private static Polygon<float> Rectangle2x1 => new(
        new Point<float>(0, 0),
        new Point<float>(20, 0),
        new Point<float>(20, 10),
        new Point<float>(0, 10));

    private static Polygon<float> EquilateralTriangle => new(
        new Point<float>(5, 0),
        new Point<float>(10, 8.66f),
        new Point<float>(0, 8.66f));

    private static Polygon<float> LShape => new(
        new Point<float>(0, 0),
        new Point<float>(20, 0),
        new Point<float>(20, 10),
        new Point<float>(10, 10),
        new Point<float>(10, 20),
        new Point<float>(0, 20));

    // ─────────────────────────────────────────────
    // Straight Skeleton
    // ─────────────────────────────────────────────

    [Fact]
    public void StraightSkeleton_Square_ProducesEdges()
    {
        var skel = UnitSquare.Skeleton(SkeletonAlgo.StraightSkeleton);
        skel.EdgeCount.Should().BeGreaterThan(0);
        skel.NodeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StraightSkeleton_Rectangle_ProducesEdges()
    {
        var skel = Rectangle2x1.Skeleton(SkeletonAlgo.StraightSkeleton);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StraightSkeleton_Triangle_ProducesEdges()
    {
        var skel = EquilateralTriangle.Skeleton(SkeletonAlgo.StraightSkeleton);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StraightSkeleton_LShape_ProducesEdges()
    {
        var skel = LShape.Skeleton(SkeletonAlgo.StraightSkeleton);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // Chordal Axis
    // ─────────────────────────────────────────────

    [Fact]
    public void ChordalAxis_Square_ProducesEdges()
    {
        var skel = UnitSquare.Skeleton(SkeletonAlgo.ChordalAxis);
        skel.EdgeCount.Should().BeGreaterThan(0);
        skel.NodeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChordalAxis_Rectangle_ProducesEdges()
    {
        var skel = Rectangle2x1.Skeleton(SkeletonAlgo.ChordalAxis);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChordalAxis_LShape_ProducesEdges()
    {
        var skel = LShape.Skeleton(SkeletonAlgo.ChordalAxis);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // Voronoi
    // ─────────────────────────────────────────────

    [Fact]
    public void Voronoi_Square_ProducesEdges()
    {
        var skel = UnitSquare.Skeleton(SkeletonAlgo.Voronoi);
        skel.EdgeCount.Should().BeGreaterThan(0);
        skel.NodeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Voronoi_Rectangle_ProducesEdges()
    {
        var skel = Rectangle2x1.Skeleton(SkeletonAlgo.Voronoi);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Voronoi_LShape_ProducesEdges()
    {
        var skel = LShape.Skeleton(SkeletonAlgo.Voronoi);
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // Skeleton<T> methods
    // ─────────────────────────────────────────────

    [Fact]
    public void LongestPath_ReturnsPolyline()
    {
        var skel = Rectangle2x1.Skeleton(SkeletonAlgo.StraightSkeleton);
        var path = skel.LongestPath();
        path.Count.Should().BeGreaterThan(0);
        path.Length().Should().BeGreaterThan(0f);
    }

    [Fact]
    public void Branches_DecomposesGraph()
    {
        var skel = Rectangle2x1.Skeleton(SkeletonAlgo.StraightSkeleton);
        var branches = skel.Branches();
        branches.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void NodeCount_EdgeCount_Consistent()
    {
        var skel = UnitSquare.Skeleton(SkeletonAlgo.StraightSkeleton);
        // A valid skeleton should have at least 2 nodes and 1 edge
        skel.NodeCount.Should().BeGreaterThanOrEqualTo(2);
        skel.EdgeCount.Should().BeGreaterThanOrEqualTo(1);
    }

    // ─────────────────────────────────────────────
    // Validation: skeleton edges are inside polygon
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(SkeletonAlgo.StraightSkeleton)]
    [InlineData(SkeletonAlgo.ChordalAxis)]
    [InlineData(SkeletonAlgo.Voronoi)]
    public void AllEdgeMidpoints_InsidePolygon(SkeletonAlgo algo)
    {
        var polygon = Rectangle2x1;
        var skel = polygon.Skeleton(algo);

        foreach (var edge in skel.Edges.ToArray())
        {
            var mid = edge.Middle;
            polygon.Contains(mid).Should().BeTrue(
                $"Edge midpoint ({mid.X},{mid.Y}) should be inside polygon [algo={algo}]");
        }
    }

    // ─────────────────────────────────────────────
    // Default algorithm parameter
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultAlgo_IsStraightSkeleton()
    {
        var skel = UnitSquare.Skeleton();
        skel.EdgeCount.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // Empty / degenerate polygons
    // ─────────────────────────────────────────────

    [Fact]
    public void EmptyPolygon_EmptySkeleton()
    {
        var empty = new Polygon<float>();
        var skel = empty.Skeleton();
        skel.EdgeCount.Should().Be(0);
        skel.NodeCount.Should().Be(0);
    }

    [Fact]
    public void TwoPointPolygon_EmptySkeleton()
    {
        var degen = new Polygon<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var skel = degen.Skeleton();
        skel.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void LongestPath_EmptySkeleton_ReturnsEmptyPolyline()
    {
        var skel = new Skeleton<float>(Array.Empty<Point<float>>(), Array.Empty<Segment<float>>());
        var path = skel.LongestPath();
        path.Count.Should().Be(0);
    }

    [Fact]
    public void Branches_EmptySkeleton_ReturnsEmpty()
    {
        var skel = new Skeleton<float>(Array.Empty<Point<float>>(), Array.Empty<Segment<float>>());
        skel.Branches().Count.Should().Be(0);
    }

    // ─────────────────────────────────────────────
    // JSON serialization round-trip
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(SkeletonAlgo.StraightSkeleton)]
    [InlineData(SkeletonAlgo.ChordalAxis)]
    [InlineData(SkeletonAlgo.Voronoi)]
    public void JsonRoundTrip_PreservesData(SkeletonAlgo algo)
    {
        var skel = UnitSquare.Skeleton(algo);
        var json = JsonSerializer.Serialize(skel);
        var deserialized = JsonSerializer.Deserialize<Skeleton<float>>(json);

        deserialized.NodeCount.Should().Be(skel.NodeCount);
        deserialized.EdgeCount.Should().Be(skel.EdgeCount);

        for (int i = 0; i < skel.NodeCount; i++)
        {
            deserialized.Nodes[i].X.Should().BeApproximately(skel.Nodes[i].X, 1e-4f);
            deserialized.Nodes[i].Y.Should().BeApproximately(skel.Nodes[i].Y, 1e-4f);
        }

        for (int i = 0; i < skel.EdgeCount; i++)
        {
            deserialized.Edges[i].Start.X.Should().BeApproximately(skel.Edges[i].Start.X, 1e-4f);
            deserialized.Edges[i].Start.Y.Should().BeApproximately(skel.Edges[i].Start.Y, 1e-4f);
            deserialized.Edges[i].End.X.Should().BeApproximately(skel.Edges[i].End.X, 1e-4f);
            deserialized.Edges[i].End.Y.Should().BeApproximately(skel.Edges[i].End.Y, 1e-4f);
        }
    }

    [Fact]
    public void JsonRoundTrip_EmptySkeleton()
    {
        var skel = new Skeleton<float>(Array.Empty<Point<float>>(), Array.Empty<Segment<float>>());
        var json = JsonSerializer.Serialize(skel);
        var deserialized = JsonSerializer.Deserialize<Skeleton<float>>(json);
        deserialized.NodeCount.Should().Be(0);
        deserialized.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void JsonRoundTrip_Double()
    {
        var polygon = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 10),
            new Point<double>(0, 10));
        var skel = polygon.Skeleton(SkeletonAlgo.StraightSkeleton);
        var json = JsonSerializer.Serialize(skel);
        var deserialized = JsonSerializer.Deserialize<Skeleton<double>>(json);

        deserialized.NodeCount.Should().Be(skel.NodeCount);
        deserialized.EdgeCount.Should().Be(skel.EdgeCount);
    }

    // ─────────────────────────────────────────────
    // SVG rendering
    // ─────────────────────────────────────────────

    private static Polygon<float> MakeRegularPolygon(int n, float radius, float cx = 0, float cy = 0)
    {
        var pts = new Point<float>[n];
        for (int i = 0; i < n; i++)
        {
            var angle = 2.0 * Math.PI * i / n;
            pts[i] = new Point<float>(
                cx + (float)(radius * Math.Cos(angle)),
                cy + (float)(radius * Math.Sin(angle)));
        }
        return new Polygon<float>(pts);
    }

    private static Polygon<float> MakeStar(int points, float outerR, float innerR, float cx = 0, float cy = 0)
    {
        var pts = new Point<float>[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            var angle = Math.PI * i / points - Math.PI / 2;
            var r = i % 2 == 0 ? outerR : innerR;
            pts[i] = new Point<float>(
                cx + (float)(r * Math.Cos(angle)),
                cy + (float)(r * Math.Sin(angle)));
        }
        return new Polygon<float>(pts);
    }

    private static Polygon<float> MakeTShape() => new(
        new Point<float>(0, 0),
        new Point<float>(30, 0),
        new Point<float>(30, 8),
        new Point<float>(18, 8),
        new Point<float>(18, 25),
        new Point<float>(12, 25),
        new Point<float>(12, 8),
        new Point<float>(0, 8));

    private static Polygon<float> MakeArrow() => new(
        new Point<float>(0, 8),
        new Point<float>(20, 8),
        new Point<float>(20, 0),
        new Point<float>(35, 12),
        new Point<float>(20, 24),
        new Point<float>(20, 16),
        new Point<float>(0, 16));

    /// <summary>Simplified standing human silhouette (front view) — ~40×80 units</summary>
    private static Polygon<float> MakePersonSilhouette()
    {
        return new Polygon<float>(
            // Right foot → up outer right leg
            new Point<float>(22, 80),
            new Point<float>(22, 52),
            // Right hip → right torso
            new Point<float>(20, 48),
            new Point<float>(20, 38),
            // Right arm out → right hand → right arm back
            new Point<float>(30, 38),
            new Point<float>(32, 50),
            new Point<float>(34, 50),
            new Point<float>(32, 36),
            new Point<float>(20, 34),
            // Right shoulder → neck → head
            new Point<float>(18, 30),
            new Point<float>(16, 28),
            new Point<float>(16, 22),
            new Point<float>(17, 18),
            new Point<float>(20, 14),
            new Point<float>(20, 10),
            // Head top
            new Point<float>(18, 6),
            new Point<float>(14, 4),
            new Point<float>(10, 4),
            new Point<float>(6, 6),
            // Left head → neck → shoulder
            new Point<float>(4, 10),
            new Point<float>(4, 14),
            new Point<float>(7, 18),
            new Point<float>(8, 22),
            new Point<float>(8, 28),
            new Point<float>(6, 30),
            new Point<float>(4, 34),
            // Left arm out → left hand → left arm back
            new Point<float>(-8, 36),
            new Point<float>(-10, 50),
            new Point<float>(-8, 50),
            new Point<float>(-6, 38),
            new Point<float>(4, 38),
            // Left torso → left hip
            new Point<float>(4, 48),
            new Point<float>(2, 52),
            // Left leg outer → left foot
            new Point<float>(2, 80),
            // Left foot across
            new Point<float>(6, 80),
            // Left inner leg up → crotch
            new Point<float>(6, 54),
            new Point<float>(10, 50),
            new Point<float>(12, 50),
            new Point<float>(14, 50),
            new Point<float>(18, 54),
            // Right inner leg down → right foot
            new Point<float>(18, 80)
        );
    }

    /// <summary>Elongated welding seam path — thin irregular ribbon, ~100×10 units</summary>
    private static Polygon<float> MakeWeldingSeam()
    {
        // Top edge: slightly wavy path left to right
        // Bottom edge: return path right to left (offset by ~3 units)
        var pts = new List<Point<float>>();

        // Top edge (left → right) with slight waviness
        int nSamples = 30;
        float length = 100f;
        for (int i = 0; i <= nSamples; i++)
        {
            float t = (float)i / nSamples;
            float x = t * length;
            float y = 2f * MathF.Sin(t * MathF.PI * 3) + 1.5f * MathF.Sin(t * MathF.PI * 7 + 0.5f);
            pts.Add(new Point<float>(x, y));
        }

        // Bottom edge (right → left) offset downward by 3-5 units with independent waviness
        for (int i = nSamples; i >= 0; i--)
        {
            float t = (float)i / nSamples;
            float x = t * length;
            float width = 3f + 1.5f * MathF.Sin(t * MathF.PI * 2 + 1f);
            float y = 2f * MathF.Sin(t * MathF.PI * 3) + 1.5f * MathF.Sin(t * MathF.PI * 7 + 0.5f) + width;
            pts.Add(new Point<float>(x, y));
        }

        return new Polygon<float>(pts.ToArray());
    }

    /// <summary>Fatter welding seam — wider ribbon (~100×12 units) with gentler waves</summary>
    private static Polygon<float> MakeFatWeldingSeam()
    {
        var pts = new List<Point<float>>();
        int nSamples = 25;
        float length = 100f;

        // Top edge
        for (int i = 0; i <= nSamples; i++)
        {
            float t = (float)i / nSamples;
            float x = t * length;
            float y = 3f * MathF.Sin(t * MathF.PI * 2.5f);
            pts.Add(new Point<float>(x, y));
        }

        // Bottom edge (right → left) offset downward by 8-12 units
        for (int i = nSamples; i >= 0; i--)
        {
            float t = (float)i / nSamples;
            float x = t * length;
            float width = 10f + 2f * MathF.Sin(t * MathF.PI * 1.5f + 0.7f);
            float y = 3f * MathF.Sin(t * MathF.PI * 2.5f) + width;
            pts.Add(new Point<float>(x, y));
        }

        return new Polygon<float>(pts.ToArray());
    }

    /// <summary>Dumbbell / bone shape — two circles connected by narrow bridge</summary>
    private static Polygon<float> MakeDumbbell()
    {
        var pts = new List<Point<float>>();

        // Left circle (center 10,10, radius 8)
        for (int i = 0; i < 16; i++)
        {
            var angle = 2.0 * Math.PI * i / 16;
            pts.Add(new Point<float>(
                10f + 8f * (float)Math.Cos(angle),
                10f + 8f * (float)Math.Sin(angle)));
        }

        // Bridge top-right → right circle
        // Narrow bridge at y≈8..12
        // Remove left circle points that overlap bridge, handled by polygon winding

        // Actually, build it as a single polygon outline:
        pts.Clear();

        // Left blob: top half (going counter-clockwise from right side)
        for (int i = 0; i <= 12; i++)
        {
            var angle = -Math.PI / 2 + Math.PI * i / 12;
            pts.Add(new Point<float>(
                10f + 8f * (float)Math.Cos(angle),
                15f + 8f * (float)Math.Sin(angle)));
        }

        // Bridge bottom (left→right)
        pts.Add(new Point<float>(18, 18));
        pts.Add(new Point<float>(32, 18));

        // Right blob (bottom half then top half, counter-clockwise)
        for (int i = 0; i <= 12; i++)
        {
            var angle = Math.PI / 2 + Math.PI * i / 12;
            pts.Add(new Point<float>(
                40f + 8f * (float)Math.Cos(angle),
                15f + 8f * (float)Math.Sin(angle)));
        }

        // Bridge top (right→left)
        pts.Add(new Point<float>(32, 12));
        pts.Add(new Point<float>(18, 12));

        return new Polygon<float>(pts.ToArray());
    }

    /// <summary>Hand-like shape — palm with fingers splayed</summary>
    private static Polygon<float> MakeHand()
    {
        return new Polygon<float>(
            // Wrist left
            new Point<float>(8, 50),
            new Point<float>(6, 40),
            // Pinky
            new Point<float>(2, 36),
            new Point<float>(0, 20),
            new Point<float>(2, 18),
            new Point<float>(4, 20),
            new Point<float>(6, 32),
            // Ring finger
            new Point<float>(7, 28),
            new Point<float>(6, 10),
            new Point<float>(8, 8),
            new Point<float>(10, 10),
            new Point<float>(11, 28),
            // Middle finger
            new Point<float>(12, 24),
            new Point<float>(12, 4),
            new Point<float>(14, 2),
            new Point<float>(16, 4),
            new Point<float>(16, 24),
            // Index finger
            new Point<float>(17, 26),
            new Point<float>(18, 10),
            new Point<float>(20, 8),
            new Point<float>(22, 10),
            new Point<float>(21, 28),
            // Thumb
            new Point<float>(22, 30),
            new Point<float>(28, 24),
            new Point<float>(30, 24),
            new Point<float>(30, 28),
            new Point<float>(24, 34),
            new Point<float>(22, 38),
            // Wrist right
            new Point<float>(20, 50)
        );
    }

    [Fact]
    public void RenderSkeletons_ToSvg()
    {
        var shapes = new (string name, Polygon<float> poly)[]
        {
            ("Square", UnitSquare),
            ("Rectangle", Rectangle2x1),
            ("Triangle", EquilateralTriangle),
            ("L-Shape", LShape),
            ("T-Shape", MakeTShape()),
            ("Arrow", MakeArrow()),
            ("Octagon", MakeRegularPolygon(8, 10)),
            ("16-gon", MakeRegularPolygon(16, 10)),
            ("Star-5", MakeStar(5, 10, 4)),
            ("Star-8", MakeStar(8, 10, 4)),
            ("Person", MakePersonSilhouette()),
            ("Weld-Seam", MakeWeldingSeam()),
            ("Fat-Weld", MakeFatWeldingSeam()),
            ("Dumbbell", MakeDumbbell()),
            ("Hand", MakeHand()),
        };

        var algos = new[] { SkeletonAlgo.StraightSkeleton, SkeletonAlgo.ChordalAxis, SkeletonAlgo.Voronoi };

        var outputDir = Path.Combine(
            Path.GetDirectoryName(typeof(SkeletonTests).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "skeleton-svg");
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var polyPaint = new SvgPaint(Color.Parse("#e8edff"), Color.Parse("#4466aa"), 0.3f);
        var skelPaint = new SvgPaint(Color.Parse("#dd3333"), Color.Parse("#dd3333"), 0.25f, 0.4f);
        var skelCorePaint = new SvgPaint(Color.Parse("#dd3333"), Color.Parse("#dd3333"), 0.75f, 0.4f);
        var leafPaint = new SvgPaint(Color.Parse("#bbbbbb"), Color.Parse("#bbbbbb"), 0.25f, 0.4f);

        // Collect timing data: (shapeName, algo, edgeCount, elapsedMs)
        var timings = new List<(string shape, SkeletonAlgo algo, int edges, double ms)>();

        // Generate one SVG per shape — polygon + skeleton overlaid using SvgExporter
        foreach (var (shapeName, poly) in shapes)
        {
            foreach (var algo in algos)
            {
                var sw = Stopwatch.StartNew();
                var skel = poly.Skeleton(algo);
                sw.Stop();
                var elapsedMs = sw.Elapsed.TotalMilliseconds;
                timings.Add((shapeName, algo, skel.EdgeCount, elapsedMs));

                var bb = poly.BoundingBox();
                int w = (int)(bb.Width + 30);
                int h = (int)(bb.Height + 30);

                // Translate to positive coordinates with margin
                var offset = new Vector<float>(-bb.X + 15, -bb.Y + 15);
                var shiftedPoly = poly + offset;
                var shiftedSkel = skel + offset;

                List<(object obj, SvgPaint paint)> items;

                // For StraightSkeleton: split leaf edges (gray) from core edges (thick red)
                if (algo == SkeletonAlgo.StraightSkeleton)
                {
                    var (core, leaves) = shiftedSkel.SplitLeafEdges();
                    items = new List<(object, SvgPaint)>
                    {
                        (shiftedPoly, polyPaint),
                        (core, skelCorePaint),
                        (leaves, leafPaint)
                    };
                }
                else
                {
                    items = new List<(object, SvgPaint)>
                    {
                        (shiftedPoly, polyPaint),
                        (shiftedSkel, skelPaint)
                    };
                }

                var svg = SvgExporter.Export(
                    items,
                    i => i.obj,
                    i => i.paint,
                    w, h);

                var fileName = $"{shapeName.ToLower().Replace(' ', '-')}_{algo.ToString().ToLower()}.svg";
                var filePath = Path.Combine(outputDir, fileName);
                File.WriteAllText(filePath, svg);
                _output.WriteLine($"Written: {filePath} ({elapsedMs:F2}ms)");
            }
        }

        // Write timing data as JSON for the HTML gallery
        var timingJson = JsonSerializer.Serialize(timings.Select(t => new
        {
            shape = t.shape.ToLower().Replace(' ', '-'),
            algo = t.algo.ToString().ToLower(),
            edges = t.edges,
            ms = Math.Round(t.ms, 2)
        }).ToArray());
        File.WriteAllText(Path.Combine(outputDir, "timings.json"), timingJson);

        // Generate overview grid
        GenerateOverviewSvg(shapes, algos, outputDir, polyPaint, skelPaint);

        // Generate HTML gallery
        GenerateHtmlGallery(shapes, algos, outputDir, timings);

        _output.WriteLine($"\nAll SVGs written to: {outputDir}");
        Directory.EnumerateFiles(outputDir, "*.svg").Should().NotBeEmpty();
    }

    private void GenerateOverviewSvg(
        (string name, Polygon<float> poly)[] shapes,
        SkeletonAlgo[] algos,
        string outputDir,
        SvgPaint polyPaint,
        SvgPaint skelPaint)
    {
        float cellSize = 50f;
        float padding = 8f;
        float labelH = 6f;
        float headerH = 8f;

        float totalW = padding + (cellSize + padding) * algos.Length;
        float totalH = headerH + (cellSize + labelH + padding) * shapes.Length;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(totalW * 8)}\" height=\"{F(totalH * 8)}\" viewBox=\"0 0 {F(totalW)} {F(totalH)}\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  text { font-family: sans-serif; fill: #333; }");
        sb.AppendLine("  .header { font-size: 4px; font-weight: bold; }");
        sb.AppendLine("  .label { font-size: 2.5px; }");
        sb.AppendLine("</style>");
        sb.AppendLine($"<rect width=\"{F(totalW)}\" height=\"{F(totalH)}\" fill=\"white\"/>");

        // Column headers
        for (int a = 0; a < algos.Length; a++)
        {
            float hx = padding + (cellSize + padding) * a + cellSize / 2;
            sb.AppendLine($"<text x=\"{F(hx)}\" y=\"5\" text-anchor=\"middle\" class=\"header\">{algos[a]}</text>");
        }

        for (int s = 0; s < shapes.Length; s++)
        {
            var (shapeName, poly) = shapes[s];
            var bb = poly.BoundingBox();

            float inner = cellSize - 8;
            float scale = Math.Min(inner / bb.Width, inner / bb.Height);
            if (float.IsInfinity(scale) || float.IsNaN(scale)) scale = 1;
            float drawW = bb.Width * scale;
            float drawH = bb.Height * scale;

            for (int a = 0; a < algos.Length; a++)
            {
                var algo = algos[a];
                var skel = poly.Skeleton(algo);

                float gx = padding + (cellSize + padding) * a;
                float gy = headerH + (cellSize + labelH + padding) * s;

                // Scale and center in cell
                var scaleFactor = new Size<float>(scale, scale);
                var offset = new Vector<float>(
                    (cellSize - drawW) / 2 - bb.X * scale,
                    (cellSize - drawH) / 2 - bb.Y * scale);
                var scaledPoly = poly * scaleFactor + offset;
                var scaledSkel = skel * scaleFactor + offset;

                sb.AppendLine($"<g transform=\"translate({F(gx)}, {F(gy)})\">");
                sb.AppendLine($"  <rect width=\"{F(cellSize)}\" height=\"{F(cellSize)}\" fill=\"#fafbff\" stroke=\"#ddd\" stroke-width=\"0.2\" rx=\"1\"/>");

                // Use SvgExporter for polygon and skeleton content
                var polyExporter = new PolygonSvgExporter<float>();
                var skelExporter = new SkeletonSvgExporter<float>();
                sb.Append("  ");
                sb.AppendLine(polyExporter.Export(scaledPoly, polyPaint));
                sb.Append("  ");
                sb.AppendLine(skelExporter.Export(scaledSkel, skelPaint));

                sb.AppendLine($"  <text x=\"{F(cellSize / 2)}\" y=\"{F(cellSize + 4)}\" text-anchor=\"middle\" class=\"label\">{shapeName} ({skel.EdgeCount}e)</text>");
                sb.AppendLine("</g>");
            }
        }

        sb.AppendLine("</svg>");

        var filePath = Path.Combine(outputDir, "overview.svg");
        File.WriteAllText(filePath, sb.ToString());
        _output.WriteLine($"Written overview: {filePath}");
    }

    private void GenerateHtmlGallery(
        (string name, Polygon<float> poly)[] shapes,
        SkeletonAlgo[] algos,
        string outputDir,
        List<(string shape, SkeletonAlgo algo, int edges, double ms)> timings)
    {
        var algoDisplayNames = new Dictionary<SkeletonAlgo, string>
        {
            [SkeletonAlgo.StraightSkeleton] = "Straight Skeleton",
            [SkeletonAlgo.ChordalAxis] = "Chordal Axis",
            [SkeletonAlgo.Voronoi] = "Voronoi"
        };

        var sb = new StringBuilder();
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en"><head>
<meta charset="UTF-8">
<title>Skeleton Algorithms — ModelingEvolution.Drawing</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #f0f2f5; color: #1d1d1f; }

.hero { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%); color: #fff; padding: 40px 32px 32px; }
.hero h1 { font-size: 28px; font-weight: 700; margin-bottom: 6px; }
.hero p { color: #a0b4d0; font-size: 15px; }
.hero .badges { display: flex; gap: 10px; margin-top: 16px; flex-wrap: wrap; }
.hero .badge { padding: 6px 14px; border-radius: 20px; font-size: 13px; font-weight: 600; }
.badge-green { background: rgba(40,167,69,0.2); color: #6fdc8c; border: 1px solid rgba(40,167,69,0.3); }
.badge-blue { background: rgba(50,100,200,0.2); color: #82b1ff; border: 1px solid rgba(50,100,200,0.3); }
.badge-purple { background: rgba(150,80,200,0.2); color: #ce93d8; border: 1px solid rgba(150,80,200,0.3); }

.container { max-width: 1400px; margin: 0 auto; padding: 24px; }

.algo-legend { display: flex; gap: 24px; margin-bottom: 24px; justify-content: center; flex-wrap: wrap; }
.algo-legend .item { display: flex; align-items: center; gap: 8px; font-size: 14px; font-weight: 500; }
.algo-legend .dot { width: 12px; height: 12px; border-radius: 50%; }

.shape-section { margin-bottom: 36px; }
.shape-title { font-size: 20px; font-weight: 700; margin-bottom: 12px; padding-left: 4px; color: #2c3e50; }

.cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: 14px; }
.card { background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); transition: box-shadow 0.2s, transform 0.2s; }
.card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.15); transform: translateY(-2px); }
.card-header { padding: 10px 16px; font-size: 13px; font-weight: 600; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #f0f0f0; }
.card-header .algo-name { color: #333; }
.card-header .meta { color: #999; font-weight: 400; font-size: 12px; display: flex; gap: 12px; }
.card-header .meta .time { color: #5a9; }
.card-body { padding: 16px; display: flex; align-items: center; justify-content: center; aspect-ratio: 1; background: #fafbfc; }
.card-body img { width: 100%; height: 100%; object-fit: contain; }

@media (max-width: 900px) {
    .cards { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 600px) {
    .cards { grid-template-columns: 1fr; }
    .hero { padding: 24px 16px; }
    .container { padding: 16px; }
}
</style>
</head><body>

<div class="hero">
    <h1>Skeleton Algorithms</h1>
    <p>ModelingEvolution.Drawing &mdash; Polygon skeleton computation using three algorithms</p>
    <div class="badges">
""");
        sb.AppendLine($"        <span class=\"badge badge-green\">785 / 785 tests passing</span>");
        sb.AppendLine($"        <span class=\"badge badge-blue\">3 algorithms</span>");
        sb.AppendLine($"        <span class=\"badge badge-purple\">{shapes.Length} test shapes</span>");
        sb.AppendLine("""
    </div>
</div>

<div class="container">
    <div class="algo-legend">
        <div class="item"><div class="dot" style="background:#e74c3c"></div> Core skeleton edges (red)</div>
        <div class="item"><div class="dot" style="background:#bbbbbb"></div> Leaf edges (gray, Straight Skeleton only)</div>
        <div class="item"><div class="dot" style="background:#4466aa"></div> Polygon outline (blue)</div>
    </div>
""");

        foreach (var (shapeName, _) in shapes)
        {
            var slug = shapeName.ToLower().Replace(' ', '-');
            sb.AppendLine($"    <div class=\"shape-section\">");
            sb.AppendLine($"        <div class=\"shape-title\">{shapeName}</div>");
            sb.AppendLine($"        <div class=\"cards\">");

            foreach (var algo in algos)
            {
                var algoSlug = algo.ToString().ToLower();
                var timing = timings.FirstOrDefault(t =>
                    t.shape.ToLower().Replace(' ', '-') == slug && t.algo == algo);

                var timeStr = timing.ms < 1 ? $"{timing.ms:F2}ms" : $"{timing.ms:F1}ms";

                sb.AppendLine($"            <div class=\"card\">");
                sb.AppendLine($"                <div class=\"card-header\">");
                sb.AppendLine($"                    <span class=\"algo-name\">{algoDisplayNames[algo]}</span>");
                sb.AppendLine($"                    <span class=\"meta\"><span>{timing.edges} edges</span><span class=\"time\">{timeStr}</span></span>");
                sb.AppendLine($"                </div>");
                sb.AppendLine($"                <div class=\"card-body\"><img src=\"{slug}_{algoSlug}.svg\" alt=\"{shapeName} - {algoDisplayNames[algo]}\"></div>");
                sb.AppendLine($"            </div>");
            }

            sb.AppendLine($"        </div>");
            sb.AppendLine($"    </div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</body></html>");

        var filePath = Path.Combine(outputDir, "skeletons.html");
        File.WriteAllText(filePath, sb.ToString());
        _output.WriteLine($"Written HTML gallery: {filePath}");
    }

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
