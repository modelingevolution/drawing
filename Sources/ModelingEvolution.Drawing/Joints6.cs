using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents 6 joint angles for a 6-DOF robot arm, stored as fields (no array allocation).
/// </summary>
/// <typeparam name="T">The numeric type used for angle values.</typeparam>
[ProtoContract]
[Joints6JsonConverterAttribute]
public struct Joints6<T> : IEquatable<Joints6<T>>, IParsable<Joints6<T>>, IFormattable
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private Degree<T> _j1;
    private Degree<T> _j2;
    private Degree<T> _j3;
    private Degree<T> _j4;
    private Degree<T> _j5;
    private Degree<T> _j6;

    [ProtoMember(1)]
    private T ProtoJ1 { readonly get => (T)_j1; set => _j1 = value; }
    [ProtoMember(2)]
    private T ProtoJ2 { readonly get => (T)_j2; set => _j2 = value; }
    [ProtoMember(3)]
    private T ProtoJ3 { readonly get => (T)_j3; set => _j3 = value; }
    [ProtoMember(4)]
    private T ProtoJ4 { readonly get => (T)_j4; set => _j4 = value; }
    [ProtoMember(5)]
    private T ProtoJ5 { readonly get => (T)_j5; set => _j5 = value; }
    [ProtoMember(6)]
    private T ProtoJ6 { readonly get => (T)_j6; set => _j6 = value; }

    /// <summary>
    /// A joint configuration with all angles at zero.
    /// </summary>
    public static readonly Joints6<T> Zero = default;

    /// <summary>
    /// Initializes a new instance with the specified joint angles.
    /// </summary>
    /// <param name="j1">Angle of joint 1.</param>
    /// <param name="j2">Angle of joint 2.</param>
    /// <param name="j3">Angle of joint 3.</param>
    /// <param name="j4">Angle of joint 4.</param>
    /// <param name="j5">Angle of joint 5.</param>
    /// <param name="j6">Angle of joint 6.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Joints6(Degree<T> j1, Degree<T> j2, Degree<T> j3, Degree<T> j4, Degree<T> j5, Degree<T> j6)
    {
        _j1 = j1;
        _j2 = j2;
        _j3 = j3;
        _j4 = j4;
        _j5 = j5;
        _j6 = j6;
    }

    /// <summary>Gets the angle of joint 1.</summary>
    public readonly Degree<T> J1 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j1; }
    /// <summary>Gets the angle of joint 2.</summary>
    public readonly Degree<T> J2 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j2; }
    /// <summary>Gets the angle of joint 3.</summary>
    public readonly Degree<T> J3 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j3; }
    /// <summary>Gets the angle of joint 4.</summary>
    public readonly Degree<T> J4 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j4; }
    /// <summary>Gets the angle of joint 5.</summary>
    public readonly Degree<T> J5 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j5; }
    /// <summary>Gets the angle of joint 6.</summary>
    public readonly Degree<T> J6 { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _j6; }

    /// <summary>
    /// Gets the joint angle at the specified index (0-based).
    /// </summary>
    /// <param name="index">Joint index, 0 through 5.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is outside 0..5.</exception>
    public readonly Degree<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => index switch
        {
            0 => _j1,
            1 => _j2,
            2 => _j3,
            3 => _j4,
            4 => _j5,
            5 => _j6,
            _ => throw new IndexOutOfRangeException($"Joint index must be 0..5, got {index}.")
        };
    }

    #region Operators

    /// <summary>Adds two joint configurations element-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator +(Joints6<T> a, Joints6<T> b) =>
        new(a._j1 + b._j1, a._j2 + b._j2, a._j3 + b._j3, a._j4 + b._j4, a._j5 + b._j5, a._j6 + b._j6);

    /// <summary>Subtracts two joint configurations element-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator -(Joints6<T> a, Joints6<T> b) =>
        new(a._j1 - b._j1, a._j2 - b._j2, a._j3 - b._j3, a._j4 - b._j4, a._j5 - b._j5, a._j6 - b._j6);

    /// <summary>Negates all joint angles.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator -(Joints6<T> a) =>
        new(-a._j1, -a._j2, -a._j3, -a._j4, -a._j5, -a._j6);

    /// <summary>Multiplies all joint angles by a scalar.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator *(Joints6<T> a, T scalar) =>
        new(a._j1 * scalar, a._j2 * scalar, a._j3 * scalar, a._j4 * scalar, a._j5 * scalar, a._j6 * scalar);

    /// <summary>Multiplies all joint angles by a scalar.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator *(T scalar, Joints6<T> a) => a * scalar;

    /// <summary>Divides all joint angles by a scalar.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> operator /(Joints6<T> a, T scalar) =>
        new(a._j1 / scalar, a._j2 / scalar, a._j3 / scalar, a._j4 / scalar, a._j5 / scalar, a._j6 / scalar);

    /// <summary>Determines whether two joint configurations are equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Joints6<T> a, Joints6<T> b) => a.Equals(b);

    /// <summary>Determines whether two joint configurations are not equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Joints6<T> a, Joints6<T> b) => !a.Equals(b);

    #endregion

    #region Methods

    /// <summary>
    /// Linearly interpolates between two joint configurations.
    /// </summary>
    /// <param name="a">The start configuration (t=0).</param>
    /// <param name="b">The end configuration (t=1).</param>
    /// <param name="t">Interpolation parameter, typically in [0,1].</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Joints6<T> Lerp(Joints6<T> a, Joints6<T> b, T t)
    {
        var oneMinusT = T.One - t;
        return new Joints6<T>(
            a._j1 * oneMinusT + b._j1 * t,
            a._j2 * oneMinusT + b._j2 * t,
            a._j3 * oneMinusT + b._j3 * t,
            a._j4 * oneMinusT + b._j4 * t,
            a._j5 * oneMinusT + b._j5 * t,
            a._j6 * oneMinusT + b._j6 * t);
    }

    /// <summary>
    /// Returns the largest absolute difference across all joints.
    /// </summary>
    /// <param name="other">The other joint configuration to compare against.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Degree<T> MaxAbsDelta(Joints6<T> other)
    {
        var d1 = (_j1 - other._j1).Abs();
        var d2 = (_j2 - other._j2).Abs();
        var d3 = (_j3 - other._j3).Abs();
        var d4 = (_j4 - other._j4).Abs();
        var d5 = (_j5 - other._j5).Abs();
        var d6 = (_j6 - other._j6).Abs();

        var max = d1;
        if (d2 > max) max = d2;
        if (d3 > max) max = d3;
        if (d4 > max) max = d4;
        if (d5 > max) max = d5;
        if (d6 > max) max = d6;
        return max;
    }

    /// <summary>
    /// Returns true if all joints are within the specified tolerance of the other configuration.
    /// </summary>
    /// <param name="other">The other joint configuration to compare against.</param>
    /// <param name="tolerance">Maximum allowed difference per joint.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsWithin(Joints6<T> other, Degree<T> tolerance) => MaxAbsDelta(other) <= tolerance;

    /// <summary>
    /// Returns true if every joint angle is between the corresponding min and max values (inclusive).
    /// </summary>
    /// <param name="min">The minimum allowed angles per joint.</param>
    /// <param name="max">The maximum allowed angles per joint.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsBetween(Joints6<T> min, Joints6<T> max) =>
        _j1 >= min._j1 && _j1 <= max._j1 &&
        _j2 >= min._j2 && _j2 <= max._j2 &&
        _j3 >= min._j3 && _j3 <= max._j3 &&
        _j4 >= min._j4 && _j4 <= max._j4 &&
        _j5 >= min._j5 && _j5 <= max._j5 &&
        _j6 >= min._j6 && _j6 <= max._j6;

    /// <summary>
    /// Copies the joint angles into a new array of Degree{T}.
    /// </summary>
    public readonly Degree<T>[] ToArray() => [_j1, _j2, _j3, _j4, _j5, _j6];

    /// <summary>
    /// Copies the joint angles into the destination span. The span must have at least 6 elements.
    /// </summary>
    /// <param name="destination">A span with at least 6 elements.</param>
    /// <exception cref="ArgumentException">Thrown when the span has fewer than 6 elements.</exception>
    public readonly void CopyTo(Span<Degree<T>> destination)
    {
        if (destination.Length < 6)
            throw new ArgumentException($"Destination span must have at least 6 elements, got {destination.Length}.", nameof(destination));
        destination[0] = _j1;
        destination[1] = _j2;
        destination[2] = _j3;
        destination[3] = _j4;
        destination[4] = _j5;
        destination[5] = _j6;
    }

    /// <summary>
    /// Creates a Joints6 from a read-only span of Degree{T}. The span must have exactly 6 elements.
    /// </summary>
    /// <param name="joints">A span of exactly 6 joint angles.</param>
    /// <exception cref="ArgumentException">Thrown when the span does not have exactly 6 elements.</exception>
    public static Joints6<T> FromSpan(ReadOnlySpan<Degree<T>> joints)
    {
        if (joints.Length != 6)
            throw new ArgumentException($"Expected 6 joint values, got {joints.Length}.", nameof(joints));
        return new Joints6<T>(joints[0], joints[1], joints[2], joints[3], joints[4], joints[5]);
    }

    /// <summary>
    /// Creates a Joints6 from an array of Degree{T}. The array must have exactly 6 elements.
    /// </summary>
    /// <param name="joints">An array of exactly 6 joint angles.</param>
    /// <exception cref="ArgumentException">Thrown when the array does not have exactly 6 elements.</exception>
    public static Joints6<T> FromArray(Degree<T>[] joints)
    {
        if (joints.Length != 6)
            throw new ArgumentException($"Expected 6 joint values, got {joints.Length}.", nameof(joints));
        return new Joints6<T>(joints[0], joints[1], joints[2], joints[3], joints[4], joints[5]);
    }

    /// <summary>
    /// Deconstructs into individual joint angles.
    /// </summary>
    /// <param name="j1">Angle of joint 1.</param>
    /// <param name="j2">Angle of joint 2.</param>
    /// <param name="j3">Angle of joint 3.</param>
    /// <param name="j4">Angle of joint 4.</param>
    /// <param name="j5">Angle of joint 5.</param>
    /// <param name="j6">Angle of joint 6.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out Degree<T> j1, out Degree<T> j2, out Degree<T> j3,
        out Degree<T> j4, out Degree<T> j5, out Degree<T> j6)
    {
        j1 = _j1;
        j2 = _j2;
        j3 = _j3;
        j4 = _j4;
        j5 = _j5;
        j6 = _j6;
    }

    #endregion

    #region Equality & Formatting

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Joints6<T> other) =>
        _j1 == other._j1 && _j2 == other._j2 && _j3 == other._j3 &&
        _j4 == other._j4 && _j5 == other._j5 && _j6 == other._j6;

    /// <inheritdoc />
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Joints6<T> j && Equals(j);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(_j1, _j2, _j3, _j4, _j5, _j6);

    /// <summary>
    /// Returns a string representation of all joint angles with degree symbols.
    /// </summary>
    public override readonly string ToString() =>
        $"{_j1}, {_j2}, {_j3}, {_j4}, {_j5}, {_j6}";

    /// <summary>
    /// Formats all joint angles using the specified format and provider.
    /// </summary>
    /// <param name="format">The format string applied to each angle value.</param>
    /// <param name="formatProvider">The format provider.</param>
    public readonly string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"{_j1.ToString(format, formatProvider)}, {_j2.ToString(format, formatProvider)}, {_j3.ToString(format, formatProvider)}, " +
        $"{_j4.ToString(format, formatProvider)}, {_j5.ToString(format, formatProvider)}, {_j6.ToString(format, formatProvider)}";

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a string of 6 comma- or space-separated numeric values into a Joints6.
    /// </summary>
    /// <param name="source">The string to parse (e.g. "10, 20, 30, 40, 50, 60").</param>
    /// <param name="provider">An optional format provider (ignored; invariant culture is always used).</param>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format.</exception>
    public static Joints6<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j1) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j2) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j3) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j4) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j5) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j6))
            throw new FormatException($"Invalid Joints6<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Joints6<T> format: {source}");

        return new Joints6<T>(j1, j2, j3, j4, j5, j6);
    }

    /// <summary>
    /// Attempts to parse a string of 6 comma- or space-separated numeric values into a Joints6.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="provider">An optional format provider (ignored; invariant culture is always used).</param>
    /// <param name="result">When this method returns, contains the parsed value, or <see cref="Zero"/> on failure.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? source, IFormatProvider? provider, out Joints6<T> result)
    {
        result = Zero;
        if (source == null) return false;

        try
        {
            var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

            if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j1) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j2) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j3) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j4) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j5) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var j6) ||
                !tokenizer.HasNoMoreTokens())
                return false;

            result = new Joints6<T>(j1, j2, j3, j4, j5, j6);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
