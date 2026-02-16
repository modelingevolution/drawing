using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Computes a skeleton via the Voronoi diagram approach:
/// 1. Densify polygon boundary (sample points along edges)
/// 2. Compute Delaunay triangulation of all sample points
/// 3. Compute Voronoi dual (connect circumcenters of adjacent triangles)
/// 4. Filter: keep only edges whose endpoints are inside the polygon
/// 5. Multi-pass iterative pruning of short dangling branches
/// </summary>
internal static class VoronoiSkeletonAlgorithm
{
    internal static Skeleton<T> Compute<T>(in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        var span = polygon.AsSpan();
        int n = span.Length;
        if (n < 3) return new Skeleton<T>(ReadOnlyMemory<Point<T>>.Empty, ReadOnlyMemory<Segment<T>>.Empty);

        // Step 1: Densify boundary
        var samples = DensifyBoundary(span);

        // Step 2: Delaunay triangulation of samples
        var dt = DelaunayTriangulation<T>.Create(samples.Span);

        // Step 3: Voronoi dual edges
        var voronoiEdges = dt.VoronoiEdges();

        // Step 4: Filter — keep only edges with both endpoints inside polygon
        var poly = polygon;
        var eps = T.CreateTruncating(1e-6);
        var filteredEdges = new PooledList<Segment<T>>(n * 2);
        foreach (var edge in voronoiEdges)
        {
            if (poly.Contains(edge.Start) && poly.Contains(edge.End))
            {
                // Skip degenerate edges
                var dx = edge.End.X - edge.Start.X;
                var dy = edge.End.Y - edge.Start.Y;
                if (T.Abs(dx) > eps || T.Abs(dy) > eps)
                    filteredEdges.Add(edge);
            }
        }

        // Step 5: Compute distance-to-boundary threshold from polygon characteristic size
        // Use perimeter / (4 * number_of_edges) as the base pruning threshold.
        // This means branches shorter than ~1/4 of an average polygon edge are noise.
        T perimeter = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var next = span[(i + 1) % n];
            var dx2 = next.X - span[i].X;
            var dy2 = next.Y - span[i].Y;
            perimeter += T.Sqrt(dx2 * dx2 + dy2 * dy2);
        }
        var pruneThreshold = perimeter / T.CreateTruncating(2 * n);

        // Step 6: Multi-pass iterative pruning of dangling branches
        var prunedEdges = IterativePruneDangling(filteredEdges, pruneThreshold);

        // Collect unique nodes
        var nodeSet = new HashSet<(double, double)>();
        var nodeList = new PooledList<Point<T>>(n);
        var prunedSpan = prunedEdges.AsSpan();
        for (int i = 0; i < prunedSpan.Length; i++)
        {
            AddNode(nodeSet, ref nodeList, prunedSpan[i].Start);
            AddNode(nodeSet, ref nodeList, prunedSpan[i].End);
        }

