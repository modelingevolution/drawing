using System.Diagnostics;
using ModelingEvolution.Drawing;

int[] pointCounts = [100, 500, 1000, 5000, 10000];
const int warmup = 5;
const long durationMs = 5000;

Console.WriteLine($"{"Points",8} {"Heap ms",10} {"Pooled ms",10} {"Ratio",8} {"Heap N",8} {"Pool N",8} {"Heap GC",8} {"Pool GC",8}");
Console.WriteLine(new string('-', 84));

foreach (var n in pointCounts)
{
    var polygon = CreateRegularPolygon(n);

    // warmup
    for (int i = 0; i < warmup; i++)
    {
        polygon.Skeleton();
        using var s = AllocationScope.Begin();
        polygon.Skeleton();
    }

    // Heap — run for 5 seconds
    var gc0Before = GC.CollectionCount(0);
    int heapCount = 0;
    var sw = Stopwatch.StartNew();
    while (sw.ElapsedMilliseconds < durationMs)
    {
        polygon.Skeleton();
        heapCount++;
    }
    sw.Stop();
    var heapMs = sw.Elapsed.TotalMilliseconds / heapCount;
    var heapGc = GC.CollectionCount(0) - gc0Before;

    // Pooled — run for 5 seconds
    gc0Before = GC.CollectionCount(0);
    int poolCount = 0;
    sw.Restart();
    while (sw.ElapsedMilliseconds < durationMs)
    {
        using var scope = AllocationScope.Begin();
        polygon.Skeleton();
        poolCount++;
    }
    sw.Stop();
    var poolMs = sw.Elapsed.TotalMilliseconds / poolCount;
    var poolGc = GC.CollectionCount(0) - gc0Before;

    var ratio = heapMs / poolMs;
    Console.WriteLine($"{n,8} {heapMs,10:F2} {poolMs,10:F2} {ratio,8:F2}x {heapCount,8} {poolCount,8} {heapGc,8} {poolGc,8}");
}

static Polygon<double> CreateRegularPolygon(int n)
{
    var points = new Point<double>[n];
    var step = 2.0 * Math.PI / n;
    const double radius = 100.0;
    for (int i = 0; i < n; i++)
    {
        var angle = i * step;
        points[i] = new Point<double>(radius * Math.Cos(angle), radius * Math.Sin(angle));
    }
    return new Polygon<double>(points);
}
