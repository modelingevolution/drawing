using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Benchmarks;

/// <summary>
/// Measures the allocation overhead in a skeleton computation hotpath.
///
/// This is NOT a benchmark of the skeleton algorithm — it measures the
/// throughput difference between heap allocation (GC pressure) and pooled
/// allocation (AllocationScope) when the same operation runs in a tight loop.
///
/// Design:
/// - Each benchmark invocation runs a batch of skeleton computations in a tight loop.
/// - OperationsPerInvoke tells BDN how many logical ops each invocation represents.
/// - GcForce=false prevents BDN from cleaning up between invocations, so garbage
///   accumulates and GC fires naturally — just like a real hotpath.
/// - GcServer=false uses workstation GC for more frequent collections.
/// - The MemoryDiagnoser reports Gen0/Gen1/Gen2 collections and bytes allocated.
///
/// What to look for in the results:
/// - "Allocated" column: heap version allocates per-op, pooled version near-zero
/// - "Gen0" column: heap version triggers GC, pooled version doesn't
/// - "Mean" column: pooled version is faster due to less GC pause time
/// - "Skel/s" column: skeletons per second — the metric we care about
/// </summary>
[MemoryDiagnoser]
[Config(typeof(Config))]
public class AllocationScopeBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithGcServer(false)
                .WithGcForce(false)
                .WithGcConcurrent(false)
                .WithStrategy(RunStrategy.Monitoring)
                .WithWarmupCount(3)
                .WithIterationCount(10)
            );

            AddColumn(new SkeletonsPerSecondColumn());
        }
    }

    /// <summary>
    /// Custom column: Skeletons/Second = 1_000_000_000 / Mean(ns).
    /// </summary>
    private class SkeletonsPerSecondColumn : IColumn
    {
        public string Id => "Skel/s";
        public string ColumnName => "Skel/s";
        public string Legend => "Skeletons per second (1e9 / Mean_ns)";
        public UnitType UnitType => UnitType.Dimensionless;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
            GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            var report = summary[benchmarkCase];
            if (report?.ResultStatistics == null) return "N/A";

            var meanNs = report.ResultStatistics.Mean;
            if (meanNs <= 0) return "N/A";
            return (1_000_000_000.0 / meanNs).ToString("N1");
        }

        public override string ToString() => ColumnName;
    }

    // ─── Parameters ────────────────────────────────────────────

    [Params(100, 500, 1000, 5000, 10000)]
    public int PointCount { get; set; }

    /// <summary>
    /// Number of skeleton computations per benchmark invocation.
    /// With GcForce=false garbage accumulates across invocations, so even
    /// moderate batch sizes build real GC pressure over the measurement window.
    /// Kept small enough that large polygons (10k pts) don't exceed BDN timeouts.
    /// </summary>
    private const int Ops = 20;

    private Polygon<double> _polygon;

    [GlobalSetup]
    public void Setup()
    {
        _polygon = CreateRegularPolygon(PointCount);

        // Warmup: verify skeleton works and pre-JIT
        _polygon.Skeleton();
    }

    // ─── Benchmarks ────────────────────────────────────────────

    /// <summary>
    /// Baseline: no AllocationScope. Every skeleton computation heap-allocates
    /// all intermediate and result arrays. Garbage accumulates, GC fires mid-loop,
    /// pause time degrades throughput.
    /// </summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
    public int Heap()
    {
        int sum = 0;
        for (int i = 0; i < Ops; i++)
        {
            var skeleton = _polygon.Skeleton();
            sum += skeleton.EdgeCount; // prevent dead code elimination
        }
        return sum;
    }

    /// <summary>
    /// With AllocationScope: each iteration allocates from MemoryPool, then
    /// Dispose() returns all memory to the pool in one shot. No garbage is
    /// created, GC doesn't fire, throughput is sustained.
    /// </summary>
    [Benchmark(OperationsPerInvoke = Ops)]
    public int Pooled()
    {
        int sum = 0;
        for (int i = 0; i < Ops; i++)
        {
            using var scope = AllocationScope.Begin();
            var skeleton = _polygon.Skeleton();
            sum += skeleton.EdgeCount;
        }
        return sum;
    }

    // ─── Helpers ───────────────────────────────────────────────

    /// <summary>
    /// Creates a regular polygon (circle approximation) with n vertices.
    /// Consistent, non-degenerate geometry for reproducible benchmarks.
    /// </summary>
    private static Polygon<double> CreateRegularPolygon(int n)
    {
        var points = new Point<double>[n];
        var step = 2.0 * Math.PI / n;
        const double radius = 100.0;
        for (int i = 0; i < n; i++)
        {
            var angle = i * step;
            points[i] = new Point<double>(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle));
        }
        return new Polygon<double>(points);
    }
}
