using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Computes a skeleton via the Chordal Axis Transform:
/// 1. Compute Constrained Delaunay Triangulation (CDT) of the polygon interior
/// 2. Classify triangles by internal edge count (terminal / sleeve / junction)
/// 3. Connect midpoints/centroids to form the skeleton
/// Based on the algorithm described in Prasad 2005 (Rectification of the Chordal Axis Transform).
/// </summary>
internal static class ChordalAxisAlgorithm
{
    internal static Skeleton<T> Compute<T>(in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        var span = polygon.Span;
        int n = span.Length;
        if (n < 3) return new Skeleton<T>(Array.Empty<Point<T>>(), Array.Empty<Segment<T>>());

        // Step 1: CDT of polygon vertices
        var dt = DelaunayTriangulation<T>.Create(span);

        // Enforce polygon edge constraints
        for (int i = 0; i < n; i++)
        {
            int a = i + 3; // offset by 3 because DelaunayTriangulation prepends 3 super-triangle vertices
            int b = ((i + 1) % n) + 3;
            dt.EnforceConstraint(a, b);
        }

        // Remove triangles outside the polygon
        dt.RemoveExteriorTriangles(polygon);

        var triangles = dt.Triangles;
        if (triangles.Count == 0)
            return new Skeleton<T>(Array.Empty<Point<T>>(), Array.Empty<Segment<T>>());

        // Build polygon edge set for boundary detection (using CDT point indices)
        var boundaryEdges = new HashSet<(int, int)>();
        for (int i = 0; i < n; i++)
        {
            int a = i + 3;
            int b = ((i + 1) % n) + 3;
            boundaryEdges.Add(a < b ? (a, b) : (b, a));
        }

        var two = T.CreateTruncating(2);
        var three = T.CreateTruncating(3);
        var skeletonEdges = new List<Segment<T>>();
        var nodeSet = new HashSet<(double, double)>();
        var nodeList = new List<Point<T>>();
        var pts = dt.Points;

        for (int t = 0; t < triangles.Count; t++)
        {
            var tri = triangles[t];
            int[] verts = { tri.A, tri.B, tri.C };

            // Find internal edges (shared with another triangle, not on polygon boundary)
            var internalEdgeMidpoints = new List<Point<T>>();
            for (int e = 0; e < 3; e++)
            {
                int ea = verts[e];
                int eb = verts[(e + 1) % 3];
                var key = ea < eb ? (ea, eb) : (eb, ea);

                // Internal = not a polygon boundary edge AND shared with another triangle
                bool isBoundary = boundaryEdges.Contains(key);
                bool isShared = dt.FindAdjacentTriangle(t, ea, eb) >= 0;

                if (isShared && !isBoundary)
                {
                    var mid = Midpoint(pts[ea], pts[eb], two);
                    internalEdgeMidpoints.Add(mid);
                }
            }

            switch (internalEdgeMidpoints.Count)
            {
                case 0:
                    // Isolated triangle — no skeleton contribution
                    break;

                case 1:
                {
                    // Terminal: connect midpoint of internal edge to opposite vertex
                    var mid = internalEdgeMidpoints[0];
                    Point<T> opposite = FindOppositeVertex(pts, tri, mid, two);
                    AddEdge(skeletonEdges, nodeSet, nodeList, mid, opposite);
                    break;
                }

                case 2:
                {
                    // Sleeve: connect midpoints of the two internal edges
                    AddEdge(skeletonEdges, nodeSet, nodeList,
                        internalEdgeMidpoints[0], internalEdgeMidpoints[1]);
                    break;
                }

                case 3:
                {
                    // Junction: connect centroid to midpoints of all three internal edges
                    var centroid = new Point<T>(
                        (pts[tri.A].X + pts[tri.B].X + pts[tri.C].X) / three,
                        (pts[tri.A].Y + pts[tri.B].Y + pts[tri.C].Y) / three);

                    foreach (var mid in internalEdgeMidpoints)
                        AddEdge(skeletonEdges, nodeSet, nodeList, centroid, mid);
                    break;
                }
            }
        }

        return new Skeleton<T>(nodeList.ToArray(), skeletonEdges.ToArray());
    }

    private static Point<T> Midpoint<T>(Point<T> a, Point<T> b, T two)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        return new Point<T>((a.X + b.X) / two, (a.Y + b.Y) / two);
    }

    private static Point<T> FindOppositeVertex<T>(IReadOnlyList<Point<T>> pts,
        DelaunayTriangulation<T>.DTriangle tri, Point<T> edgeMid, T two)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int[] verts = { tri.A, tri.B, tri.C };
        var eps = T.CreateTruncating(1e-9);

        for (int i = 0; i < 3; i++)
        {
            int ea = verts[i];
            int eb = verts[(i + 1) % 3];
            int ec = verts[(i + 2) % 3];

            var mid = Midpoint(pts[ea], pts[eb], two);
            var dx = mid.X - edgeMid.X;
            var dy = mid.Y - edgeMid.Y;
            if (T.Abs(dx) < eps && T.Abs(dy) < eps)
                return pts[ec];
        }

        // Fallback: return centroid
        var three = T.CreateTruncating(3);
        return new Point<T>(
            (pts[tri.A].X + pts[tri.B].X + pts[tri.C].X) / three,
            (pts[tri.A].Y + pts[tri.B].Y + pts[tri.C].Y) / three);
    }

    private static void AddEdge<T>(List<Segment<T>> edges, HashSet<(double, double)> nodeSet,
        List<Point<T>> nodeList, Point<T> a, Point<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        AddNode(nodeSet, nodeList, a);
        AddNode(nodeSet, nodeList, b);
        edges.Add(new Segment<T>(a, b));
    }

    private static void AddNode<T>(HashSet<(double, double)> nodeSet, List<Point<T>> nodeList, Point<T> pt)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var roundedKey = (Math.Round(Convert.ToDouble(pt.X), 7), Math.Round(Convert.ToDouble(pt.Y), 7));
        if (nodeSet.Add(roundedKey))
            nodeList.Add(pt);
    }
}
