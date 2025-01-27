using ProtoBuf;
using System.Collections;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using ModelingEvolution.Drawing.Svg;
using System.Collections.Generic;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace ModelingEvolution.Drawing;

[SvgExporterAttribute(typeof(PolygonSvgExporterFactory))]
[JsonConverter(typeof(PolygonJsonConverterFactory))]
[ProtoContract]
/// <summary>
/// This struct is not immutable, athrough operators are immutable.
/// </summary>
public readonly record struct Polygon<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)] internal readonly IList<Point<T>> _points;

    public T Area()
    {
        int n = _points.Count;
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

        area = T.Abs(area) / (T.One + T.One);
        return area;
    }
    // Implement computation of bounding box
    public Rectangle<T> BoundingBox()
    {
        if (_points.Count == 0)
        {
            return new Rectangle<T>(new Point<T>(T.Zero, T.Zero), new Size<T>(T.Zero, T.Zero));
        }

        T minX = _points[0].X;
        T minY = _points[0].Y;
        T maxX = _points[0].X;
        T maxY = _points[0].Y;

        for (int i = 1; i < _points.Count; i++)
        {
            if (_points[i].X < minX)
            {
                minX = _points[i].X;
            }

            if (_points[i].Y < minY)
            {
                minY = _points[i].Y;
            }

            if (_points[i].X > maxX)
            {
                maxX = _points[i].X;
            }

            if (_points[i].Y > maxY)
            {
                maxY = _points[i].Y;
            }
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }
    public bool Contains(Point<T> item)
    {
        return _points.Contains(item);
    }
    [JsonIgnore]
    public bool IsReadOnly => true;

    public static Polygon<T> operator *(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x * f).ToList(a._points.Count));
    }

    public static Polygon<T> operator /(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x / f).ToList(a._points.Count));
    }
    public bool IsOverlapping(in Polygon<T> other)
    {
        return this.Intersect(other).Count > 0;
    }
    public static IEnumerable<D> Cluster<D>(
        IEnumerable<D> items,
        Func<D, Polygon<T>> polygonGetter,
        Func<IEnumerable<D>, Polygon<T>, D> factory)
    {
        if (items == null || !items.Any())
            return Array.Empty<D>();

        var itemList = items.ToList();
        var visited = new HashSet<int>();
        var clusters = new List<List<int>>();

        // For each item, start a new cluster if not already visited
        for (int i = 0; i < itemList.Count; i++)
        {
            if (visited.Contains(i))
                continue;

            var cluster = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (!visited.Add(current))
                    continue;

                cluster.Add(current);

                // Enqueue all overlapping items
                for (int j = 0; j < itemList.Count; j++)
                {
                    if (!visited.Contains(j) &&
                        polygonGetter(itemList[current]).IsOverlapping(polygonGetter(itemList[j])))
                    {
                        queue.Enqueue(j);
                    }
                }
            }

            clusters.Add(cluster);
        }

        // Merge each cluster into a single object of type D
        return clusters.Select(clusterIndices =>
        {
            var clusterItems = clusterIndices.Select(index => itemList[index]).ToList();
            var result = Union(clusterItems.Select(polygonGetter)).Single();
            // Start with the first item as the base for merging
            return factory(clusterItems, result);

        });
    }
    public static IEnumerable<Polygon<T>> Cluster(IEnumerable<Polygon<T>> polygons)
    {
        if (polygons == null! || !polygons!.Any())
            return Array.Empty<Polygon<T>>();

        var polygonList = polygons.ToList();
        var visited = new HashSet<int>();
        var clusters = new List<List<Polygon<T>>>();

        // For each polygon, start a new cluster if not already visited
        for (int i = 0; i < polygonList.Count; i++)
        {
            if (visited.Contains(i))
                continue;

            var cluster = new List<Polygon<T>>();
            var queue = new Queue<int>();
            queue.Enqueue(i);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (!visited.Add(current))
                    continue;

                cluster.Add(polygonList[current]);

                // Enqueue all overlapping polygons
                for (int j = 0; j < polygonList.Count; j++)
                {
                    if (!visited.Contains(j) && polygonList[current].IsOverlapping(polygonList[j])) 
                        queue.Enqueue(j);
                }
            }

            clusters.Add(cluster);
        }

        // Union polygons in each cluster
        return clusters.Select(x => Union(x).Single());
    }
    public bool Equals(Polygon<T> other)
    {
        if (object.ReferenceEquals(_points, other._points)) return true;
        return this._points.SequenceEqual(other._points);
    }

    public override int GetHashCode()
    {
        return _points.GetHashCode();
    }

    /// <summary>
    /// Converts polygon points to Clipper format with scaling for precision
    /// </summary>
    private Path ToClipperPath()
    {
        Path result = new Path(_points.Count);
        foreach (var pt in _points)
        {
            result.Add(new IntPoint(
                Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(pt.X), 1000000m)),
                Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(pt.Y), 1000000m))
            ));
        }
        return result;
    }
    /// <summary>
    /// Merges two polygons using the | operator.
    /// Returns the merged polygon if the result is a single polygon.
    /// Throws InvalidOperationException if the merge would result in multiple disconnected polygons.
    /// </summary>
    public static Polygon<T> operator |(in Polygon<T> a, in Polygon<T> b)
    {
        var results = a.Union(b);
        if (results.Count != 1)
        {
            throw new InvalidOperationException("Cannot merge polygons - operation would result in multiple disconnected polygons");
        }
        return results[0];
    }
    /// <summary>
    /// Intersects this polygon with another polygon.
    /// Returns a list of resulting polygons, as the intersection may create multiple distinct polygons.
    /// </summary>
    public List<Polygon<T>> Intersect(in Polygon<T> other)
    {
        // Convert both polygons to Clipper format
        Path subject = ToClipperPath();
        Path clip = other.ToClipperPath();

        // Create Clipper instance
        Clipper c = new Clipper();
        c.AddPath(subject, PolyType.ptSubject, true);
        c.AddPath(clip, PolyType.ptClip, true);

        // Perform intersection operation
        Paths solution = new Paths();
        c.Execute(ClipType.ctIntersection, solution);

        // Convert results back to our format
        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipperPath(path));
        }

        return results;
    }
    /// <summary>
    /// Performs subtraction between two polygons using the - operator.
    /// Returns the result if it is a single polygon.
    /// Throws InvalidOperationException if the subtraction would result in multiple disconnected polygons.
    /// </summary>
    public static Polygon<T> operator -(in Polygon<T> a, in Polygon<T> b)
    {
        var results = a.Subtract(b);
        if (results.Count != 1)
        {
            throw new InvalidOperationException("Cannot subtract polygons - operation would result in multiple disconnected polygons");
        }
        return results[0];
    }
    /// <summary>
    /// Subtracts another polygon from this polygon.
    /// Returns a list of resulting polygons, as the subtraction may create multiple distinct polygons.
    /// </summary>
    public List<Polygon<T>> Subtract(in Polygon<T> other)
    {
        // Convert both polygons to Clipper format
        Path subject = ToClipperPath();
        Path clip = other.ToClipperPath();

        // Create Clipper instance
        Clipper c = new Clipper();
        c.AddPath(subject, PolyType.ptSubject, true);
        c.AddPath(clip, PolyType.ptClip, true);

        // Perform difference operation
        Paths solution = new Paths();
        c.Execute(ClipType.ctDifference, solution);

        // Convert results back to our format
        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipperPath(path));
        }

        return results;
    }
    /// <summary>
    /// Performs intersection between two polygons using the & operator.
    /// Returns the intersected polygon if the result is a single polygon.
    /// Throws InvalidOperationException if the intersection would result in multiple disconnected polygons.
    /// </summary>
    public static Polygon<T> operator &(in Polygon<T> a,in Polygon<T> b)
    {
        var results = a.Intersect(b);
        if (results.Count != 1)
        {
            throw new InvalidOperationException("Cannot intersect polygons - operation would result in multiple disconnected polygons");
        }
        return results[0];
    }
    /// <summary>
    /// Creates a polygon from a Clipper path, reversing the scaling
    /// </summary>
    private static Polygon<T> FromClipperPath(Path path)
    {
        var resultPoints = new List<Point<T>>(path.Count);
        foreach (IntPoint pt in path)
        {
            resultPoints.Add(new Point<T>(
                T.CreateTruncating(Convert.ToDecimal(pt.X) / 1000000m),
                T.CreateTruncating(Convert.ToDecimal(pt.Y) / 1000000m)
            ));
        }
        return new Polygon<T>(resultPoints);
    }

    /// <summary>
    /// Merges this polygon with another polygon using a boolean union operation.
    /// Returns a list of resulting polygons, as the merge may create multiple distinct polygons.
    /// </summary>
    public List<Polygon<T>> Union(Polygon<T> other)
    {
        // Convert both polygons to Clipper format
        Path subject = ToClipperPath();
        Path clip = other.ToClipperPath();

        // Create Clipper instance
        Clipper c = new Clipper();
        c.AddPath(subject, PolyType.ptSubject, true);
        c.AddPath(clip, PolyType.ptClip, true);

        // Perform union operation
        Paths solution = new Paths();
        c.Execute(ClipType.ctUnion, solution);

        // Convert results back to our format
        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipperPath(path));
        }

        return results;
    }

    /// <summary>
    /// Merges multiple polygons into a single result set using a boolean union operation.
    /// Returns a list of resulting polygons, as the merge may create multiple distinct polygons.
    /// </summary>
    public static List<Polygon<T>> Union(IEnumerable<Polygon<T>> polygons)
    {
        if (!polygons.Any())
            return new List<Polygon<T>>();

        // Convert all polygons to Clipper format
        Paths subj = new Paths();
        foreach (var polygon in polygons)
        {
            subj.Add(polygon.ToClipperPath());
        }

        // Create Clipper instance
        Clipper c = new Clipper();
        c.AddPaths(subj, PolyType.ptSubject, true);

        // Perform union operation
        Paths solution = new Paths();
        c.Execute(ClipType.ctUnion, solution);

        // Convert results back to our format
        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipperPath(path));
        }

        return results;
    }

    public Polygon<T> Intersect(Rectangle<T> rect)
    {
        // Convert polygon points to Clipper format
        Path subj = ToClipperPath();

        // Convert rectangle to polygon in Clipper format
        Path clip = new Path
        {
            new IntPoint(Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Left), 1000000m)),
                        Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Top), 1000000m))),
            new IntPoint(Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Right), 1000000m)),
                        Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Top), 1000000m))),
            new IntPoint(Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Right), 1000000m)),
                        Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Bottom), 1000000m))),
            new IntPoint(Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Left), 1000000m)),
                        Convert.ToInt64(decimal.Multiply(Convert.ToDecimal(rect.Bottom), 1000000m)))
        };

        // Create Clipper instance
        Clipper c = new Clipper();
        c.AddPath(subj, PolyType.ptSubject, true);
        c.AddPath(clip, PolyType.ptClip, true);

        // Perform intersection
        Paths solution = new Paths();
        c.Execute(ClipType.ctIntersection, solution);

        // If no intersection, return empty polygon
        if (solution.Count == 0)
            return new Polygon<T>(new List<Point<T>>());

        // Convert result back to our format
        return FromClipperPath(solution[0]);
    }

    // Sutherland-Hodgman polygon clipping
    //private static List<Point<T>> ClipPolygon(IList<Point<T>> polygon, Point<T> edgeStart, Point<T> edgeEnd)
    //{
    //    List<Point<T>> clippedPolygon = new List<Point<T>>();

    //    for (int i = 0; i < polygon.Count; i++)
    //    {
    //        Point<T> currentPoint = polygon[i];
    //        Point<T> prevPoint = polygon[(i - 1 + polygon.Count) % polygon.Count];

    //        bool currentInside = IsInside(currentPoint, edgeStart, edgeEnd);
    //        bool prevInside = IsInside(prevPoint, edgeStart, edgeEnd);

    //        if (currentInside && prevInside)
    //        {
    //            // Both points are inside, add current point
    //            clippedPolygon.Add(currentPoint);
    //        }
    //        else if (!currentInside && prevInside)
    //        {
    //            // Leaving the clip area, add intersection point
    //            clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
    //        }
    //        else if (currentInside && !prevInside)
    //        {
    //            // Entering the clip area, add intersection point and current point
    //            clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
    //            clippedPolygon.Add(currentPoint);
    //        }
    //    }

    //    return clippedPolygon;
    //}

    // Helper function to check if a point is inside the clipping edge
    //private static bool IsInside(Point<T> p, Point<T> edgeStart, Point<T> edgeEnd)
    //{
    //    return (edgeEnd.X - edgeStart.X) * (p.Y - edgeStart.Y) > (edgeEnd.Y - edgeStart.Y) * (p.X - edgeStart.X);
    //}

    //// Calculate intersection of line segment (p1, p2) with edge (edgeStart, edgeEnd)
    //private static Point<T> GetIntersection(Point<T> p1, Point<T> p2, Point<T> edgeStart, Point<T> edgeEnd)
    //{
    //    T A1 = p2.Y - p1.Y;
    //    T B1 = p1.X - p2.X;
    //    T C1 = A1 * p1.X + B1 * p1.Y;

    //    T A2 = edgeEnd.Y - edgeStart.Y;
    //    T B2 = edgeStart.X - edgeEnd.X;
    //    T C2 = A2 * edgeStart.X + B2 * edgeStart.Y;

    //    T det = A1 * B2 - A2 * B1;

    //    if (T.Abs(det) <= T.Epsilon) // Lines are parallel
    //    {
    //        return new Point<T>(); // No intersection
    //    }

    //    T x = (B2 * C1 - B1 * C2) / det;
    //    T y = (A1 * C2 - A2 * C1) / det;

    //    return new Point<T>(x, y);
    //}

    public static Polygon<T> operator -(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x - f).ToList(a._points.Count));
    }

    public static Polygon<T> operator +(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x + f).ToList(a._points.Count));
    }

    /// <summary>
    ///  Adds point at the end of the polygon.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Polygon<T> operator +(Polygon<T> a, ModelingEvolution.Drawing.Point<T> f)
    {
        var ret = new Polygon<T>(a.Points.ToList(a._points.Count + 1));
        ret._points.Add(f);
        return ret;
    }


    public void InsertAt(int index, Point<T> point) => _points.Insert(index, point);
    public void Add(int index, Point<T> point) => _points.Add(point);
    public void RemoveAt(int index) => _points.RemoveAt(index);

    public Polygon(IList<Point<T>> points)
    {
        _points = points;
    }

    public Polygon() : this(Array.Empty<T>())
    {
        
    }
    public Polygon(params Point<T>[] points) : this(points.ToList())
    {
        
    }

    public Polygon(IReadOnlyList<T> points)
    {
        _points = new List<Point<T>>(points.Count / 2);
        for (int i = 0; i < points.Count; i += 2)
        {
            _points.Add(new Point<T>(points[i], points[i + 1]));
        }
    }

    [JsonIgnore]
    public int Count => _points.Count;

    public Point<T> this[int index]
    {
        get { return _points[index]; }
        set => _points[index] = value;  
    }

    public IReadOnlyList<Point<T>> Points => (IReadOnlyList<Point<T>>)_points;

    
}