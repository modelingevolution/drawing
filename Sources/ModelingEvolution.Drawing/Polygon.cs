using System.Collections;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public readonly record struct Polygon<T> : IEnumerable<Point<T>>, IReadOnlyList<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Point<T>[] _points;
    public T Area()
    {
        int n = _points.Length;
        if (n < 3)
        {
            throw new ArgumentException("A polygon must have at least 3 points.");
        }
        T area = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var current = _points[i];
            var next = _points[(i + 1) % n];
            area += current.X * next.Y;
            area -= current.Y * next.X;
        }
        area = T.Abs(area) / (T.One+T.One);
        return area;
    }
    public static Polygon<T> operator *(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Select(x => x * f).ToArray());
    }
    public static Polygon<T> operator /(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Select(x => x / f).ToArray());
    }

    public Polygon<T> Intersect(Rectangle<T> rect)
    {
        var outputList = this._points.ToList();

        // Clip against each edge of the rectangle
        outputList = ClipPolygon(outputList, new Point<T>(rect.Left, rect.Top), new Point<T>(rect.Right, rect.Top));   // Top edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Right, rect.Top), new Point<T>(rect.Right, rect.Bottom)); // Right edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Right, rect.Bottom), new Point<T>(rect.Left, rect.Bottom)); // Bottom edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Left, rect.Bottom), new Point<T>(rect.Left, rect.Top));   // Left edge

        return new Polygon<T>(outputList.ToArray());
    }

    // Sutherland-Hodgman polygon clipping
    private static List<Point<T>> ClipPolygon(List<Point<T>> polygon, Point<T> edgeStart, Point<T> edgeEnd)
    {
        List<Point<T>> clippedPolygon = new List<Point<T>>();

        for (int i = 0; i < polygon.Count; i++)
        {
            Point<T> currentPoint = polygon[i];
            Point<T> prevPoint = polygon[(i - 1 + polygon.Count) % polygon.Count];

            bool currentInside = IsInside(currentPoint, edgeStart, edgeEnd);
            bool prevInside = IsInside(prevPoint, edgeStart, edgeEnd);

            if (currentInside && prevInside)
            {
                // Both points are inside, add current point
                clippedPolygon.Add(currentPoint);
            }
            else if (!currentInside && prevInside)
            {
                // Leaving the clip area, add intersection point
                clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
            }
            else if (currentInside && !prevInside)
            {
                // Entering the clip area, add intersection point and current point
                clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
                clippedPolygon.Add(currentPoint);
            }
        }

        return clippedPolygon;
    }

    // Helper function to check if a point is inside the clipping edge
    private static bool IsInside(Point<T> p, Point<T> edgeStart, Point<T> edgeEnd)
    {
        return (edgeEnd.X - edgeStart.X) * (p.Y - edgeStart.Y) > (edgeEnd.Y - edgeStart.Y) * (p.X - edgeStart.X);
    }

    // Calculate intersection of line segment (p1, p2) with edge (edgeStart, edgeEnd)
    private static Point<T> GetIntersection(Point<T> p1, Point<T> p2, Point<T> edgeStart, Point<T> edgeEnd)
    {
        T A1 = p2.Y - p1.Y;
        T B1 = p1.X - p2.X;
        T C1 = A1 * p1.X + B1 * p1.Y;

        T A2 = edgeEnd.Y - edgeStart.Y;
        T B2 = edgeStart.X - edgeEnd.X;
        T C2 = A2 * edgeStart.X + B2 * edgeStart.Y;

        T det = A1 * B2 - A2 * B1;

        if (T.Abs(det) <= T.Epsilon) // Lines are parallel
        {
            return new Point<T>();  // No intersection
        }

        T x = (B2 * C1 - B1 * C2) / det;
        T y = (A1 * C2 - A2 * C1) / det;

        return new Point<T>(x, y);
    }
    public static Polygon<T> operator -(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Select(x => x - f).ToArray());
    }
    public static Polygon<T> operator +(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Select(x => x + f).ToArray());
    }
    private Polygon(Point<T>[] points)
    {
        _points = points;
    }
    public Polygon(IReadOnlyList<T> points)
    {
        _points = new Point<T>[points.Count / 2];
        for (int i = 0; i < points.Count; i += 2)
        {
            _points[i / 2] = new Point<T>(points[i], points[i + 1]);
        }
    }



    public int Count => _points.Length;

    public Point<T> this[int index] => _points[index];
    public IEnumerator<Point<T>> GetEnumerator()
    {
        for (int i = 0; i < _points.Length; i++)
            yield return _points[i];
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return _points.GetEnumerator();
    }
}