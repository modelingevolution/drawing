using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a frequency in hertz (cycles per second).
/// </summary>
/// <typeparam name="T">The numeric type used for the frequency value.</typeparam>
[DebuggerDisplay("{_val} Hz")]
[ProtoContract]
public readonly record struct Frequency<T> : IComparisonOperators<Frequency<T>, Frequency<T>, bool>, IComparable<Frequency<T>>, IComparable, IParsable<Frequency<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    [ProtoMember(1)]
    internal readonly T _val;

    /// <summary>
    /// Gets a frequency value representing zero hertz.
    /// </summary>
    public static Frequency<T> Zero { get; } = new(T.Zero);

    private Frequency(T value) => _val = value;

    /// <summary>
    /// Creates a new frequency from a value in hertz.
    /// </summary>
    public static Frequency<T> FromHertz(T hz) => new(hz);

    /// <summary>
    /// Creates a new frequency from a value in kilohertz.
    /// </summary>
    public static Frequency<T> FromKilohertz(T khz) => new(khz * T.CreateChecked(1000));

    /// <summary>
    /// Gets the raw frequency value in hertz.
    /// </summary>
    public T Hertz => _val;

    /// <summary>
    /// Gets the period of one cycle (reciprocal of frequency).
    /// Returns <see cref="TimeSpan.MaxValue"/> when the frequency is zero.
    /// </summary>
    public TimeSpan Period =>
        T.IsZero(_val)
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(1.0 / double.CreateChecked(_val));

    /// <summary>
    /// Implicitly converts a numeric value (interpreted as hertz) to a frequency.
    /// </summary>
    public static implicit operator Frequency<T>(T src) => new(src);

    /// <summary>
    /// Explicitly converts a frequency to its underlying value in hertz.
    /// </summary>
    public static explicit operator T(Frequency<T> src) => src._val;

    #region Arithmetic

    /// <summary>Returns the sum of two frequencies.</summary>
    public static Frequency<T> operator +(Frequency<T> a, Frequency<T> b) => new(a._val + b._val);
    /// <summary>Returns the difference of two frequencies.</summary>
    public static Frequency<T> operator -(Frequency<T> a, Frequency<T> b) => new(a._val - b._val);
    /// <summary>Returns the negation of a frequency.</summary>
    public static Frequency<T> operator -(Frequency<T> a) => new(-a._val);
    /// <summary>Scales a frequency by a scalar on the right.</summary>
    public static Frequency<T> operator *(Frequency<T> a, T scalar) => new(a._val * scalar);
    /// <summary>Scales a frequency by a scalar on the left.</summary>
    public static Frequency<T> operator *(T scalar, Frequency<T> a) => new(scalar * a._val);
    /// <summary>Divides a frequency by a scalar.</summary>
    public static Frequency<T> operator /(Frequency<T> a, T scalar) => new(a._val / scalar);

    /// <summary>
    /// Returns the absolute value of this frequency.
    /// </summary>
    public Frequency<T> Abs() => new(T.Abs(_val));

    #endregion

    #region Comparison

    /// <summary>Returns true if the left frequency is strictly less than the right.</summary>
    public static bool operator <(Frequency<T> left, Frequency<T> right) => left._val < right._val;
    /// <summary>Returns true if the left frequency is strictly greater than the right.</summary>
    public static bool operator >(Frequency<T> left, Frequency<T> right) => left._val > right._val;
    /// <summary>Returns true if the left frequency is less than or equal to the right.</summary>
    public static bool operator <=(Frequency<T> left, Frequency<T> right) => left._val <= right._val;
    /// <summary>Returns true if the left frequency is greater than or equal to the right.</summary>
    public static bool operator >=(Frequency<T> left, Frequency<T> right) => left._val >= right._val;

    /// <summary>Compares this frequency to another. Returns the comparison of the underlying hertz values.</summary>
    public int CompareTo(Frequency<T> other) => _val.CompareTo(other._val);
    /// <summary>Compares this frequency to another object. Returns 1 for null; throws if the object is not a <see cref="Frequency{T}"/>.</summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Frequency<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Frequency<T>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a frequency in hertz, accepting an optional "Hz" unit and SI prefix
    /// (e.g. "180", "180 Hz", "2 MHz", "500 mHz"). Uses the invariant culture by default.</summary>
    public static Frequency<T> Parse(string s, IFormatProvider? provider)
        => new(SiPrefix.Parse<T>(s, "Hz", provider));

    /// <summary>Tries to parse a frequency in hertz, accepting an optional "Hz" unit and SI prefix.
    /// Returns false for null or unparseable input.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Frequency<T> result)
    {
        result = Zero;
        if (!SiPrefix.TryParse<T>(s, "Hz", provider, out var val)) return false;
        result = new Frequency<T>(val);
        return true;
    }

    #endregion

    /// <summary>Returns the canonical "<c>{value} Hz</c>" string representation.</summary>
    public override string ToString() => $"{_val} Hz";
}
