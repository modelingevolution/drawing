using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Clipper2Lib;
using ModelingEvolution.Drawing.Svg;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

[SvgExporterAttribute(typeof(PolygonSvgExporterFactory))]
[JsonConverter(typeof(PolygonJsonConverterFactory))]
[ProtoContract]
/// <summary>
/// Represents an immutable polygon defined by a collection of points backed by ReadOnlyMemory.
/// Supports geometric operations including area calculation, boolean operations, and transformations.
/// Mutation methods (Add, InsertAt, RemoveAt) return new polygon instances.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly record struct Polygon<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)]
    internal readonly Point<T>[] _points;

    // Tracks actual element count; 0 means use _points.Length (for deserialization compat)
    [ProtoMember(2)]
    internal readonly int _length;

    // Offset into _points for pool-backed slices; 0 for owned arrays and deserialization compat
    [ProtoMember(3)]
    internal readonly int _offset;

    /// <summary>
    /// Gets the number of vertices in this polygon.
    /// </summary>
    [JsonIgnore]
    public int Count => _points != null
        ? (_length > 0 ? _length : _points.Length)
        : 0;

    /// <summary>
    /// Gets a read-only span over the polygon's vertices. This is the preferred high-performance access path.
    /// </summary>
    public ReadOnlySpan<Point<T>> Span => _points != null
        ? (_length > 0 ? _points.AsSpan(_offset, _length) : _points.AsSpan())
        : ReadOnlySpan<Point<T>>.Empty;

    /// <summary>
    /// Gets a ReadOnlyMemory over the polygon's vertices.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Memory => _points != null
        ? (_length > 0 ? new ReadOnlyMemory<Point<T>>(_points, _offset, _length) : _points.AsMemory())
        : ReadOnlyMemory<Point<T>>.Empty;

    /// <summary>
    /// Gets the vertex at the specified index.
    /// </summary>
    public Point<T> this[int index] => Span[index];

    /// <summary>
    /// Gets a read-only list of all vertices. For high-performance code, prefer Span instead.
    /// </summary>
    public IReadOnlyList<Point<T>> Points
    {
        get
        {
            if (_points == null) return Array.Empty<Point<T>>();
            if (_offset == 0 && (_length == 0 || _length == _points.Length)) return _points;
            // Pool-backed or offset slice: copy only the valid portion
            return Span.ToArray();
        }
    }

    [JsonIgnore]
    public bool IsReadOnly => true;

    #region Constructors

    /// <summary>
    /// Initializes a new empty polygon.
    /// </summary>
    public Polygon()
    {
        _points = Array.Empty<Point<T>>();
        _offset = 0;
        _length = 0;
    }

    /// <summary>
    /// Initializes a new polygon wrapping a ReadOnlyMemory of points. Zero-copy — requires array-backed memory.
    /// Supports non-zero offsets for pooled buffer slices (e.g., multiple polygons sharing one ArrayPool buffer).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the memory is not backed by an array.</exception>
    public Polygon(ReadOnlyMemory<Point<T>> memory)
    {
        if (memory.Length == 0)
        {
            _points = Array.Empty<Point<T>>();
            _offset = 0;
            _length = 0;
            return;
        }

        if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<Point<T>> segment))
            throw new ArgumentException(
                "Memory must be backed by an array. " +
                "Use MemoryPool<T>.Shared or pass a Point<T>[] directly.", nameof(memory));

        _points = segment.Array!;
        _offset = segment.Offset;
        _length = segment.Count;
    }

    /// <summary>
    /// Initializes a new polygon from the specified points array.
    /// </summary>
    public Polygon(params Point<T>[] points)
    {
        _points = points;
        _offset = 0;
        _length = points.Length;
    }

    /// <summary>
    /// Initializes a new polygon from the specified list of points (copies to array).
    /// </summary>
    public Polygon(IList<Point<T>> points)
    {
        _offset = 0;
        if (points is Point<T>[] arr)
        {
            _points = arr;
            _length = arr.Length;
        }
        else
        {
            _points = new Point<T>[points.Count];
            points.CopyTo(_points, 0);
            _length = _points.Length;
        }
    }

    /// <summary>
    /// Initializes a new polygon from a flat list of coordinate values (x1, y1, x2, y2, ...).
    /// </summary>
    public Polygon(IReadOnlyList<T> coords)
    {
        _points = new Point<T>[coords.Count / 2];
        _offset = 0;
        for (int i = 0; i < coords.Count; i += 2)
            _points[i / 2] = new Point<T>(coords[i], coords[i + 1]);
        _length = _points.Length;
    }

    #endregion

    #region Immutable mutations

    /// <summary>
    /// Returns a new polygon with the specified point appended.
    /// </summary>
    public Polygon<T> Add(Point<T> point)
    {
        var span = Span;
        var newPoints = new Point<T>[span.Length + 1];
        span.CopyTo(newPoints);
        newPoints[span.Length] = point;
        return new Polygon<T>(newPoints);
    }

    /// <summary>
    /// Returns a new polygon with the specified point appended, using pooled memory.
    /// Caller must dispose the returned IMemoryOwner when the polygon is no longer needed.
    /// </summary>
    public Polygon<T> Add(Point<T> point, MemoryPool<Point<T>> pool, out IMemoryOwner<Point<T>> owner)
    {
        var span = Span;
        int newLength = span.Length + 1;
        owner = pool.Rent(newLength);
        var dest = owner.Memory.Span;
        span.CopyTo(dest);
        dest[span.Length] = point;
        return new Polygon<T>(owner.Memory.Slice(0, newLength));
    }

    /// <summary>
    /// Returns a new polygon with the specified point inserted at the given index.
    /// </summary>
    public Polygon<T> InsertAt(int index, Point<T> point)
    {
        var span = Span;
        var newPoints = new Point<T>[span.Length + 1];
        span[..index].CopyTo(newPoints);
        newPoints[index] = point;
        span[index..].CopyTo(newPoints.AsSpan(index + 1));
        return new Polygon<T>(newPoints);
    }

    /// <summary>
    /// Returns a new polygon with the specified point inserted at the given index, using pooled memory.
    /// Caller must dispose the returned IMemoryOwner when the polygon is no longer needed.
    /// </summary>
    public Polygon<T> InsertAt(int index, Point<T> point, MemoryPool<Point<T>> pool, out IMemoryOwner<Point<T>> owner)
    {
        var span = Span;
        int newLength = span.Length + 1;
        owner = pool.Rent(newLength);
        var dest = owner.Memory.Span;
        span[..index].CopyTo(dest);
        dest[index] = point;
        span[index..].CopyTo(dest[(index + 1)..]);
        return new Polygon<T>(owner.Memory.Slice(0, newLength));
    }

    /// <summary>
    /// Returns a new polygon with the point at the specified index removed.
    /// </summary>
    public Polygon<T> RemoveAt(int index)
    {
        var span = Span;
        var newPoints = new Point<T>[span.Length - 1];
        span[..index].CopyTo(newPoints);
        span[(index + 1)..].CopyTo(newPoints.AsSpan(index));
        return new Polygon<T>(newPoints);
    }

    /// <summary>
    /// Returns a new polygon with the point at the specified index removed, using pooled memory.
    /// Caller must dispose the returned IMemoryOwner when the polygon is no longer needed.
    /// </summary>
    public Polygon<T> RemoveAt(int index, MemoryPool<Point<T>> pool, out IMemoryOwner<Point<T>> owner)
    {
        var span = Span;
        int newLength = span.Length - 1;
        owner = pool.Rent(newLength);
        var dest = owner.Memory.Span;
        span[..index].CopyTo(dest);
        span[(index + 1)..].CopyTo(dest[index..]);
        return new Polygon<T>(owner.Memory.Slice(0, newLength));
    }

    #endregion

    #region Geometry

    /// <summary>
    /// Calculates the area of this polygon using the shoelace formula.
    /// </summary>
    public T Area()
    {
        var span = Span;
        int n = span.Length;
        if (n < 3)
            throw new ArgumentException("A polygon must have at least 3 points.");

        T area = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var current = span[i];
            var next = span[(i + 1) % n];
            area += current.X * next.Y;
            area -= current.Y * next.X;
        }

        area = T.Abs(area) / (T.One + T.One);
        return area;
    }

    /// <summary>
    /// Computes the bounding box that encloses all points of this polygon.
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        var span = Span;
        if (span.Length == 0)
            return new Rectangle<T>(new Point<T>(T.Zero, T.Zero), new Size<T>(T.Zero, T.Zero));

        T minX = span[0].X, minY = span[0].Y;
        T maxX = span[0].X, maxY = span[0].Y;

        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].X < minX) minX = span[i].X;
            if (span[i].Y < minY) minY = span[i].Y;
            if (span[i].X > maxX) maxX = span[i].X;
            if (span[i].Y > maxY) maxY = span[i].Y;
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Determines whether the polygon contains the specified point in its vertex collection.
    /// </summary>
    public bool Contains(in Point<T> item)
    {
        var span = Span;
        for (int i = 0; i < span.Length; i++)
            if (span[i].Equals(item)) return true;
        return false;
    }

    /// <summary>
    /// Determines whether this polygon overlaps with another polygon.
    /// </summary>
    public bool IsOverlapping(in Polygon<T> other)
    {
        return this.Intersect(other).Count > 0;
    }

    /// <summary>
    /// Simplifies the polygon by removing points that are closer than epsilon to the line segments.
    /// </summary>
    public Polygon<T> Simplify(T epsilon)
    {
        if (Count <= 3)
            return this;

        var path = ToClipper2Path();
        var simplified = Clipper.SimplifyPath(path, Convert.ToDouble(epsilon));

        return simplified.Count < 3 ? this : FromClipper2Path(simplified);
    }

    /// <summary>
    /// Finds all segments where the given infinite line intersects this polygon.
    /// For convex polygons, this yields 0 or 1 segment. For concave polygons, multiple segments.
    /// </summary>
    public IReadOnlyList<Segment<T>> Intersections(Line<T> line) => Drawing.Intersections.Of(line, this);

    /// <summary>
    /// Finds the first segment where the given infinite line intersects this polygon.
    /// Returns null if the line does not intersect the polygon.
    /// Optimized: zero heap allocation.
    /// </summary>
    public Segment<T>? FirstIntersection(Line<T> line) => Drawing.Intersections.FirstOf(line, this);

    #endregion

    #region Conversions

    public static explicit operator Rectangle<T>(Polygon<T> t)
    {
        return t.BoundingBox();
    }

    public static implicit operator Polygon<T>(Rectangle<T> t)
    {
        return new Polygon<T>(t.Points().ToArray());
    }

    #endregion

    #region Operators

    public static Polygon<T> operator *(in Polygon<T> a, in Size<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] * f;
        return new Polygon<T>(points);
    }

    public static Polygon<T> operator /(in Polygon<T> a, in Size<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] / f;
        return new Polygon<T>(points);
    }

    public static Polygon<T> operator -(in Polygon<T> a, in Vector<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] - f;
        return new Polygon<T>(points);
    }

    public static Polygon<T> operator +(in Polygon<T> a, in Vector<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] + f;
        return new Polygon<T>(points);
    }

    /// <summary>
    /// Returns a new polygon with the specified point appended.
    /// </summary>
    public static Polygon<T> operator +(in Polygon<T> a, in Point<T> f)
    {
        return a.Add(f);
    }

    /// <summary>
    /// Merges two polygons using the | operator.
    /// </summary>
    public static Polygon<T> operator |(in Polygon<T> a, in Polygon<T> b)
    {
        var results = Union([a, b], true);
        if (results.Count != 1)
            throw new InvalidOperationException("Cannot merge polygons - operation would result in multiple disconnected polygons");
        return results[0];
    }

    /// <summary>
    /// Subtracts polygon b from polygon a using the - operator.
    /// </summary>
    public static Polygon<T> operator -(in Polygon<T> a, in Polygon<T> b)
    {
        var results = a.Subtract(b);
        if (results.Count != 1)
            throw new InvalidOperationException("Cannot subtract polygons - operation would result in multiple disconnected polygons");
        return results[0];
    }

    /// <summary>
    /// Intersects two polygons using the &amp; operator.
    /// </summary>
    public static Polygon<T> operator &(in Polygon<T> a, in Polygon<T> b)
    {
        var results = a.Intersect(b);
        if (results.Count != 1)
            throw new InvalidOperationException("Cannot intersect polygons - operation would result in multiple disconnected polygons");
        return results[0];
    }

    #endregion

    #region Clipper2 boolean operations

    private static Polygon<T> FromClipper2Path(PathD path)
    {
        var points = new Point<T>[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            points[i] = new Point<T>(
                T.CreateTruncating(path[i].x),
                T.CreateTruncating(path[i].y));
        }
        return new Polygon<T>(points);
    }

    private PathD ToClipper2Path()
    {
        var span = Span;
        var path = new PathD(span.Length);
        for (int i = 0; i < span.Length; i++)
        {
            path.Add(new PointD(
                Convert.ToDouble(span[i].X),
                Convert.ToDouble(span[i].Y)));
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

    public List<Polygon<T>> Intersect(in Polygon<T> other)
    {
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathsD { other.ToClipper2Path() };
        var solution = Clipper.Intersect(subject, clip, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
            results.Add(FromClipper2Path(path));
        return results;
    }

    public List<Polygon<T>> Subtract(in Polygon<T> other)
    {
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathsD { other.ToClipper2Path() };
        var solution = Clipper.Difference(subject, clip, FillRule.NonZero);

        var results = new List<Polygon<T>>(solution.Count);
        foreach (var path in solution)
            results.Add(FromClipper2Path(path));
        return results;
    }

    public IEnumerable<Polygon<T>> Intersect(Rectangle<T> rect)
    {
        var subject = new PathsD { ToClipper2Path() };
        var clip = new PathD
        {
            new PointD(Convert.ToDouble(rect.Left), Convert.ToDouble(rect.Top)),
            new PointD(Convert.ToDouble(rect.Right), Convert.ToDouble(rect.Top)),
            new PointD(Convert.ToDouble(rect.Right), Convert.ToDouble(rect.Bottom)),
            new PointD(Convert.ToDouble(rect.Left), Convert.ToDouble(rect.Bottom))
        };

        var solution = Clipper.Intersect(subject, new PathsD { clip }, FillRule.NonZero);

        if (solution.Count == 0)
            yield break;

        foreach (var i in solution)
            yield return FromClipper2Path(i);
    }

    #endregion

    #region Static union / cluster operations

    public static Polygon<T> UnionRecursive(IEnumerable<Polygon<T>> polygons)
    {
        var array = polygons.ToArray();
        var tmp = array[0];
        for (int i = 1; i < array.Length; i++)
            tmp = tmp.Union(array[i], true).Single();
        return tmp;
    }

    public static List<Polygon<T>> Union(IEnumerable<Polygon<T>> polygons, bool removeHoles = false)
    {
        if (!polygons.Any())
            return new List<Polygon<T>>();

        var subject = new PathsD();
        foreach (var polygon in polygons)
            subject.Add(polygon.ToClipper2Path());

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

        return clusters.Select(clusterIndices =>
        {
            var clusterItems = clusterIndices.Select(index => itemList[index]).ToList();
            var result = Union(clusterItems.Select(polygonGetter), true).Single();
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

        return clusters.Select(clusterIndices =>
        {
            var clusterItems = clusterIndices.Select(index => itemList[index]).ToList();
            var result = UnionRecursive(clusterItems.Select(polygonGetter));
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

                for (int j = 0; j < polygonList.Count; j++)
                {
                    if (!visited.Contains(j) && polygonList[current].IsOverlapping(polygonList[j]))
                        queue.Enqueue(j);
                }
            }

            clusters.Add(cluster);
        }

        return clusters.Select(x => Union(x, true).Single());
    }

    public static IEnumerable<Polygon<T>> ClusterRecursive(IEnumerable<Polygon<T>> polygons)
    {
        if (polygons == null! || !polygons!.Any())
            return Array.Empty<Polygon<T>>();

        var polygonList = polygons.ToList();
        var visited = new HashSet<int>();
        var clusters = new List<List<Polygon<T>>>();

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

                for (int j = 0; j < polygonList.Count; j++)
                {
                    if (!visited.Contains(j) && polygonList[current].IsOverlapping(polygonList[j]))
                        queue.Enqueue(j);
                }
            }

            clusters.Add(cluster);
        }

        return clusters.Select(x => UnionRecursive(x));
    }

    #endregion

    #region Equality

    public bool Equals(Polygon<T> other) => Span.SequenceEqual(other.Span);

    public override int GetHashCode()
    {
        var hash = new System.HashCode();
        var span = Span;
        for (int i = 0; i < span.Length; i++)
            hash.Add(span[i]);
        return hash.ToHashCode();
    }

    #endregion

    /// <summary>
    /// Returns the backing data as a correctly-sized array. Allocates if pool-backed.
    /// </summary>
    internal Point<T>[] AsArray()
    {
        if (_points == null) return Array.Empty<Point<T>>();
        if (_offset == 0 && (_length == 0 || _length == _points.Length)) return _points;
        return Span.ToArray();
    }
}
