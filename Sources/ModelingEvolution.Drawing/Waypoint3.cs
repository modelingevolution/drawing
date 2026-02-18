using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a timestamped 3D pose — an element of a <see cref="Trajectory3{T}"/>.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and time.</typeparam>
[ProtoContract]
public readonly record struct Waypoint3<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// The pose at this waypoint.
    /// </summary>
    [ProtoMember(1)]
    public Pose3<T> Pose { get; init; }

    /// <summary>
    /// The time associated with this waypoint (seconds from trajectory start).
    /// </summary>
    [ProtoMember(2)]
    public T Time { get; init; }

    /// <summary>
    /// Initializes a new waypoint with the specified pose and time.
    /// </summary>
    public Waypoint3(Pose3<T> pose, T time)
    {
        Pose = pose;
        Time = time;
    }

    /// <summary>
    /// Gets the position component of this waypoint's pose.
    /// </summary>
    public Point3<T> Position => Pose.Position;

    /// <summary>
    /// Gets the rotation component of this waypoint's pose.
    /// </summary>
    public Rotation3<T> Rotation => Pose.Rotation;

    public override string ToString() => $"Waypoint3({Pose}, t={Time})";
}
