using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an immutable open polyline defined by a sequence of points.
/// Unlike Polygon, a polyline does not close — Edges() yields n-1 segments with no wrap-around.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[JsonConverter(typeof(PolylineJsonConverterFactory))]
[ProtoContract]
[Svg.SvgExporter(typeof(PolylineSvgExporterFactory))]
public readonly record struct Polyline<T> : IBoundingBox<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)]
    internal readonly Point<T>[] _points;

    [ProtoMember(2)]
    internal readonly int _length;

    [ProtoMember(3)]
    internal readonly int _offset;

    /// <summary>
    /// Gets the number of vertices in this polyline.
    /// </summary>
    [JsonIgnore]
    public int Count => _points != null
        ? (_length > 0 ? _length : _points.Length)
        : 0;

    /// <summary>
    /// Gets a read-only span over the polyline's vertices.
    /// </summary>
    public ReadOnlySpan<Point<T>> Span => _points != null
        ? (_length > 0 ? _points.AsSpan(_offset, _length) : _points.AsSpan())
        : ReadOnlySpan<Point<T>>.Empty;

    /// <summary>
    /// Gets a ReadOnlyMemory over the polyline's vertices.
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
            return Span.ToArray();
        }
    }

    /// <summary>
    /// Gets the first vertex of the polyline.
    /// </summary>
    [JsonIgnore]
    public Point<T> Start => Span[0];

    /// <summary>
    /// Gets the last vertex of the polyline.
    /// </summary>
    [JsonIgnore]
    public Point<T> End => Span[^1];

    #region Constructors

    /// <summary>
    /// Initializes a new empty polyline.
    /// </summary>
    public Polyline()
    {
        _points = Array.Empty<Point<T>>();
        _offset = 0;
        _length = 0;
    }

    /// <summary>
    /// Initializes a new polyline wrapping a ReadOnlyMemory of points. Zero-copy — requires array-backed memory.
    /// </summary>
    public Polyline(ReadOnlyMemory<Point<T>> memory)
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
    /// Initializes a new polyline from the specified points array.
    /// </summary>
    public Polyline(params Point<T>[] points)
    {
        _points = points;
        _offset = 0;
        _length = points.Length;
    }

    /// <summary>
    /// Initializes a new polyline from the specified list of points (copies to array).
    /// </summary>
    public Polyline(IList<Point<T>> points)
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
    /// Initializes a new polyline from a flat list of coordinate values (x1, y1, x2, y2, ...).
    /// </summary>
    public Polyline(IReadOnlyList<T> coords)
    {
        _points = new Point<T>[coords.Count / 2];
        _offset = 0;
        for (int i = 0; i < coords.Count; i += 2)
            _points[i / 2] = new Point<T>(coords[i], coords[i + 1]);
        _length = _points.Length;
    }

    #endregion

    #region Geometry

    /// <summary>
    /// Computes the total length of this polyline (sum of all edge lengths).
    /// </summary>
    public T Length()
    {
        var span = Span;
        int n = span.Length;
        if (n < 2) return T.Zero;

        T length = T.Zero;
        for (int i = 0; i < n - 1; i++)
        {
            var dx = span[i + 1].X - span[i].X;
            var dy = span[i + 1].Y - span[i].Y;
            length += T.Sqrt(dx * dx + dy * dy);
        }
        return length;
    }

    /// <summary>
    /// Computes the bounding box that encloses all points of this polyline.
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
    /// Returns a new polyline with the vertex order reversed.
    /// </summary>
    public Polyline<T> Reverse()
    {
        var span = Span;
        var reversed = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            reversed[i] = span[span.Length - 1 - i];
        return new Polyline<T>(reversed);
    }

    /// <summary>
    /// Returns the edges of this polyline as segments. Open — yields n-1 segments with no wrap-around.
    /// </summary>
    public Segment<T>[] Edges()
    {
        var span = Span;
        int n = span.Length;
        if (n < 2) return Array.Empty<Segment<T>>();

        var edges = new Segment<T>[n - 1];
        for (int i = 0; i < n - 1; i++)
            edges[i] = new Segment<T>(span[i], span[i + 1]);
        return edges;
    }

    /// <summary>
    /// Returns a new polyline scaled by the given size factor around the origin.
    /// </summary>
    public Polyline<T> Scale(T factor)
    {
        var span = Span;
        if (span.Length == 0) return this;

        // Scale around the midpoint of bounding box
        var bb = BoundingBox();
        var cx = bb.X + bb.Width / (T.One + T.One);
        var cy = bb.Y + bb.Height / (T.One + T.One);
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = new Point<T>(cx + (span[i].X - cx) * factor, cy + (span[i].Y - cy) * factor);
        return new Polyline<T>(points);
    }

    /// <summary>
    /// Finds all segments where the given infinite line intersects this polyline.
    /// </summary>
    public IReadOnlyList<Segment<T>> Intersections(Line<T> line) => Drawing.Intersections.Of(line, this);

    /// <summary>
    /// Finds the first segment where the given infinite line intersects this polyline.
    /// </summary>
    public Segment<T>? FirstIntersection(Line<T> line) => Drawing.Intersections.FirstOf(line, this);

    #endregion

    #region Operators

    /// <summary>
    /// Scales all points by the given size factor (component-wise multiplication).
    /// </summary>
    public static Polyline<T> operator *(in Polyline<T> a, in Size<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] * f;
        return new Polyline<T>(points);
    }

    /// <summary>
    /// Scales all points by the inverse of the given size factor (component-wise division).
    /// </summary>
    public static Polyline<T> operator /(in Polyline<T> a, in Size<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] / f;
        return new Polyline<T>(points);
    }

    /// <summary>
    /// Rotates all points around the specified origin by the given angle.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="origin">The center of rotation. Defaults to the origin (0, 0).</param>
    /// <returns>A new rotated polyline.</returns>
    public Polyline<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        var span = Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i].Rotate(angle, origin);
        return new Polyline<T>(points);
    }

    /// <summary>
    /// Rotates the polyline around the origin by the given angle.
    /// </summary>
    public static Polyline<T> operator +(in Polyline<T> a, Degree<T> angle) =>
        a.Rotate(angle);

    /// <summary>
    /// Rotates the polyline around the origin by the negation of the given angle.
    /// </summary>
    public static Polyline<T> operator -(in Polyline<T> a, Degree<T> angle) =>
        a.Rotate(-angle);

    /// <summary>
    /// Translates all points by subtracting the given vector.
    /// </summary>
    public static Polyline<T> operator -(in Polyline<T> a, in Vector<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] - f;
        return new Polyline<T>(points);
    }

    /// <summary>
    /// Translates all points by adding the given vector.
    /// </summary>
    public static Polyline<T> operator +(in Polyline<T> a, in Vector<T> f)
    {
        var span = a.Span;
        var points = new Point<T>[span.Length];
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] + f;
        return new Polyline<T>(points);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Polyline<T> other) => Span.SequenceEqual(other.Span);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new System.HashCode();
        var span = Span;
        for (int i = 0; i < span.Length; i++)
            hash.Add(span[i]);
        return hash.ToHashCode();
    }

    #endregion

    internal Point<T>[] AsArray()
    {
        if (_points == null) return Array.Empty<Point<T>>();
        if (_offset == 0 && (_length == 0 || _length == _points.Length)) return _points;
        return Span.ToArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"Polyline({Count} points)";
}
