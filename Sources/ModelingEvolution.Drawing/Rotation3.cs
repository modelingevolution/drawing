using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D rotation using Euler angles (Roll, Pitch, Yaw) in degrees.
/// Convention: Rotation order is Z-Y-X (Yaw, Pitch, Roll) - also known as Tait-Bryan angles.
/// </summary>
/// <typeparam name="T">The numeric type used for angles.</typeparam>
[ProtoContract]
[Rotation3JsonConverterAttribute]
public struct Rotation3<T> : IEquatable<Rotation3<T>>, IParsable<Rotation3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private T _rx; // Roll - rotation around X axis
    private T _ry; // Pitch - rotation around Y axis
    private T _rz; // Yaw - rotation around Z axis

    private static readonly T Deg2Rad = T.Pi / T.CreateTruncating(180);
    private static readonly T Rad2Deg = T.CreateTruncating(180) / T.Pi;
    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>
    /// Represents zero rotation (identity).
    /// </summary>
    public static readonly Rotation3<T> Identity = new(T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Initializes a new instance with the specified Euler angles in degrees.
    /// </summary>
    /// <param name="rx">Roll - rotation around X axis in degrees.</param>
    /// <param name="ry">Pitch - rotation around Y axis in degrees.</param>
    /// <param name="rz">Yaw - rotation around Z axis in degrees.</param>
    public Rotation3(T rx, T ry, T rz)
    {
        _rx = rx;
        _ry = ry;
        _rz = rz;
    }

    /// <summary>
    /// Gets or sets Roll (rotation around X axis) in degrees.
    /// </summary>
    [ProtoMember(1)]
    public T Rx
    {
        readonly get => _rx;
        set => _rx = value;
    }

    /// <summary>
    /// Gets or sets Pitch (rotation around Y axis) in degrees.
    /// </summary>
    [ProtoMember(2)]
    public T Ry
    {
        readonly get => _ry;
        set => _ry = value;
    }

    /// <summary>
    /// Gets or sets Yaw (rotation around Z axis) in degrees.
    /// </summary>
    [ProtoMember(3)]
    public T Rz
    {
        readonly get => _rz;
        set => _rz = value;
    }

    /// <summary>
    /// Gets a value indicating whether this rotation is identity (no rotation).
    /// </summary>
    public readonly bool IsIdentity => _rx == T.Zero && _ry == T.Zero && _rz == T.Zero;

    /// <summary>
    /// Creates a rotation from degrees.
    /// </summary>
    public static Rotation3<T> FromDegrees(T rx, T ry, T rz) => new(rx, ry, rz);

    /// <summary>
    /// Creates a rotation from radians.
    /// </summary>
    public static Rotation3<T> FromRadians(T rx, T ry, T rz) => new(rx * Rad2Deg, ry * Rad2Deg, rz * Rad2Deg);

    /// <summary>
    /// Gets the rotation angles in radians.
    /// </summary>
    public readonly (T rx, T ry, T rz) ToRadians() => (_rx * Deg2Rad, _ry * Deg2Rad, _rz * Deg2Rad);

    /// <summary>
    /// Converts to a quaternion representation.
    /// </summary>
    public readonly Quaternion<T> ToQuaternion()
    {
        var (rx, ry, rz) = ToRadians();
        var halfRx = rx / Two;
        var halfRy = ry / Two;
        var halfRz = rz / Two;

        var cx = T.Cos(halfRx);
        var sx = T.Sin(halfRx);
        var cy = T.Cos(halfRy);
        var sy = T.Sin(halfRy);
        var cz = T.Cos(halfRz);
        var sz = T.Sin(halfRz);

        // ZYX order
        return new Quaternion<T>(
            w: cx * cy * cz + sx * sy * sz,
            x: sx * cy * cz - cx * sy * sz,
            y: cx * sy * cz + sx * cy * sz,
            z: cx * cy * sz - sx * sy * cz);
    }

    /// <summary>
    /// Creates a rotation from a quaternion.
    /// </summary>
    public static Rotation3<T> FromQuaternion(Quaternion<T> q)
    {
        // Extract Euler angles from quaternion (ZYX order)
        var sinp = Two * (q.W * q.Y - q.Z * q.X);
        T rx, ry, rz;

        var gimbalThreshold = T.One - T.CreateTruncating(1e-6);

        if (T.Abs(sinp) >= gimbalThreshold)
        {
            // Gimbal lock: Ry ≈ ±90°. Rx and Rz share one degree of freedom.
            // Convention: set Rz = 0, recover Rx from rotation matrix R[0][1], R[0][2].
            ry = T.CopySign(T.Pi / Two, sinp);
            rz = T.Zero;

            var r01 = Two * (q.X * q.Y - q.W * q.Z);
            var r02 = Two * (q.X * q.Z + q.W * q.Y);

            rx = sinp > T.Zero
                ? T.Atan2(r01, r02)
                : T.Atan2(-r01, -r02);
        }
        else
        {
            // Normal case
            var sinrCosp = Two * (q.W * q.X + q.Y * q.Z);
            var cosrCosp = T.One - Two * (q.X * q.X + q.Y * q.Y);
            rx = T.Atan2(sinrCosp, cosrCosp);

            ry = T.Asin(sinp);

            var sinyCosp = Two * (q.W * q.Z + q.X * q.Y);
            var cosyCosp = T.One - Two * (q.Y * q.Y + q.Z * q.Z);
            rz = T.Atan2(sinyCosp, cosyCosp);
        }

        return new Rotation3<T>(rx * Rad2Deg, ry * Rad2Deg, rz * Rad2Deg);
    }

    /// <summary>
    /// Rotates a vector by this rotation.
    /// </summary>
    public readonly Vector3<T> Rotate(Vector3<T> v)
    {
        var q = ToQuaternion();
        return q.Rotate(v);
    }

    /// <summary>
    /// Rotates a point by this rotation around the origin.
    /// </summary>
    public readonly Point3<T> Rotate(Point3<T> p)
    {
        var v = Rotate((Vector3<T>)p);
        return (Point3<T>)v;
    }

    /// <summary>
    /// Returns the inverse rotation.
    /// </summary>
    public readonly Rotation3<T> Inverse() => new(-_rx, -_ry, -_rz);

    /// <summary>
    /// Combines two rotations.
    /// </summary>
    public static Rotation3<T> Combine(Rotation3<T> a, Rotation3<T> b)
    {
        var qa = a.ToQuaternion();
        var qb = b.ToQuaternion();
        var combined = qa * qb;
        return FromQuaternion(combined);
    }

    /// <summary>
    /// Linearly interpolates between two rotations (spherical interpolation via quaternions).
    /// </summary>
    public static Rotation3<T> Slerp(Rotation3<T> a, Rotation3<T> b, T t)
    {
        var qa = a.ToQuaternion();
        var qb = b.ToQuaternion();
        var result = Quaternion<T>.Slerp(qa, qb, t);
        return FromQuaternion(result);
    }

    /// <summary>
    /// Converts this rotation to a different numeric type.
    /// </summary>
    public Rotation3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Rotation3<U>(U.CreateTruncating(_rx), U.CreateTruncating(_ry), U.CreateTruncating(_rz));
    }

    #region Operators

    public static Rotation3<T> operator +(Rotation3<T> a, Rotation3<T> b) => Combine(a, b);
    public static Rotation3<T> operator -(Rotation3<T> r) => r.Inverse();
    public static bool operator ==(Rotation3<T> a, Rotation3<T> b) => a._rx == b._rx && a._ry == b._ry && a._rz == b._rz;
    public static bool operator !=(Rotation3<T> a, Rotation3<T> b) => !(a == b);

    #endregion

    #region Conversions

    public static implicit operator Rotation3<T>((T rx, T ry, T rz) tuple) => new(tuple.rx, tuple.ry, tuple.rz);
    public static implicit operator (T rx, T ry, T rz)(Rotation3<T> r) => (r._rx, r._ry, r._rz);

    /// <summary>
    /// Converts a rotation to a unit vector (versor) representing the direction
    /// that the +Z axis would point after applying this rotation.
    /// </summary>
    public static explicit operator Vector3<T>(Rotation3<T> r) => r.Rotate(Vector3<T>.EZ);

    #endregion

    #region Equality & Formatting

    public readonly bool Equals(Rotation3<T> other) => this == other;
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rotation3<T> r && Equals(r);
    public override readonly int GetHashCode() => HashCode.Combine(_rx, _ry, _rz);
    public override readonly string ToString() => $"{{Rx={_rx}, Ry={_ry}, Rz={_rz}}}";

    #endregion

    #region Parsing

    public static Rotation3<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz))
            throw new FormatException($"Invalid Rotation3<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Rotation3<T> format: {source}");

        return new Rotation3<T>(rx, ry, rz);
    }

    public static bool TryParse(string? source, IFormatProvider? provider, out Rotation3<T> result)
    {
        result = Identity;
        if (source == null) return false;

        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz) ||
            !tokenizer.HasNoMoreTokens())
            return false;

        result = new Rotation3<T>(rx, ry, rz);
        return true;
    }

    #endregion
}
