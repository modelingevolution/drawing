using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A percentage constrained to the closed range [0, 100]. A distinct value type so APIs can accept
/// "a percentage" without bare-double ambiguity (vs. an absolute <see cref="Speed{T,TUnit}"/>).
/// The value is stored as a <see cref="float"/> to preserve fractional percentages (e.g. 12.5%);
/// the range invariant is enforced at construction.
/// <para>
/// Equality is the record-default exact <see cref="float"/> comparison, so two percentages reached via
/// different float paths (e.g. <see cref="FromFraction"/>(0.2f) vs <c>new(20f)</c>) are not guaranteed
/// equal. <see cref="ToString"/>/<see cref="Parse(string, IFormatProvider?)"/> use the invariant culture
/// deliberately, so the textual form round-trips stably (it is persisted in serialized welding programs).
/// </para>
/// </summary>
public readonly record struct Percentage :
    IComparable<Percentage>, IComparable, IParsable<Percentage>
{
    /// <summary>Lowest valid percentage.</summary>
    public const float Min = 0f;
    /// <summary>Highest valid percentage.</summary>
    public const float Max = 100f;

    private readonly float _value;

    /// <summary>Creates a percentage; <paramref name="value"/> must be in [0, 100].</summary>
    /// <exception cref="ArgumentOutOfRangeException">When out of range or NaN.</exception>
    public Percentage(float value)
    {
        if (float.IsNaN(value) || value < Min || value > Max)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Percentage must be in [{Min}, {Max}].");
        _value = value;
    }

    /// <summary>The percentage value in [0, 100].</summary>
    public float Value => _value;

    /// <summary>The value as a fraction in [0, 1] (i.e. <see cref="Value"/> / 100).</summary>
    public float Fraction => _value / 100f;

    /// <summary>0%.</summary>
    public static Percentage Zero => new(0f);

    /// <summary>100%.</summary>
    public static Percentage Full => new(100f);

    /// <summary>Creates a percentage from a fraction in [0, 1] (e.g. 0.25 → 25%).</summary>
    public static Percentage FromFraction(float fraction) => new(fraction * 100f);

    /// <summary>Clamps an arbitrary value into [0, 100] rather than throwing.</summary>
    public static Percentage Clamp(float value) => new(Math.Clamp(float.IsNaN(value) ? 0f : value, Min, Max));

    #region Arithmetic
    // Policy: construction VALIDATES (throws on out-of-range data); arithmetic SATURATES (clamps a
    // computed result into [0,100]). A bad input is a bug; a saturated motion-knob result is expected.

    /// <summary>Compounds two percentages: <c>50% * 50% = 25%</c>. Result is always in range.</summary>
    public static Percentage operator *(Percentage a, Percentage b) => FromFraction(a.Fraction * b.Fraction);

    /// <summary>Adds two percentages, clamping the sum into [0, 100] (e.g. <c>60% + 60% = 100%</c>).</summary>
    public static Percentage operator +(Percentage a, Percentage b) => Clamp(a._value + b._value);

    /// <summary>Subtracts two percentages, clamping the difference into [0, 100] (e.g. <c>20% - 50% = 0%</c>).</summary>
    public static Percentage operator -(Percentage a, Percentage b) => Clamp(a._value - b._value);

    #endregion

    #region Comparison

    /// <inheritdoc/>
    public int CompareTo(Percentage other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Percentage other ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(Percentage)}");
    }

    /// <inheritdoc/>
    public static bool operator <(Percentage a, Percentage b) => a._value < b._value;
    /// <inheritdoc/>
    public static bool operator >(Percentage a, Percentage b) => a._value > b._value;
    /// <inheritdoc/>
    public static bool operator <=(Percentage a, Percentage b) => a._value <= b._value;
    /// <inheritdoc/>
    public static bool operator >=(Percentage a, Percentage b) => a._value >= b._value;

    #endregion

    #region Parsing

    /// <summary>Parses "20" or "20%".</summary>
    public static Percentage Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid percentage: '{s}'. Expected '20' or '20%' within [0, 100].");
        return result;
    }

    /// <summary>Tries to parse "20" or "20%". Fails on out-of-range values.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Percentage result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim();
        if (t.EndsWith('%')) t = t[..^1].TrimEnd();
        if (!float.TryParse(t, NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var v)) return false;
        if (float.IsNaN(v) || v < Min || v > Max) return false;
        result = new Percentage(v);
        return true;
    }

    #endregion

    /// <summary>Formats as "{value}%", e.g. "20%".</summary>
    public override string ToString() => $"{_value.ToString(CultureInfo.InvariantCulture)}%";
}
