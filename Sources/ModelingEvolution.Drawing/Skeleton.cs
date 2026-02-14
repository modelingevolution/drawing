using System.Numerics;
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
public readonly record struct Skeleton<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)]
    internal readonly Point<T>[] _nodes;

    [ProtoMember(2)]
    internal readonly Segment<T>[] _edges;

    /// <summary>
    /// Initializes a new skeleton from the given nodes and edges.
    /// </summary>
    /// <param name="nodes">The skeleton's interior node positions.</param>
    /// <param name="edges">The skeleton's edge segments connecting nodes.</param>
    public Skeleton(Point<T>[] nodes, Segment<T>[] edges)
    {
        _nodes = nodes ?? Array.Empty<Point<T>>();
        _edges = edges ?? Array.Empty<Segment<T>>();
    }

    /// <summary>
    /// Gets a read-only span over the skeleton's node positions.
    /// </summary>
    [JsonIgnore]
    public ReadOnlySpan<Point<T>> Nodes => _nodes != null ? _nodes.AsSpan() : ReadOnlySpan<Point<T>>.Empty;

    /// <summary>
    /// Gets a read-only span over the skeleton's edge segments.
    /// </summary>
    [JsonIgnore]
    public ReadOnlySpan<Segment<T>> Edges => _edges != null ? _edges.AsSpan() : ReadOnlySpan<Segment<T>>.Empty;

    /// <summary>
    /// Gets the number of nodes in this skeleton.
    /// </summary>
    [JsonIgnore]
    public int NodeCount => _nodes != null ? _nodes.Length : 0;

    /// <summary>
    /// Gets the number of edges in this skeleton.
    /// </summary>
    [JsonIgnore]
    public int EdgeCount => _edges != null ? _edges.Length : 0;

    /// <summary>
    /// Finds the longest path through the skeleton graph (tree diameter via 2x BFS).
    /// </summary>
    public Polyline<T> LongestPath()
    {
        if (_edges == null || _edges.Length == 0 || _nodes == null || _nodes.Length == 0)
            return new Polyline<T>();

        var eps = T.CreateTruncating(1e-7);
        // Build adjacency from edges
        var adj = BuildAdjacency(eps);

        // BFS from node 0 to find farthest node
        var farthest1 = BfsFarthest(adj, 0);
        // BFS from farthest1 to find the actual diameter endpoint
        var farthest2 = BfsFarthest(adj, farthest1);
        // Reconstruct path from farthest1 to farthest2
        var path = BfsPath(adj, farthest1, farthest2);

        var nodes = _nodes;
        var points = new Point<T>[path.Length];
        for (int i = 0; i < path.Length; i++)
            points[i] = nodes[path[i]];
        return new Polyline<T>(points);
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
        if (_edges == null || _edges.Length == 0 || _nodes == null || _nodes.Length == 0)
            return Array.Empty<Polyline<T>>();

        var eps = T.CreateTruncating(1e-7);
        var adj = BuildAdjacency(eps);
        int n = _nodes.Length;

        // Find junction (degree >= 3) and leaf (degree == 1) nodes
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

                var nodes = _nodes;
                var branchPoints = new Point<T>[path.Count];
                for (int pi = 0; pi < path.Count; pi++)
                    branchPoints[pi] = nodes[path[pi]];
                branches.Add(new Polyline<T>(branchPoints));
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
        if (_edges == null || _edges.Length == 0 || _nodes == null || _nodes.Length == 0)
            return (this, new Skeleton<T>(Array.Empty<Point<T>>(), Array.Empty<Segment<T>>()));

        var eps = T.CreateTruncating(1e-7);
        var adj = BuildAdjacency(eps);

        // Find leaf node indices (degree == 1)
        var leafNodes = new HashSet<int>();
        foreach (var (idx, neighbors) in adj)
        {
            if (neighbors.Count == 1)
                leafNodes.Add(idx);
        }

        var cellSize = eps * T.CreateTruncating(2);
        var spatialHash = BuildSpatialHash(cellSize);

        var coreEdges = new List<Segment<T>>();
        var leafEdges = new List<Segment<T>>();

        foreach (var edge in _edges)
        {
            int a = FindNodeHashed(edge.Start, eps, cellSize, spatialHash);
            int b = FindNodeHashed(edge.End, eps, cellSize, spatialHash);

            if (leafNodes.Contains(a) || leafNodes.Contains(b))
                leafEdges.Add(edge);
            else
                coreEdges.Add(edge);
        }

        return (
            new Skeleton<T>(_nodes, coreEdges.ToArray()),
            new Skeleton<T>(_nodes, leafEdges.ToArray()));
    }

    #region Geometry

    /// <summary>
    /// Computes the bounding box enclosing all skeleton nodes.
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        if (_nodes == null || _nodes.Length == 0)
            return new Rectangle<T>(new Point<T>(T.Zero, T.Zero), new Size<T>(T.Zero, T.Zero));

        T minX = _nodes[0].X, minY = _nodes[0].Y;
        T maxX = _nodes[0].X, maxY = _nodes[0].Y;

        for (int i = 1; i < _nodes.Length; i++)
        {
            if (_nodes[i].X < minX) minX = _nodes[i].X;
            if (_nodes[i].Y < minY) minY = _nodes[i].Y;
            if (_nodes[i].X > maxX) maxX = _nodes[i].X;
            if (_nodes[i].Y > maxY) maxY = _nodes[i].Y;
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Returns a new skeleton scaled by the given factor around the bounding-box center.
    /// </summary>
    public Skeleton<T> Scale(T factor)
    {
        if (_nodes == null || _nodes.Length == 0) return this;

        var bb = BoundingBox();
        var two = T.One + T.One;
        var cx = bb.X + bb.Width / two;
        var cy = bb.Y + bb.Height / two;

        var newNodes = new Point<T>[_nodes.Length];
        for (int i = 0; i < _nodes.Length; i++)
            newNodes[i] = new Point<T>(cx + (_nodes[i].X - cx) * factor, cy + (_nodes[i].Y - cy) * factor);

        var newEdges = new Segment<T>[_edges.Length];
        for (int i = 0; i < _edges.Length; i++)
        {
            var s = _edges[i].Start;
            var e = _edges[i].End;
            newEdges[i] = new Segment<T>(
                new Point<T>(cx + (s.X - cx) * factor, cy + (s.Y - cy) * factor),
                new Point<T>(cx + (e.X - cx) * factor, cy + (e.Y - cy) * factor));
        }

        return new Skeleton<T>(newNodes, newEdges);
    }

    /// <summary>
    /// Returns a new skeleton rotated by the given angle around the specified origin.
    /// </summary>
    public Skeleton<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        if (_nodes == null || _nodes.Length == 0) return this;

        var newNodes = new Point<T>[_nodes.Length];
        for (int i = 0; i < _nodes.Length; i++)
            newNodes[i] = _nodes[i].Rotate(angle, origin);

        var newEdges = new Segment<T>[_edges.Length];
        for (int i = 0; i < _edges.Length; i++)
            newEdges[i] = new Segment<T>(
                _edges[i].Start.Rotate(angle, origin),
                _edges[i].End.Rotate(angle, origin));

        return new Skeleton<T>(newNodes, newEdges);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Translates all nodes and edges by adding the given vector.
    /// </summary>
    public static Skeleton<T> operator +(in Skeleton<T> a, in Vector<T> v)
    {
        if (a._nodes == null) return a;
        var newNodes = new Point<T>[a._nodes.Length];
        for (int i = 0; i < a._nodes.Length; i++)
            newNodes[i] = a._nodes[i] + v;
        var newEdges = new Segment<T>[a._edges.Length];
        for (int i = 0; i < a._edges.Length; i++)
            newEdges[i] = new Segment<T>(a._edges[i].Start + v, a._edges[i].End + v);
        return new Skeleton<T>(newNodes, newEdges);
    }

    /// <summary>
    /// Translates all nodes and edges by subtracting the given vector.
    /// </summary>
    public static Skeleton<T> operator -(in Skeleton<T> a, in Vector<T> v)
    {
        if (a._nodes == null) return a;
        var newNodes = new Point<T>[a._nodes.Length];
        for (int i = 0; i < a._nodes.Length; i++)
            newNodes[i] = a._nodes[i] - v;
        var newEdges = new Segment<T>[a._edges.Length];
        for (int i = 0; i < a._edges.Length; i++)
            newEdges[i] = new Segment<T>(a._edges[i].Start - v, a._edges[i].End - v);
        return new Skeleton<T>(newNodes, newEdges);
    }

    /// <summary>
    /// Scales all nodes and edges by the given size factor (component-wise multiplication).
    /// </summary>
    public static Skeleton<T> operator *(in Skeleton<T> a, in Size<T> f)
    {
        if (a._nodes == null) return a;
        var newNodes = new Point<T>[a._nodes.Length];
        for (int i = 0; i < a._nodes.Length; i++)
            newNodes[i] = a._nodes[i] * f;
        var newEdges = new Segment<T>[a._edges.Length];
        for (int i = 0; i < a._edges.Length; i++)
            newEdges[i] = new Segment<T>(a._edges[i].Start * f, a._edges[i].End * f);
        return new Skeleton<T>(newNodes, newEdges);
    }

    /// <summary>
    /// Scales all nodes and edges by the inverse of the given size factor (component-wise division).
    /// </summary>
    public static Skeleton<T> operator /(in Skeleton<T> a, in Size<T> f)
    {
        if (a._nodes == null) return a;
        var newNodes = new Point<T>[a._nodes.Length];
        for (int i = 0; i < a._nodes.Length; i++)
            newNodes[i] = a._nodes[i] / f;
        var newEdges = new Segment<T>[a._edges.Length];
        for (int i = 0; i < a._edges.Length; i++)
            newEdges[i] = new Segment<T>(a._edges[i].Start / f, a._edges[i].End / f);
        return new Skeleton<T>(newNodes, newEdges);
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
        if (_edges == null) return result;

        // Half-plane early-out: if both endpoints are on the same side of the line,
        // the segment cannot intersect it. This avoids the full intersection math.
        if (line.IsVertical)
        {
            var vx = line.VerticalX;
            foreach (var edge in _edges)
            {
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
            foreach (var edge in _edges)
            {
                // Signed distance proxy: A*x - y + B (same sign = same side)
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
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var hit = Drawing.Intersections.Of(segment, edge);
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
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var chord = Drawing.Intersections.Of(edge, circle);
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
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var hits = Drawing.Intersections.Of(edge, triangle);
            result.AddRange(hits);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given rectangle with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Rectangle<T> rect)
    {
        var result = new List<Point<T>>();
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var hits = Drawing.Intersections.Of(edge, rect);
            result.AddRange(hits);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given polygon with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Polygon<T> polygon)
    {
        var result = new List<Point<T>>();
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var hits = Drawing.Intersections.Of(edge, polygon);
            result.AddRange(hits);
        }
        return result;
    }

    /// <summary>
    /// Finds all intersection points of the given polyline with this skeleton's edges.
    /// </summary>
    public List<Point<T>> Intersections(in Polyline<T> polyline)
    {
        var result = new List<Point<T>>();
        if (_edges == null) return result;
        foreach (var edge in _edges)
        {
            var hits = Drawing.Intersections.Of(edge, polyline);
            result.AddRange(hits);
        }
        return result;
    }

    #endregion

    private Dictionary<(int, int), List<int>> BuildSpatialHash(T cellSize)
    {
        int n = _nodes.Length;
        var spatialHash = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < n; i++)
        {
            var key = QuantizePoint(_nodes[i], cellSize);
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
        int n = _nodes.Length;
        for (int i = 0; i < n; i++)
            adj[i] = new List<int>();

        var cellSize = eps * T.CreateTruncating(2);
        var spatialHash = BuildSpatialHash(cellSize);

        foreach (var edge in _edges)
        {
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

        // Check 3x3 neighborhood to handle points near cell boundaries
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            var key = (center.Item1 + dx, center.Item2 + dy);
            if (!spatialHash.TryGetValue(key, out var candidates)) continue;
            foreach (var i in candidates)
            {
                if (T.Abs(_nodes[i].X - point.X) < eps && T.Abs(_nodes[i].Y - point.Y) < eps)
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
