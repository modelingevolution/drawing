using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an immutable open polyline defined by a sequence of 3D points.
/// Unlike Polygon, a polyline does not close — Edges() yields n-1 segments with no wrap-around.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[JsonConverter(typeof(Polyline3JsonConverterFactory))]
[ProtoContract]
public readonly record struct Polyline3<T> : IPoolable<Polyline3<T>, Lease<Point3<T>>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly ReadOnlyMemory<Point3<T>> _points;

    [ProtoMember(1)]
    private Point3<T>[] ProtoPoints
    {
        get
        {
            if (_points.Length == 0) return Array.Empty<Point3<T>>();
            if (MemoryMarshal.TryGetArray(_points, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _points.ToArray();
        }
        init => _points = value ?? ReadOnlyMemory<Point3<T>>.Empty;
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
    public ReadOnlySpan<Point3<T>> AsSpan() => _points.Span;

    /// <summary>
    /// Gets the vertex at the specified index.
    /// </summary>
    public Point3<T> this[int index] => _points.Span[index];

    /// <summary>
    /// Gets a read-only list of all vertices. For high-performance code, prefer AsSpan() instead.
    /// </summary>
    public IReadOnlyList<Point3<T>> Points
    {
        get
        {
            if (_points.Length == 0) return Array.Empty<Point3<T>>();
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
    public Point3<T> Start => _points.Span[0];

    /// <summary>
    /// Gets the last vertex of the polyline.
    /// </summary>
    [JsonIgnore]
    public Point3<T> End => _points.Span[^1];

    #region Constructors

    /// <summary>
    /// Initializes a new empty polyline.
    /// </summary>
    public Polyline3()
    {
        _points = ReadOnlyMemory<Point3<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new polyline wrapping a ReadOnlyMemory of points. Zero-copy.
    /// </summary>
    public Polyline3(ReadOnlyMemory<Point3<T>> memory)
    {
        _points = memory;
    }

    /// <summary>
    /// Initializes a new polyline from the specified points array.
    /// </summary>
    public Polyline3(params Point3<T>[] points)
    {
        _points = points;
    }

    /// <summary>
    /// Initializes a new polyline from the specified list of points (copies to array).
    /// </summary>
    public Polyline3(IList<Point3<T>> points)
    {
        if (points is Point3<T>[] arr)
            _points = arr;
        else
        {
            var tmp = new Point3<T>[points.Count];
            for (int i = 0; i < points.Count; i++)
                tmp[i] = points[i];
            _points = tmp;
        }
    }

    /// <summary>
    /// Initializes a new polyline from a flat list of coordinate values (x1, y1, z1, x2, y2, z2, ...).
    /// </summary>
    public Polyline3(IReadOnlyList<T> coords)
    {
        var tmp = new Point3<T>[coords.Count / 3];
        for (int i = 0; i < coords.Count; i += 3)
            tmp[i / 3] = new Point3<T>(coords[i], coords[i + 1], coords[i + 2]);
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
            length += Point3<T>.Distance(span[i], span[i + 1]);
        return length;
    }

    /// <summary>
    /// Computes the centroid (center of mass) of this polyline, weighted by segment length.
    /// Each segment contributes its midpoint weighted by its length.
    /// </summary>
    public Point3<T> Centroid()
    {
        var span = AsSpan();
        if (span.Length == 0) return Point3<T>.Zero;
        if (span.Length == 1) return span[0];

        T totalLength = T.Zero;
        T cx = T.Zero;
        T cy = T.Zero;
        T cz = T.Zero;

        for (int i = 0; i < span.Length - 1; i++)
        {
            var segLen = Point3<T>.Distance(span[i], span[i + 1]);
            var two = T.CreateTruncating(2);
            cx += (span[i].X + span[i + 1].X) / two * segLen;
            cy += (span[i].Y + span[i + 1].Y) / two * segLen;
            cz += (span[i].Z + span[i + 1].Z) / two * segLen;
            totalLength += segLen;
        }

        if (totalLength == T.Zero) return span[0];

        return new Point3<T>(cx / totalLength, cy / totalLength, cz / totalLength);
    }

    /// <summary>
    /// Returns a new polyline with the vertex order reversed.
    /// </summary>
    public Polyline3<T> Reverse()
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var reversed = mem.Span;
        for (int i = 0; i < span.Length; i++)
            reversed[i] = span[span.Length - 1 - i];
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Returns the edges of this polyline as 3D segments. Open — yields n-1 segments with no wrap-around.
    /// </summary>
    public ReadOnlyMemory<Segment3<T>> Edges()
    {
        var span = AsSpan();
        int n = span.Length;
        if (n < 2) return ReadOnlyMemory<Segment3<T>>.Empty;
        var mem = Alloc.Memory<Segment3<T>>(n - 1);
        var edges = mem.Span;
        for (int i = 0; i < n - 1; i++)
            edges[i] = new Segment3<T>(span[i], span[i + 1]);
        return mem;
    }

    /// <summary>
    /// Returns a new polyline scaled by the given factor around the centroid.
    /// </summary>
    public Polyline3<T> Scale(T factor)
    {
        var span = AsSpan();
        if (span.Length == 0) return this;

        var c = Centroid();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
        {
            points[i] = new Point3<T>(
                c.X + (span[i].X - c.X) * factor,
                c.Y + (span[i].Y - c.Y) * factor,
                c.Z + (span[i].Z - c.Z) * factor);
        }
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Simplifies this polyline using the Ramer-Douglas-Peucker algorithm in 3D.
    /// Removes points closer than <paramref name="epsilon"/> to the line between their neighbors.
    /// </summary>
    public Polyline3<T> Simplify(T epsilon)
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

            var a = span[start];
            var b = span[end];
            var ab = (Vector3<T>)(b - a);
            var abLenSq = ab.LengthSquared;

            for (int i = start + 1; i < end; i++)
            {
                T distSq;
                if (abLenSq < T.CreateTruncating(1e-18))
                {
                    // Degenerate segment — distance to start point
                    distSq = Point3<T>.DistanceSquared(span[i], a);
                }
                else
                {
                    // Distance from point to line via cross product: |AP x AB|^2 / |AB|^2
                    var ap = (Vector3<T>)(span[i] - a);
                    var cross = Vector3<T>.Cross(ap, ab);
                    distSq = cross.LengthSquared / abLenSq;
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

        var mem = Alloc.Memory<Point3<T>>(count);
        var dst = mem.Span;
        int idx = 0;
        for (int i = 0; i < span.Length; i++)
            if (keep[i]) dst[idx++] = span[i];

        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Returns a new polyline with all points rotated by the given rotation.
    /// </summary>
    public Polyline3<T> Transform(Rotation3<T> rotation)
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = rotation.Rotate(span[i]);
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Returns a new polyline with all points transformed by the given pose (rotation + translation).
    /// </summary>
    public Polyline3<T> Transform(Pose3<T> pose)
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = pose.TransformPoint(span[i]);
        return new Polyline3<T>(mem);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Translates all points by adding the given vector.
    /// </summary>
    public static Polyline3<T> operator +(in Polyline3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] + f;
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Translates all points by subtracting the given vector.
    /// </summary>
    public static Polyline3<T> operator -(in Polyline3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] - f;
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Scales all points by a uniform scalar.
    /// </summary>
    public static Polyline3<T> operator *(in Polyline3<T> a, T scalar)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] * scalar;
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Divides all points by a uniform scalar.
    /// </summary>
    public static Polyline3<T> operator /(in Polyline3<T> a, T scalar)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var points = mem.Span;
        for (int i = 0; i < span.Length; i++)
            points[i] = span[i] / scalar;
        return new Polyline3<T>(mem);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Polyline3<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
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
    public Lease<Point3<T>> DetachFrom(AllocationScope scope)
    {
        if (!MemoryMarshal.TryGetArray(_points, out var seg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        var owner = scope.UntrackMemory(new Memory<Point3<T>>(seg.Array!, seg.Offset, seg.Count));
        return new Lease<Point3<T>> { _owner = owner };
    }

    internal Point3<T>[] AsArray()
    {
        if (_points.Length == 0) return Array.Empty<Point3<T>>();
        if (MemoryMarshal.TryGetArray(_points, out var seg)
            && seg.Offset == 0 && seg.Count == seg.Array!.Length)
            return seg.Array;
        return _points.ToArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"Polyline3({Count} points)";
}
