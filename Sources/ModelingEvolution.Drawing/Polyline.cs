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
public readonly record struct Polyline<T> : IBoundingBox<T>, IPoolable<Polyline<T>, Lease<Point<T>>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly ReadOnlyMemory<Point<T>> _points;

    [ProtoMember(1)]
    private Point<T>[] ProtoPoints
    {
        get
        {
            if (_points.Length == 0) return Array.Empty<Point<T>>();
            if (MemoryMarshal.TryGetArray(_points, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _points.ToArray();
        }
        init => _points = value ?? ReadOnlyMemory<Point<T>>.Empty;
    }

    /// <summary>
    /// Gets the number of vertices in this polyline.
    /// </summary>
    [JsonIgnore]
    public int Count => _points.Length;

    /// <summary>
    /// Returns a read-only span over the polyline's vertices.
    /// Hoist the result before loops — this is a method call, not a field access.
    /// </summary>
    public ReadOnlySpan<Point<T>> AsSpan() => _points.Span;

    /// <summary>
    /// Gets the vertex at the specified index.
    /// </summary>
    public Point<T> this[int index] => _points.Span[index];

    /// <summary>
    /// Gets a read-only list of all vertices. For high-performance code, prefer AsSpan() instead.
    /// </summary>
    public IReadOnlyList<Point<T>> Points
    {
        get
        {
            if (_points.Length == 0) return Array.Empty<Point<T>>();
            if (MemoryMarshal.TryGetArray(_points, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _points.ToArray();
        }
    }

    /// <summary>
    /// Gets the first vertex of the polyline.
    /// </summary>
    [JsonIgnore]
    public Point<T> Start => _points.Span[0];

    /// <summary>
    /// Gets the last vertex of the polyline.
    /// </summary>
    [JsonIgnore]
    public Point<T> End => _points.Span[^1];

    #region Constructors

    /// <summary>
    /// Initializes a new empty polyline.
    /// </summary>
    public Polyline()
    {
        _points = ReadOnlyMemory<Point<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new polyline wrapping a ReadOnlyMemory of points. Zero-copy.
    /// </summary>
    public Polyline(ReadOnlyMemory<Point<T>> memory)
    {
        _points = memory;
    }

    /// <summary>
    /// Initializes a new polyline from the specified points array.
    /// </summary>
    public Polyline(params Point<T>[] points)
    {
        _points = points;
    }

    /// <summary>
    /// Initializes a new polyline from the specified list of points (copies to array).
    /// </summary>
    public Polyline(IList<Point<T>> points)
    {
        if (points is Point<T>[] arr)
            _points = arr;
        else
        {
            var tmp = new Point<T>[points.Count];
            points.CopyTo(tmp, 0);
            _points = tmp;
        }
    }

    /// <summary>
    /// Initializes a new polyline from a flat list of coordinate values (x1, y1, x2, y2, ...).
    /// </summary>
    public Polyline(IReadOnlyList<T> coords)
    {
        var tmp = new Point<T>[coords.Count / 2];
        for (int i = 0; i < coords.Count; i += 2)
            tmp[i / 2] = new Point<T>(coords[i], coords[i + 1]);
        _points = tmp;
    }

    #endregion

    #region Geometry

    /// <summary>
    /// Computes the total length of this polyline (sum of all edge lengths).
    /// </summary>
    public T Length()
    {
        var span = AsSpan();
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
        var span = AsSpan();
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
        var span = AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var reversed = mem.Span;
        for (int i = 0; i < span.Length; i++)
            reversed[i] = span[span.Length - 1 - i];
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Densifies the polyline by inserting points along each edge so that consecutive points
    /// are at most 1 unit apart.
    /// </summary>
    public Polyline<T> Densify()
    {
        if (Count < 2) return this;
        return new Polyline<T>(Densification.Densify(AsSpan()));
    }

    /// <summary>
    /// Computes a PCA-based rigid alignment that maps this polyline onto the target point cloud.
    /// </summary>
    /// <param name="target">The target point cloud to align to.</param>
    /// <param name="densify">If true, densifies this polyline before alignment for uniform point spacing.</param>
    public AlignmentResult<T> AlignTo(ReadOnlySpan<Point<T>> target, bool densify = false)
    {
        var source = densify ? Densify().AsSpan() : AsSpan();
        return Alignment.Pca(source, target);
    }

    /// <summary>
    /// Returns the edges of this polyline as segments. Open — yields n-1 segments with no wrap-around.
    /// </summary>
    public ReadOnlyMemory<Segment<T>> Edges()
    {
        var span = AsSpan();
        int n = span.Length;
        if (n < 2) return ReadOnlyMemory<Segment<T>>.Empty;
        var mem = Alloc.Memory<Segment<T>>(n - 1);
        var edges = mem.Span;
        for (int i = 0; i < n - 1; i++)
            edges[i] = new Segment<T>(span[i], span[i + 1]);
        return mem;
    }

    /// <summary>
    /// Returns a new polyline scaled by the given size factor around the origin.
    /// </summary>
    public Polyline<T> Scale(T factor)
    {
        var span = AsSpan();
        if (span.Length == 0) return this;

        // Scale around the midpoint of bounding box
        var bb = BoundingBox();
        var cx = bb.X + bb.Width / (T.One + T.One);
        var cy = bb.Y + bb.Height / (T.One + T.One);
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = new Point<T>(cx + (span[i].X - cx) * factor, cy + (span[i].Y - cy) * factor);
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Simplifies this polyline using the Ramer-Douglas-Peucker algorithm.
    /// Removes points closer than <paramref name="epsilon"/> to the line between their neighbors.
    /// </summary>
    public Polyline<T> Simplify(T epsilon)
    {
        var span = AsSpan();
        if (span.Length < 3) return this;

        var epsSq = epsilon * epsilon;
        Span<bool> keep = span.Length <= 1024 ? stackalloc bool[span.Length] : new bool[span.Length];
        keep[0] = true;
        keep[span.Length - 1] = true;

        // Iterative stack-based RDP
        int stackSize = span.Length * 2;
        Span<int> stack = stackSize <= 1024 ? stackalloc int[stackSize] : new int[stackSize];
        int top = 0;
        stack[top++] = 0;
        stack[top++] = span.Length - 1;

        while (top >= 2)
        {
            int end = stack[--top];
            int start = stack[--top];

            var maxDistSq = T.Zero;
            int maxIdx = start;

            var ax = span[start].X;
            var ay = span[start].Y;
            var dx = span[end].X - ax;
            var dy = span[end].Y - ay;
            var lenSq = dx * dx + dy * dy;

            for (int i = start + 1; i < end; i++)
            {
                T distSq;
                if (lenSq < T.CreateTruncating(1e-18))
                {
                    var px = span[i].X - ax;
                    var py = span[i].Y - ay;
                    distSq = px * px + py * py;
                }
                else
                {
                    // Perpendicular distance squared = (cross product)^2 / lenSq
                    var cross = (span[i].X - ax) * dy - (span[i].Y - ay) * dx;
                    distSq = cross * cross / lenSq;
                }

                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    maxIdx = i;
                }
            }

            if (maxDistSq > epsSq)
            {
                keep[maxIdx] = true;
                stack[top++] = start;
                stack[top++] = maxIdx;
                stack[top++] = maxIdx;
                stack[top++] = end;
            }
        }

        int count = 0;
        for (int i = 0; i < span.Length; i++)
            if (keep[i]) count++;

        if (count == span.Length) return this;

        var mem = Alloc.Memory<Point<T>>(count);
        var dst = mem.Span;
        int idx = 0;
        for (int i = 0; i < span.Length; i++)
            if (keep[i]) dst[idx++] = span[i];

        return new Polyline<T>(mem);
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
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] * f;
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Scales all points by the inverse of the given size factor (component-wise division).
    /// </summary>
    public static Polyline<T> operator /(in Polyline<T> a, in Size<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] / f;
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Rotates all points around the specified origin by the given angle.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="origin">The center of rotation. Defaults to the origin (0, 0).</param>
    /// <returns>A new rotated polyline.</returns>
    public Polyline<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i].Rotate(angle, origin);
        return new Polyline<T>(mem);
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
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] - f;
        return new Polyline<T>(mem);
    }

    /// <summary>
    /// Translates all points by adding the given vector.
    /// </summary>
    public static Polyline<T> operator +(in Polyline<T> a, in Vector<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] + f;
        return new Polyline<T>(mem);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Polyline<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new System.HashCode();
        var span = AsSpan();
        for (int i = 0; i < span.Length; i++)
            hash.Add(span[i]);
        return hash.ToHashCode();
    }

    #endregion

    /// <summary>
    /// Detaches this polyline's backing memory from the given scope and returns a lease.
    /// The caller becomes responsible for disposing the lease to return memory to the pool.
    /// </summary>
    public Lease<Point<T>> DetachFrom(AllocationScope scope)
    {
        if (!MemoryMarshal.TryGetArray(_points, out var seg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        var owner = scope.UntrackMemory(new Memory<Point<T>>(seg.Array!, seg.Offset, seg.Count));
        return new Lease<Point<T>> { _owner = owner };
    }

    internal Point<T>[] AsArray()
    {
        if (_points.Length == 0) return Array.Empty<Point<T>>();
        if (MemoryMarshal.TryGetArray(_points, out var seg)
            && seg.Offset == 0 && seg.Count == seg.Array!.Length)
            return seg.Array;
        return _points.ToArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"Polyline({Count} points)";
}
