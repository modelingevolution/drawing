using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Bowyer-Watson incremental Delaunay triangulation with CDT (constrained) support.
/// Used internally by skeleton algorithms.
/// </summary>
internal class DelaunayTriangulation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly record struct DTriangle(int A, int B, int C)
    {
        public bool Contains(int idx) => A == idx || B == idx || C == idx;
    }

    private readonly List<Point<T>> _points;
    private readonly List<DTriangle> _triangles;
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Eps = T.CreateTruncating(1e-10);

    private DelaunayTriangulation(List<Point<T>> points, List<DTriangle> triangles)
    {
        _points = points;
        _triangles = triangles;
    }

    public IReadOnlyList<DTriangle> Triangles => _triangles;
    public IReadOnlyList<Point<T>> Points => _points;

    /// <summary>
    /// Creates a Delaunay triangulation from the given points using Bowyer-Watson.
    /// </summary>
    public static DelaunayTriangulation<T> Create(ReadOnlySpan<Point<T>> inputPoints)
    {
        var points = new List<Point<T>>(inputPoints.Length + 3);

        // Compute bounding box to create super-triangle
        T minX = T.MaxValue, minY = T.MaxValue;
        T maxX = T.MinValue, maxY = T.MinValue;
        for (int i = 0; i < inputPoints.Length; i++)
        {
            if (inputPoints[i].X < minX) minX = inputPoints[i].X;
            if (inputPoints[i].Y < minY) minY = inputPoints[i].Y;
            if (inputPoints[i].X > maxX) maxX = inputPoints[i].X;
            if (inputPoints[i].Y > maxY) maxY = inputPoints[i].Y;
        }

        var dx = maxX - minX;
        var dy = maxY - minY;
        var dmax = T.Max(dx, dy);
        var midX = (minX + maxX) / Two;
        var midY = (minY + maxY) / Two;
        var margin = dmax * T.CreateTruncating(10);

        // Super-triangle vertices
        var st0 = new Point<T>(midX - margin, midY - margin);
        var st1 = new Point<T>(midX + margin, midY - margin);
        var st2 = new Point<T>(midX, midY + margin);

        points.Add(st0); // index 0
        points.Add(st1); // index 1
        points.Add(st2); // index 2

        var triangles = new List<DTriangle> { new(0, 1, 2) };

        // Insert each point
        for (int p = 0; p < inputPoints.Length; p++)
        {
            var pt = inputPoints[p];
            int pIdx = points.Count;
            points.Add(pt);

            // Find all triangles whose circumcircle contains the point
            var badTriangles = new List<int>();
            for (int t = 0; t < triangles.Count; t++)
            {
                if (InCircumcircle(points, triangles[t], pt))
                    badTriangles.Add(t);
            }

            // Find boundary polygon (edges shared by exactly one bad triangle)
            var polygon = new List<(int A, int B)>();
            foreach (var ti in badTriangles)
            {
                var tri = triangles[ti];
                CheckEdge(tri.A, tri.B, badTriangles, triangles, polygon);
                CheckEdge(tri.B, tri.C, badTriangles, triangles, polygon);
                CheckEdge(tri.C, tri.A, badTriangles, triangles, polygon);
            }

            // Remove bad triangles (in reverse order to preserve indices)
            badTriangles.Sort();
            for (int i = badTriangles.Count - 1; i >= 0; i--)
                triangles.RemoveAt(badTriangles[i]);

            // Re-triangulate with the new point
            foreach (var (a, b) in polygon)
                triangles.Add(new DTriangle(a, b, pIdx));
        }

        // Remove all triangles that share a vertex with the super-triangle
        triangles.RemoveAll(t => t.A < 3 || t.B < 3 || t.C < 3);

        return new DelaunayTriangulation<T>(points, triangles);
    }

    /// <summary>
    /// Enforce edge constraints for CDT. Ensures the edge between pointA and pointB exists.
    /// Uses simple edge-flip approach.
    /// </summary>
    public void EnforceConstraint(int pointIndexA, int pointIndexB)
    {
        // Check if the constraint edge already exists
        for (int t = 0; t < _triangles.Count; t++)
        {
            var tri = _triangles[t];
            if (HasEdge(tri, pointIndexA, pointIndexB))
                return;
        }

        // Find triangles intersected by the constraint edge and flip them
        // Simple approach: find two triangles sharing the crossing edge and flip
        var maxIter = _triangles.Count * 2;
        for (int iter = 0; iter < maxIter; iter++)
        {
            bool found = false;
            for (int t = 0; t < _triangles.Count && !found; t++)
            {
                var tri = _triangles[t];
                // Check each edge of this triangle for intersection with constraint
                if (TryFlipForConstraint(t, pointIndexA, pointIndexB))
                {
                    found = true;
                    break;
                }
            }
            if (!found) break;

            // Check if constraint now exists
            for (int t = 0; t < _triangles.Count; t++)
            {
                if (HasEdge(_triangles[t], pointIndexA, pointIndexB))
                    return;
            }
        }
    }

    /// <summary>
    /// Remove triangles whose centroid falls outside the boundary polygon.
    /// </summary>
    public void RemoveExteriorTriangles(in Polygon<T> boundary)
    {
        var poly = boundary;
        _triangles.RemoveAll(t =>
        {
            var centroid = TriangleCentroid(t);
            return !poly.Contains(centroid);
        });
    }

    /// <summary>
    /// Compute Voronoi dual edges: for each pair of adjacent triangles, connect circumcenters.
    /// </summary>
    public List<Segment<T>> VoronoiEdges()
    {
        var result = new List<Segment<T>>();
        var edgeToTriangle = new Dictionary<(int, int), int>();

        for (int t = 0; t < _triangles.Count; t++)
        {
            var tri = _triangles[t];
            AddEdge(edgeToTriangle, tri.A, tri.B, t, result);
            AddEdge(edgeToTriangle, tri.B, tri.C, t, result);
            AddEdge(edgeToTriangle, tri.C, tri.A, t, result);
        }

        return result;
    }

    public Point<T> Circumcenter(in DTriangle t)
    {
        return ComputeCircumcenter(_points[t.A], _points[t.B], _points[t.C]);
    }

    public Point<T> TriangleCentroid(in DTriangle t)
    {
        var three = T.CreateTruncating(3);
        return new Point<T>(
            (_points[t.A].X + _points[t.B].X + _points[t.C].X) / three,
            (_points[t.A].Y + _points[t.B].Y + _points[t.C].Y) / three);
    }

    /// <summary>
    /// Returns the number of edges that are internal (shared with another triangle in the CDT).
    /// </summary>
    public int InternalEdgeCount(int triIndex)
    {
        var tri = _triangles[triIndex];
        int count = 0;
        if (FindAdjacentTriangle(triIndex, tri.A, tri.B) >= 0) count++;
        if (FindAdjacentTriangle(triIndex, tri.B, tri.C) >= 0) count++;
        if (FindAdjacentTriangle(triIndex, tri.C, tri.A) >= 0) count++;
        return count;
    }

    /// <summary>
    /// Returns edge midpoint.
    /// </summary>
    public Point<T> EdgeMidpoint(int a, int b)
    {
        return new Point<T>(
            (_points[a].X + _points[b].X) / Two,
            (_points[a].Y + _points[b].Y) / Two);
    }

    /// <summary>
    /// Returns the index of the adjacent triangle sharing the edge (a,b), or -1.
    /// </summary>
    public int FindAdjacentTriangle(int excludeTriIndex, int a, int b)
    {
        for (int t = 0; t < _triangles.Count; t++)
        {
            if (t == excludeTriIndex) continue;
            if (HasEdge(_triangles[t], a, b)) return t;
        }
        return -1;
    }

    /// <summary>
    /// Gets the indices of internal edges (shared with adjacent triangles) for a given triangle.
    /// Returns pairs of point indices.
    /// </summary>
    public List<(int A, int B)> GetInternalEdges(int triIndex)
    {
        var tri = _triangles[triIndex];
        var result = new List<(int, int)>(3);
        if (FindAdjacentTriangle(triIndex, tri.A, tri.B) >= 0) result.Add((tri.A, tri.B));
        if (FindAdjacentTriangle(triIndex, tri.B, tri.C) >= 0) result.Add((tri.B, tri.C));
        if (FindAdjacentTriangle(triIndex, tri.C, tri.A) >= 0) result.Add((tri.C, tri.A));
        return result;
    }

    /// <summary>
    /// Gets the vertex opposite to the given internal edge in the triangle.
    /// </summary>
    public int OppositeVertex(int triIndex, int edgeA, int edgeB)
    {
        var tri = _triangles[triIndex];
        if (tri.A != edgeA && tri.A != edgeB) return tri.A;
        if (tri.B != edgeA && tri.B != edgeB) return tri.B;
        return tri.C;
    }

    // ════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════

    private static bool InCircumcircle(List<Point<T>> pts, DTriangle tri, Point<T> p)
    {
        var a = pts[tri.A];
        var b = pts[tri.B];
        var c = pts[tri.C];

        var ax = a.X - p.X; var ay = a.Y - p.Y;
        var bx = b.X - p.X; var by = b.Y - p.Y;
        var cx = c.X - p.X; var cy = c.Y - p.Y;

        var det = ax * (by * (cx * cx + cy * cy) - cy * (bx * bx + by * by))
                - bx * (ay * (cx * cx + cy * cy) - cy * (ax * ax + ay * ay))
                + cx * (ay * (bx * bx + by * by) - by * (ax * ax + ay * ay));

        // CCW winding → det > 0 means inside; CW → det < 0 means inside.
        // Use epsilon tolerance so co-circular points (det ≈ 0) are treated as "inside",
        // ensuring Bowyer-Watson flips degenerate configurations (e.g., regular n-gons).
        var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return cross > T.Zero ? det > -Eps : det < Eps;
    }

    private static void CheckEdge(int a, int b, List<int> badTriangles, List<DTriangle> triangles,
        List<(int, int)> polygon)
    {
        int count = 0;
        foreach (var ti in badTriangles)
        {
            if (HasEdge(triangles[ti], a, b)) count++;
        }

        if (count == 1)
            polygon.Add((a, b));
    }

    private static bool HasEdge(DTriangle t, int a, int b)
    {
        return (t.A == a && t.B == b) || (t.B == a && t.C == b) || (t.C == a && t.A == b)
            || (t.A == b && t.B == a) || (t.B == b && t.C == a) || (t.C == b && t.A == a);
    }

    private static Point<T> ComputeCircumcenter(Point<T> a, Point<T> b, Point<T> c)
    {
        var ax = a.X; var ay = a.Y;
        var bx = b.X; var by = b.Y;
        var cx = c.X; var cy = c.Y;

        var d = Two * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (T.Abs(d) < Eps)
        {
            // Degenerate — return centroid as fallback
            var three = T.CreateTruncating(3);
            return new Point<T>((ax + bx + cx) / three, (ay + by + cy) / three);
        }

        var aSq = ax * ax + ay * ay;
        var bSq = bx * bx + by * by;
        var cSq = cx * cx + cy * cy;

        var ux = (aSq * (by - cy) + bSq * (cy - ay) + cSq * (ay - by)) / d;
        var uy = (aSq * (cx - bx) + bSq * (ax - cx) + cSq * (bx - ax)) / d;

        return new Point<T>(ux, uy);
    }

    private void AddEdge(Dictionary<(int, int), int> edgeToTriangle, int a, int b, int triIndex,
        List<Segment<T>> result)
    {
        var key = a < b ? (a, b) : (b, a);
        if (edgeToTriangle.TryGetValue(key, out var otherTri))
        {
            // Adjacent pair found — connect circumcenters
            var cc1 = Circumcenter(_triangles[otherTri]);
            var cc2 = Circumcenter(_triangles[triIndex]);
            result.Add(new Segment<T>(cc1, cc2));
            edgeToTriangle.Remove(key);
        }
        else
        {
            edgeToTriangle[key] = triIndex;
        }
    }

    private bool TryFlipForConstraint(int triIndex, int constraintA, int constraintB)
    {
        var tri = _triangles[triIndex];
        int[] verts = { tri.A, tri.B, tri.C };

        for (int e = 0; e < 3; e++)
        {
            int ea = verts[e];
            int eb = verts[(e + 1) % 3];
            int ec = verts[(e + 2) % 3]; // opposite vertex

            if (ea == constraintA || ea == constraintB || eb == constraintA || eb == constraintB)
                continue;

            // Check if edge (ea, eb) intersects constraint (constraintA, constraintB)
            var segEdge = new Segment<T>(_points[ea], _points[eb]);
            var segConstraint = new Segment<T>(_points[constraintA], _points[constraintB]);
            var hit = Intersections.Of(segEdge, segConstraint);
            if (hit == null) continue;

            // Find adjacent triangle
            int adjIdx = FindAdjacentTriangle(triIndex, ea, eb);
            if (adjIdx < 0) continue;

            var adjTri = _triangles[adjIdx];
            int od = -1; // opposite vertex in adjacent triangle
            if (adjTri.A != ea && adjTri.A != eb) od = adjTri.A;
            else if (adjTri.B != ea && adjTri.B != eb) od = adjTri.B;
            else od = adjTri.C;

            // Flip: replace (ea, eb) edge with (ec, od) edge
            _triangles[triIndex] = new DTriangle(ec, ea, od);
            _triangles[adjIdx] = new DTriangle(ec, od, eb);
            return true;
        }

        return false;
    }
}
