using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an electric voltage in volts.
/// </summary>
/// <typeparam name="T">The numeric type used for the voltage value.</typeparam>
[DebuggerDisplay("{_val} V")]
[ProtoContract]
public readonly record struct Volts<T> : IComparisonOperators<Volts<T>, Volts<T>, bool>, IComparable<Volts<T>>, IComparable, IParsable<Volts<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    [ProtoMember(1)]
    internal readonly T _val;

    /// <summary>
    /// Gets a voltage value representing zero volts.
    /// </summary>
    public static Volts<T> Zero { get; } = new(T.Zero);

    private Volts(T value) => _val = value;

    /// <summary>
    /// Creates a new voltage from the given value in volts.
    /// </summary>
    public static Volts<T> From(T value) => new(value);

    /// <summary>
    /// Implicitly converts a numeric value (interpreted as volts) to a voltage.
    /// </summary>
    public static implicit operator Volts<T>(T src) => new(src);

    /// <summary>
    /// Explicitly converts a voltage to its underlying value in volts.
    /// </summary>
    public static explicit operator T(Volts<T> src) => src._val;

    #region Arithmetic

    /// <summary>Returns the sum of two voltages.</summary>
    public static Volts<T> operator +(Volts<T> a, Volts<T> b) => new(a._val + b._val);
    /// <summary>Returns the difference of two voltages.</summary>
    public static Volts<T> operator -(Volts<T> a, Volts<T> b) => new(a._val - b._val);
    /// <summary>Returns the negation of a voltage.</summary>
    public static Volts<T> operator -(Volts<T> a) => new(-a._val);
    /// <summary>Scales a voltage by a scalar on the right.</summary>
    public static Volts<T> operator *(Volts<T> a, T scalar) => new(a._val * scalar);
    /// <summary>Scales a voltage by a scalar on the left.</summary>
    public static Volts<T> operator *(T scalar, Volts<T> a) => new(scalar * a._val);
    /// <summary>Divides a voltage by a scalar.</summary>
    public static Volts<T> operator /(Volts<T> a, T scalar) => new(a._val / scalar);

    /// <summary>
    /// Returns the absolute value of this voltage.
    /// </summary>
    public Volts<T> Abs() => new(T.Abs(_val));

    #endregion

    #region Comparison

    /// <summary>Returns true if the left voltage is strictly less than the right.</summary>
    public static bool operator <(Volts<T> left, Volts<T> right) => left._val < right._val;
    /// <summary>Returns true if the left voltage is strictly greater than the right.</summary>
    public static bool operator >(Volts<T> left, Volts<T> right) => left._val > right._val;
    /// <summary>Returns true if the left voltage is less than or equal to the right.</summary>
    public static bool operator <=(Volts<T> left, Volts<T> right) => left._val <= right._val;
    /// <summary>Returns true if the left voltage is greater than or equal to the right.</summary>
    public static bool operator >=(Volts<T> left, Volts<T> right) => left._val >= right._val;

    /// <summary>Compares this voltage to another. Returns the comparison of the underlying volt values.</summary>
    public int CompareTo(Volts<T> other) => _val.CompareTo(other._val);
    /// <summary>Compares this voltage to another object. Returns 1 for null; throws if the object is not an <see cref="Volts{T}"/>.</summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Volts<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Volts<T>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a voltage in volts, accepting an optional "V" unit and SI prefix
    /// (e.g. "180", "180 V", "1.5 kV", "500 mV"). Uses the invariant culture by default.</summary>
    public static Volts<T> Parse(string s, IFormatProvider? provider)
        => new(SiPrefix.Parse<T>(s, "V", provider));

    /// <summary>Tries to parse a voltage in volts, accepting an optional "V" unit and SI prefix.
    /// Returns false for null or unparseable input.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Volts<T> result)
    {
        result = Zero;
        if (!SiPrefix.TryParse<T>(s, "V", provider, out var val)) return false;
        result = new Volts<T>(val);
        return true;
    }

    #endregion

    /// <summary>Returns the canonical "<c>{value} V</c>" string representation.</summary>
    public override string ToString() => $"{_val} V";
}
