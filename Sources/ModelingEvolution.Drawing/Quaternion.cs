using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a quaternion for 3D rotations using generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type used for components.</typeparam>
[ProtoContract]
public readonly struct Quaternion<T> : IEquatable<Quaternion<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly T _w;
    private readonly T _x;
    private readonly T _y;
    private readonly T _z;

    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T SlerpThreshold = T.One - T.CreateTruncating(1e-6);
    private static readonly T OppositeThreshold = T.CreateTruncating(1e-12);

    /// <summary>
    /// Represents the identity quaternion (no rotation).
    /// </summary>
    public static readonly Quaternion<T> Identity = new(T.One, T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Initializes a new quaternion with the specified components.
    /// </summary>
    public Quaternion(T w, T x, T y, T z)
    {
        _w = w;
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>
    /// Gets or initializes the W (scalar) component.
    /// </summary>
    [ProtoMember(1)]
    public T W
    {
        get => _w;
        init => _w = value;
    }

    /// <summary>
    /// Gets or initializes the X component.
    /// </summary>
    [ProtoMember(2)]
    public T X
    {
        get => _x;
        init => _x = value;
    }

    /// <summary>
    /// Gets or initializes the Y component.
    /// </summary>
    [ProtoMember(3)]
    public T Y
    {
        get => _y;
        init => _y = value;
    }

    /// <summary>
    /// Gets or initializes the Z component.
    /// </summary>
    [ProtoMember(4)]
    public T Z
    {
        get => _z;
        init => _z = value;
    }

    /// <summary>
    /// Gets the magnitude (length) of this quaternion.
    /// </summary>
    public T Length => T.Sqrt(LengthSquared);

    /// <summary>
    /// Gets the squared magnitude of this quaternion.
    /// </summary>
    public T LengthSquared => _w * _w + _x * _x + _y * _y + _z * _z;

    /// <summary>
    /// Gets a value indicating whether this quaternion is normalized (unit length).
    /// </summary>
    public bool IsNormalized => T.Abs(LengthSquared - T.One) < T.Epsilon;

    /// <summary>
    /// Returns a normalized (unit) quaternion.
    /// </summary>
    public Quaternion<T> Normalize()
    {
        var len = Length;
        if (len == T.Zero)
            throw new ArgumentException("Cannot normalize zero quaternion.");
        return new Quaternion<T>(_w / len, _x / len, _y / len, _z / len);
    }

    /// <summary>
    /// Returns the conjugate of this quaternion.
    /// </summary>
    public Quaternion<T> Conjugate() => new(_w, -_x, -_y, -_z);

    /// <summary>
    /// Returns the inverse of this quaternion.
    /// </summary>
    public Quaternion<T> Inverse()
    {
        var lenSq = LengthSquared;
        return new Quaternion<T>(_w / lenSq, -_x / lenSq, -_y / lenSq, -_z / lenSq);
    }

    /// <summary>
    /// Creates a quaternion from an axis and angle.
    /// </summary>
    /// <param name="axis">The rotation axis (should be normalized).</param>
    /// <param name="angleRadians">The rotation angle in radians.</param>
    public static Quaternion<T> FromAxisAngle(Vector3<T> axis, T angleRadians)
    {
        var halfAngle = angleRadians / Two;
        var s = T.Sin(halfAngle);
        return new Quaternion<T>(
            T.Cos(halfAngle),
            axis.X * s,
            axis.Y * s,
            axis.Z * s);
    }

    /// <summary>
    /// Creates the rotation Rz(yaw)·Ry(pitch)·Rx(roll) from angles in radians: the URDF <c>rpy</c> convention and
    /// the ZYX convention of <see cref="Rotation3{T}"/> and of the asset files.
    /// </summary>
    /// <param name="roll">Rotation about X, applied first, in radians.</param>
    /// <param name="pitch">Rotation about Y, in radians.</param>
    /// <param name="yaw">Rotation about Z, applied last, in radians.</param>
    public static Quaternion<T> FromRpy(T roll, T pitch, T yaw)
    {
        var cx = T.Cos(roll / Two);
        var sx = T.Sin(roll / Two);
        var cy = T.Cos(pitch / Two);
        var sy = T.Sin(pitch / Two);
        var cz = T.Cos(yaw / Two);
        var sz = T.Sin(yaw / Two);

        return new Quaternion<T>(
            cx * cy * cz + sx * sy * sz,
            sx * cy * cz - cx * sy * sz,
            cx * sy * cz + sx * cy * sz,
            cx * cy * sz - sx * sy * cz);
    }

    /// <summary>
    /// Creates the shortest-arc rotation that turns the direction of <paramref name="from"/> into the direction of
    /// <paramref name="to"/>. For opposite directions it turns by π about an axis perpendicular to <paramref name="from"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when either vector is zero.</exception>
    public static Quaternion<T> FromTo(Vector3<T> from, Vector3<T> to)
    {
        var f = from.Normalize();
        var t = to.Normalize();
        var d = Vector3<T>.Dot(f, t);

        if (T.One + d < OppositeThreshold)
        {
            var axis = Vector3<T>.Cross(Vector3<T>.EX, f);
            if (axis.LengthSquared < OppositeThreshold)
                axis = Vector3<T>.Cross(Vector3<T>.EY, f);
            axis = axis.Normalize();
            return new Quaternion<T>(T.Zero, axis.X, axis.Y, axis.Z);
        }

        var c = Vector3<T>.Cross(f, t);
        return new Quaternion<T>(T.One + d, c.X, c.Y, c.Z).Normalize();
    }

    /// <summary>
    /// Creates the rotation whose rotated unit axes are <paramref name="x"/>, <paramref name="y"/> and
    /// <paramref name="z"/>: Rotate(EX) = x, Rotate(EY) = y, Rotate(EZ) = z. The axes must form a right-handed
    /// orthonormal basis (the columns of the rotation matrix).
    /// </summary>
    public static Quaternion<T> FromAxes(Vector3<T> x, Vector3<T> y, Vector3<T> z) =>
        FromMatrix3x3(Matrix3x3<T>.FromColumns(x, y, z));

    /// <summary>
    /// Creates the unit quaternion of a rotation matrix by Shepperd's method: the branch is picked by the largest of
    /// the trace and the diagonal elements, so no division by a small number happens for any rotation.
    /// The result is normalized. This is the only matrix → rotation conversion in the library.
    /// </summary>
    /// <param name="m">A proper rotation matrix (orthonormal, determinant +1).</param>
    public static Quaternion<T> FromMatrix3x3(in Matrix3x3<T> m)
    {
        var quarter = T.One / (Two * Two);
        var trace = m.M11 + m.M22 + m.M33;
        Quaternion<T> q;

        if (trace > T.Zero)
        {
            var s = T.Sqrt(trace + T.One) * Two;
            q = new Quaternion<T>(quarter * s, (m.M32 - m.M23) / s, (m.M13 - m.M31) / s, (m.M21 - m.M12) / s);
        }
        else if (m.M11 > m.M22 && m.M11 > m.M33)
        {
            var s = T.Sqrt(T.One + m.M11 - m.M22 - m.M33) * Two;
            q = new Quaternion<T>((m.M32 - m.M23) / s, quarter * s, (m.M12 + m.M21) / s, (m.M13 + m.M31) / s);
        }
        else if (m.M22 > m.M33)
        {
            var s = T.Sqrt(T.One + m.M22 - m.M11 - m.M33) * Two;
            q = new Quaternion<T>((m.M13 - m.M31) / s, (m.M12 + m.M21) / s, quarter * s, (m.M23 + m.M32) / s);
        }
        else
        {
            var s = T.Sqrt(T.One + m.M33 - m.M11 - m.M22) * Two;
            q = new Quaternion<T>((m.M21 - m.M12) / s, (m.M13 + m.M31) / s, (m.M23 + m.M32) / s, quarter * s);
        }

        return q.Normalize();
    }

    /// <summary>
    /// Returns the rotation matrix of this unit quaternion: its columns are Rotate(EX), Rotate(EY), Rotate(EZ).
    /// This is the only rotation → matrix conversion in the library.
    /// </summary>
    public Matrix3x3<T> ToMatrix3x3()
    {
        T xx = _x * _x, yy = _y * _y, zz = _z * _z;
        T xy = _x * _y, xz = _x * _z, yz = _y * _z;
        T wx = _w * _x, wy = _w * _y, wz = _w * _z;
        return new Matrix3x3<T>(
            T.One - Two * (yy + zz), Two * (xy - wz), Two * (xz + wy),
            Two * (xy + wz), T.One - Two * (xx + zz), Two * (yz - wx),
            Two * (xz - wy), Two * (yz + wx), T.One - Two * (xx + yy));
    }

    /// <summary>
    /// Rotates a vector by this quaternion.
    /// </summary>
    public Vector3<T> Rotate(Vector3<T> v)
    {
        // q * v * q^-1, optimized
        var qv = new Quaternion<T>(T.Zero, v.X, v.Y, v.Z);
        var result = this * qv * Conjugate();
        return new Vector3<T>(result._x, result._y, result._z);
    }

    /// <summary>
    /// Computes the dot product of two quaternions.
    /// </summary>
    public static T Dot(Quaternion<T> a, Quaternion<T> b)
    {
        return a._w * b._w + a._x * b._x + a._y * b._y + a._z * b._z;
    }

    /// <summary>
    /// Spherical linear interpolation between two quaternions.
    /// </summary>
    public static Quaternion<T> Slerp(Quaternion<T> a, Quaternion<T> b, T t)
    {
        var dot = Dot(a, b);

        // If the dot product is negative, negate one quaternion to take the shorter path
        if (dot < T.Zero)
        {
            b = new Quaternion<T>(-b._w, -b._x, -b._y, -b._z);
            dot = -dot;
        }

        // If quaternions are very close, use linear interpolation
        // Note: T.Epsilon is the smallest representable positive value (e.g. 1.4e-45 for float),
        // NOT machine epsilon. We need a practical threshold to avoid division by sin(~0).
        if (dot > SlerpThreshold)
        {
            return new Quaternion<T>(
                a._w + t * (b._w - a._w),
                a._x + t * (b._x - a._x),
                a._y + t * (b._y - a._y),
                a._z + t * (b._z - a._z)).Normalize();
        }

        var theta0 = T.Acos(dot);
        var theta = theta0 * t;
        var sinTheta = T.Sin(theta);
        var sinTheta0 = T.Sin(theta0);

        var s0 = T.Cos(theta) - dot * sinTheta / sinTheta0;
        var s1 = sinTheta / sinTheta0;

        return new Quaternion<T>(
            s0 * a._w + s1 * b._w,
            s0 * a._x + s1 * b._x,
            s0 * a._y + s1 * b._y,
            s0 * a._z + s1 * b._z);
    }

    /// <summary>
    /// Converts this quaternion to a different numeric type.
    /// </summary>
    public Quaternion<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Quaternion<U>(U.CreateTruncating(_w), U.CreateTruncating(_x), U.CreateTruncating(_y), U.CreateTruncating(_z));
    }

    #region Operators

    public static Quaternion<T> operator *(Quaternion<T> a, Quaternion<T> b)
    {
        return new Quaternion<T>(
            a._w * b._w - a._x * b._x - a._y * b._y - a._z * b._z,
            a._w * b._x + a._x * b._w + a._y * b._z - a._z * b._y,
            a._w * b._y - a._x * b._z + a._y * b._w + a._z * b._x,
            a._w * b._z + a._x * b._y - a._y * b._x + a._z * b._w);
    }

    public static Quaternion<T> operator *(Quaternion<T> q, T scalar) => new(q._w * scalar, q._x * scalar, q._y * scalar, q._z * scalar);
    public static Quaternion<T> operator *(T scalar, Quaternion<T> q) => q * scalar;
    public static Quaternion<T> operator +(Quaternion<T> a, Quaternion<T> b) => new(a._w + b._w, a._x + b._x, a._y + b._y, a._z + b._z);
    public static Quaternion<T> operator -(Quaternion<T> a, Quaternion<T> b) => new(a._w - b._w, a._x - b._x, a._y - b._y, a._z - b._z);
    public static Quaternion<T> operator -(Quaternion<T> q) => new(-q._w, -q._x, -q._y, -q._z);
    public static bool operator ==(Quaternion<T> a, Quaternion<T> b) => a._w == b._w && a._x == b._x && a._y == b._y && a._z == b._z;
    public static bool operator !=(Quaternion<T> a, Quaternion<T> b) => !(a == b);

    #endregion

    #region Conversions

    public static explicit operator System.Numerics.Quaternion(Quaternion<T> q) =>
        new(float.CreateTruncating(q._x), float.CreateTruncating(q._y), float.CreateTruncating(q._z), float.CreateTruncating(q._w));

    public static explicit operator Quaternion<T>(System.Numerics.Quaternion q) =>
        new(T.CreateTruncating(q.W), T.CreateTruncating(q.X), T.CreateTruncating(q.Y), T.CreateTruncating(q.Z));

    #endregion

    #region Equality & Formatting

    public bool Equals(Quaternion<T> other) => this == other;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Quaternion<T> q && Equals(q);
    public override int GetHashCode() => HashCode.Combine(_w, _x, _y, _z);
    public override string ToString() => $"{{W={_w}, X={_x}, Y={_y}, Z={_z}}}";

    #endregion
}
