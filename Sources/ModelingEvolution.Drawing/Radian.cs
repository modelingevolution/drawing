using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides static methods for trigonometric operations with radian values.
/// </summary>
public static class Radian
{
    /// <summary>
    /// Calculates the sine of an angle specified in radians.
    /// </summary>
    /// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
    /// <param name="rad">The angle in radians.</param>
    /// <returns>The sine of the specified angle.</returns>
    public static T Sin<T>(Radian<T> rad) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => T.Sin(rad._val);
    /// <summary>
    /// Calculates the cosine of an angle specified in radians.
    /// </summary>
    /// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
    /// <param name="rad">The angle in radians.</param>
    /// <returns>The cosine of the specified angle.</returns>
    public static T Cos<T>(Radian<T> rad) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => T.Cos(rad._val);
    
}
/// <summary>
/// Represents an angle measurement in radians with generic numeric type support.
/// </summary>
/// <typeparam name="T">The numeric type that supports trigonometric functions.</typeparam>
[DebuggerDisplay("{_val}rad")]
public readonly record struct Radian<T>: IComparisonOperators<Radian<T>, Radian<T>, bool>, IComparable<Radian<T>>, IComparable
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>
{
    /// <summary>
    /// Gets a radian value representing zero radians.
    /// </summary>
    public static Radian<T> Zero { get; } = new Radian<T>();
    internal readonly T _val;
    private Radian(T value)
    {
        _val = value;
    }

    

    /// <summary>
    /// Converts this radian value to a different numeric type by truncating the value.
    /// </summary>
    /// <typeparam name="U">The target numeric type.</typeparam>
    /// <returns>A radian instance with the truncated value in the target type.</returns>
    public Radian<U> Truncate<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>
    {
        var v = U.CreateTruncating(this._val);
        return new Radian<U>(v);
    }
    /// <summary>
    /// Creates a new radian instance with the specified value.
    /// </summary>
    /// <param name="radians">The angle value in radians.</param>
    /// <returns>A new radian instance.</returns>
    public static Radian<T> FromRadian(T radians) => new Radian<T>(radians);

    /// <summary>
    /// Determines whether the first radian value is less than or equal to the second.
    /// </summary>
    /// <param name="first">The first radian value to compare.</param>
    /// <param name="second">The second radian value to compare.</param>
    /// <returns>true if the first value is less than or equal to the second; otherwise, false.</returns>
    public static bool operator <=(Radian<T> first, Radian<T> second)
    {
        return first._val <= second._val;
    }
    /// <summary>
    /// Determines whether the first radian value is greater than or equal to the second.
    /// </summary>
    /// <param name="first">The first radian value to compare.</param>
    /// <param name="second">The second radian value to compare.</param>
    /// <returns>true if the first value is greater than or equal to the second; otherwise, false.</returns>
    public static bool operator >=(Radian<T> first, Radian<T> second)
    {
        return first._val >= second._val;
    }

    /// <summary>
    /// Subtracts one radian value from another.
    /// </summary>
    /// <param name="a">The first radian value.</param>
    /// <param name="b">The second radian value to subtract.</param>
    /// <returns>The difference between the two radian values.</returns>
    public static Radian<T> operator -(Radian<T> a, Radian<T> b)
    {
        return new Radian<T>(a._val - b._val);
    }
    /// <summary>
    /// Adds two radian values together.
    /// </summary>
    /// <param name="a">The first radian value.</param>
    /// <param name="b">The second radian value to add.</param>
    /// <returns>The sum of the two radian values.</returns>
    public static Radian<T> operator +(Radian<T> a, Radian<T> b)
    {
        return new Radian<T>(a._val + b._val);
    }
    /// <summary>
    /// Determines whether the first radian value is less than the second.
    /// </summary>
    /// <param name="first">The first radian value to compare.</param>
    /// <param name="second">The second radian value to compare.</param>
    /// <returns>true if the first value is less than the second; otherwise, false.</returns>
    public static bool operator <(Radian<T> first, Radian<T> second)
    {
        return first._val < second._val;
    }
    /// <summary>
    /// Determines whether the first radian value is greater than the second.
    /// </summary>
    /// <param name="first">The first radian value to compare.</param>
    /// <param name="second">The second radian value to compare.</param>
    /// <returns>true if the first value is greater than the second; otherwise, false.</returns>
    public static bool operator >(Radian<T> first, Radian<T> second)
    {
        return first._val > second._val;
    }

    /// <summary>
    /// Negates this radian value.
    /// </summary>
    public static Radian<T> operator -(Radian<T> a) => new Radian<T>(-a._val);

    /// <summary>
    /// Multiplies a radian value by a scalar.
    /// </summary>
    public static Radian<T> operator *(Radian<T> a, T scalar) => new Radian<T>(a._val * scalar);

    /// <summary>
    /// Multiplies a radian value by a scalar.
    /// </summary>
    public static Radian<T> operator *(T scalar, Radian<T> a) => new Radian<T>(scalar * a._val);

    /// <summary>
    /// Divides a radian value by a scalar.
    /// </summary>
    public static Radian<T> operator /(Radian<T> a, T scalar) => new Radian<T>(a._val / scalar);

    /// <summary>
    /// Returns the absolute value of this radian.
    /// </summary>
    public Radian<T> Abs() => new Radian<T>(T.Abs(_val));

    /// <summary>
    /// Normalizes this angle to the range (-π, π].
    /// </summary>
    public Radian<T> Normalize()
    {
        var pi = T.Pi;
        var twoPi = pi + pi;
        var v = _val % twoPi;
        if (v > pi) v -= twoPi;
        if (v <= -pi) v += twoPi;
        return new Radian<T>(v);
    }

    /// <summary>
    /// Explicitly converts a radian value to its underlying numeric type.
    /// </summary>
    /// <param name="src">The radian value to convert.</param>
    /// <returns>The underlying numeric value.</returns>
    public static explicit operator T(Radian<T> src)
    {
        return src._val;
    }
    /// <summary>
    /// Implicitly converts a degree value to radians.
    /// </summary>
    /// <param name="src">The degree value to convert.</param>
    /// <returns>The equivalent radian value.</returns>
    public static implicit operator Radian<T>(Degree<T> src)
    {
        var val = (T)src;
        var tmp = val / T.CreateChecked(180) * T.Pi;
        return new Radian<T>(tmp);
    }

    /// <summary>
    /// Compares this radian value to another object.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>A value indicating the relative order of the compared values.</returns>
    /// <exception cref="ArgumentException">Thrown when the object is not of type Radian{T}.</exception>
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Radian<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Radian<T>)}");
    }

    /// <summary>
    /// Compares this radian value to another radian value.
    /// </summary>
    /// <param name="other">The other radian value to compare to.</param>
    /// <returns>A value indicating the relative order of the compared values.</returns>
    public int CompareTo(Radian<T> other)
    {
        return _val.CompareTo(other._val);
    }

    /// <summary>
    /// Returns a string representation of this radian value with the radian suffix.
    /// </summary>
    /// <returns>A string representation of the radian value.</returns>
    public override string ToString()
    {
        return $"{_val}rad";
    }
}