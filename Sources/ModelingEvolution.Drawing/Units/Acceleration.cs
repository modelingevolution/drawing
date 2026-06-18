using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A unit-aware acceleration: a numeric value tagged at compile time with the unit <typeparamref name="TUnit"/>
/// it is expressed in. The value is stored in <typeparamref name="TUnit"/>'s own units; conversion to the
/// SI base (m/s²) happens through <see cref="IUnit{T}"/>'s static-abstract members. Mirrors <see cref="Speed{T,TUnit}"/>.
/// </summary>
/// <typeparam name="T">Backing numeric type (e.g. <see cref="double"/>).</typeparam>
/// <typeparam name="TUnit">The unit this value is expressed in.</typeparam>
public readonly record struct Acceleration<T, TUnit> :
    IComparisonOperators<Acceleration<T, TUnit>, Acceleration<T, TUnit>, bool>,
    IComparable<Acceleration<T, TUnit>>,
    IComparable,
    IAdditionOperators<Acceleration<T, TUnit>, Acceleration<T, TUnit>, Acceleration<T, TUnit>>,
    ISubtractionOperators<Acceleration<T, TUnit>, Acceleration<T, TUnit>, Acceleration<T, TUnit>>,
    IUnaryNegationOperators<Acceleration<T, TUnit>, Acceleration<T, TUnit>>,
    IMultiplyOperators<Acceleration<T, TUnit>, T, Acceleration<T, TUnit>>,
    IDivisionOperators<Acceleration<T, TUnit>, T, Acceleration<T, TUnit>>,
    IDivisionOperators<Acceleration<T, TUnit>, Acceleration<T, TUnit>, T>,
    IParsable<Acceleration<T, TUnit>>
    where T : INumber<T>
    where TUnit : IAccelerationUnit<T>
{
    private readonly T _value;

    /// <summary>Creates an acceleration from a value expressed in <typeparamref name="TUnit"/>.</summary>
    public Acceleration(T value) => _value = value;

    /// <summary>The raw value, in <typeparamref name="TUnit"/>.</summary>
    public T Value => _value;

    /// <summary>An acceleration of zero.</summary>
    public static Acceleration<T, TUnit> Zero => new(T.Zero);

    /// <summary>This acceleration expressed in the SI base unit (m/s²).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ToSi() => TUnit.ToSi(_value);

    /// <summary>Constructs from a value already expressed in the SI base unit (m/s²).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Acceleration<T, TUnit> FromSi(T si) => new(TUnit.FromSi(si));

    /// <summary>Converts to another unit of the same (acceleration) dimension.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Acceleration<T, TOther> To<TOther>() where TOther : IAccelerationUnit<T>
        => Acceleration<T, TOther>.FromSi(ToSi());

    #region Arithmetic

    /// <summary>Adds two accelerations expressed in the same unit.</summary>
    public static Acceleration<T, TUnit> operator +(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => new(a._value + b._value);

    /// <summary>Subtracts two accelerations expressed in the same unit.</summary>
    public static Acceleration<T, TUnit> operator -(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => new(a._value - b._value);

    /// <summary>Negates an acceleration.</summary>
    public static Acceleration<T, TUnit> operator -(Acceleration<T, TUnit> a) => new(-a._value);

    /// <summary>Scales an acceleration by a scalar.</summary>
    public static Acceleration<T, TUnit> operator *(Acceleration<T, TUnit> a, T scalar) => new(a._value * scalar);

    /// <summary>Scales an acceleration by a scalar.</summary>
    public static Acceleration<T, TUnit> operator *(T scalar, Acceleration<T, TUnit> a) => new(scalar * a._value);

    /// <summary>Scales an acceleration by a percentage, e.g. <c>maxAccel * new Percentage(50)</c> → half.</summary>
    public static Acceleration<T, TUnit> operator *(Acceleration<T, TUnit> a, Percentage p) => new(a._value * T.CreateChecked(p.Fraction));

    /// <summary>Scales an acceleration by a percentage, e.g. <c>new Percentage(50) * maxAccel</c> → half.</summary>
    public static Acceleration<T, TUnit> operator *(Percentage p, Acceleration<T, TUnit> a) => new(T.CreateChecked(p.Fraction) * a._value);

    /// <summary>Divides an acceleration by a scalar.</summary>
    public static Acceleration<T, TUnit> operator /(Acceleration<T, TUnit> a, T scalar) => new(a._value / scalar);

    /// <summary>Recovers the full acceleration from one that is <paramref name="p"/> of it, e.g. half / 50% = full.</summary>
    public static Acceleration<T, TUnit> operator /(Acceleration<T, TUnit> a, Percentage p) => new(a._value / T.CreateChecked(p.Fraction));

    /// <summary>The dimensionless ratio of two accelerations expressed in the same unit.</summary>
    public static T operator /(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => a._value / b._value;

    /// <summary>Adds an acceleration expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Acceleration<T, TUnit> Add<TOther>(Acceleration<T, TOther> other) where TOther : IAccelerationUnit<T>
        => FromSi(ToSi() + other.ToSi());

    /// <summary>Subtracts an acceleration expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Acceleration<T, TUnit> Subtract<TOther>(Acceleration<T, TOther> other) where TOther : IAccelerationUnit<T>
        => FromSi(ToSi() - other.ToSi());

    /// <summary>Returns the absolute value of this acceleration.</summary>
    public Acceleration<T, TUnit> Abs() => new(T.Abs(_value));

    #endregion

    #region Comparison

    /// <inheritdoc/>
    public static bool operator <(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => a._value < b._value;
    /// <inheritdoc/>
    public static bool operator >(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => a._value > b._value;
    /// <inheritdoc/>
    public static bool operator <=(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => a._value <= b._value;
    /// <inheritdoc/>
    public static bool operator >=(Acceleration<T, TUnit> a, Acceleration<T, TUnit> b) => a._value >= b._value;

    /// <inheritdoc/>
    public int CompareTo(Acceleration<T, TUnit> other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Acceleration<T, TUnit> other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(Acceleration<T, TUnit>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static Acceleration<T, TUnit> Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new Acceleration<T, TUnit>(T.Parse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture));
    }

    /// <summary>Tries to parse a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Acceleration<T, TUnit> result)
    {
        result = Zero;
        if (s is null) return false;
        if (!T.TryParse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var val))
            return false;
        result = new Acceleration<T, TUnit>(val);
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

    /// <summary>Formats as "{value} {symbol}", e.g. "100 mm/s²".</summary>
    public override string ToString() => $"{_value} {TUnit.Symbol}";
}
