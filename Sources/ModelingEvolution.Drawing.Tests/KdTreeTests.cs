using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class KdTreeTests
{
    [Fact]
    public void Build_EmptySpan_ReturnsEmptyTree()
    {
        var tree = KdTree<float>.Build(ReadOnlySpan<Point<float>>.Empty);
        tree.Count.Should().Be(0);
    }

    [Fact]
    public void Build_SinglePoint_CountIsOne()
    {
        var pts = new[] { new Point<float>(3, 4) };
        var tree = KdTree<float>.Build(pts);
        tree.Count.Should().Be(1);
    }

    [Fact]
    public void NearestNeighbour_SinglePoint_ReturnsThatPoint()
    {
        var p = new Point<float>(3, 4);
        var tree = KdTree<float>.Build(new[] { p });

        var (point, index, distSq) = tree.NearestNeighbour(new Point<float>(5, 6));
        point.Should().Be(p);
        index.Should().Be(0);
        distSq.Should().BeApproximately(8f, 1e-6f); // (5-3)^2 + (6-4)^2 = 8
    }

    [Fact]
    public void NearestNeighbour_EmptyTree_Throws()
    {
        var tree = KdTree<float>.Build(ReadOnlySpan<Point<float>>.Empty);
        var act = () => tree.NearestNeighbour(new Point<float>(0, 0));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NearestNeighbour_FindsClosestAmongMany()
    {
        var pts = new[]
        {
            new Point<float>(0, 0),
            new Point<float>(10, 10),
            new Point<float>(3, 4),
            new Point<float>(7, 1),
            new Point<float>(-5, 8),
        };
        var tree = KdTree<float>.Build(pts);

        // Query near (3, 4) — should return that exact point
        var (point, index, _) = tree.NearestNeighbour(new Point<float>(3.1f, 3.9f));
        point.Should().Be(pts[2]);
        index.Should().Be(2);
    }

    [Fact]
    public void NearestNeighbour_ExactMatch_ReturnsZeroDistance()
    {
        var pts = new[]
        {
            new Point<float>(1, 2),
            new Point<float>(3, 4),
            new Point<float>(5, 6),
        };
        var tree = KdTree<float>.Build(pts);

        var (point, _, distSq) = tree.NearestNeighbour(new Point<float>(3, 4));
        point.Should().Be(pts[1]);
        distSq.Should().Be(0f);
    }

    [Fact]
    public void NearestNeighbour_Grid_AllQueriesCorrect()
    {
        // Build a 10x10 grid of points
        var pts = new Point<float>[100];
        for (int y = 0; y < 10; y++)
        for (int x = 0; x < 10; x++)
            pts[y * 10 + x] = new Point<float>(x, y);

        var tree = KdTree<float>.Build(pts);
        tree.Count.Should().Be(100);

        // Query points near grid intersections — should find the nearest grid point
        var (p1, _, _) = tree.NearestNeighbour(new Point<float>(2.3f, 4.1f));
        p1.Should().Be(new Point<float>(2, 4));

        var (p2, _, _) = tree.NearestNeighbour(new Point<float>(7.8f, 0.2f));
        p2.Should().Be(new Point<float>(8, 0));

        var (p3, _, _) = tree.NearestNeighbour(new Point<float>(9.9f, 9.9f));
        p3.Should().Be(new Point<float>(9, 9)); // should be (9, 9) as it's closest, regardless of rounding
    }

    [Fact]
    public void GetNearestNeighbours_ReturnsKClosest()
    {
        var pts = new[]
        {
            new Point<float>(0, 0),   // 0
            new Point<float>(1, 0),   // 1
            new Point<float>(2, 0),   // 2
            new Point<float>(10, 0),  // 3
            new Point<float>(20, 0),  // 4
        };
        var tree = KdTree<float>.Build(pts);

        var results = tree.GetNearestNeighbours(new Point<float>(0.5f, 0), 3);
        results.Length.Should().Be(3);
        var span = results.Span;

        // Should be ordered by distance: closest first
        span[0].DistanceSquared.Should().BeLessThanOrEqualTo(span[1].DistanceSquared);
        span[1].DistanceSquared.Should().BeLessThanOrEqualTo(span[2].DistanceSquared);

        // The 3 closest are indices 0, 1, 2
        var indices = results.ToArray().Select(r => r.Index).OrderBy(i => i).ToArray();
        indices.Should().BeEquivalentTo(new[] { 0, 1, 2 });
    }

    [Fact]
    public void GetNearestNeighbours_KGreaterThanCount_ReturnsAll()
    {
        var pts = new[]
        {
            new Point<float>(1, 2),
            new Point<float>(3, 4),
        };
        var tree = KdTree<float>.Build(pts);

        var results = tree.GetNearestNeighbours(new Point<float>(0, 0), 10);
        results.Length.Should().Be(2);
    }

    [Fact]
    public void GetNearestNeighbours_K0_ReturnsEmpty()
    {
        var pts = new[] { new Point<float>(1, 2) };
        var tree = KdTree<float>.Build(pts);

        var results = tree.GetNearestNeighbours(new Point<float>(0, 0), 0);
        results.Length.Should().Be(0);
    }

    [Fact]
    public void GetNearestNeighbours_OrderedByDistance()
    {
        var pts = new Point<float>[50];
        var rng = new Random(42);
        for (int i = 0; i < 50; i++)
            pts[i] = new Point<float>((float)(rng.NextDouble() * 100), (float)(rng.NextDouble() * 100));

        var tree = KdTree<float>.Build(pts);
        var query = new Point<float>(50, 50);
        var results = tree.GetNearestNeighbours(query, 10);

        results.Length.Should().Be(10);
        var span = results.Span;
        for (int i = 1; i < span.Length; i++)
            span[i].DistanceSquared.Should().BeGreaterThanOrEqualTo(span[i - 1].DistanceSquared);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var tree = new KdTree<float>();
        tree.Count.Should().Be(0);

        tree.Add(new Point<float>(1, 2), 0);
        tree.Count.Should().Be(1);

        tree.Add(new Point<float>(3, 4), 1);
        tree.Count.Should().Be(2);
    }

    [Fact]
    public void Add_ThenQuery_FindsAddedPoints()
    {
        var tree = new KdTree<float>();
        tree.Add(new Point<float>(0, 0), 0);
        tree.Add(new Point<float>(10, 10), 1);
        tree.Add(new Point<float>(5, 5), 2);

        var (_, index, _) = tree.NearestNeighbour(new Point<float>(4, 6));
        index.Should().Be(2);
    }

    [Fact]
    public void NearestNeighbour_MatchesBruteForce()
    {
        // Verify KD-tree results match brute-force for random data
        var rng = new Random(123);
        var pts = new Point<float>[200];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = new Point<float>((float)(rng.NextDouble() * 1000), (float)(rng.NextDouble() * 1000));

        var tree = KdTree<float>.Build(pts);

        for (int q = 0; q < 50; q++)
        {
            var query = new Point<float>((float)(rng.NextDouble() * 1000), (float)(rng.NextDouble() * 1000));

            // KD-tree result
            var (_, kdIdx, kdDist) = tree.NearestNeighbour(query);

            // Brute-force result
            int bfIdx = 0;
            float bfDist = float.MaxValue;
            for (int i = 0; i < pts.Length; i++)
            {
                var dx = query.X - pts[i].X;
                var dy = query.Y - pts[i].Y;
                var d = dx * dx + dy * dy;
                if (d < bfDist) { bfDist = d; bfIdx = i; }
            }

            kdIdx.Should().Be(bfIdx, $"query {q}: ({query.X}, {query.Y})");
            kdDist.Should().BeApproximately(bfDist, 1e-3f);
        }
    }

    [Fact]
    public void GetNearestNeighbours_MatchesBruteForce()
    {
        var rng = new Random(456);
        var pts = new Point<float>[100];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = new Point<float>((float)(rng.NextDouble() * 500), (float)(rng.NextDouble() * 500));

        var tree = KdTree<float>.Build(pts);
        int k = 5;

        for (int q = 0; q < 20; q++)
        {
            var query = new Point<float>((float)(rng.NextDouble() * 500), (float)(rng.NextDouble() * 500));

            // KD-tree result
            var kdResults = tree.GetNearestNeighbours(query, k);

            // Brute-force: sort all by distance
            var bruteForce = pts
                .Select((p, i) => (Index: i, DistSq: (query.X - p.X) * (query.X - p.X) + (query.Y - p.Y) * (query.Y - p.Y)))
                .OrderBy(x => x.DistSq)
                .Take(k)
                .ToArray();

            kdResults.Length.Should().Be(k);
            var kdSpan = kdResults.Span;
            for (int i = 0; i < k; i++)
            {
                kdSpan[i].Index.Should().Be(bruteForce[i].Index, $"query {q}, rank {i}");
                kdSpan[i].DistanceSquared.Should().BeApproximately(bruteForce[i].DistSq, 1e-3f);
            }
        }
    }

    [Fact]
    public void Build_WithDoubles_Works()
    {
        var pts = new[]
        {
            new Point<double>(1.5, 2.5),
            new Point<double>(3.7, 4.2),
            new Point<double>(0.1, 8.9),
        };
        var tree = KdTree<double>.Build(pts);
        tree.Count.Should().Be(3);

        var (point, index, _) = tree.NearestNeighbour(new Point<double>(3.6, 4.3));
        point.Should().Be(pts[1]);
        index.Should().Be(1);
    }

    [Fact]
    public void NearestNeighbour_DuplicatePoints()
    {
        var pts = new[]
        {
            new Point<float>(5, 5),
            new Point<float>(5, 5),
            new Point<float>(5, 5),
            new Point<float>(10, 10),
        };
        var tree = KdTree<float>.Build(pts);
        tree.Count.Should().Be(4);

        var (point, _, distSq) = tree.NearestNeighbour(new Point<float>(5, 5));
        point.Should().Be(new Point<float>(5, 5));
        distSq.Should().Be(0f);
    }

    [Fact]
    public void NearestNeighbour_CollinearPoints()
    {
        // All points on a line — tests that the tree handles degenerate splits
        var pts = new Point<float>[20];
        for (int i = 0; i < 20; i++)
            pts[i] = new Point<float>(i * 2f, 0);

        var tree = KdTree<float>.Build(pts);

        var (_, index, _) = tree.NearestNeighbour(new Point<float>(7f, 0.1f));
        // Nearest should be (6, 0) at index 3 or (8, 0) at index 4
        // 7 is equidistant from 6 and 8 on X, but 6 is at dist sqrt(1+0.01)=1.005, 8 at sqrt(1+0.01)=1.005
        // Both are equidistant, either is acceptable
        (index == 3 || index == 4).Should().BeTrue();
    }

    [Fact]
    public void NearestNeighbour_LargeDataset()
    {
        // Stress test with 10000 points
        var rng = new Random(789);
        var pts = new Point<float>[10000];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = new Point<float>((float)(rng.NextDouble() * 10000), (float)(rng.NextDouble() * 10000));

        var tree = KdTree<float>.Build(pts);
        tree.Count.Should().Be(10000);

        // Verify a few random queries against brute force
        for (int q = 0; q < 10; q++)
        {
            var query = new Point<float>((float)(rng.NextDouble() * 10000), (float)(rng.NextDouble() * 10000));
            var (_, kdIdx, _) = tree.NearestNeighbour(query);

            // Brute-force
            int bfIdx = 0;
            float bfDist = float.MaxValue;
            for (int i = 0; i < pts.Length; i++)
            {
                var dx = query.X - pts[i].X;
                var dy = query.Y - pts[i].Y;
                var d = dx * dx + dy * dy;
                if (d < bfDist) { bfDist = d; bfIdx = i; }
            }

            kdIdx.Should().Be(bfIdx);
        }
    }
}
