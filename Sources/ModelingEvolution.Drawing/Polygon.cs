using ProtoBuf;
using System.Collections;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using ModelingEvolution.Drawing.Svg;
using System.Collections.Generic;

using System.IO;
using Clipper2Lib;

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
    [ProtoMember(1)] 
    internal readonly IList<Point<T>> _points;

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
    public static explicit operator Rectangle<T>(Polygon<T> t)
    {
        return t.BoundingBox();
    }
    public static implicit operator Polygon<T>(Rectangle<T> t)
    {
        return new Polygon<T>(t.Points().ToList());
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
    public bool Contains(in Point<T> item)
    {
        return _points.Contains(item);
    }
    [JsonIgnore]
    public bool IsReadOnly => true;

    public static Polygon<T> operator *(in Polygon<T> a,in Size<T> f)
    {
        var points = new List<Point<T>>(a._points.Count);
        for (var index = 0; index < a.Points.Count; index++)
        {
            var point = a.Points[index];
            points.Add(point * f);
        }

        return new Polygon<T>(points);
    }

    public static Polygon<T> operator /(in Polygon<T> a,in Size<T> f)
    {
        var points = new List<Point<T>>(a._points.Count);
        for (var index = 0; index < a.Points.Count; index++)
        {
            var point = a.Points[index];
            points.Add(point / f);
        }

        return new Polygon<T>(points);
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
            var result = Union(clusterItems.Select(polygonGetter), true).Single();
            // Start with the first item as the base for merging
            return factory(clusterItems, result);

        });
    }
    public static IEnumerable<D> ClusterRecursive<D>(
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
            var result = UnionRecursive(clusterItems.Select(polygonGetter));
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
        return clusters.Select(x => Union(x, true).Single());
    }
    public static IEnumerable<Polygon<T>> ClusterRecursive(IEnumerable<Polygon<T>> polygons)
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
        return clusters.Select(x => UnionRecursive(x));
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
    private static Polygon<T> FromClipper2Path(PathD path)
    {
        var resultPoints = new List<Point<T>>(path.Count);
        foreach (PointD pt in path)
        {
            resultPoints.Add(new Point<T>(
                T.CreateTruncating(pt.x),
                T.CreateTruncating(pt.y)
            ));
        }
        return new Polygon<T>(resultPoints);
    }
    private PathD ToClipper2Path()
    {
        var path = new PathD(_points.Count);
        foreach (var pt in _points)
        {
            path.Add(new PointD(
                Convert.ToDouble(pt.X),
                Convert.ToDouble(pt.Y)
            ));
        }

        if (!Clipper.IsPositive(path)) path.Reverse();
        return path;
    }
    public List<Polygon<T>> Union2(in Polygon<T> other, bool removeHoles = false)
    {
        var subject = new PathsD { ToClipper2Path(), other.ToClipper2Path() };
        
        var solution = Clipper.Union(subject, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            if (!Clipper.IsPositive(path) && removeHoles)
                break;
            results.Add(FromClipper2Path(path));
        }

        return results;
    }
    public List<Polygon<T>> Union(in Polygon<T> other, bool removeHoles = false)
    {
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathsD { other.ToClipper2Path() };

        
        var solution = Clipper.Union(subject, clip, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            if (!Clipper.IsPositive(path) && removeHoles)
                break;
            results.Add(FromClipper2Path(path));
        }

        return results;
    }
    
    /// <summary>
    /// Merges two polygons using the | operator.
    /// Returns the merged polygon if the result is a single polygon.
    /// Throws InvalidOperationException if the merge would result in multiple disconnected polygons.
    /// </summary>
    public static Polygon<T> operator |(in Polygon<T> a, in Polygon<T> b)
    {
        var results = Union([a,b], true);
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
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathsD { other.ToClipper2Path() };

        var solution = Clipper.Intersect(subject, clip, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipper2Path(path));
        }

        return results;
    }
    public List<Polygon<T>> Subtract(in Polygon<T> other)
    {
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathsD { other.ToClipper2Path() };

        var solution = Clipper.Difference(subject, clip, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            results.Add(FromClipper2Path(path));
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

    public static Polygon<T> UnionRecursive(IEnumerable<Polygon<T>> polygons)
    {
        var array = polygons.ToArray();
        var tmp = array[0];

        for (int i = 1; i < array.Length; i++)
        {
            tmp = tmp.Union(array[i], true).Single();
        }

        return tmp;
    }

    public static List<Polygon<T>> Union(IEnumerable<Polygon<T>> polygons, bool removeHoles = false)
    {
        if (!polygons.Any())
            return new List<Polygon<T>>();

        var subject = new PathsD();
        foreach (var polygon in polygons)
        {
            subject.Add(polygon.ToClipper2Path());
        }

        var solution = Clipper.Union(subject, FillRule.NonZero);

        
        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
        {
            if (!Clipper.IsPositive(path) && removeHoles)
                break;

            var simplified = Clipper.SimplifyPath(path, Double.Epsilon, true);
            results.Add(FromClipper2Path(simplified));
            
        }

        return results;
    }


    /// <summary>
    /// Simplifies the polygon by removing points that are closer than epsilon to the line segments.
    /// </summary>
    /// <param name="epsilon">The maximum distance between the original and simplified curves.</param>
    /// <returns>A new simplified polygon.</returns>
    public Polygon<T> Simplify(T epsilon)
    {
        if (_points.Count <= 3)
            return this;

        var path = ToClipper2Path();
        var simplified = Clipper.SimplifyPath(path, Convert.ToDouble(epsilon));

        // If simplification resulted in too few points, return original
        return simplified.Count < 3 ? this : FromClipper2Path(simplified);
    }

    

    public Polygon<T> Intersect(in Rectangle<T> rect)
    {
        var subject = new PathsD { ToClipper2Path() };

        var clip = new PathD {
            new PointD(Convert.ToDouble(rect.Left), Convert.ToDouble(rect.Top)),
            new PointD(Convert.ToDouble(rect.Right), Convert.ToDouble(rect.Top)),
            new PointD(Convert.ToDouble(rect.Right), Convert.ToDouble(rect.Bottom)),
            new PointD(Convert.ToDouble(rect.Left), Convert.ToDouble(rect.Bottom))
        };

        var solution = Clipper.Intersect(subject, new PathsD { clip }, FillRule.NonZero);

        if (solution.Count == 0)
            return new Polygon<T>(new List<Point<T>>());

        return FromClipper2Path(solution[0]);
    }

    

    public static Polygon<T> operator -(in Polygon<T> a, in ModelingEvolution.Drawing.Vector<T> f)
    {
        var newPoints = new List<Point<T>>(a._points.Count);
        for (int i = 0; i < a._points.Count; i++)
        {
            newPoints.Add(a._points[i] - f);
        }
        return new Polygon<T>(newPoints);
    }

    public static Polygon<T> operator +(in Polygon<T> a, in ModelingEvolution.Drawing.Vector<T> f)
    {
        var newPoints = new List<Point<T>>(a._points.Count);
        for (int i = 0; i < a._points.Count; i++)
        {
            newPoints.Add(a._points[i] + f);
        }
        return new Polygon<T>(newPoints);
    }

    /// <summary>
    ///  Adds point at the end of the polygon.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Polygon<T> operator +(in Polygon<T> a, in ModelingEvolution.Drawing.Point<T> f)
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
    public int Count => _points?.Count ?? 0;

    public Point<T> this[int index]
    {
        get { return _points[index]; }
        set => _points[index] = value;  
    }

    public IReadOnlyList<Point<T>> Points => (IReadOnlyList<Point<T>>)(_points ?? Array.Empty<Point<T>>());

    
}