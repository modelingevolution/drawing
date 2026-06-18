using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// Marker for units of the acceleration dimension (SI base: metres per second squared). Constraining
/// <see cref="Acceleration{T,TUnit}"/> to this interface prevents tagging an acceleration with a non-acceleration unit.
/// </summary>
public interface IAccelerationUnit<T> : IUnit<T> where T : INumber<T>;

/// <summary>
/// SI base unit of acceleration: metres per second squared. <see cref="ToSi"/>/<see cref="FromSi"/> are identity.
/// </summary>
public readonly struct MetrePerSecondSquared<T> : IAccelerationUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value;
    /// <inheritdoc/>
    public static T FromSi(T si) => si;
    /// <inheritdoc/>
    public static string Symbol => "m/s²";
}

/// <summary>
/// Millimetres per second squared. Linear: 1 mm/s² = 1e-3 m/s². The factor is an inline literal so the JIT
/// folds it to an immediate (verified zero-cost vs raw double, same as the speed units).
/// </summary>
public readonly struct MillimetrePerSecondSquared<T> : IAccelerationUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value * T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static T FromSi(T si) => si / T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static string Symbol => "mm/s²";
}
