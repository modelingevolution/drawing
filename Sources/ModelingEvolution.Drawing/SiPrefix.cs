using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Shared parser for unit-bearing quantity strings with optional SI metric prefixes.
///
/// Used by the quantity value types (<see cref="Amps{T}"/>, <see cref="Volts{T}"/>,
/// <see cref="Frequency{T}"/>, <see cref="Speed{T}"/>, …) so that <c>Parse(x.ToString())</c>
/// round-trips (the canonical <c>"{value} {unit}"</c> form parses back) AND SI-prefixed input
/// such as <c>"1.5 kA"</c> or <c>"500 mA"</c> is accepted.
///
/// The prefix table is CASE-SENSITIVE — <c>M</c> is mega (1e6) while <c>m</c> is milli (1e-3):
/// <list type="bullet">
///   <item><c>T</c> = 1e12</item><item><c>G</c> = 1e9</item><item><c>M</c> = 1e6</item>
///   <item><c>k</c> = 1e3 (canonical; ASCII <c>K</c> accepted as a lenient input alias —
///         upper-case K is SI Kelvin but has no conflicting prefix meaning here)</item>
///   <item>(none) = 1e0</item><item><c>m</c> = 1e-3</item>
///   <item><c>µ</c> = 1e-6 (ASCII <c>u</c> accepted as an alias)</item>
///   <item><c>n</c> = 1e-9</item><item><c>p</c> = 1e-12</item>
/// </list>
/// Input aliases: kilo ∈ {k, K}, micro ∈ {µ, u}; every other prefix is exact-case.
/// </summary>
public static class SiPrefix
{
    /// <summary>
    /// Maps a recognised SI-prefix character to its base-10 exponent. Returns <c>false</c> for any
    /// character that is not a known prefix (so the caller can reject an unknown trailing letter).
    /// </summary>
    private static bool TryGetExponent(char prefix, out int exponent)
    {
        switch (prefix)
        {
            case 'T': exponent = 12; return true;
            case 'G': exponent = 9; return true;
            case 'M': exponent = 6; return true;
            case 'k': case 'K': exponent = 3; return true;  // 'k' canonical; 'K' lenient input alias (see remarks)
            case 'm': exponent = -3; return true;
            case 'µ': case 'u': exponent = -6; return true;  // U+00B5 micro sign; 'u' ASCII alias
            case 'n': exponent = -9; return true;
            case 'p': exponent = -12; return true;
            default: exponent = 0; return false;
        }
    }

    /// <summary>
    /// Tries to parse <paramref name="s"/> as a number in the given <paramref name="unit"/> with an
    /// optional SI prefix. The unit (if non-empty) is stripped first (Ordinal), then an optional
    /// single-character SI prefix, then the bare number is parsed with <see cref="NumberStyles.Float"/>
    /// using the invariant culture by default.
    /// </summary>
    /// <returns><c>false</c> for null/whitespace input, an unknown trailing prefix letter, or an
    /// unparseable number.</returns>
    public static bool TryParse<T>(string? s, string unit, IFormatProvider? provider, out T value)
        where T : INumberBase<T>
    {
        value = T.Zero;
        if (string.IsNullOrWhiteSpace(s)) return false;

        var span = s.AsSpan().Trim();

        // Strip the unit suffix if present (Ordinal, exact), then any trailing whitespace before it.
        if (unit.Length > 0 && span.EndsWith(unit, StringComparison.Ordinal))
            span = span[..^unit.Length].TrimEnd();

        if (span.Length == 0) return false;

        // An optional SI prefix is the last char IF it is not part of the number itself
        // (digit / decimal point / sign). A known prefix is stripped; an unknown trailing
        // letter is rejected.
        var exponent = 0;
        var last = span[^1];
        if (!char.IsDigit(last) && last != '.' && last != ',' && last != '+' && last != '-')
        {
            if (!TryGetExponent(last, out exponent))
                return false;  // trailing letter that is not a known SI prefix
            span = span[..^1].TrimEnd();
            if (span.Length == 0) return false;
        }

        if (!T.TryParse(span, NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var num))
            return false;

        var scale = exponent == 0 ? T.One : T.CreateChecked(Math.Pow(10, exponent));
        value = num * scale;
        return true;
    }

    /// <summary>
    /// Parses <paramref name="s"/> as a number in the given <paramref name="unit"/> with an optional
    /// SI prefix. Throws <see cref="FormatException"/> when the input cannot be parsed.
    /// </summary>
    public static T Parse<T>(string s, string unit, IFormatProvider? provider)
        where T : INumberBase<T>
    {
        ArgumentNullException.ThrowIfNull(s);
        if (!TryParse<T>(s, unit, provider, out var value))
            throw new FormatException($"'{s}' is not a valid {(unit.Length > 0 ? unit + " " : "")}value.");
        return value;
    }
}