        return new Skeleton<T>(nodeList.ToReadOnlyMemory(), prunedEdges.ToReadOnlyMemory());
    }

    private static ReadOnlyMemory<Point<T>> DensifyBoundary<T>(ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        var five = T.CreateTruncating(5);

        // Compute average edge length
        T totalLen = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var next = vertices[(i + 1) % n];
            var dx = next.X - vertices[i].X;
            var dy = next.Y - vertices[i].Y;
            totalLen += T.Sqrt(dx * dx + dy * dy);
        }
        var avgLen = totalLen / T.CreateTruncating(n);
        var spacing = avgLen / five;
        if (spacing <= T.CreateTruncating(1e-10))
            spacing = T.CreateTruncating(0.01);

        var samples = new PooledList<Point<T>>(n * 5);
        for (int i = 0; i < n; i++)
        {
            var start = vertices[i];
            var end = vertices[(i + 1) % n];
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var edgeLen = T.Sqrt(dx * dx + dy * dy);

            int steps = int.Max(1, Convert.ToInt32(edgeLen / spacing));
            for (int s = 0; s < steps; s++)
            {
                var t = T.CreateTruncating(s) / T.CreateTruncating(steps);
                samples.Add(new Point<T>(start.X + dx * t, start.Y + dy * t));
            }
        }

        return samples.ToReadOnlyMemory();
    }

    /// <summary>
    /// Prunes short dangling branches (leaf-to-junction chains) whose total length
    /// is below the threshold. Repeats until stable, since pruning a branch can
    /// expose new leaf nodes.
    /// </summary>
    private static PooledList<Segment<T>> IterativePruneDangling<T>(PooledList<Segment<T>> edges, T threshold)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (edges.Count == 0) return edges;

        var current = edges;
        int maxPasses = 50;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            var curSpan = current.AsSpan();

            // Build adjacency: node → list of edge indices
            var adj = new Dictionary<(double, double), List<int>>();
            for (int i = 0; i < curSpan.Length; i++)
            {
                var sk = RoundKey(curSpan[i].Start);
                var ek = RoundKey(curSpan[i].End);
                if (!adj.TryGetValue(sk, out var ls)) { ls = new List<int>(); adj[sk] = ls; }
                ls.Add(i);
                if (!adj.TryGetValue(ek, out var le)) { le = new List<int>(); adj[ek] = le; }
                le.Add(i);
            }

            // Find leaf nodes (degree 1)
            var leafNodes = new List<(double, double)>();
            foreach (var (node, edgeIdxs) in adj)
            {
                if (edgeIdxs.Count == 1)
                    leafNodes.Add(node);
            }

            if (leafNodes.Count == 0) break;

            // For each leaf, trace the branch (chain of degree-2 nodes) until a junction (degree >= 3)
            // and compute total branch length
            var edgesToRemove = new HashSet<int>();

            foreach (var leaf in leafNodes)
            {
                T branchLen = T.Zero;
                var branchEdges = new List<int>();
                var currentNode = leaf;
                var visited = new HashSet<(double, double)> { currentNode };

                while (true)
                {
                    if (!adj.TryGetValue(currentNode, out var nodeEdges)) break;

                    // Find the unvisited edge from currentNode
                    int nextEdgeIdx = -1;
                    foreach (var ei in nodeEdges)
                    {
                        if (!edgesToRemove.Contains(ei) && !branchEdges.Contains(ei))
                        {
                            nextEdgeIdx = ei;
                            break;
                        }
                    }

                    if (nextEdgeIdx < 0) break;

                    branchEdges.Add(nextEdgeIdx);
                    branchLen += curSpan[nextEdgeIdx].Length;

                    // Find the other end of this edge
                    var sk = RoundKey(curSpan[nextEdgeIdx].Start);
                    var ek = RoundKey(curSpan[nextEdgeIdx].End);
                    var otherNode = sk == currentNode ? ek : sk;

                    if (visited.Contains(otherNode)) break; // cycle
                    visited.Add(otherNode);

                    // If other node is a junction (degree >= 3) or another leaf, stop
                    if (!adj.TryGetValue(otherNode, out var otherEdges) || otherEdges.Count != 2)
                        break;

                    currentNode = otherNode;
                }

                // Prune entire branch if its total length < threshold
                if (branchLen < threshold)
                {
                    foreach (var ei in branchEdges)
                        edgesToRemove.Add(ei);
                }
            }

            if (edgesToRemove.Count == 0) break;

            var next = new PooledList<Segment<T>>(curSpan.Length - edgesToRemove.Count);
            for (int i = 0; i < curSpan.Length; i++)
            {
                if (!edgesToRemove.Contains(i))
                    next.Add(curSpan[i]);
            }

            current.Dispose();
            current = next;
        }

        return current;
    }

    private static (double, double) RoundKey<T>(Point<T> pt)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        return (Math.Round(Convert.ToDouble(pt.X), 7), Math.Round(Convert.ToDouble(pt.Y), 7));
    }

    private static void AddNode<T>(HashSet<(double, double)> nodeSet, ref PooledList<Point<T>> nodeList, Point<T> pt)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var key = (Math.Round(Convert.ToDouble(pt.X), 7), Math.Round(Convert.ToDouble(pt.Y), 7));
        if (nodeSet.Add(key))
            nodeList.Add(pt);
    }
}
