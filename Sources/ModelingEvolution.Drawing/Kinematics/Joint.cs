using System.Numerics;

namespace ModelingEvolution.Drawing.Kinematics;

/// <summary>
/// The joint that moves link <paramref name="Child"/> relative to its parent link (<c>Links[Child].Parent</c>).
/// Units are metres and radians; the joint value is the vendor joint value (epic-129 design § "Drawing: the kinematic chain").
/// </summary>
/// <param name="Name">The joint name.</param>
/// <param name="Type">Revolute or prismatic.</param>
/// <param name="Child">The index of the child link this joint moves.</param>
/// <param name="Origin">The child link frame in the parent link frame at joint value 0.</param>
/// <param name="Axis">The unit joint axis in the child link frame.</param>
/// <param name="Min">The lower joint limit (radians or metres).</param>
/// <param name="Max">The upper joint limit (radians or metres).</param>
/// <param name="MaxVelocity">The velocity limit (radians or metres per second).</param>
/// <param name="MaxAcceleration">The acceleration limit (per second squared), or null when the vendor gives none.</param>
/// <typeparam name="T">The numeric type.</typeparam>
public readonly record struct Joint<T>(
    string Name, JointType Type, int Child,
    Frame3<T> Origin,
    Vector3<T> Axis,
    T Min, T Max, T MaxVelocity, T? MaxAcceleration)
    where T : struct, INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>;
