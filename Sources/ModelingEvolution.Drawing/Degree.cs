using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides static methods for trigonometric operations with degree values.
/// </summary>
public static class Degree
{
    /// <summary>
    /// Calculates the sine of an angle specified in degrees.
    /// </summary>
    /// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
    /// <param name="degree">The angle in degrees.</param>
    /// <returns>The sine of the specified angle.</returns>
    public static T Sin<T>(Degree<T> degree) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => Radian.Sin((Radian<T>)degree);
    /// <summary>
    /// Calculates the cosine of an angle specified in degrees.
    /// </summary>
    /// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
    /// <param name="degree">The angle in degrees.</param>
    /// <returns>The cosine of the specified angle.</returns>
    public static T Cos<T>(Degree<T> degree) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => Radian.Cos((Radian<T>)degree);
}
/// <summary>
/// Represents an angle measurement in degrees with generic numeric type support.
/// </summary>
/// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
[DebuggerDisplay("{_val}")]
public readonly record struct Degree<T> : IComparisonOperators<Degree<T>, Degree<T>, bool>, IComparable<Degree<T>>, IComparable 
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>
{
    /// <summary>
    /// Gets a degree value representing zero degrees.
    /// </summary>
    public static Degree<T> Zero { get; } = new Degree<T>();
    internal readonly T _val;
    /// <summary>
    /// Implicitly converts a numeric value to a degree measurement.
    /// </summary>
    /// <param name="src">The numeric value representing degrees.</param>
    /// <returns>A degree instance with the specified value.</returns>
    public static implicit operator Degree<T>(T src) => new(src);
    /// <summary>
    /// Creates a new degree instance with the specified value.
    /// </summary>
    /// <param name="degrees">The angle value in degrees.</param>
    /// <returns>A new degree instance.</returns>
    public static Degree<T> Create(T degrees) => new Degree<T>(degrees);
    private Degree(T value)
    {
        _val = value;
    }

    /// <summary>
    /// Converts this degree value to a different numeric type by truncating the value.
    /// </summary>
    /// <typeparam name="U">The target numeric type.</typeparam>
    /// <returns>A degree instance with the truncated value in the target type.</returns>
    public Degree<U> Truncate<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>
    {
        var v = U.CreateTruncating(this._val);
        return new Degree<U>(v);
    }
    
    /// <summary>
    /// Explicitly converts a degree value to its underlying numeric type.
    /// </summary>
    /// <param name="src">The degree value to convert.</param>
    /// <returns>The underlying numeric value.</returns>
    public static explicit operator T(Degree<T> src)
    {
        return src._val;
    }
    /// <summary>
    /// Subtracts one degree value from another.
    /// </summary>
    /// <param name="a">The first degree value.</param>
    /// <param name="b">The second degree value to subtract.</param>
    /// <returns>The difference between the two degree values.</returns>
    /// <summary>
    /// Negates a degree value.
    /// </summary>
    /// <param name="a">The degree value to negate.</param>
    /// <returns>The negated degree value.</returns>
    public static Degree<T> operator -(Degree<T> a) => new Degree<T>(-a._val);

    public static Degree<T> operator -(Degree<T> a, Degree<T> b)
    {
        return new Degree<T>(a._val - b._val);
    }
    /// <summary>
    /// Adds two degree values together.
    /// </summary>
    /// <param name="a">The first degree value.</param>
    /// <param name="b">The second degree value to add.</param>
    /// <returns>The sum of the two degree values.</returns>
    public static Degree<T> operator +(Degree<T> a, Degree<T> b)
    {
        return new Degree<T>(a._val + b._val);
    }

    /// <summary>
    /// Multiplies a degree value by a scalar.
    /// </summary>
    public static Degree<T> operator *(Degree<T> a, T scalar) => new Degree<T>(a._val * scalar);

    /// <summary>
    /// Multiplies a degree value by a scalar.
    /// </summary>
    public static Degree<T> operator *(T scalar, Degree<T> a) => new Degree<T>(scalar * a._val);

    /// <summary>
    /// Divides a degree value by a scalar.
    /// </summary>
    public static Degree<T> operator /(Degree<T> a, T scalar) => new Degree<T>(a._val / scalar);

    /// <summary>
    /// Returns the absolute value of this degree.
    /// </summary>
    public Degree<T> Abs() => new Degree<T>(T.Abs(_val));

    /// <summary>
    /// Normalizes this angle to the range (-180, 180].
    /// </summary>
    public Degree<T> Normalize()
    {
        var half = T.CreateTruncating(180);
        var full = T.CreateTruncating(360);
        var v = _val % full;
        if (v > half) v -= full;
        if (v <= -half) v += full;
        return new Degree<T>(v);
    }

    /// <summary>
    /// Determines whether the first degree value is less than or equal to the second.
    /// </summary>
    /// <param name="first">The first degree value to compare.</param>
    /// <param name="second">The second degree value to compare.</param>
    /// <returns>true if the first value is less than or equal to the second; otherwise, false.</returns>
    public static bool operator <=(Degree<T> first, Degree<T> second)
    {
        return first._val <= second._val;
    }
    /// <summary>
    /// Determines whether the first degree value is greater than or equal to the second.
    /// </summary>
    /// <param name="first">The first degree value to compare.</param>
    /// <param name="second">The second degree value to compare.</param>
    /// <returns>true if the first value is greater than or equal to the second; otherwise, false.</returns>
    public static bool operator >=(Degree<T> first, Degree<T> second)
    {
        return first._val >= second._val;
    }

    /// <summary>
    /// Determines whether the first degree value is less than the second.
    /// </summary>
    /// <param name="first">The first degree value to compare.</param>
    /// <param name="second">The second degree value to compare.</param>
    /// <returns>true if the first value is less than the second; otherwise, false.</returns>
    public static bool operator <(Degree<T> first, Degree<T> second)
    {
        return first._val < second._val;
    }

    /// <summary>
    /// Determines whether the first degree value is greater than the second.
    /// </summary>
    /// <param name="first">The first degree value to compare.</param>
    /// <param name="second">The second degree value to compare.</param>
    /// <returns>true if the first value is greater than the second; otherwise, false.</returns>
    public static bool operator >(Degree<T> first, Degree<T> second)
    {
        return first._val > second._val;
    }

    /// <summary>
    /// Implicitly converts a radian value to degrees.
    /// </summary>
    /// <param name="src">The radian value to convert.</param>
    /// <returns>The equivalent degree value.</returns>
    public static implicit operator Degree<T>(Radian<T> src)
    {
        var tmp = (T)src / T.Pi * T.CreateChecked(180);
        return new Degree<T>(tmp);
    }

    /// <summary>
    /// Gets the underlying numeric value of this degree measurement.
    /// </summary>
    /// <returns>The numeric value in degrees.</returns>
    public T GetValue()
    {
        return _val; 
    }
    

    /// <summary>
    /// Compares this degree value to another degree value.
    /// </summary>
    /// <param name="other">The other degree value to compare to.</param>
    /// <returns>A value indicating the relative order of the compared values.</returns>
    public int CompareTo(Degree<T> other)
    {
        return _val.CompareTo(other._val);
    }

    /// <summary>
    /// Compares this degree value to another object.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>A value indicating the relative order of the compared values.</returns>
    /// <exception cref="ArgumentException">Thrown when the object is not of type Degree{T}.</exception>
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Degree<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Degree<T>)}");
    }

    /// <summary>
    /// Returns a string representation of this degree value with the degree symbol.
    /// </summary>
    /// <returns>A string representation of the degree value.</returns>
    public override string ToString()
    {
        return $"{_val}\u00b0";
    }
}