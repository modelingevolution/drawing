using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an electric current in amperes.
/// </summary>
/// <typeparam name="T">The numeric type used for the current value.</typeparam>
[DebuggerDisplay("{_val} A")]
[ProtoContract]
[JsonConverter(typeof(ParsableJsonConverterFactory))]
public readonly record struct Amps<T> : IComparisonOperators<Amps<T>, Amps<T>, bool>, IComparable<Amps<T>>, IComparable, IParsable<Amps<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    [ProtoMember(1)]
    internal readonly T _val;

    /// <summary>
    /// Gets a current value representing zero amperes.
    /// </summary>
    public static Amps<T> Zero { get; } = new(T.Zero);

    private Amps(T value) => _val = value;

    /// <summary>
    /// Creates a new current from the given value in amperes.
    /// </summary>
    public static Amps<T> From(T value) => new(value);

    /// <summary>
    /// Implicitly converts a numeric value (interpreted as amperes) to a current.
    /// </summary>
    public static implicit operator Amps<T>(T src) => new(src);

    /// <summary>
    /// Explicitly converts a current to its underlying value in amperes.
    /// </summary>
    public static explicit operator T(Amps<T> src) => src._val;

    #region Arithmetic

    /// <summary>Returns the sum of two currents.</summary>
    public static Amps<T> operator +(Amps<T> a, Amps<T> b) => new(a._val + b._val);
    /// <summary>Returns the difference of two currents.</summary>
    public static Amps<T> operator -(Amps<T> a, Amps<T> b) => new(a._val - b._val);
    /// <summary>Returns the negation of a current.</summary>
    public static Amps<T> operator -(Amps<T> a) => new(-a._val);
    /// <summary>Scales a current by a scalar on the right.</summary>
    public static Amps<T> operator *(Amps<T> a, T scalar) => new(a._val * scalar);
    /// <summary>Scales a current by a scalar on the left.</summary>
    public static Amps<T> operator *(T scalar, Amps<T> a) => new(scalar * a._val);
    /// <summary>Divides a current by a scalar.</summary>
    public static Amps<T> operator /(Amps<T> a, T scalar) => new(a._val / scalar);

    /// <summary>
    /// Returns the absolute value of this current.
    /// </summary>
    public Amps<T> Abs() => new(T.Abs(_val));

    #endregion

    #region Comparison

    /// <summary>Returns true if the left current is strictly less than the right.</summary>
    public static bool operator <(Amps<T> left, Amps<T> right) => left._val < right._val;
    /// <summary>Returns true if the left current is strictly greater than the right.</summary>
    public static bool operator >(Amps<T> left, Amps<T> right) => left._val > right._val;
    /// <summary>Returns true if the left current is less than or equal to the right.</summary>
    public static bool operator <=(Amps<T> left, Amps<T> right) => left._val <= right._val;
    /// <summary>Returns true if the left current is greater than or equal to the right.</summary>
    public static bool operator >=(Amps<T> left, Amps<T> right) => left._val >= right._val;

    /// <summary>Compares this current to another. Returns the comparison of the underlying ampere values.</summary>
    public int CompareTo(Amps<T> other) => _val.CompareTo(other._val);
    /// <summary>Compares this current to another object. Returns 1 for null; throws if the object is not an <see cref="Amps{T}"/>.</summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Amps<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Amps<T>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a current in amperes, accepting an optional "A" unit and SI prefix
    /// (e.g. "180", "180 A", "1.5 kA", "500 mA"). Uses the invariant culture by default.</summary>
    public static Amps<T> Parse(string s, IFormatProvider? provider)
        => new(SiPrefix.Parse<T>(s, "A", provider));

    /// <summary>Tries to parse a current in amperes, accepting an optional "A" unit and SI prefix.
    /// Returns false for null or unparseable input.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Amps<T> result)
    {
        result = Zero;
        if (!SiPrefix.TryParse<T>(s, "A", provider, out var val)) return false;
        result = new Amps<T>(val);
        return true;
    }

    #endregion

    /// <summary>Returns the canonical "<c>{value} A</c>" string representation.</summary>
    public override string ToString() => $"{_val} A";
}
