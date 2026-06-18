using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// Marker for units of the length dimension (SI base: metres). Constraining <see cref="Length{T,TUnit}"/>
/// to this interface prevents tagging a length with a non-length unit.
/// </summary>
public interface ILengthUnit<T> : IUnit<T> where T : INumber<T>;

/// <summary>
/// SI base unit of length: metres. <see cref="ToSi"/>/<see cref="FromSi"/> are identity.
/// </summary>
public readonly struct Metre<T> : ILengthUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value;
    /// <inheritdoc/>
    public static T FromSi(T si) => si;
    /// <inheritdoc/>
    public static string Symbol => "m";
}

/// <summary>
/// Millimetres. Linear: 1 mm = 1e-3 m. The factor is an inline literal so the JIT folds it to an
/// immediate (verified zero-cost vs raw double, same as the speed units).
/// </summary>
public readonly struct Millimetre<T> : ILengthUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value * T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static T FromSi(T si) => si / T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static string Symbol => "mm";
}
