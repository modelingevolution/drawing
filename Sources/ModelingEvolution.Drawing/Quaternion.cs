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
public struct Quaternion<T> : IEquatable<Quaternion<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private T _w;
    private T _x;
    private T _y;
    private T _z;

    private static readonly T Two = T.CreateTruncating(2);

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
    /// Gets or sets the W (scalar) component.
    /// </summary>
    [ProtoMember(1)]
    public T W
    {
        readonly get => _w;
        set => _w = value;
    }

    /// <summary>
    /// Gets or sets the X component.
    /// </summary>
    [ProtoMember(2)]
    public T X
    {
        readonly get => _x;
        set => _x = value;
    }

    /// <summary>
    /// Gets or sets the Y component.
    /// </summary>
    [ProtoMember(3)]
    public T Y
    {
        readonly get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Gets or sets the Z component.
    /// </summary>
    [ProtoMember(4)]
    public T Z
    {
        readonly get => _z;
        set => _z = value;
    }

    /// <summary>
    /// Gets the magnitude (length) of this quaternion.
    /// </summary>
    public readonly T Length => T.Sqrt(LengthSquared);

    /// <summary>
    /// Gets the squared magnitude of this quaternion.
    /// </summary>
    public readonly T LengthSquared => _w * _w + _x * _x + _y * _y + _z * _z;

    /// <summary>
    /// Gets a value indicating whether this quaternion is normalized (unit length).
    /// </summary>
    public readonly bool IsNormalized => T.Abs(LengthSquared - T.One) < T.Epsilon;

    /// <summary>
    /// Returns a normalized (unit) quaternion.
    /// </summary>
    public readonly Quaternion<T> Normalize()
    {
        var len = Length;
        if (len == T.Zero)
            throw new ArgumentException("Cannot normalize zero quaternion.");
        return new Quaternion<T>(_w / len, _x / len, _y / len, _z / len);
    }

    /// <summary>
    /// Returns the conjugate of this quaternion.
    /// </summary>
    public readonly Quaternion<T> Conjugate() => new(_w, -_x, -_y, -_z);

    /// <summary>
    /// Returns the inverse of this quaternion.
    /// </summary>
    public readonly Quaternion<T> Inverse()
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
    /// Rotates a vector by this quaternion.
    /// </summary>
    public readonly Vector3<T> Rotate(Vector3<T> v)
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
        if (dot > T.One - T.Epsilon)
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

    public readonly bool Equals(Quaternion<T> other) => this == other;
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Quaternion<T> q && Equals(q);
    public override readonly int GetHashCode() => HashCode.Combine(_w, _x, _y, _z);
    public override readonly string ToString() => $"{{W={_w}, X={_x}, Y={_y}, Z={_z}}}";

    #endregion
}
