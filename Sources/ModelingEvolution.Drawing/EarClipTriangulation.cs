using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Simple ear-clipping polygon triangulation.
/// Produces n-2 triangles for a simple polygon with n vertices.
/// More robust than Bowyer-Watson CDT for polygons with co-circular vertices.
/// </summary>
internal static class EarClipTriangulation
{
    internal readonly record struct Triangle(int A, int B, int C);

    /// <summary>
    /// Triangulates a simple polygon using ear clipping.
    /// Returns triangles as index triplets referencing the input points.
    /// </summary>
    internal static PooledList<Triangle> Triangulate<T>(ReadOnlySpan<Point<T>> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = polygon.Length;
        var result = new PooledList<Triangle>(n - 2);
        if (n < 3) return result;

        // Copy points for local use
        var ptsMem = Alloc.Memory<Point<T>>(n);
        var pts = ptsMem.Span;
        for (int i = 0; i < n; i++) pts[i] = polygon[i];

        // Ensure CCW winding (ear clipping assumes CCW)
        if (SignedArea(pts) < T.Zero)
            pts.Reverse();

        // Build index linked list
        var prevMem = Alloc.Memory<int>(n);
        var nextMem = Alloc.Memory<int>(n);
        var prev = prevMem.Span;
        var next = nextMem.Span;
        for (int i = 0; i < n; i++)
        {
            prev[i] = (i - 1 + n) % n;
            next[i] = (i + 1) % n;
        }

        int remaining = n;
        int current = 0;
        int safety = n * n; // prevent infinite loop

        while (remaining > 3 && safety-- > 0)
        {
            bool earFound = false;
            int start = current;

            do
            {
                int p = prev[current];
                int nx = next[current];

                if (IsConvex(pts[p], pts[current], pts[nx]) &&
                    !AnyPointInside(pts, prev, next, remaining, p, current, nx))
                {
                    // Clip this ear
                    result.Add(new Triangle(p, current, nx));

                    // Remove current from linked list
                    next[p] = nx;
                    prev[nx] = p;
                    remaining--;
                    earFound = true;
                    current = nx;
                    break;
                }

                current = next[current];
            }
            while (current != start);

            if (!earFound) break; // degenerate polygon
        }

        // Add the last triangle
        if (remaining == 3)
        {
            int a = current;
            int b = next[a];
            int c = next[b];
            result.Add(new Triangle(a, b, c));
        }

        return result;
    }

    private static T SignedArea<T>(Span<Point<T>> pts)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = pts.Length;
        T area = T.Zero;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += pts[i].X * pts[j].Y - pts[j].X * pts[i].Y;
        }
        return area;
    }

    /// <summary>
    /// Returns true if vertex B is convex (left turn) in CCW polygon.
    /// </summary>
    private static bool IsConvex<T>(Point<T> a, Point<T> b, Point<T> c)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return cross > T.Zero;
    }

    /// <summary>
    /// Checks if any polygon vertex (other than p, cur, nx) lies inside triangle (p, cur, nx).
    /// </summary>
    private static bool AnyPointInside<T>(Span<Point<T>> pts, Span<int> prev, Span<int> next,
        int remaining, int p, int cur, int nx)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int check = next[nx];
        int count = remaining - 3;
        for (int i = 0; i < count; i++)
        {
            if (PointInTriangle(pts[check], pts[p], pts[cur], pts[nx]))
                return true;
            check = next[check];
        }
        return false;
    }

    private static bool PointInTriangle<T>(Point<T> pt, Point<T> a, Point<T> b, Point<T> c)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T d1 = Cross(a, b, pt);
        T d2 = Cross(b, c, pt);
        T d3 = Cross(c, a, pt);

        bool hasNeg = (d1 < T.Zero) || (d2 < T.Zero) || (d3 < T.Zero);
        bool hasPos = (d1 > T.Zero) || (d2 > T.Zero) || (d3 > T.Zero);

        return !(hasNeg && hasPos);
    }

    private static T Cross<T>(Point<T> a, Point<T> b, Point<T> p)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
        ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
    }
}
