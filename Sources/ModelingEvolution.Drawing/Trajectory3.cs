using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an immutable time-parameterized path through 3D poses.
/// Each element is a <see cref="Waypoint3{T}"/> containing a <see cref="Pose3{T}"/> and a timestamp.
/// For untimed paths, use <see cref="PosePath3{T}"/>.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and time.</typeparam>
[JsonConverter(typeof(Trajectory3JsonConverterFactory))]
[ProtoContract]
public readonly record struct Trajectory3<T> : IPoolable<Trajectory3<T>, Lease<Waypoint3<T>>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal readonly ReadOnlyMemory<Waypoint3<T>> _waypoints;

    [ProtoMember(1)]
    private Waypoint3<T>[] ProtoWaypoints
    {
        get
        {
            if (_waypoints.Length == 0) return Array.Empty<Waypoint3<T>>();
            if (MemoryMarshal.TryGetArray(_waypoints, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _waypoints.ToArray();
        }
        init => _waypoints = value ?? ReadOnlyMemory<Waypoint3<T>>.Empty;
    }

    /// <summary>
    /// Gets the number of waypoints in this trajectory.
    /// </summary>
    [JsonIgnore]
    public int Count => _waypoints.Length;

    /// <summary>
    /// Returns a read-only span over the trajectory's waypoints.
    /// Hoist the result before loops — this is a method call, not a field access.
    /// </summary>
    public ReadOnlySpan<Waypoint3<T>> AsSpan() => _waypoints.Span;

    /// <summary>
    /// Gets the waypoint at the specified index.
    /// </summary>
    public Waypoint3<T> this[int index] => _waypoints.Span[index];

    /// <summary>
    /// Gets a read-only list of all waypoints. For high-performance code, prefer AsSpan() instead.
    /// </summary>
    public IReadOnlyList<Waypoint3<T>> Waypoints
    {
        get
        {
            if (_waypoints.Length == 0) return Array.Empty<Waypoint3<T>>();
            if (MemoryMarshal.TryGetArray(_waypoints, out var seg)
                && seg.Offset == 0 && seg.Count == seg.Array!.Length)
                return seg.Array;
            return _waypoints.ToArray();
        }
    }

    /// <summary>
    /// Gets the first waypoint.
    /// </summary>
    [JsonIgnore]
    public Waypoint3<T> Start => _waypoints.Span[0];

    /// <summary>
    /// Gets the last waypoint.
    /// </summary>
    [JsonIgnore]
    public Waypoint3<T> End => _waypoints.Span[^1];

    #region Constructors

    /// <summary>
    /// Initializes a new empty trajectory.
    /// </summary>
    public Trajectory3()
    {
        _waypoints = ReadOnlyMemory<Waypoint3<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new trajectory wrapping a ReadOnlyMemory of waypoints. Zero-copy.
    /// </summary>
    public Trajectory3(ReadOnlyMemory<Waypoint3<T>> memory)
    {
        _waypoints = memory;
    }

    /// <summary>
    /// Initializes a new trajectory from the specified waypoints array.
    /// </summary>
    public Trajectory3(params Waypoint3<T>[] waypoints)
    {
        _waypoints = waypoints;
    }

    /// <summary>
    /// Initializes a new trajectory from the specified list of waypoints (copies to array).
    /// </summary>
    public Trajectory3(IList<Waypoint3<T>> waypoints)
    {
        if (waypoints is Waypoint3<T>[] arr)
            _waypoints = arr;
        else
        {
            var tmp = new Waypoint3<T>[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
                tmp[i] = waypoints[i];
            _waypoints = tmp;
        }
    }

    #endregion

    #region Geometry & Kinematics

    /// <summary>
    /// Gets the total duration of this trajectory (last time minus first time).
    /// </summary>
    public T Duration()
    {
        var span = AsSpan();
        if (span.Length < 2) return T.Zero;
        return span[^1].Time - span[0].Time;
    }

    /// <summary>
    /// Computes the total positional length of this trajectory (sum of distances between consecutive positions).
    /// </summary>
    public T Length()
    {
        var span = AsSpan();
        int n = span.Length;
        if (n < 2) return T.Zero;

        T length = T.Zero;
        for (int i = 0; i < n - 1; i++)
            length += Point3<T>.Distance(span[i].Position, span[i + 1].Position);
        return length;
    }

    /// <summary>
    /// Interpolates the pose at the given time using linear position interpolation and Slerp for rotation.
    /// Clamps to the first/last waypoint if <paramref name="time"/> is outside the trajectory range.
    /// </summary>
    public Pose3<T> AtTime(T time)
    {
        var span = AsSpan();
        if (span.Length == 0) return Pose3<T>.Identity;
        if (span.Length == 1) return span[0].Pose;

        // Clamp to range
        if (time <= span[0].Time) return span[0].Pose;
        if (time >= span[^1].Time) return span[^1].Pose;

        // Binary search for the interval
        int lo = 0, hi = span.Length - 2;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (span[mid + 1].Time < time)
                lo = mid + 1;
            else
                hi = mid;
        }

        var a = span[lo];
        var b = span[lo + 1];
        var dt = b.Time - a.Time;
        if (dt == T.Zero) return a.Pose;

        var t = (time - a.Time) / dt;
        return Pose3<T>.Lerp(a.Pose, b.Pose, t);
    }

    /// <summary>
    /// Returns a new trajectory with the waypoint order and timestamps reversed.
    /// The new first waypoint has time 0, preserving the original time intervals.
    /// </summary>
    public Trajectory3<T> Reverse()
    {
        var span = AsSpan();
        if (span.Length == 0) return this;

        var totalTime = Duration();
        var mem = Alloc.Memory<Waypoint3<T>>(span.Length);
        var dst = mem.Span;
        var t0 = span[0].Time;
        for (int i = 0; i < span.Length; i++)
        {
            var original = span[span.Length - 1 - i];
            var newTime = totalTime - (original.Time - t0);
            dst[i] = new Waypoint3<T>(original.Pose, t0 + newTime);
        }
        return new Trajectory3<T>(mem);
    }

    /// <summary>
    /// Returns a new trajectory with all poses transformed by the given pose (rotation + translation).
    /// Timestamps are preserved.
    /// </summary>
    public Trajectory3<T> Transform(Pose3<T> pose)
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Waypoint3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = new Waypoint3<T>(pose.Multiply(span[i].Pose), span[i].Time);
        return new Trajectory3<T>(mem);
    }

    /// <summary>
    /// Strips timestamps and returns a <see cref="PosePath3{T}"/>.
    /// </summary>
    public PosePath3<T> ToPosePath()
    {
        var span = AsSpan();
        var mem = Alloc.Memory<Pose3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = span[i].Pose;
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
    /// Resamples the trajectory at uniform time intervals.
    /// </summary>
    /// <param name="interval">Time between consecutive samples.</param>
    public Trajectory3<T> Resample(T interval)
    {
        var span = AsSpan();
        if (span.Length < 2) return this;

        var duration = Duration();
        var t0 = span[0].Time;
        int sampleCount = int.CreateChecked(T.Floor(duration / interval)) + 1;
        var mem = Alloc.Memory<Waypoint3<T>>(sampleCount);
        var dst = mem.Span;

        for (int i = 0; i < sampleCount; i++)
        {
            var time = t0 + interval * T.CreateTruncating(i);
            if (time > t0 + duration) time = t0 + duration;
            dst[i] = new Waypoint3<T>(AtTime(time), time);
        }

        return new Trajectory3<T>(mem);
    }

    /// <summary>
    /// Creates a <see cref="TrajectoryController{T}"/> for real-time playback of this trajectory.
    /// </summary>
    public TrajectoryController<T> Control() => new(this);

    #endregion

    #region Operators

    /// <summary>
    /// Translates all positions by adding the given vector (rotations and timestamps unchanged).
    /// </summary>
    public static Trajectory3<T> operator +(in Trajectory3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Waypoint3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = new Waypoint3<T>(span[i].Pose + f, span[i].Time);
        return new Trajectory3<T>(mem);
    }

    /// <summary>
    /// Translates all positions by subtracting the given vector (rotations and timestamps unchanged).
    /// </summary>
    public static Trajectory3<T> operator -(in Trajectory3<T> a, in Vector3<T> f)
    {
        var span = a.AsSpan();
        var mem = Alloc.Memory<Waypoint3<T>>(span.Length);
        var dst = mem.Span;
        for (int i = 0; i < span.Length; i++)
            dst[i] = new Waypoint3<T>(span[i].Pose - f, span[i].Time);
        return new Trajectory3<T>(mem);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Trajectory3<T> other) => AsSpan().SequenceEqual(other.AsSpan());

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
    /// Detaches this trajectory's backing memory from the given scope and returns a lease.
    /// </summary>
    public Lease<Waypoint3<T>> DetachFrom(AllocationScope scope)
    {
        if (!MemoryMarshal.TryGetArray(_waypoints, out var seg))
            throw new InvalidOperationException("Cannot detach non-array-backed memory.");
        var owner = scope.UntrackMemory(new Memory<Waypoint3<T>>(seg.Array!, seg.Offset, seg.Count));
        return new Lease<Waypoint3<T>> { _owner = owner };
    }

    /// <inheritdoc />
    public override string ToString() => $"Trajectory3({Count} waypoints)";
}
