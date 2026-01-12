using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a point in 3D space with generic numeric coordinates.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
[Point3JsonConverterAttribute]
public struct Point3<T> : IEquatable<Point3<T>>, IParsable<Point3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private T _x;
    private T _y;
    private T _z;

    /// <summary>
    /// Represents a point at the origin (0, 0, 0).
    /// </summary>
    public static readonly Point3<T> Zero = new(T.Zero, T.Zero, T.Zero);

    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>
    /// Initializes a new instance of the Point3 struct with the specified coordinates.
    /// </summary>
    public Point3(T x, T y, T z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>
    /// Initializes a new instance of the Point3 struct from a System.Numerics.Vector3.
    /// </summary>
    public Point3(System.Numerics.Vector3 vector)
    {
        _x = T.CreateTruncating(vector.X);
        _y = T.CreateTruncating(vector.Y);
        _z = T.CreateTruncating(vector.Z);
    }

    /// <summary>
    /// Gets or sets the X coordinate of this point.
    /// </summary>
    [ProtoMember(1)]
    public T X
    {
        readonly get => _x;
        set => _x = value;
    }

    /// <summary>
    /// Gets or sets the Y coordinate of this point.
    /// </summary>
    [ProtoMember(2)]
    public T Y
    {
        readonly get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Gets or sets the Z coordinate of this point.
    /// </summary>
    [ProtoMember(3)]
    public T Z
    {
        readonly get => _z;
        set => _z = value;
    }

    /// <summary>
    /// Gets a value indicating whether this point is at the origin.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => _x == T.Zero && _y == T.Zero && _z == T.Zero;

    /// <summary>
    /// Generates a random point within a unit cube [0,1]^3.
    /// </summary>
    public static Point3<T> Random()
    {
        return new Point3<T>(
            T.CreateTruncating(System.Random.Shared.NextDouble()),
            T.CreateTruncating(System.Random.Shared.NextDouble()),
            T.CreateTruncating(System.Random.Shared.NextDouble()));
    }

    /// <summary>
    /// Generates a random point within the specified dimensions.
    /// </summary>
    public static Point3<T> Random(T width, T height, T depth)
    {
        return new Point3<T>(
            T.CreateTruncating(System.Random.Shared.NextDouble()) * width,
            T.CreateTruncating(System.Random.Shared.NextDouble()) * height,
            T.CreateTruncating(System.Random.Shared.NextDouble()) * depth);
    }

    /// <summary>
    /// Calculates the midpoint between two points.
    /// </summary>
    public static Point3<T> Middle(Point3<T> a, Point3<T> b)
    {
        return new Point3<T>((a._x + b._x) / Two, (a._y + b._y) / Two, (a._z + b._z) / Two);
    }

    /// <summary>
    /// Calculates the Euclidean distance between two points.
    /// </summary>
    public static T Distance(Point3<T> a, Point3<T> b)
    {
        var dx = b._x - a._x;
        var dy = b._y - a._y;
        var dz = b._z - a._z;
        return T.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the squared distance between two points (more efficient than Distance).
    /// </summary>
    public static T DistanceSquared(Point3<T> a, Point3<T> b)
    {
        var dx = b._x - a._x;
        var dy = b._y - a._y;
        var dz = b._z - a._z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Linearly interpolates between two points.
    /// </summary>
    public static Point3<T> Lerp(Point3<T> a, Point3<T> b, T t)
    {
        return new Point3<T>(
            a._x + (b._x - a._x) * t,
            a._y + (b._y - a._y) * t,
            a._z + (b._z - a._z) * t);
    }

    /// <summary>
    /// Converts this point to a different numeric type.
    /// </summary>
    public Point3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Point3<U>(U.CreateTruncating(_x), U.CreateTruncating(_y), U.CreateTruncating(_z));
    }

    #region Operators

    public static Point3<T> operator +(Point3<T> pt, Vector3<T> v) => new(pt._x + v.X, pt._y + v.Y, pt._z + v.Z);
    public static Point3<T> operator -(Point3<T> pt, Vector3<T> v) => new(pt._x - v.X, pt._y - v.Y, pt._z - v.Z);
    public static Vector3<T> operator -(Point3<T> a, Point3<T> b) => new(a._x - b._x, a._y - b._y, a._z - b._z);
    public static Point3<T> operator *(Point3<T> pt, T scalar) => new(pt._x * scalar, pt._y * scalar, pt._z * scalar);
    public static Point3<T> operator /(Point3<T> pt, T scalar) => new(pt._x / scalar, pt._y / scalar, pt._z / scalar);
    public static Pose3<T> operator +(Point3<T> pt, Rotation3<T> r) => new(pt, r);
    public static bool operator ==(Point3<T> a, Point3<T> b) => a._x == b._x && a._y == b._y && a._z == b._z;
    public static bool operator !=(Point3<T> a, Point3<T> b) => !(a == b);

    #endregion

    #region Conversions

    public static implicit operator Vector3<T>(Point3<T> pt) => new(pt._x, pt._y, pt._z);
    public static explicit operator Point3<T>(Vector3<T> v) => new(v.X, v.Y, v.Z);

    public static implicit operator Point3<T>((T x, T y, T z) tuple) => new(tuple.x, tuple.y, tuple.z);
    public static implicit operator (T x, T y, T z)(Point3<T> pt) => (pt._x, pt._y, pt._z);

    public static explicit operator System.Numerics.Vector3(Point3<T> pt) =>
        new(float.CreateTruncating(pt._x), float.CreateTruncating(pt._y), float.CreateTruncating(pt._z));

    public static explicit operator Point3<T>(System.Numerics.Vector3 v) =>
        new(T.CreateTruncating(v.X), T.CreateTruncating(v.Y), T.CreateTruncating(v.Z));

    #endregion

    #region Equality & Formatting

    public readonly bool Equals(Point3<T> other) => this == other;
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Point3<T> pt && Equals(pt);
    public override readonly int GetHashCode() => HashCode.Combine(_x, _y, _z);

    public override readonly string ToString() => $"{{X={_x}, Y={_y}, Z={_z}}}";

    #endregion

    #region Parsing

    public static Point3<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            throw new FormatException($"Invalid Point3<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Point3<T> format: {source}");

        return new Point3<T>(x, y, z);
    }

    public static bool TryParse(string? source, IFormatProvider? provider, out Point3<T> result)
    {
        result = Zero;
        if (source == null) return false;

        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
            !tokenizer.HasNoMoreTokens())
            return false;

        result = new Point3<T>(x, y, z);
        return true;
    }

    #endregion
}
