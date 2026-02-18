using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Fluent builder for constructing <see cref="Trajectory3{T}"/> by appending waypoints.
/// Tracks current time and last pose for speed-based <see cref="MoveTo"/> operations.
/// Supports corner blending via <see cref="Corner{T}"/> to create smooth arcs at waypoints.
/// </summary>
public sealed class Trajectory3Builder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private Waypoint3<T>[] _buffer;
    private Corner<T>[] _corners;
    private Speed<T>[] _cornerSpeeds;
    private int _count;
    private T _currentTime;
    private bool _hasCorners;

    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Epsilon = T.CreateTruncating(1e-9);

    public Trajectory3Builder(int initialCapacity = 64)
    {
        _buffer = new Waypoint3<T>[initialCapacity];
        _corners = new Corner<T>[initialCapacity];
        _cornerSpeeds = new Speed<T>[initialCapacity];
        _count = 0;
        _currentTime = T.Zero;
    }

    /// <summary>Gets the number of waypoints added so far.</summary>
    public int Count => _count;

    /// <summary>Gets the current accumulated time.</summary>
    public T CurrentTime => _currentTime;

    /// <summary>
    /// Number of arc subdivision points per 90-degree turn. Default is 8.
    /// Higher values produce smoother arcs.
    /// </summary>
    public int ArcResolution { get; set; } = 8;

    #region Add methods

    /// <summary>
    /// Adds a waypoint with the given pose at the current accumulated time.
    /// If this is the first waypoint, time starts at zero.
    /// </summary>
    public Trajectory3Builder<T> Add(Pose3<T> pose)
    {
        EnsureCapacity(1);
        _buffer[_count++] = new Waypoint3<T>(pose, _currentTime);
        return this;
    }

    /// <summary>
    /// Adds a waypoint with the given pose at an explicit time.
    /// Updates the current accumulated time.
    /// </summary>
    public Trajectory3Builder<T> Add(Pose3<T> pose, T time)
    {
        EnsureCapacity(1);
        _buffer[_count++] = new Waypoint3<T>(pose, time);
        _currentTime = time;
        return this;
    }

    /// <summary>
    /// Adds a waypoint directly. Updates the current accumulated time.
    /// </summary>
    public Trajectory3Builder<T> Add(Waypoint3<T> waypoint)
    {
        EnsureCapacity(1);
        _buffer[_count++] = waypoint;
        _currentTime = waypoint.Time;
        return this;
    }

    /// <summary>
    /// Adds a waypoint from raw position, rotation, and time values.
    /// </summary>
    public Trajectory3Builder<T> Add(T x, T y, T z, T rx, T ry, T rz, T time)
    {
        return Add(new Pose3<T>(x, y, z, rx, ry, rz), time);
    }

    #endregion

    #region MoveTo methods

    /// <summary>
    /// Moves to the given pose at the specified speed.
    /// The time delta is computed as distance / speed from the last waypoint's position.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Pose3<T> pose, Speed<T> speed)
    {
        if (_count == 0)
            return Add(pose);

        var lastPose = _buffer[_count - 1].Pose;
        var distance = Pose3<T>.Distance(lastPose, pose);
        var dt = distance / speed.Value;
        _currentTime += dt;

        EnsureCapacity(1);
        _buffer[_count++] = new Waypoint3<T>(pose, _currentTime);
        return this;
    }

    /// <summary>
    /// Moves to the given pose at the specified speed with a corner blend.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Pose3<T> pose, Speed<T> speed, Corner<T> corner)
    {
        if (_count == 0)
            return Add(pose);

        var lastPose = _buffer[_count - 1].Pose;
        var distance = Pose3<T>.Distance(lastPose, pose);
        var dt = distance / speed.Value;
        _currentTime += dt;

        EnsureCapacity(1);
        _corners[_count] = corner;
        _hasCorners = _hasCorners || !corner.IsSharp;
        _buffer[_count++] = new Waypoint3<T>(pose, _currentTime);
        return this;
    }

    /// <summary>
    /// Moves to the given pose at the specified speed with a corner blend and explicit corner speed.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Pose3<T> pose, Speed<T> speed, Corner<T> corner, Speed<T> cornerSpeed)
    {
        if (_count == 0)
            return Add(pose);

        var lastPose = _buffer[_count - 1].Pose;
        var distance = Pose3<T>.Distance(lastPose, pose);
        var dt = distance / speed.Value;
        _currentTime += dt;

        EnsureCapacity(1);
        _corners[_count] = corner;
        _cornerSpeeds[_count] = cornerSpeed;
        _hasCorners = _hasCorners || !corner.IsSharp;
        _buffer[_count++] = new Waypoint3<T>(pose, _currentTime);
        return this;
    }

    /// <summary>
    /// Moves to the given position and rotation at the specified speed.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Point3<T> position, Rotation3<T> rotation, Speed<T> speed)
        => MoveTo(new Pose3<T>(position, rotation), speed);

    /// <summary>
    /// Moves to the given position and rotation at the specified speed with a corner blend.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Point3<T> position, Rotation3<T> rotation, Speed<T> speed, Corner<T> corner)
        => MoveTo(new Pose3<T>(position, rotation), speed, corner);

    /// <summary>
    /// Moves to the given position and rotation at the specified speed with a corner blend and explicit corner speed.
    /// </summary>
    public Trajectory3Builder<T> MoveTo(Point3<T> position, Rotation3<T> rotation, Speed<T> speed, Corner<T> corner, Speed<T> cornerSpeed)
        => MoveTo(new Pose3<T>(position, rotation), speed, corner, cornerSpeed);

    #endregion

    #region Append methods

    /// <summary>
    /// Appends a span of waypoints. Updates the current accumulated time to the last waypoint's time.
    /// </summary>
    public Trajectory3Builder<T> AddRange(ReadOnlySpan<Waypoint3<T>> waypoints)
    {
        EnsureCapacity(waypoints.Length);
        waypoints.CopyTo(_buffer.AsSpan(_count));
        _count += waypoints.Length;
        if (waypoints.Length > 0)
            _currentTime = waypoints[^1].Time;
        return this;
    }

    /// <summary>
    /// Appends all waypoints from an existing trajectory, shifting timestamps so the first
    /// appended waypoint starts at the current accumulated time.
    /// </summary>
    public Trajectory3Builder<T> AddTrajectory(in Trajectory3<T> trajectory)
    {
        var span = trajectory.AsSpan();
        if (span.Length == 0) return this;

        var offset = _currentTime - span[0].Time;
        EnsureCapacity(span.Length);
        for (int i = 0; i < span.Length; i++)
            _buffer[_count++] = new Waypoint3<T>(span[i].Pose, span[i].Time + offset);
        _currentTime = span[^1].Time + offset;
        return this;
    }

    /// <summary>
    /// Appends a pose path with uniformly distributed timestamps between startTime and endTime.
    /// </summary>
    public Trajectory3Builder<T> AddPath(in PosePath3<T> path, T startTime, T endTime)
    {
        var span = path.AsSpan();
        if (span.Length == 0) return this;
        if (span.Length == 1)
            return Add(span[0], startTime);

        var n = T.CreateTruncating(span.Length - 1);
        EnsureCapacity(span.Length);
        for (int i = 0; i < span.Length; i++)
        {
            var t = startTime + (endTime - startTime) * T.CreateTruncating(i) / n;
            _buffer[_count++] = new Waypoint3<T>(span[i], t);
        }
        _currentTime = endTime;
        return this;
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds an immutable <see cref="Trajectory3{T}"/> from the accumulated waypoints.
    /// Corners with <see cref="Corner{T}.Round"/> are expanded into smooth arcs.
    /// Uses <see cref="Alloc"/> to respect active <see cref="AllocationScope"/>.
    /// </summary>
    public Trajectory3<T> Build()
    {
        if (_count == 0) return new Trajectory3<T>();

        // Fast path: no corners to expand
        if (!_hasCorners)
        {
            var mem = Alloc.Memory<Waypoint3<T>>(_count);
            _buffer.AsSpan(0, _count).CopyTo(mem.Span);
            return new Trajectory3<T>(mem);
        }

        // Slow path: expand corners into arc waypoints
        var output = new List<Waypoint3<T>>(_count * 2);
        var timeShift = T.Zero;

        for (int i = 0; i < _count; i++)
        {
            // First and last waypoints cannot have corners
            if (i == 0 || i == _count - 1 || _corners[i].IsSharp)
            {
                output.Add(new Waypoint3<T>(_buffer[i].Pose, _buffer[i].Time + timeShift));
                continue;
            }

            ExpandCorner(output, i, ref timeShift);
        }

        var result = Alloc.Memory<Waypoint3<T>>(output.Count);
        CollectionsMarshal.AsSpan(output).CopyTo(result.Span);
        return new Trajectory3<T>(result);
    }

    private void ExpandCorner(List<Waypoint3<T>> output, int index, ref T timeShift)
    {
        var prev = _buffer[index - 1];
        var curr = _buffer[index];
        var next = _buffer[index + 1];
        var corner = _corners[index];
        var cornerSpeed = _cornerSpeeds[index];

        var prevPos = prev.Position;
        var currPos = curr.Position;
        var nextPos = next.Position;

        // Directions from corner toward adjacent waypoints
        var dIn = prevPos - currPos;   // Vector3<T>: from curr toward prev
        var dOut = nextPos - currPos;  // Vector3<T>: from curr toward next
        var distIn = dIn.Length;
        var distOut = dOut.Length;

        // Clamp radius to half the shorter adjacent segment (prevents overlap)
        var maxR = T.Min(distIn, distOut) / Two;
        var r = T.Min(corner.Radius, maxR);

        if (distIn < Epsilon || distOut < Epsilon || r < Epsilon)
        {
            output.Add(new Waypoint3<T>(curr.Pose, curr.Time + timeShift));
            return;
        }

        var dInNorm = dIn / distIn;
        var dOutNorm = dOut / distOut;

        // Angle between the two directions from the corner vertex (interior angle)
        var cosTheta = T.Clamp(Vector3<T>.Dot(dInNorm, dOutNorm), -T.One, T.One);
        var theta = T.Acos(cosTheta);

        // Deflection angle (how much the path actually turns)
        var alpha = T.Pi - theta;

        // If nearly straight, skip blending
        if (alpha < T.CreateTruncating(0.01))
        {
            output.Add(new Waypoint3<T>(curr.Pose, curr.Time + timeShift));
            return;
        }

        // Blend start/end points
        var p1 = currPos + dInNorm * r;   // on incoming segment, r from corner
        var p2 = currPos + dOutNorm * r;  // on outgoing segment, r from corner

        // Arc geometry: inscribed circle tangent to both segments at P1 and P2
        var halfTheta = theta / Two;
        var sinHalf = T.Sin(halfTheta);
        var cosHalf = T.Cos(halfTheta);

        if (T.Abs(cosHalf) < Epsilon)
        {
            // Degenerate case (U-turn), fall back to sharp corner
            output.Add(new Waypoint3<T>(curr.Pose, curr.Time + timeShift));
            return;
        }

        var arcRadius = r * sinHalf / cosHalf; // R = r * tan(theta/2)
        var bisector = dInNorm + dOutNorm;
        var bisectorLen = bisector.Length;

        if (bisectorLen < Epsilon)
        {
            output.Add(new Waypoint3<T>(curr.Pose, curr.Time + timeShift));
            return;
        }

        bisector = bisector / bisectorLen;
        var centerDist = r / cosHalf;
        var arcCenter = currPos + bisector * centerDist;

        // Radius vectors from arc center to blend points
        var v1 = p1 - arcCenter;
        var v2 = p2 - arcCenter;

        // Rotation axis: cross(v1, v2) gives correct direction for P1→P2 arc
        var arcNormal = Vector3<T>.Cross(v1, v2);
        var arcNormalLen = arcNormal.Length;

        if (arcNormalLen < Epsilon)
        {
            output.Add(new Waypoint3<T>(curr.Pose, curr.Time + timeShift));
            return;
        }

        arcNormal = arcNormal / arcNormalLen;

        // Timestamps (with accumulated shift from previous corners)
        var prevTime = prev.Time + timeShift;
        var currTime = curr.Time + timeShift;
        var nextTime = next.Time + timeShift;

        // P1 time: proportional position on incoming segment
        var t_P1 = prevTime + (currTime - prevTime) * (distIn - r) / distIn;
        // P2 natural time: proportional position on outgoing segment
        var t_P2_natural = currTime + (nextTime - currTime) * r / distOut;

        // Arc length and duration
        var arcLen = arcRadius * alpha;
        var naturalDuration = t_P2_natural - t_P1;
        T arcDuration;

        if (cornerSpeed._val > T.Zero)
            arcDuration = arcLen / cornerSpeed._val;
        else
            arcDuration = naturalDuration;

        var t_P2 = t_P1 + arcDuration;

        // Update cumulative time shift
        timeShift += arcDuration - naturalDuration;

        // Rotation interpolation: compute rotations at blend start/end
        var rotFracIn = (distIn - r) / distIn;  // how far P1 is along prev→curr (0..1)
        var rotFracOut = r / distOut;            // how far P2 is along curr→next (0..1)
        var rotP1 = Rotation3<T>.Slerp(prev.Rotation, curr.Rotation, rotFracIn);
        var rotP2 = Rotation3<T>.Slerp(curr.Rotation, next.Rotation, rotFracOut);

        // Number of arc points (proportional to turn angle)
        int arcPointCount = Math.Max(3,
            (int)Math.Ceiling(double.CreateTruncating(alpha) / (Math.PI / 2.0) * ArcResolution));

        // Generate arc points via Rodrigues rotation
        for (int j = 0; j < arcPointCount; j++)
        {
            var t = T.CreateTruncating(j) / T.CreateTruncating(arcPointCount - 1);

            // Rotate v1 around arcNormal by angle (alpha * t)
            var angle = alpha * t;
            var cosA = T.Cos(angle);
            var sinA = T.Sin(angle);
            var crossNV1 = Vector3<T>.Cross(arcNormal, v1);
            var vRot = v1 * cosA + crossNV1 * sinA;

            var arcPos = arcCenter + vRot;
            var arcTime = t_P1 + arcDuration * t;
            var arcRot = Rotation3<T>.Slerp(rotP1, rotP2, t);

            output.Add(new Waypoint3<T>(new Pose3<T>(arcPos, arcRot), arcTime));
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int additional)
    {
        var required = _count + additional;
        if (required <= _buffer.Length) return;
        var newSize = _buffer.Length;
        while (newSize < required) newSize *= 2;
        Array.Resize(ref _buffer, newSize);
        Array.Resize(ref _corners, newSize);
        Array.Resize(ref _cornerSpeeds, newSize);
    }
}
