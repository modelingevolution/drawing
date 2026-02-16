using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Specifies which algorithm to use for skeleton computation.
/// </summary>
public enum SkeletonAlgo
{
    /// <summary>
    /// Uses the straight skeleton algorithm (simplified Felkel-Obdrzalek wavefront shrinking).
    /// </summary>
    StraightSkeleton,

    /// <summary>
    /// Uses the Chordal Axis Transform based on a Constrained Delaunay Triangulation.
    /// </summary>
    ChordalAxis,

    /// <summary>
    /// Uses the Voronoi diagram approach with boundary densification and edge filtering.
    /// </summary>
    Voronoi
}

/// <summary>
/// Represents the skeleton (medial axis approximation) of a polygon, consisting of
/// interior nodes and edges forming a graph.
/// </summary>
[JsonConverter(typeof(SkeletonJsonConverterFactory))]
[ProtoContract]
[Svg.SvgExporter(typeof(SkeletonSvgExporterFactory))]
public readonly record struct Skeleton<T> : IPoolable<Skeleton<T>, Lease<Point<T>, Segment<T>>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly ReadOnlyMemory<Point<T>> _nodes;
    internal readonly ReadOnlyMemory<Segment<T>> _edges;

    [ProtoMember(1)]
    private Point<T>[] ProtoNodes
    {
        get
        {
            if (_nodes.Length == 0) return Array.Empty<Point<T>>();
            if (MemoryMarshal.TryGetArray(_nodes, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _nodes.ToArray();
        }
        init => _nodes = value ?? ReadOnlyMemory<Point<T>>.Empty;
    }

    [ProtoMember(2)]
    private Segment<T>[] ProtoEdges
    {
        get
        {
            if (_edges.Length == 0) return Array.Empty<Segment<T>>();
            if (MemoryMarshal.TryGetArray(_edges, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _edges.ToArray();
        }
        init => _edges = value ?? ReadOnlyMemory<Segment<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new skeleton from the given nodes and edges arrays.
    /// </summary>
    public Skeleton(Point<T>[] nodes, Segment<T>[] edges)
    {
        _nodes = nodes ?? ReadOnlyMemory<Point<T>>.Empty;
        _edges = edges ?? ReadOnlyMemory<Segment<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new skeleton from Memory-backed nodes and edges. Zero-copy.
    /// </summary>
    public Skeleton(ReadOnlyMemory<Point<T>> nodes, ReadOnlyMemory<Segment<T>> edges)
    {
        _nodes = nodes;
        _edges = edges;
    }

    /// <summary>
    /// Returns a read-only span over the skeleton's node positions.
    /// Hoist the result before loops — this is a method call, not a field access.
    /// </summary>
    public ReadOnlySpan<Point<T>> Nodes() => _nodes.Span;

    /// <summary>
    /// Returns a read-only span over the skeleton's edge segments.
    /// Hoist the result before loops — this is a method call, not a field access.
    /// </summary>
    public ReadOnlySpan<Segment<T>> Edges() => _edges.Span;

    /// <summary>
    /// Gets the number of nodes in this skeleton.
    /// </summary>
    [JsonIgnore]
    public int NodeCount => _nodes.Length;

    /// <summary>
    /// Gets the number of edges in this skeleton.
    /// </summary>
    [JsonIgnore]
    public int EdgeCount => _edges.Length;

    /// <inheritdoc />
    public Lease<Point<T>, Segment<T>> DetachFrom(AllocationScope scope)
    {
        if (!MemoryMarshal.TryGetArray(_nodes, out var nodeSeg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        if (!MemoryMarshal.TryGetArray(_edges, out var edgeSeg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        var nodeOwner = scope.UntrackMemory(new Memory<Point<T>>(nodeSeg.Array!, nodeSeg.Offset, nodeSeg.Count));
        var edgeOwner = scope.UntrackMemory(new Memory<Segment<T>>(edgeSeg.Array!, edgeSeg.Offset, edgeSeg.Count));
        return new Lease<Point<T>, Segment<T>> { _o1 = nodeOwner, _o2 = edgeOwner };
    }

    /// <summary>
    /// Finds the longest path through the skeleton graph (tree diameter via 2x BFS).
    /// </summary>
    public Polyline<T> LongestPath()
    {
        if (EdgeCount == 0 || NodeCount == 0)
            return new Polyline<T>();

        var eps = T.CreateTruncating(1e-7);
        var adj = BuildAdjacency(eps);

        // Find a node that has at least one edge (spine may share nodes with no edges)
        int startNode = -1;
        foreach (var (idx, neighbors) in adj)
        {
            if (neighbors.Count > 0) { startNode = idx; break; }
        }
        if (startNode < 0) return new Polyline<T>();

        var farthest1 = BfsFarthest(adj, startNode);
        var farthest2 = BfsFarthest(adj, farthest1);
        var path = BfsPath(adj, farthest1, farthest2);

        var nodesSpan = Nodes();
        var mem = Alloc.Memory<Point<T>>(path.Length);
        var span = mem.Span;
        for (int i = 0; i < path.Length; i++)
            span[i] = nodesSpan[path[i]];
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Returns the spine of the skeleton — the core subgraph after removing all leaf branches.
    /// A leaf branch is the path from a degree-1 node to the nearest junction (degree >= 3).
    /// For shapes with no junctions (e.g. triangle), returns an empty skeleton.
    /// For a rectangle skeleton (>-<), returns the central segment (-).
    /// Chain with <see cref="LongestPath"/> to get the central polyline: <c>skeleton.Spine().LongestPath()</c>.
    /// </summary>
    public Skeleton<T> Spine()
    {
        var (core, _) = SplitLeafEdges();
        return core;
    }

    /// <summary>
    /// Decomposes the skeleton into individual branches (paths between junction/leaf nodes).
    /// </summary>
    public IReadOnlyList<Polyline<T>> Branches()
    {
        if (EdgeCount == 0 || NodeCount == 0)
            return Array.Empty<Polyline<T>>();

        var eps = T.CreateTruncating(1e-7);
        var adj = BuildAdjacency(eps);
        int n = NodeCount;

        var isEndpoint = new bool[n];
        for (int i = 0; i < n; i++)
            isEndpoint[i] = adj[i].Count != 2;

        var visited = new HashSet<(int, int)>();
        var branches = new List<Polyline<T>>();

        for (int start = 0; start < n; start++)
        {
            if (!isEndpoint[start]) continue;
            foreach (var next in adj[start])
            {
                if (visited.Contains((start, next))) continue;

                var path = new List<int> { start };
                int prev = start, cur = next;
                while (true)
                {
                    visited.Add((prev, cur));
                    visited.Add((cur, prev));
                    path.Add(cur);
                    if (isEndpoint[cur]) break;

                    int nextNode = -1;
                    foreach (var nb in adj[cur])
                    {
                        if (nb != prev) { nextNode = nb; break; }
                    }
                    if (nextNode < 0) break;
                    prev = cur;
                    cur = nextNode;
                }

                var nodesSpan = Nodes();
                var mem = Alloc.Memory<Point<T>>(path.Count);
                var branchSpan = mem.Span;
                for (int pi = 0; pi < path.Count; pi++)
                    branchSpan[pi] = nodesSpan[path[pi]];
                branches.Add(new Polyline<T>(mem));
            }
        }

        return branches;
    }

    /// <summary>
    /// Splits the skeleton into core edges and leaf edges.
    /// Leaf edges are those where at least one endpoint is a leaf node (degree 1).
    /// </summary>
    public (Skeleton<T> Core, Skeleton<T> Leaves) SplitLeafEdges()
    {
        if (EdgeCount == 0 || NodeCount == 0)
            return (this, new Skeleton<T>(Array.Empty<Point<T>>(), Array.Empty<Segment<T>>()));

        var eps = T.CreateTruncating(1e-7);
        var adj = BuildAdjacency(eps);
        var cellSize = eps * T.CreateTruncating(2);
        var spatialHash = BuildSpatialHash(cellSize);

        var edgesSpan = Edges();
        int edgeCount = edgesSpan.Length;

        // Map each edge to its node indices
        var edgeNodes = new (int a, int b)[edgeCount];
        for (int i = 0; i < edgeCount; i++)
        {
            edgeNodes[i] = (
                FindNodeHashed(edgesSpan[i].Start, eps, cellSize, spatialHash),
                FindNodeHashed(edgesSpan[i].End, eps, cellSize, spatialHash));
        }

        // Build edge lookup: node pair → edge index
        var edgeLookup = new Dictionary<(int, int), int>();
        for (int i = 0; i < edgeCount; i++)
        {
            var (a, b) = edgeNodes[i];
            var key = a < b ? (a, b) : (b, a);
            edgeLookup.TryAdd(key, i);
        }

        // For each leaf node, trace through degree-2 nodes until junction (degree >= 3)
        var isLeafEdge = new bool[edgeCount];
        foreach (var (nodeIdx, neighbors) in adj)
        {
            if (neighbors.Count != 1) continue; // not a leaf

            var prev = nodeIdx;
            var cur = neighbors[0];
            while (true)
            {
                // Mark the edge prev→cur as leaf
                var key = prev < cur ? (prev, cur) : (cur, prev);
                if (edgeLookup.TryGetValue(key, out var ei))
                    isLeafEdge[ei] = true;

                // Stop if cur is a junction (degree >= 3) or another leaf
                if (adj[cur].Count != 2) break;

                // Continue through the degree-2 node
                var next = adj[cur][0] == prev ? adj[cur][1] : adj[cur][0];
                prev = cur;
                cur = next;
            }
        }

        var coreEdges = new List<Segment<T>>();
        var leafEdges = new List<Segment<T>>();
        for (int i = 0; i < edgeCount; i++)
        {
            if (isLeafEdge[i])
                leafEdges.Add(edgesSpan[i]);
            else
                coreEdges.Add(edgesSpan[i]);
        }

        var coreMem = Alloc.Memory<Segment<T>>(coreEdges.Count);
        coreEdges.CopyTo(coreMem.Span);
        var leafMem = Alloc.Memory<Segment<T>>(leafEdges.Count);
        leafEdges.CopyTo(leafMem.Span);

        return (
            new Skeleton<T>(_nodes, coreMem),
            new Skeleton<T>(_nodes, leafMem));
    }

    #region Geometry

    /// <summary>
    /// Computes the bounding box enclosing all skeleton nodes.
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        if (NodeCount == 0)
            return new Rectangle<T>(new Point<T>(T.Zero, T.Zero), new Size<T>(T.Zero, T.Zero));

        var nodesSpan = Nodes();
        T minX = nodesSpan[0].X, minY = nodesSpan[0].Y;
        T maxX = nodesSpan[0].X, maxY = nodesSpan[0].Y;

        for (int i = 1; i < nodesSpan.Length; i++)
        {
            if (nodesSpan[i].X < minX) minX = nodesSpan[i].X;
            if (nodesSpan[i].Y < minY) minY = nodesSpan[i].Y;
            if (nodesSpan[i].X > maxX) maxX = nodesSpan[i].X;
            if (nodesSpan[i].Y > maxY) maxY = nodesSpan[i].Y;
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Returns a new skeleton scaled by the given factor around the bounding-box center.
    /// </summary>
    public Skeleton<T> Scale(T factor)
    {
        if (NodeCount == 0) return this;

        var bb = BoundingBox();
        var two = T.One + T.One;
        var cx = bb.X + bb.Width / two;
        var cy = bb.Y + bb.Height / two;

        var nodesSpan = Nodes();
        var edgesSpan = Edges();

        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = new Point<T>(cx + (nodesSpan[i].X - cx) * factor, cy + (nodesSpan[i].Y - cy) * factor);

        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var s = edgesSpan[i].Start;
            var e = edgesSpan[i].End;
            newEdges[i] = new Segment<T>(
                new Point<T>(cx + (s.X - cx) * factor, cy + (s.Y - cy) * factor),
                new Point<T>(cx + (e.X - cx) * factor, cy + (e.Y - cy) * factor));
        }

        return new Skeleton<T>(nodesMem, edgesMem);
    }

    /// <summary>
    /// Returns a new skeleton rotated by the given angle around the specified origin.
    /// </summary>
    public Skeleton<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        if (NodeCount == 0) return this;

        var nodesSpan = Nodes();
        var edgesSpan = Edges();

        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = nodesSpan[i].Rotate(angle, origin);

        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
            newEdges[i] = new Segment<T>(
                edgesSpan[i].Start.Rotate(angle, origin),
                edgesSpan[i].End.Rotate(angle, origin));

        return new Skeleton<T>(nodesMem, edgesMem);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Translates all nodes and edges by adding the given vector.
    /// </summary>
    public static Skeleton<T> operator +(in Skeleton<T> a, in Vector<T> v)
    {
        if (a.NodeCount == 0) return a;
        var nodesSpan = a.Nodes();
        var edgesSpan = a.Edges();
        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = nodesSpan[i] + v;
        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
            newEdges[i] = new Segment<T>(edgesSpan[i].Start + v, edgesSpan[i].End + v);
        return new Skeleton<T>(nodesMem, edgesMem);
    }

    /// <summary>
    /// Translates all nodes and edges by subtracting the given vector.
    /// </summary>
    public static Skeleton<T> operator -(in Skeleton<T> a, in Vector<T> v)
    {
        if (a.NodeCount == 0) return a;
        var nodesSpan = a.Nodes();
        var edgesSpan = a.Edges();
        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = nodesSpan[i] - v;
        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
            newEdges[i] = new Segment<T>(edgesSpan[i].Start - v, edgesSpan[i].End - v);
        return new Skeleton<T>(nodesMem, edgesMem);
    }

    /// <summary>
    /// Scales all nodes and edges by the given size factor (component-wise multiplication).
    /// </summary>
    public static Skeleton<T> operator *(in Skeleton<T> a, in Size<T> f)
    {
        if (a.NodeCount == 0) return a;
        var nodesSpan = a.Nodes();
        var edgesSpan = a.Edges();
        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = nodesSpan[i] * f;
        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
            newEdges[i] = new Segment<T>(edgesSpan[i].Start * f, edgesSpan[i].End * f);
        return new Skeleton<T>(nodesMem, edgesMem);
    }

    /// <summary>
    /// Scales all nodes and edges by the inverse of the given size factor (component-wise division).
    /// </summary>
    public static Skeleton<T> operator /(in Skeleton<T> a, in Size<T> f)
    {
        if (a.NodeCount == 0) return a;
        var nodesSpan = a.Nodes();
        var edgesSpan = a.Edges();
        var nodesMem = Alloc.Memory<Point<T>>(nodesSpan.Length);
        var newNodes = nodesMem.Span;
        for (int i = 0; i < nodesSpan.Length; i++)
            newNodes[i] = nodesSpan[i] / f;
        var edgesMem = Alloc.Memory<Segment<T>>(edgesSpan.Length);
        var newEdges = edgesMem.Span;
        for (int i = 0; i < edgesSpan.Length; i++)
            newEdges[i] = new Segment<T>(edgesSpan[i].Start / f, edgesSpan[i].End / f);
        return new Skeleton<T>(nodesMem, edgesMem);
    }

    /// <summary>
    /// Rotates the skeleton around the origin by the given angle.
    /// </summary>
    public static Skeleton<T> operator +(in Skeleton<T> a, Degree<T> angle) =>
        a.Rotate(angle);

    /// <summary>
    /// Rotates the skeleton around the origin by the negation of the given angle.
    /// </summary>
    public static Skeleton<T> operator -(in Skeleton<T> a, Degree<T> angle) =>
        a.Rotate(-angle);

    #endregion

    #region Intersections

    /// <summary>
    /// Finds all intersection points of the given line with this skeleton's edges.
    /// Uses half-plane test per edge to skip segments that cannot intersect.
    /// </summary>
    public List<Point<T>> Intersections(in Line<T> line)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        if (edgesSpan.Length == 0) return result;

        if (line.IsVertical)
        {
            var vx = line.VerticalX;
            for (int i = 0; i < edgesSpan.Length; i++)
            {
                var edge = edgesSpan[i];
                var d1 = edge.Start.X - vx;
                var d2 = edge.End.X - vx;
                if (d1 > T.Zero && d2 > T.Zero) continue;
                if (d1 < T.Zero && d2 < T.Zero) continue;

                var hit = Drawing.Intersections.Of(line, edge);
                if (hit != null) result.Add(hit.Value);
            }
        }
        else
        {
            var eq = line.Equation;
            for (int i = 0; i < edgesSpan.Length; i++)
            {
                var edge = edgesSpan[i];
                var d1 = eq.A * edge.Start.X - edge.Start.Y + eq.B;
                var d2 = eq.A * edge.End.X - edge.End.Y + eq.B;
                if (d1 > T.Zero && d2 > T.Zero) continue;
                if (d1 < T.Zero && d2 < T.Zero) continue;

                var hit = Drawing.Intersections.Of(line, edge);
                if (hit != null) result.Add(hit.Value);
            }
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given segment with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Segment<T> segment)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var hit = Drawing.Intersections.Of(segment, edgesSpan[i]);
            if (hit != null) result.Add(hit.Value);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given circle with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Circle<T> circle)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var chord = Drawing.Intersections.Of(edgesSpan[i], circle);
            if (chord != null)
            {
                result.Add(chord.Value.Start);
                result.Add(chord.Value.End);
            }
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given triangle with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Triangle<T> triangle)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var hits = Drawing.Intersections.Of(edgesSpan[i], triangle);
            var hitsSpan = hits.Span;
            for (int j = 0; j < hitsSpan.Length; j++)
                result.Add(hitsSpan[j]);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given rectangle with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Rectangle<T> rect)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var hits = Drawing.Intersections.Of(edgesSpan[i], rect);
            var hitsSpan = hits.Span;
            for (int j = 0; j < hitsSpan.Length; j++)
                result.Add(hitsSpan[j]);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given polygon with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Polygon<T> polygon)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var hits = Drawing.Intersections.Of(edgesSpan[i], polygon);
            var hitsSpan = hits.Span;
            for (int j = 0; j < hitsSpan.Length; j++)
                result.Add(hitsSpan[j]);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given polyline with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Polyline<T> polyline)
    {
        var result = new List<Point<T>>();
        var edgesSpan = Edges();
        for (int i = 0; i < edgesSpan.Length; i++)
        {
            var hits = Drawing.Intersections.Of(edgesSpan[i], polyline);
            var hitsSpan = hits.Span;
            for (int j = 0; j < hitsSpan.Length; j++)
                result.Add(hitsSpan[j]);
        }
        return result;
    }

    #endregion

    private Dictionary<(int, int), List<int>> BuildSpatialHash(T cellSize)
    {
        var nodesSpan = Nodes();
        int n = nodesSpan.Length;
        var spatialHash = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < n; i++)
        {
            var key = QuantizePoint(nodesSpan[i], cellSize);
            if (!spatialHash.TryGetValue(key, out var list))
            {
                list = new List<int>();
                spatialHash[key] = list;
            }
            list.Add(i);
        }
        return spatialHash;
    }

    private Dictionary<int, List<int>> BuildAdjacency(T eps)
    {
        var adj = new Dictionary<int, List<int>>();
        int n = NodeCount;
        for (int i = 0; i < n; i++)
            adj[i] = new List<int>();

        var cellSize = eps * T.CreateTruncating(2);
        var spatialHash = BuildSpatialHash(cellSize);

        var edgesSpan = Edges();
        for (int ei = 0; ei < edgesSpan.Length; ei++)
        {
            var edge = edgesSpan[ei];
            int a = FindNodeHashed(edge.Start, eps, cellSize, spatialHash);
            int b = FindNodeHashed(edge.End, eps, cellSize, spatialHash);
            if (a < 0 || b < 0 || a == b) continue;
            if (!adj[a].Contains(b)) adj[a].Add(b);
            if (!adj[b].Contains(a)) adj[b].Add(a);
        }

        return adj;
    }

    private static (int, int) QuantizePoint(Point<T> point, T cellSize)
    {
        var ix = int.CreateTruncating(T.Floor(point.X / cellSize));
        var iy = int.CreateTruncating(T.Floor(point.Y / cellSize));
        return (ix, iy);
    }

    private int FindNodeHashed(Point<T> point, T eps, T cellSize, Dictionary<(int, int), List<int>> spatialHash)
    {
        var center = QuantizePoint(point, cellSize);
        var nodesSpan = Nodes();

        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            var key = (center.Item1 + dx, center.Item2 + dy);
            if (!spatialHash.TryGetValue(key, out var candidates)) continue;
            foreach (var i in candidates)
            {
                if (T.Abs(nodesSpan[i].X - point.X) < eps && T.Abs(nodesSpan[i].Y - point.Y) < eps)
                    return i;
            }
        }
        return -1;
    }

    private static int BfsFarthest(Dictionary<int, List<int>> adj, int start)
    {
        var visited = new HashSet<int> { start };
        var queue = new Queue<int>();
        queue.Enqueue(start);
        int last = start;
        while (queue.Count > 0)
        {
            last = queue.Dequeue();
            foreach (var nb in adj[last])
            {
                if (visited.Add(nb))
                    queue.Enqueue(nb);
            }
        }
        return last;
    }

    private static int[] BfsPath(Dictionary<int, List<int>> adj, int from, int to)
    {
        var visited = new HashSet<int> { from };
        var parent = new Dictionary<int, int> { [from] = -1 };
        var queue = new Queue<int>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == to) break;
            foreach (var nb in adj[cur])
            {
                if (visited.Add(nb))
                {
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
        }

        // Reconstruct
        var path = new List<int>();
        for (int c = to; c >= 0; c = parent.GetValueOrDefault(c, -1))
            path.Add(c);
        path.Reverse();
        return path.ToArray();
    }
}
