using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D vector with X, Y, and Z components, using generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type used for vector components.</typeparam>
[ProtoContract]
[Vector3JsonConverterAttribute]
public struct Vector3<T> : IFormattable, IEquatable<Vector3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private T _x;
    private T _y;
    private T _z;

    /// <summary>
    /// Represents the unit vector in the X direction (1, 0, 0).
    /// </summary>
    public static readonly Vector3<T> EX = new(T.One, T.Zero, T.Zero);

    /// <summary>
    /// Represents the unit vector in the Y direction (0, 1, 0).
    /// </summary>
    public static readonly Vector3<T> EY = new(T.Zero, T.One, T.Zero);

    /// <summary>
    /// Represents the unit vector in the Z direction (0, 0, 1).
    /// </summary>
    public static readonly Vector3<T> EZ = new(T.Zero, T.Zero, T.One);

    /// <summary>
    /// Represents the zero vector (0, 0, 0).
    /// </summary>
    public static readonly Vector3<T> Zero = new(T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Initializes a new instance of the Vector3 struct with the specified components.
    /// </summary>
    public Vector3(T x, T y, T z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>
    /// Gets or sets the X component of this vector.
    /// </summary>
    [ProtoMember(1)]
    public T X
    {
        get => _x;
        set => _x = value;
    }

    /// <summary>
    /// Gets or sets the Y component of this vector.
    /// </summary>
    [ProtoMember(2)]
    public T Y
    {
        get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Gets or sets the Z component of this vector.
    /// </summary>
    [ProtoMember(3)]
    public T Z
    {
        get => _z;
        set => _z = value;
    }

    /// <summary>
    /// Gets the length (magnitude) of this vector.
    /// </summary>
    [JsonIgnore]
    public T Length => T.Sqrt(LengthSquared);

    /// <summary>
    /// Gets the squared length of this vector.
    /// </summary>
    [JsonIgnore]
    public T LengthSquared => _x * _x + _y * _y + _z * _z;

    /// <summary>
    /// Creates a vector from the specified components.
    /// </summary>
    public static Vector3<T> From(T x, T y, T z) => new(x, y, z);

    /// <summary>
    /// Creates a vector with all components set to the same value.
    /// </summary>
    public static Vector3<T> From(T s) => new(s, s, s);

    /// <summary>
    /// Creates a random unit vector.
    /// </summary>
    public static Vector3<T> Random()
    {
        var theta = T.CreateTruncating(System.Random.Shared.NextDouble()) * T.Pi * T.CreateTruncating(2);
        var phi = T.Acos(T.CreateTruncating(2 * System.Random.Shared.NextDouble() - 1));

        var x = T.Sin(phi) * T.Cos(theta);
        var y = T.Sin(phi) * T.Sin(theta);
        var z = T.Cos(phi);
        return new Vector3<T>(x, y, z);
    }

    /// <summary>
    /// Returns a normalized (unit) vector in the same direction as this vector.
    /// </summary>
    public Vector3<T> Normalize()
    {
        T ls = LengthSquared;
        if (T.Abs(ls - T.One) < T.Epsilon)
            return this;
        if (ls == T.Zero)
            throw new ArgumentException("Cannot normalize zero vector.");
        T l = T.Sqrt(ls);
        return this / l;
    }

    /// <summary>
    /// Negates this vector (reverses its direction).
    /// </summary>
    public void Negate()
    {
        _x = -_x;
        _y = -_y;
        _z = -_z;
    }

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    public static T Dot(Vector3<T> a, Vector3<T> b) => a._x * b._x + a._y * b._y + a._z * b._z;

    /// <summary>
    /// Calculates the cross product of two vectors.
    /// </summary>
    public static Vector3<T> Cross(Vector3<T> a, Vector3<T> b) => new(
        a._y * b._z - a._z * b._y,
        a._z * b._x - a._x * b._z,
        a._x * b._y - a._y * b._x);

    /// <summary>
    /// Calculates the angle between two vectors in radians.
    /// </summary>
    public static T AngleBetween(Vector3<T> a, Vector3<T> b)
    {
        T cosTheta = Dot(a, b) / (a.Length * b.Length);
        return T.Acos(T.Clamp(cosTheta, -T.One, T.One));
    }

    /// <summary>
    /// Computes the rotation that transforms this vector to align with the target vector.
    /// </summary>
    /// <param name="target">The target direction to rotate towards.</param>
    /// <returns>A rotation R such that R.Rotate(this.Normalize()) ≈ target.Normalize().</returns>
    /// <remarks>
    /// When vectors are parallel, returns identity. When anti-parallel, rotation axis is chosen arbitrarily.
    /// </remarks>
    public Rotation3<T> RotationTo(Vector3<T> target)
    {
        var epsilon = T.CreateTruncating(1e-6);

        var from = Normalize();
        var to = target.Normalize();

        var dot = Dot(from, to);

        // Parallel vectors (same direction)
        if (dot > T.One - epsilon)
            return Rotation3<T>.Identity;

        // Anti-parallel vectors (opposite direction)
        if (dot < -T.One + epsilon)
        {
            // Find a perpendicular axis
            var axis = T.Abs(from.X) < T.CreateTruncating(0.9)
                ? Cross(from, EX).Normalize()
                : Cross(from, EY).Normalize();

            // 180 degree rotation around the perpendicular axis
            return RotationFromAxisAngle(axis, T.Pi);
        }

        // General case: rotation around cross product axis
        var rotationAxis = Cross(from, to).Normalize();
        var angle = T.Acos(T.Clamp(dot, -T.One, T.One));

        return RotationFromAxisAngle(rotationAxis, angle);
    }

    /// <summary>
    /// Creates a Rotation3 from axis-angle representation.
    /// </summary>
    private static Rotation3<T> RotationFromAxisAngle(Vector3<T> axis, T angleRadians)
    {
        var halfAngle = angleRadians / T.CreateTruncating(2);
        var s = T.Sin(halfAngle);
        var c = T.Cos(halfAngle);

        // Quaternion components
        var qw = c;
        var qx = axis.X * s;
        var qy = axis.Y * s;
        var qz = axis.Z * s;

        // Convert quaternion to Euler angles (ZYX convention)
        // Roll (X)
        var sinRoll = T.CreateTruncating(2) * (qw * qx + qy * qz);
        var cosRoll = T.One - T.CreateTruncating(2) * (qx * qx + qy * qy);
        var rx = T.Atan2(sinRoll, cosRoll);

        // Pitch (Y)
        var sinPitch = T.CreateTruncating(2) * (qw * qy - qz * qx);
        sinPitch = T.Clamp(sinPitch, -T.One, T.One);
        var ry = T.Asin(sinPitch);

        // Yaw (Z)
        var sinYaw = T.CreateTruncating(2) * (qw * qz + qx * qy);
        var cosYaw = T.One - T.CreateTruncating(2) * (qy * qy + qz * qz);
        var rz = T.Atan2(sinYaw, cosYaw);

        // rx, ry, rz are in radians — convert via Radian<T> → Degree<T>
        return new Rotation3<T>(
            (Degree<T>)Radian<T>.FromRadian(rx),
            (Degree<T>)Radian<T>.FromRadian(ry),
            (Degree<T>)Radian<T>.FromRadian(rz));
    }

    /// <summary>
    /// Calculates the projection of this vector onto another vector.
    /// </summary>
    public Vector3<T> ProjectOnto(Vector3<T> direction)
    {
        T dot = Dot(this, direction);
        T lenSq = direction.LengthSquared;
        return direction * (dot / lenSq);
    }

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    public static Vector3<T> Lerp(Vector3<T> a, Vector3<T> b, T t) => a + (b - a) * t;

    /// <summary>
    /// Converts this vector to a different numeric type.
    /// </summary>
    public Vector3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Vector3<U>(U.CreateTruncating(_x), U.CreateTruncating(_y), U.CreateTruncating(_z));
    }

    #region Operators

    /// <summary>
    /// Adds two 3D vectors together.
    /// </summary>
    public static Vector3<T> operator +(Vector3<T> a, Vector3<T> b) => new(a._x + b._x, a._y + b._y, a._z + b._z);
    /// <summary>
    /// Subtracts one 3D vector from another.
    /// </summary>
    public static Vector3<T> operator -(Vector3<T> a, Vector3<T> b) => new(a._x - b._x, a._y - b._y, a._z - b._z);
    /// <summary>
    /// Negates a 3D vector (reverses its direction).
    /// </summary>
    public static Vector3<T> operator -(Vector3<T> v) => new(-v._x, -v._y, -v._z);
    /// <summary>
    /// Multiplies a 3D vector by a scalar.
    /// </summary>
    public static Vector3<T> operator *(Vector3<T> v, T scalar) => new(v._x * scalar, v._y * scalar, v._z * scalar);
    /// <summary>
    /// Multiplies a scalar by a 3D vector.
    /// </summary>
    public static Vector3<T> operator *(T scalar, Vector3<T> v) => new(v._x * scalar, v._y * scalar, v._z * scalar);
    /// <summary>
    /// Divides a 3D vector by a scalar.
    /// </summary>
    public static Vector3<T> operator /(Vector3<T> v, T scalar) => new(v._x / scalar, v._y / scalar, v._z / scalar);
    /// <summary>
    /// Computes the dot product of two 3D vectors.
    /// </summary>
    public static T operator *(Vector3<T> a, Vector3<T> b) => Dot(a, b);
    /// <summary>
    /// Combines a 3D vector (as position) with a rotation to create a pose.
    /// </summary>
    public static Pose3<T> operator +(Vector3<T> v, Rotation3<T> r) => new((Point3<T>)v, r);
    /// <summary>
    /// Determines whether two 3D vectors are equal.
    /// </summary>
    public static bool operator ==(Vector3<T> a, Vector3<T> b) => a._x == b._x && a._y == b._y && a._z == b._z;
    /// <summary>
    /// Determines whether two 3D vectors are not equal.
    /// </summary>
    public static bool operator !=(Vector3<T> a, Vector3<T> b) => !(a == b);

    #endregion

    #region Conversions

    /// <summary>
    /// Implicitly converts a tuple to a Vector3.
    /// </summary>
    public static implicit operator Vector3<T>((T x, T y, T z) tuple) => new(tuple.x, tuple.y, tuple.z);
    /// <summary>
    /// Implicitly converts a Vector3 to a tuple.
    /// </summary>
    public static implicit operator (T x, T y, T z)(Vector3<T> v) => (v._x, v._y, v._z);

    /// <summary>
    /// Explicitly converts a generic Vector3 to a System.Numerics.Vector3 (float precision).
    /// </summary>
    public static explicit operator System.Numerics.Vector3(Vector3<T> v) =>
        new(float.CreateTruncating(v._x), float.CreateTruncating(v._y), float.CreateTruncating(v._z));

    /// <summary>
    /// Explicitly converts a System.Numerics.Vector3 to a generic Vector3.
    /// </summary>
    public static explicit operator Vector3<T>(System.Numerics.Vector3 v) =>
        new(T.CreateTruncating(v.X), T.CreateTruncating(v.Y), T.CreateTruncating(v.Z));

    /// <summary>
    /// Converts a direction vector to a rotation that would rotate +Z axis to point in this direction.
    /// Uses ZYX Euler angle convention where rotation order is Rx first, then Ry, then Rz.
    /// </summary>
    public static explicit operator Rotation3<T>(Vector3<T> v)
    {
        var len = v.Length;
        if (len == T.Zero)
            return Rotation3<T>.Identity;

        // Normalize the vector
        var x = v._x / len;
        var y = v._y / len;
        var z = v._z / len;

        // For ZYX quaternion order, rotation is applied as: Rz(Ry(Rx(v)))
        // Starting with +Z = (0, 0, 1), after Rx then Ry then Rz (with rz=0):
        // x = cos(rx)*sin(ry)
        // y = -sin(rx)
        // z = cos(rx)*cos(ry)
        //
        // Therefore: rx = -asin(y), ry = atan2(x, z)

        // Roll (rotation around X) - determines Y component (result in radians)
        var rx = T.Asin(T.Clamp(-y, -T.One, T.One));

        // Pitch (rotation around Y) - determines X/Z ratio (result in radians)
        var ry = T.Atan2(x, z);

        // Yaw (rotation around Z) - not needed for direction, set to 0
        return new Rotation3<T>(
            (Degree<T>)Radian<T>.FromRadian(rx),
            (Degree<T>)Radian<T>.FromRadian(ry),
            Degree<T>.Zero);
    }

    #endregion

    #region Equality & Formatting

    /// <summary>
    /// Determines whether the specified vector is equal to this vector.
    /// </summary>
    public bool Equals(Vector3<T> other) => this == other;
    /// <summary>
    /// Determines whether the specified object is equal to this vector.
    /// </summary>
    public override bool Equals(object? obj) => obj is Vector3<T> v && Equals(v);
    /// <summary>
    /// Returns the hash code for this vector.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(_x, _y, _z);

    /// <summary>
    /// Returns a string representation of this vector.
    /// </summary>
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Returns a string representation of this vector using the specified format and format provider.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        formatProvider ??= CultureInfo.CurrentCulture;
        format ??= string.Empty;
        var separator = NumericListTokenizer.GetSeparator(formatProvider);
        return string.Format(formatProvider, $"{{0:{format}}}{separator}{{1:{format}}}{separator}{{2:{format}}}", _x, _y, _z);
    }

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a string representation of a 3D vector.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="provider">An optional format provider for parsing.</param>
    /// <returns>The parsed 3D vector.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format.</exception>
    public static Vector3<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            throw new FormatException($"Invalid Vector3<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Vector3<T> format: {source}");

        return new Vector3<T>(x, y, z);
    }

    /// <summary>
    /// Tries to parse a string representation of a 3D vector.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="provider">An optional format provider for parsing.</param>
    /// <param name="result">When this method returns, contains the parsed vector if successful.</param>
    /// <returns>true if the string was parsed successfully; otherwise, false.</returns>
    public static bool TryParse(string? source, IFormatProvider? provider, out Vector3<T> result)
    {
        result = Zero;
        if (source == null) return false;

        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
            !tokenizer.HasNoMoreTokens())
            return false;

        result = new Vector3<T>(x, y, z);
        return true;
    }

    #endregion
}
