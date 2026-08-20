using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A unit-aware angular speed: a numeric value tagged at compile time with the unit
/// <typeparamref name="TUnit"/> it is expressed in. Mirrors <see cref="Speed{T,TUnit}"/>; conversion to the
/// SI base (rad/s) happens through <see cref="IUnit{T}"/>'s static-abstract members.
/// <para>
/// The value is signed: a negative angular speed is a rotation in the opposite sense, which is part of
/// the contract for axes that are commanded by velocity rather than by target.
/// </para>
/// </summary>
/// <typeparam name="T">Backing numeric type (e.g. <see cref="double"/>).</typeparam>
/// <typeparam name="TUnit">The unit this value is expressed in.</typeparam>
public readonly record struct AngularSpeed<T, TUnit> :
    IComparisonOperators<AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>, bool>,
    IComparable<AngularSpeed<T, TUnit>>,
    IComparable,
    IAdditionOperators<AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>>,
    ISubtractionOperators<AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>>,
    IUnaryNegationOperators<AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>>,
    IMultiplyOperators<AngularSpeed<T, TUnit>, T, AngularSpeed<T, TUnit>>,
    IDivisionOperators<AngularSpeed<T, TUnit>, T, AngularSpeed<T, TUnit>>,
    IDivisionOperators<AngularSpeed<T, TUnit>, AngularSpeed<T, TUnit>, T>,
    IParsable<AngularSpeed<T, TUnit>>
    where T : INumber<T>
    where TUnit : IAngularSpeedUnit<T>
{
    private readonly T _value;

    /// <summary>Creates an angular speed from a value expressed in <typeparamref name="TUnit"/>.</summary>
    public AngularSpeed(T value) => _value = value;

    /// <summary>The raw value, in <typeparamref name="TUnit"/>.</summary>
    public T Value => _value;

    /// <summary>An angular speed of zero.</summary>
    public static AngularSpeed<T, TUnit> Zero => new(T.Zero);

    /// <summary>This angular speed expressed in the SI base unit (rad/s).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ToSi() => TUnit.ToSi(_value);

    /// <summary>Constructs from a value already expressed in the SI base unit (rad/s).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AngularSpeed<T, TUnit> FromSi(T si) => new(TUnit.FromSi(si));

    /// <summary>Converts to another unit of the same (angular-speed) dimension.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AngularSpeed<T, TOther> To<TOther>() where TOther : IAngularSpeedUnit<T>
        => AngularSpeed<T, TOther>.FromSi(ToSi());

    #region Arithmetic

    /// <summary>Adds two angular speeds expressed in the same unit.</summary>
    public static AngularSpeed<T, TUnit> operator +(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => new(a._value + b._value);

    /// <summary>Subtracts two angular speeds expressed in the same unit.</summary>
    public static AngularSpeed<T, TUnit> operator -(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => new(a._value - b._value);

    /// <summary>Negates an angular speed, i.e. reverses the sense of rotation.</summary>
    public static AngularSpeed<T, TUnit> operator -(AngularSpeed<T, TUnit> a) => new(-a._value);

    /// <summary>Scales an angular speed by a scalar.</summary>
    public static AngularSpeed<T, TUnit> operator *(AngularSpeed<T, TUnit> a, T scalar) => new(a._value * scalar);

    /// <summary>Scales an angular speed by a scalar.</summary>
    public static AngularSpeed<T, TUnit> operator *(T scalar, AngularSpeed<T, TUnit> a) => new(scalar * a._value);

    /// <summary>Scales an angular speed by a percentage, e.g. <c>maxSpeed * new Percentage(50)</c> → half speed.</summary>
    public static AngularSpeed<T, TUnit> operator *(AngularSpeed<T, TUnit> a, Percentage p) => new(a._value * T.CreateChecked(p.Fraction));

    /// <summary>Scales an angular speed by a percentage, e.g. <c>new Percentage(50) * maxSpeed</c> → half speed.</summary>
    public static AngularSpeed<T, TUnit> operator *(Percentage p, AngularSpeed<T, TUnit> a) => new(T.CreateChecked(p.Fraction) * a._value);

    /// <summary>Divides an angular speed by a scalar.</summary>
    public static AngularSpeed<T, TUnit> operator /(AngularSpeed<T, TUnit> a, T scalar) => new(a._value / scalar);

    /// <summary>Recovers the full angular speed from one that is <paramref name="p"/> of it, e.g. half / 50% = full.</summary>
    public static AngularSpeed<T, TUnit> operator /(AngularSpeed<T, TUnit> a, Percentage p) => new(a._value / T.CreateChecked(p.Fraction));

    /// <summary>The dimensionless ratio of two angular speeds expressed in the same unit.</summary>
    public static T operator /(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => a._value / b._value;

    /// <summary>Adds an angular speed expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AngularSpeed<T, TUnit> Add<TOther>(AngularSpeed<T, TOther> other) where TOther : IAngularSpeedUnit<T>
        => FromSi(ToSi() + other.ToSi());

    /// <summary>Subtracts an angular speed expressed in a different unit, normalising through SI. Result is in this unit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AngularSpeed<T, TUnit> Subtract<TOther>(AngularSpeed<T, TOther> other) where TOther : IAngularSpeedUnit<T>
        => FromSi(ToSi() - other.ToSi());

    /// <summary>Returns the absolute value of this angular speed, i.e. its magnitude without a sense.</summary>
    public AngularSpeed<T, TUnit> Abs() => new(T.Abs(_value));

    #endregion

    #region Comparison

    /// <inheritdoc/>
    public static bool operator <(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => a._value < b._value;
    /// <inheritdoc/>
    public static bool operator >(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => a._value > b._value;
    /// <inheritdoc/>
    public static bool operator <=(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => a._value <= b._value;
    /// <inheritdoc/>
    public static bool operator >=(AngularSpeed<T, TUnit> a, AngularSpeed<T, TUnit> b) => a._value >= b._value;

    /// <inheritdoc/>
    public int CompareTo(AngularSpeed<T, TUnit> other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is AngularSpeed<T, TUnit> other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(AngularSpeed<T, TUnit>)}");
    }

    #endregion

    #region Parsing

    /// <summary>Parses a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static AngularSpeed<T, TUnit> Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new AngularSpeed<T, TUnit>(T.Parse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture));
    }

    /// <summary>Tries to parse a value in <typeparamref name="TUnit"/>; an optional trailing unit symbol is allowed.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out AngularSpeed<T, TUnit> result)
    {
        result = Zero;
        if (s is null) return false;
        if (!T.TryParse(StripSymbol(s), NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var val))
            return false;
        result = new AngularSpeed<T, TUnit>(val);
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

    /// <summary>Formats as "{value} {symbol}", e.g. "12.5 °/s".</summary>
    public override string ToString() => $"{_value} {TUnit.Symbol}";
}
