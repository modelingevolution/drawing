using System.Diagnostics;
using System.Text;
using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class SkeletonPerformanceTests
{
    // ─────────────────────────────────────────────
    // Polygon generators
    // ─────────────────────────────────────────────

    private static Polygon<float> MakeSquare(float size) => new(
        new Point<float>(0, 0),
        new Point<float>(size, 0),
        new Point<float>(size, size),
        new Point<float>(0, size));

    private static Polygon<float> MakeRectangle(float w, float h) => new(
        new Point<float>(0, 0),
        new Point<float>(w, 0),
        new Point<float>(w, h),
        new Point<float>(0, h));

    private static Polygon<float> MakeLShape() => new(
        new Point<float>(0, 0),
        new Point<float>(20, 0),
        new Point<float>(20, 10),
        new Point<float>(10, 10),
        new Point<float>(10, 20),
        new Point<float>(0, 20));

    private static Polygon<float> MakeRegularPolygon(int n, float radius)
    {
        var pts = new Point<float>[n];
        for (int i = 0; i < n; i++)
        {
            var angle = 2.0 * Math.PI * i / n;
            pts[i] = new Point<float>(
                (float)(radius * Math.Cos(angle)),
                (float)(radius * Math.Sin(angle)));
        }
        return new Polygon<float>(pts);
    }

    private static Polygon<float> MakeStar(int points, float outerR, float innerR)
    {
        var pts = new Point<float>[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            var angle = Math.PI * i / points;
            var r = i % 2 == 0 ? outerR : innerR;
            pts[i] = new Point<float>(
                (float)(r * Math.Cos(angle)),
                (float)(r * Math.Sin(angle)));
        }
        return new Polygon<float>(pts);
    }

    // ─────────────────────────────────────────────
    // Benchmark runner
    // ─────────────────────────────────────────────

    private record BenchmarkResult(
        string Algorithm,
        string Shape,
        int Vertices,
        int Iterations,
        double TotalMs,
        double AvgMs,
        double MinMs,
        double MaxMs,
        int EdgeCount,
        int NodeCount);

    private static BenchmarkResult RunBenchmark(
        string algoName, SkeletonAlgo algo, string shapeName,
        Polygon<float> polygon, int warmup, int iterations)
    {
        // Warmup
        for (int i = 0; i < warmup; i++)
            polygon.Skeleton(algo);

        var times = new double[iterations];
        int edgeCount = 0, nodeCount = 0;
        var sw = new Stopwatch();

        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var skel = polygon.Skeleton(algo);
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
            edgeCount = skel.EdgeCount;
            nodeCount = skel.NodeCount;
        }

        Array.Sort(times);
        return new BenchmarkResult(
            algoName, shapeName,
            polygon.Count, iterations,
            times.Sum(),
            times.Average(),
            times[0],
            times[^1],
            edgeCount, nodeCount);
    }

    // ─────────────────────────────────────────────
    // Performance test — writes results to markdown
    // ─────────────────────────────────────────────

    [Fact]
    public void SkeletonPerformance_AllAlgos_AllShapes()
    {
        const int warmup = 3;
        const int iterations = 20;

        var shapes = new (string name, Polygon<float> poly)[]
        {
            ("Square (4v)", MakeSquare(10)),
            ("Rectangle (4v)", MakeRectangle(20, 10)),
            ("L-Shape (6v)", MakeLShape()),
            ("Octagon (8v)", MakeRegularPolygon(8, 10)),
            ("16-gon (16v)", MakeRegularPolygon(16, 10)),
            ("32-gon (32v)", MakeRegularPolygon(32, 10)),
            ("64-gon (64v)", MakeRegularPolygon(64, 10)),
            ("Star-5 (10v)", MakeStar(5, 10, 4)),
            ("Star-8 (16v)", MakeStar(8, 10, 4)),
        };

        var algos = new (string name, SkeletonAlgo algo)[]
        {
            ("StraightSkeleton", SkeletonAlgo.StraightSkeleton),
            ("ChordalAxis", SkeletonAlgo.ChordalAxis),
            ("Voronoi", SkeletonAlgo.Voronoi),
        };

        var results = new List<BenchmarkResult>();

        foreach (var (algoName, algo) in algos)
        {
            foreach (var (shapeName, poly) in shapes)
            {
                var result = RunBenchmark(algoName, algo, shapeName, poly, warmup, iterations);
                results.Add(result);
            }
        }

        // Build markdown report
        var sb = new StringBuilder();
        sb.AppendLine("# Polygon Skeleton Performance Results");
        sb.AppendLine();
        sb.AppendLine($"- **Date**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Runtime**: .NET {Environment.Version}");
        sb.AppendLine($"- **OS**: {Environment.OSVersion}");
        sb.AppendLine($"- **Warmup iterations**: {warmup}");
        sb.AppendLine($"- **Measured iterations**: {iterations}");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Algorithm | Shape | Vertices | Avg (ms) | Min (ms) | Max (ms) | Edges | Nodes |");
        sb.AppendLine("|-----------|-------|----------|----------|----------|----------|-------|-------|");
        foreach (var r in results)
        {
            sb.AppendLine($"| {r.Algorithm} | {r.Shape} | {r.Vertices} | {r.AvgMs:F3} | {r.MinMs:F3} | {r.MaxMs:F3} | {r.EdgeCount} | {r.NodeCount} |");
        }
        sb.AppendLine();

        // Per-algorithm breakdown
        foreach (var algoGroup in results.GroupBy(r => r.Algorithm))
        {
            sb.AppendLine($"## {algoGroup.Key}");
            sb.AppendLine();
            sb.AppendLine("| Shape | Vertices | Avg (ms) | Min (ms) | Max (ms) | Total (ms) | Edges | Nodes |");
            sb.AppendLine("|-------|----------|----------|----------|----------|------------|-------|-------|");
            foreach (var r in algoGroup)
            {
                sb.AppendLine($"| {r.Shape} | {r.Vertices} | {r.AvgMs:F3} | {r.MinMs:F3} | {r.MaxMs:F3} | {r.TotalMs:F1} | {r.EdgeCount} | {r.NodeCount} |");
            }
            sb.AppendLine();
        }

        // Scaling analysis
        sb.AppendLine("## Scaling Analysis (Regular Polygons)");
        sb.AppendLine();
        sb.AppendLine("| Algorithm | 8v Avg (ms) | 16v Avg (ms) | 32v Avg (ms) | 64v Avg (ms) | 8→64 Ratio |");
        sb.AppendLine("|-----------|-------------|--------------|--------------|--------------|------------|");
        foreach (var algoGroup in results.GroupBy(r => r.Algorithm))
        {
            var oct = algoGroup.FirstOrDefault(r => r.Shape.Contains("8v") && r.Shape.Contains("gon"));
            var p16 = algoGroup.FirstOrDefault(r => r.Shape.Contains("16v") && r.Shape.Contains("gon"));
            var p32 = algoGroup.FirstOrDefault(r => r.Shape.Contains("32v"));
            var p64 = algoGroup.FirstOrDefault(r => r.Shape.Contains("64v"));

            if (oct != null && p64 != null)
            {
                var ratio = p64.AvgMs / Math.Max(oct.AvgMs, 0.001);
                sb.AppendLine($"| {algoGroup.Key} | {oct.AvgMs:F3} | {p16?.AvgMs:F3} | {p32?.AvgMs:F3} | {p64.AvgMs:F3} | {ratio:F1}x |");
            }
        }
        sb.AppendLine();

        var report = sb.ToString();

        // Write to file
        var outputPath = Path.Combine(
            Path.GetDirectoryName(typeof(SkeletonPerformanceTests).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "skeleton-performance-results.md");
        outputPath = Path.GetFullPath(outputPath);

        File.WriteAllText(outputPath, report);

        // Basic validation — all algorithms should produce results
        results.Should().AllSatisfy(r => r.EdgeCount.Should().BeGreaterThan(0));
    }
}
