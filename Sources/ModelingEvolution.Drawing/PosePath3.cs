using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an immutable open path defined by a sequence of 3D poses (position + orientation).
/// This is the geometric counterpart — no time information. For time-parameterized paths, use <see cref="Trajectory3{T}"/>.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[JsonConverter(typeof(PosePath3JsonConverterFactory))]
[ProtoContract]
public readonly record struct PosePath3<T> : IPoolable<PosePath3<T>, Lease<Pose3<T>>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly ReadOnlyMemory<Pose3<T>> _poses;

    [ProtoMember(1)]
    private Pose3<T>[] ProtoPoses
    {
        get
        {
            if (_poses.Length == 0) return Array.Empty<Pose3<T>>();
            if (MemoryMarshal.TryGetArray(_poses, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _poses.ToArray();
        }
        init => _poses = value ?? ReadOnlyMemory<Pose3<T>>.Empty;
    }

    /// <summary>
    /// Gets the number of poses in this path.
    /// </summary>
    [JsonIgnore]
    public int Count => _poses.Length;

    /// <summary>
    /// Returns a read-only span over the path's poses.
    /// Hoist the result before loops — this is a method call, not a field access.
    /// </summary>
    public ReadOnlySpan<Pose3<T>> AsSpan() => _poses.Span;

    /// <summary>
    /// Gets the pose at the specified index.
    /// </summary>
    public Pose3<T> this[int index] => _poses.Span[index];

    /// <summary>
    /// Gets a read-only list of all poses. For high-performance code, prefer AsSpan() instead.
    /// </summary>
    public IReadOnlyList<Pose3<T>> Poses
    {
        get
        {
            if (_poses.Length == 0) return Array.Empty<Pose3<T>>();
            if (MemoryMarshal.TryGetArray(_poses, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _poses.ToArray();
        }
    }

    /// <summary>
    /// Gets the first pose of the path.
    /// </summary>
    [JsonIgnore]
    public Pose3<T> Start => _poses.Span[0];

    /// <summary>
    /// Gets the last pose of the path.
    /// </summary>
    [JsonIgnore]
    public Pose3<T> End => _poses.Span[^1];

    #region Constructors

    /// <summary>
    /// Initializes a new empty path.
    /// </summary>
    public PosePath3()
    {
        _poses = ReadOnlyMemory<Pose3<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new path wrapping a ReadOnlyMemory of poses. Zero-copy.
    /// </summary>
    public PosePath3(ReadOnlyMemory<Pose3<T>> memory)
    {
        _poses = memory;
    }

    /// <summary>
    /// Initializes a new path from the specified poses array.
    /// </summary>
    public PosePath3(params Pose3<T>[] poses)
    {
        _poses = poses;
    }

    /// <summary>
    /// Initializes a new path from the specified list of poses (copies to array).
    /// </summary>
    public PosePath3(IList<Pose3<T>> poses)
    {
        if (poses is Pose3<T>[] arr)
            _poses = arr;
        else
        {
            var tmp = new Pose3<T>[poses.Count];
            for (int i = 0; i < poses.Count; i++)
                tmp[i] = poses[i];
            _poses = tmp;
        }
    }

    #endregion

    #region Geometry

    /// <summary>
    /// Computes the total positional length of this path (sum of distances between consecutive positions).
    /// </summary>
    public T Length()
    {
        var span = AsSpan();
        int n = span.Length;
        if (n < 2) return T.Zero;

        T length = T.Zero;
        for (int i = 0; i < n - 1; i++)
            length += Pose3<T>.Distance(span[i], span[i + 1]);
        return length;
    }

    /// <summary>
    /// Returns a new path with the pose order reversed.
    /// </summary>
    public PosePath3<T> Reverse()
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Pose3<T>>(span.Length);
        var reversed = mem.Span;
        for (int i = 0; i < span.Length; i++)
            reversed[i] = span[span.Length - 1 - i];
        return new PosePath3<T>(mem);
    }

    /// <summary>
    /// Returns a new path with all poses transformed by the given pose (rotation + translation).
    /// </summary>
    public PosePath3<T> Transform(Pose3<T> pose)
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Pose3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = pose.Multiply(span[i]);
        return new PosePath3<T>(mem);
    }

    /// <summary>
    /// Extracts just the positions as a <see cref="Polyline3{T}"/>.
    /// </summary>
    public Polyline3<T> ToPolyline3()
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Point3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = span[i].Position;
        return new Polyline3<T>(mem);
    }

    /// <summary>
    /// Creates a <see cref="Trajectory3{T}"/> by assigning uniform timestamps from 0 to <paramref name="totalTime"/>.
    /// </summary>
    public Trajectory3<T> ToTrajectory(T totalTime)
    {
        var span = AsSpan();
        if (span.Length == 0) return new Trajectory3<T>();
        if (span.Length == 1) return new Trajectory3<T>(new Waypoint3<T>(span[0], T.Zero));

        var mem = Alloc.Memory<Waypoint3<T>>(span.Length);
        var dst = mem.Span;
        var n = T.CreateTruncating(span.Length - 1);
        for (int i = 0; i < span.Length; i++)
        {
            var t = totalTime * T.CreateTruncating(i) / n;
            dst[i] = new Waypoint3<T>(span[i], t);
        }
        return new Trajectory3<T>(mem);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Translates all positions by adding the given vector (rotations unchanged).
    /// </summary>
    public static PosePath3<T> operator +(in PosePath3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Pose3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = span[i] + f;
        return new PosePath3<T>(mem);
    }

    /// <summary>
    /// Translates all positions by subtracting the given vector (rotations unchanged).
    /// </summary>
    public static PosePath3<T> operator -(in PosePath3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Pose3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = span[i] - f;
        return new PosePath3<T>(mem);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(PosePath3<T> other) => AsSpan().SequenceEqual(other.AsSpan());

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
    /// Detaches this path's backing memory from the given scope and returns a lease.
    /// </summary>
    public Lease<Pose3<T>> DetachFrom(AllocationScope scope)
    {
        if (!MemoryMarshal.TryGetArray(_poses, out var seg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        var owner = scope.UntrackMemory(new Memory<Pose3<T>>(seg.Array!, seg.Offset, seg.Count));
        return new Lease<Pose3<T>> { _owner = owner };
    }

    /// <inheritdoc />
    public override string ToString() => $"PosePath3({Count} poses)";
}
