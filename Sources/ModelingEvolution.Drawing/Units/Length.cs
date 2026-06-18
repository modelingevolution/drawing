using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A unit-aware length/distance: a numeric value tagged at compile time with the unit
/// <typeparamref name="TUnit"/> it is expressed in. Mirrors <see cref="Speed{T,TUnit}"/>; conversion to the
/// SI base (metres) happens through <see cref="IUnit{T}"/>'s static-abstract members.
/// </summary>
/// <typeparam name="T">Backing numeric type (e.g. <see cref="double"/>).</typeparam>
/// <typeparam name="TUnit">The unit this value is expressed in.</typeparam>
public readonly record struct Length<T, TUnit> :
    IComparisonOperators<Length<T, TUnit>, Length<T, TUnit>, bool>,
    IComparable<Length<T, TUnit>>,
    IComparable,
    IAdditionOperators<Length<T, TUnit>, Length<T, TUnit>, Length<T, TUnit>>,
    ISubtractionOperators<Length<T, TUnit>, Length<T, TUnit>, Length<T, TUnit>>,
    IUnaryNegationOperators<Length<T, TUnit>, Length<T, TUnit>>,
    IMultiplyOperators<Length<T, TUnit>, T, Length<T, TUnit>>,
    IDivisionOperators<Length<T, TUnit>, T, Length<T, TUnit>>,
    IDivisionOperators<Length<T, TUnit>, Length<T, TUnit>, T>,
    IParsable<Length<T, TUnit>>
    where T : INumber<T>
    where TUnit : ILengthUnit<T>
{
    private readonly T _value;

    /// <summary>Creates a length from a value expressed in <typeparamref name="TUnit"/>.</summary>
    public Length(T value) => _value = value;

    /// <summary>The raw value, in <typeparamref name="TUnit"/>.</summary>
    public T Value => _value;

    /// <summary>A length of zero.</summary>
    public static Length<T, TUnit> Zero => new(T.Zero);

    /// <summary>This length expressed in the SI base unit (metres).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ToSi() => TUnit.ToSi(_value);

    /// <summary>Constructs from a value already expressed in the SI base unit (metres).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Length<T, TUnit> FromSi(T si) => new(TUnit.FromSi(si));

    /// <summary>Converts to another unit of the same (length) dimension.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Length<T, TOther> To<TOther>() where TOther : ILengthUnit<T>
        => Length<T, TOther>.FromSi(ToSi());

    #region Arithmetic

    /// <summary>Adds two lengths expressed in the same unit.</summary>
    public static Length<T, TUnit> operator +(Length<T, TUnit> a, Length<T, TUnit> b) => new(a._value + b._value);

    /// <summary>Subtracts two lengths expressed in the same unit.</summary>
    public static Length<T, TUnit> operator -(Length<T, TUnit> a, Length<T, TUnit> b) => new(a._value - b._value);

    /// <summary>Negates a length.</summary>
    public static Length<T, TUnit> operator -(Length<T, TUnit> a) => new(-a._value);

    /// <summary>Scales a length by a scalar.</summary>
    public static Length<T, TUnit> operator *(Length<T, TUnit> a, T scalar) => new(a._value * scalar);

    /// <summary>Scales a length by a scalar.</summary>
    public static Length<T, TUnit> operator *(T scalar, Length<T, TUnit> a) => new(scalar * a._value);

    /// <summary>Scales a length by a percentage.</summary>
    public static Length<T, TUnit> operator *(Length<T, TUnit> a, Percentage p) => new(a._value * T.CreateChecked(p.Fraction));

    /// <summary>Scales a length by a percentage.</summary>
    public static Length<T, TUnit> operator *(Percentage p, Length<T, TUnit> a) => new(T.CreateChecked(p.Fraction) * a._value);

    /// <summary>Divides a length by a scalar.</summary>
    public static Length<T, TUnit> operator /(Length<T, TUnit> a, T scalar) => new(a._value / scalar);

    /// <summary>Recovers the full length from one that is <paramref name="p"/> of it, e.g. half / 50% = full.</summary>
    public static Length<T, TUnit> operator /(Length<T, TUnit> a, Percentage p) => new(a._value / T.CreateChecked(p.Fraction));

    /// <summary>The dimensionless ratio of two lengths expressed in the same unit.</summary>
    public static T operator /(Length<T, TUnit> a, Length<T, TUnit> b) => a._value / b._value;

    /// <summary>Kinematics: the speed needed to cover this distance in <paramref name="dt"/> (v = s / t). SI (m/s).</summary>
    public static Speed<T, MetrePerSecond<T>> operator /(Length<T, TUnit> s, TimeSpan dt)
        => Speed<T, MetrePerSecond<T>>.FromSi(s.ToSi() / T.CreateChecked(dt.TotalSeconds));

    /// <summary>
    /// Kinematics: time to travel this distance at <paramref name="speed"/> (t = s / v). A method, not an
    /// operator, because the speed's unit type-parameter is free (operators can't introduce one).
    /// </summary>
    public TimeSpan TimeToTravelAt<TSpeedUnit>(Speed<T, TSpeedUnit> speed) where TSpeedUnit : ISpeedUnit<T>
        => TimeSpan.FromSeconds(double.CreateChecked(ToSi() / speed.ToSi()));

    /// <summary>Adds a length expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Length<T, TUnit> Add<TOther>(Length<T, TOther> other) where TOther : ILengthUnit<T>
        => FromSi(ToSi() + other.ToSi());

    /// <summary>Subtracts a length expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Length<T, TUnit> Subtract<TOther>(Length<T, TOther> other) where TOther : ILengthUnit<T>
        => FromSi(ToSi() - other.ToSi());

    /// <summary>Returns the absolute value of this length.</summary>
    public Length<T, TUnit> Abs() => new(T.Abs(_value));

    #endregion

    #region Comparison

    /// <inheritdoc/>
    public static bool operator <(Length<T, TUnit> a, Length<T, TUnit> b) => a._value < b._value;
    /// <inheritdoc/>
    public static bool operator >(Length<T, TUnit> a, Length<T, TUnit> b) => a._value > b._value;
    /// <inheritdoc/>
    public static bool operator <=(Length<T, TUnit> a, Length<T, TUnit> b) => a._value <= b._value;
    /// <inheritdoc/>
    public static bool operator >=(Length<T, TUnit> a, Length<T, TUnit> b) => a._value >= b._value;

    /// <inheritdoc/>
    public int CompareTo(Length<T, TUnit> other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Length<T, TUnit> other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(Length<T, TUnit>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static Length<T, TUnit> Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new Length<T, TUnit>(T.Parse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture));
    }

    /// <summary>Tries to parse a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Length<T, TUnit> result)
    {
        result = Zero;
        if (s is null) return false;
        if (!T.TryParse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var val))
            return false;
        result = new Length<T, TUnit>(val);
        return true;
    }

    private static string StripSymbol(string s)
    {
        s = s.Trim();
        return s.EndsWith(TUnit.Symbol, StringComparison.Ordinal)
            ? s[..^TUnit.Symbol.Length].TrimEnd()
            : s;
    }

    #endregion

    /// <summary>Formats as "{value} {symbol}", e.g. "12.5 mm".</summary>
    public override string ToString() => $"{_value} {TUnit.Symbol}";
}
