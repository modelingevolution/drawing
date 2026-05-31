using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a scalar speed (magnitude of velocity) in millimetres per minute (mm/min).
/// </summary>
/// <typeparam name="T">The numeric type used for the speed value.</typeparam>
[DebuggerDisplay("{_val} mm/min")]
[ProtoContract]
[JsonConverter(typeof(ParsableJsonConverterFactory))]
public readonly record struct Speed<T> : IComparisonOperators<Speed<T>, Speed<T>, bool>, IComparable<Speed<T>>, IComparable, IParsable<Speed<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    [ProtoMember(1)]
    internal readonly T _val;

    /// <summary>
    /// Gets a speed value representing zero.
    /// </summary>
    public static Speed<T> Zero { get; } = new(T.Zero);

    private Speed(T value) => _val = value;

    /// <summary>
    /// Creates a new speed from the given value.
    /// </summary>
    public static Speed<T> From(T value) => new(value);

    /// <summary>
    /// Implicitly converts a numeric value to a speed.
    /// </summary>
    public static implicit operator Speed<T>(T src) => new(src);

    /// <summary>
    /// Explicitly converts a speed to its underlying numeric value.
    /// </summary>
    public static explicit operator T(Speed<T> src) => src._val;

    #region Arithmetic

    public static Speed<T> operator +(Speed<T> a, Speed<T> b) => new(a._val + b._val);
    public static Speed<T> operator -(Speed<T> a, Speed<T> b) => new(a._val - b._val);
    public static Speed<T> operator -(Speed<T> a) => new(-a._val);
    public static Speed<T> operator *(Speed<T> a, T scalar) => new(a._val * scalar);
    public static Speed<T> operator *(T scalar, Speed<T> a) => new(scalar * a._val);
    public static Speed<T> operator /(Speed<T> a, T scalar) => new(a._val / scalar);

    /// <summary>
    /// Computes time required to travel the given distance at this speed.
    /// </summary>
    public T TimeFor(T distance) => distance / _val;

    /// <summary>
    /// Computes distance traveled in the given time at this speed.
    /// </summary>
    public T DistanceIn(T time) => _val * time;

    /// <summary>
    /// Returns the absolute value of this speed.
    /// </summary>
    public Speed<T> Abs() => new(T.Abs(_val));

    #endregion

    #region Comparison

    public static bool operator <(Speed<T> left, Speed<T> right) => left._val < right._val;
    public static bool operator >(Speed<T> left, Speed<T> right) => left._val > right._val;
    public static bool operator <=(Speed<T> left, Speed<T> right) => left._val <= right._val;
    public static bool operator >=(Speed<T> left, Speed<T> right) => left._val >= right._val;

    public int CompareTo(Speed<T> other) => _val.CompareTo(other._val);
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Speed<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Speed<T>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a speed, accepting the canonical "mm/min" unit and an optional SI prefix on
    /// the value (e.g. "6.4", "6.4 mm/min", "1.5 kmm/min"). Uses the invariant culture by default.</summary>
    public static Speed<T> Parse(string s, IFormatProvider? provider)
        => new(SiPrefix.Parse<T>(s, "mm/min", provider));

    /// <summary>Tries to parse a speed, accepting the canonical "mm/min" unit and an optional SI prefix.
    /// Returns false for null or unparseable input.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Speed<T> result)
    {
        result = Zero;
        if (!SiPrefix.TryParse<T>(s, "mm/min", provider, out var val)) return false;
        result = new Speed<T>(val);
        return true;
    }

    #endregion

    public override string ToString() => $"{_val} mm/min";
}
