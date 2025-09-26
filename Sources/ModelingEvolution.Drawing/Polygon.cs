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
/// Represents a polygon defined by a collection of points. This struct supports various geometric operations
/// including area calculation, boolean operations, and transformations.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly record struct Polygon<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)] 
    internal readonly IList<Point<T>> _points;

    /// <summary>
    /// Calculates the area of this polygon using the shoelace formula.
    /// </summary>
    /// <returns>The area of the polygon.</returns>
    /// <exception cref="ArgumentException">Thrown when the polygon has fewer than 3 points.</exception>
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
    /// <summary>
    /// Explicitly converts a polygon to its bounding rectangle.
    /// </summary>
    /// <param name="t">The polygon to convert.</param>
    /// <returns>The bounding rectangle of the polygon.</returns>
    public static explicit operator Rectangle<T>(Polygon<T> t)
    {
        return t.BoundingBox();
    }
    /// <summary>
    /// Implicitly converts a rectangle to a polygon with four corner points.
    /// </summary>
    /// <param name="t">The rectangle to convert.</param>
    /// <returns>A polygon representing the rectangle.</returns>
    public static implicit operator Polygon<T>(Rectangle<T> t)
    {
        return new Polygon<T>(t.Points().ToList());
    }
    /// <summary>
    /// Computes the bounding box (axis-aligned rectangle) that encloses all points of this polygon.
    /// </summary>
    /// <returns>The smallest rectangle that contains all points of the polygon.</returns>
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
    /// <summary>
    /// Determines whether the polygon contains the specified point in its vertex collection.
    /// </summary>
    /// <param name="item">The point to locate in the polygon.</param>
    /// <returns>true if the point is found in the polygon's vertices; otherwise, false.</returns>
    public bool Contains(in Point<T> item)
    {
        return _points.Contains(item);
    }
    /// <summary>
    /// Gets a value indicating whether the polygon is read-only. Always returns true.
    /// </summary>
    [JsonIgnore]
    public bool IsReadOnly => true;

    /// <summary>
    /// Multiplies (scales) a polygon by a size, scaling each vertex.
    /// </summary>
    /// <param name="a">The polygon to scale.</param>
    /// <param name="f">The size to scale by.</param>
    /// <returns>A new polygon with scaled vertices.</returns>
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

    /// <summary>
    /// Divides (scales down) a polygon by a size, scaling each vertex.
    /// </summary>
    /// <param name="a">The polygon to scale.</param>
    /// <param name="f">The size to divide by.</param>
    /// <returns>A new polygon with scaled vertices.</returns>
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
    /// <summary>
    /// Determines whether this polygon overlaps with another polygon.
    /// </summary>
    /// <param name="other">The other polygon to check for overlap.</param>
    /// <returns>true if the polygons overlap; otherwise, false.</returns>
    public bool IsOverlapping(in Polygon<T> other)
    {
        return this.Intersect(other).Count > 0;
    }
    /// <summary>
    /// Clusters items based on their polygon representations, grouping overlapping items together.
    /// </summary>
    /// <typeparam name="D">The type of items to cluster.</typeparam>
    /// <param name="items">The items to cluster.</param>
    /// <param name="polygonGetter">Function to extract polygon from each item.</param>
    /// <param name="factory">Function to create a clustered item from a group of items and their union polygon.</param>
    /// <returns>An enumerable of clustered items.</returns>
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
    /// <summary>
    /// Recursively clusters items based on their polygon representations, grouping overlapping items together.
    /// Uses recursive union operations for merging polygon clusters.
    /// </summary>
    /// <typeparam name="D">The type of items to cluster.</typeparam>
    /// <param name="items">The items to cluster.</param>
    /// <param name="polygonGetter">Function to extract polygon from each item.</param>
    /// <param name="factory">Function to create a clustered item from a group of items and their union polygon.</param>
    /// <returns>An enumerable of clustered items with recursively merged polygons.</returns>
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
    /// <summary>
    /// Clusters polygons by grouping overlapping ones together and merging each cluster into a single polygon.
    /// </summary>
    /// <param name="polygons">The polygons to cluster.</param>
    /// <returns>An enumerable of clustered polygons, where each result represents a merged cluster.</returns>
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
    /// <summary>
    /// Recursively clusters polygons by grouping overlapping ones together and merging each cluster using recursive union operations.
    /// </summary>
    /// <param name="polygons">The polygons to cluster.</param>
    /// <returns>An enumerable of clustered polygons, where each result represents a recursively merged cluster.</returns>
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
    /// <summary>
    /// Determines whether the specified polygon is equal to this polygon by comparing their point sequences.
    /// </summary>
    /// <param name="other">The polygon to compare with this polygon.</param>
    /// <returns>true if the specified polygon is equal to this polygon; otherwise, false.</returns>
    public bool Equals(Polygon<T> other)
    {
        if (object.ReferenceEquals(_points, other._points)) return true;
        return this._points.SequenceEqual(other._points);
    }

    /// <summary>
    /// Returns the hash code for this polygon based on its point collection.
    /// </summary>
    /// <returns>A hash code for this polygon.</returns>
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
    /// <summary>
    /// Computes the union of this polygon with another polygon using an alternative algorithm.
    /// </summary>
    /// <param name="other">The other polygon to union with.</param>
    /// <param name="removeHoles">Whether to remove holes from the result.</param>
    /// <returns>A list of resulting polygons from the union operation.</returns>
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
    /// <summary>
    /// Computes the union of this polygon with another polygon.
    /// </summary>
    /// <param name="other">The other polygon to union with.</param>
    /// <param name="removeHoles">Whether to remove holes from the result.</param>
    /// <returns>A list of resulting polygons from the union operation.</returns>
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
    /// <param name="other">The polygon to intersect with this polygon.</param>
    /// <returns>A list of polygons representing the intersection areas.</returns>
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
    /// <summary>
    /// Subtracts another polygon from this polygon (boolean difference operation).
    /// </summary>
    /// <param name="other">The polygon to subtract from this polygon.</param>
    /// <returns>A list of resulting polygons from the subtraction operation.</returns>
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
    /// <param name="a">The polygon to subtract from.</param>
    /// <param name="b">The polygon to subtract.</param>
    /// <returns>The resulting polygon after subtraction.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the subtraction results in multiple disconnected polygons.</exception>
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
    /// Performs intersection between two polygons using the &amp; operator.
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
    /// Computes the union of multiple polygons using recursive operations, merging them one by one.
    /// </summary>
    /// <param name="polygons">The collection of polygons to union.</param>
    /// <returns>A single polygon representing the union of all input polygons.</returns>
    /// <exception cref="InvalidOperationException">Thrown when polygons collection is empty or contains no polygons.</exception>
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

    /// <summary>
    /// Computes the union of multiple polygons using batch operations.
    /// </summary>
    /// <param name="polygons">The collection of polygons to union.</param>
    /// <param name="removeHoles">Whether to remove holes from the result polygons.</param>
    /// <returns>A list of polygons representing the union of all input polygons.</returns>
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

    

    /// <summary>
    /// Computes the intersection of this polygon with the specified rectangle.
    /// Returns the intersection areas as a collection of polygons.
    /// </summary>
    /// <param name="rect">The rectangle to intersect with this polygon.</param>
    /// <returns>An enumerable collection of polygons representing the intersection areas.</returns>
    public IEnumerable<Polygon<T>> Intersect( Rectangle<T> rect)
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
            yield break;

        foreach(var i in solution)
            yield return FromClipper2Path(i);
    }

    

    /// <summary>
    /// Translates a polygon by subtracting a vector from each vertex.
    /// </summary>
    /// <param name="a">The polygon to translate.</param>
    /// <param name="f">The vector to subtract from each vertex.</param>
    /// <returns>A new polygon with all vertices translated by the negative of the vector.</returns>
    public static Polygon<T> operator -(in Polygon<T> a, in ModelingEvolution.Drawing.Vector<T> f)
    {
        var newPoints = new List<Point<T>>(a._points.Count);
        for (int i = 0; i < a._points.Count; i++)
        {
            newPoints.Add(a._points[i] - f);
        }
        return new Polygon<T>(newPoints);
    }

    /// <summary>
    /// Translates a polygon by adding a vector to each vertex.
    /// </summary>
    /// <param name="a">The polygon to translate.</param>
    /// <param name="f">The vector to add to each vertex.</param>
    /// <returns>A new polygon with all vertices translated by the vector.</returns>
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
    /// Adds a point at the end of the polygon, creating a new polygon with an additional vertex.
    /// </summary>
    /// <param name="a">The polygon to add the point to.</param>
    /// <param name="f">The point to add to the polygon.</param>
    /// <returns>A new polygon with the specified point added as the last vertex.</returns>
    public static Polygon<T> operator +(in Polygon<T> a, in ModelingEvolution.Drawing.Point<T> f)
    {
        var ret = new Polygon<T>(a.Points.ToList(a._points.Count + 1));
        ret._points.Add(f);
        return ret;
    }


    /// <summary>
    /// Inserts a point at the specified index in the polygon's vertex collection.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the point.</param>
    /// <param name="point">The point to insert.</param>
    public void InsertAt(int index, Point<T> point) => _points.Insert(index, point);
    
    /// <summary>
    /// Adds a point to the end of the polygon's vertex collection.
    /// </summary>
    /// <param name="index">This parameter is not used and will be ignored.</param>
    /// <param name="point">The point to add.</param>
    public void Add(int index, Point<T> point) => _points.Add(point);
    
    /// <summary>
    /// Removes the point at the specified index from the polygon's vertex collection.
    /// </summary>
    /// <param name="index">The zero-based index of the point to remove.</param>
    public void RemoveAt(int index) => _points.RemoveAt(index);

    /// <summary>
    /// Initializes a new instance of the Polygon struct with the specified list of points.
    /// </summary>
    /// <param name="points">The list of points that define the polygon vertices.</param>
    public Polygon(IList<Point<T>> points)
    {
        _points = points;
    }

    /// <summary>
    /// Initializes a new instance of the Polygon struct with no points (empty polygon).
    /// </summary>
    public Polygon() : this(Array.Empty<T>())
    {
        
    }
    /// <summary>
    /// Initializes a new instance of the Polygon struct with the specified array of points.
    /// </summary>
    /// <param name="points">The array of points that define the polygon vertices.</param>
    public Polygon(params Point<T>[] points) : this(points.ToList())
    {
        
    }

    /// <summary>
    /// Initializes a new instance of the Polygon struct from a flat list of coordinate values.
    /// The coordinates are interpreted as pairs (x1, y1, x2, y2, ...).
    /// </summary>
    /// <param name="points">The flat list of coordinate values.</param>
    public Polygon(IReadOnlyList<T> points)
    {
        _points = new List<Point<T>>(points.Count / 2);
        for (int i = 0; i < points.Count; i += 2)
        {
            _points.Add(new Point<T>(points[i], points[i + 1]));
        }
    }

    /// <summary>
    /// Gets the number of vertices in this polygon.
    /// </summary>
    [JsonIgnore]
    public int Count => _points?.Count ?? 0;

    /// <summary>
    /// Gets or sets the vertex at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the vertex to get or set.</param>
    /// <returns>The vertex at the specified index.</returns>
    public Point<T> this[int index]
    {
        get { return _points[index]; }
        set => _points[index] = value;  
    }

    /// <summary>
    /// Gets a read-only collection of all vertices in this polygon.
    /// </summary>
    public IReadOnlyList<Point<T>> Points => (IReadOnlyList<Point<T>>)(_points ?? Array.Empty<Point<T>>());

    
}