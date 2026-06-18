using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A unit-aware speed: a numeric value tagged at compile time with the unit <typeparamref name="TUnit"/>
/// it is expressed in. The value is stored in <typeparamref name="TUnit"/>'s own units; conversion to the
/// SI base (m/s) happens through <see cref="IUnit{T}"/>'s static-abstract members.
/// </summary>
/// <typeparam name="T">Backing numeric type (e.g. <see cref="double"/>).</typeparam>
/// <typeparam name="TUnit">The unit this value is expressed in.</typeparam>
public readonly record struct Speed<T, TUnit> :
    IComparisonOperators<Speed<T, TUnit>, Speed<T, TUnit>, bool>,
    IComparable<Speed<T, TUnit>>,
    IComparable,
    IAdditionOperators<Speed<T, TUnit>, Speed<T, TUnit>, Speed<T, TUnit>>,
    ISubtractionOperators<Speed<T, TUnit>, Speed<T, TUnit>, Speed<T, TUnit>>,
    IUnaryNegationOperators<Speed<T, TUnit>, Speed<T, TUnit>>,
    IMultiplyOperators<Speed<T, TUnit>, T, Speed<T, TUnit>>,
    IDivisionOperators<Speed<T, TUnit>, T, Speed<T, TUnit>>,
    IDivisionOperators<Speed<T, TUnit>, Speed<T, TUnit>, T>,
    IParsable<Speed<T, TUnit>>
    where T : INumber<T>
    where TUnit : ISpeedUnit<T>
{
    private readonly T _value;

    /// <summary>Creates a speed from a value expressed in <typeparamref name="TUnit"/>.</summary>
    public Speed(T value) => _value = value;

    /// <summary>The raw value, in <typeparamref name="TUnit"/>.</summary>
    public T Value => _value;

    /// <summary>A speed of zero.</summary>
    public static Speed<T, TUnit> Zero => new(T.Zero);

    /// <summary>This speed expressed in the SI base unit (m/s).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ToSi() => TUnit.ToSi(_value);

    /// <summary>Constructs from a value already expressed in the SI base unit (m/s).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Speed<T, TUnit> FromSi(T si) => new(TUnit.FromSi(si));

    /// <summary>Converts to another unit of the same (speed) dimension.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Speed<T, TOther> To<TOther>() where TOther : ISpeedUnit<T>
        => Speed<T, TOther>.FromSi(ToSi());

    #region Arithmetic

    /// <summary>Adds two speeds expressed in the same unit.</summary>
    public static Speed<T, TUnit> operator +(Speed<T, TUnit> a, Speed<T, TUnit> b) => new(a._value + b._value);

    /// <summary>Subtracts two speeds expressed in the same unit.</summary>
    public static Speed<T, TUnit> operator -(Speed<T, TUnit> a, Speed<T, TUnit> b) => new(a._value - b._value);

    /// <summary>Negates a speed.</summary>
    public static Speed<T, TUnit> operator -(Speed<T, TUnit> a) => new(-a._value);

    /// <summary>Scales a speed by a scalar.</summary>
    public static Speed<T, TUnit> operator *(Speed<T, TUnit> a, T scalar) => new(a._value * scalar);

    /// <summary>Scales a speed by a scalar.</summary>
    public static Speed<T, TUnit> operator *(T scalar, Speed<T, TUnit> a) => new(scalar * a._value);

    /// <summary>Scales a speed by a percentage, e.g. <c>maxSpeed * new Percentage(50)</c> → half speed.</summary>
    public static Speed<T, TUnit> operator *(Speed<T, TUnit> a, Percentage p) => new(a._value * T.CreateChecked(p.Fraction));

    /// <summary>Scales a speed by a percentage, e.g. <c>new Percentage(50) * maxSpeed</c> → half speed.</summary>
    public static Speed<T, TUnit> operator *(Percentage p, Speed<T, TUnit> a) => new(T.CreateChecked(p.Fraction) * a._value);

    /// <summary>Divides a speed by a scalar.</summary>
    public static Speed<T, TUnit> operator /(Speed<T, TUnit> a, T scalar) => new(a._value / scalar);

    /// <summary>Recovers the full speed from one that is <paramref name="p"/> of it, e.g. half / 50% = full.</summary>
    public static Speed<T, TUnit> operator /(Speed<T, TUnit> a, Percentage p) => new(a._value / T.CreateChecked(p.Fraction));

    /// <summary>The dimensionless ratio of two speeds expressed in the same unit.</summary>
    public static T operator /(Speed<T, TUnit> a, Speed<T, TUnit> b) => a._value / b._value;

    /// <summary>Kinematics: distance covered over a time span (s = v · t). Result is in SI (metres).</summary>
    public static Length<T, Metre<T>> operator *(Speed<T, TUnit> v, TimeSpan dt)
        => Length<T, Metre<T>>.FromSi(v.ToSi() * T.CreateChecked(dt.TotalSeconds));

    /// <summary>Kinematics: distance covered over a time span (s = v · t). Result is in SI (metres).</summary>
    public static Length<T, Metre<T>> operator *(TimeSpan dt, Speed<T, TUnit> v)
        => Length<T, Metre<T>>.FromSi(T.CreateChecked(dt.TotalSeconds) * v.ToSi());

    /// <summary>Kinematics: average acceleration over a time span (a = Δv / t). Result is in SI (m/s²).</summary>
    public static Acceleration<T, MetrePerSecondSquared<T>> operator /(Speed<T, TUnit> v, TimeSpan dt)
        => Acceleration<T, MetrePerSecondSquared<T>>.FromSi(v.ToSi() / T.CreateChecked(dt.TotalSeconds));

    /// <summary>
    /// Kinematics: time to reach this speed from rest at <paramref name="acceleration"/> (t = v / a). A
    /// method, not an operator, because the acceleration's unit type-parameter is free.
    /// </summary>
    public TimeSpan TimeToReach<TAccelUnit>(Acceleration<T, TAccelUnit> acceleration) where TAccelUnit : IAccelerationUnit<T>
        => TimeSpan.FromSeconds(double.CreateChecked(ToSi() / acceleration.ToSi()));

    /// <summary>Adds a speed expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Speed<T, TUnit> Add<TOther>(Speed<T, TOther> other) where TOther : ISpeedUnit<T>
        => FromSi(ToSi() + other.ToSi());

    /// <summary>Subtracts a speed expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Speed<T, TUnit> Subtract<TOther>(Speed<T, TOther> other) where TOther : ISpeedUnit<T>
        => FromSi(ToSi() - other.ToSi());

    /// <summary>Returns the absolute value of this speed.</summary>
    public Speed<T, TUnit> Abs() => new(T.Abs(_value));

    #endregion

    #region Comparison

    /// <inheritdoc/>
    public static bool operator <(Speed<T, TUnit> a, Speed<T, TUnit> b) => a._value < b._value;
    /// <inheritdoc/>
    public static bool operator >(Speed<T, TUnit> a, Speed<T, TUnit> b) => a._value > b._value;
    /// <inheritdoc/>
    public static bool operator <=(Speed<T, TUnit> a, Speed<T, TUnit> b) => a._value <= b._value;
    /// <inheritdoc/>
    public static bool operator >=(Speed<T, TUnit> a, Speed<T, TUnit> b) => a._value >= b._value;

    /// <inheritdoc/>
    public int CompareTo(Speed<T, TUnit> other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Speed<T, TUnit> other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(Speed<T, TUnit>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static Speed<T, TUnit> Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new Speed<T, TUnit>(T.Parse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture));
    }

    /// <summary>Tries to parse a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Speed<T, TUnit> result)
    {
        result = Zero;
        if (s is null) return false;
        if (!T.TryParse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var val))
            return false;
        result = new Speed<T, TUnit>(val);
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

    /// <summary>Formats as "{value} {symbol}", e.g. "12.5 mm/s".</summary>
    public override string ToString() => $"{_value} {TUnit.Symbol}";
}
