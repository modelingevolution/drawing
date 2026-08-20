using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// Marker for units of the angular-speed dimension (SI base: radians per second). Constraining
/// <see cref="AngularSpeed{T,TUnit}"/> to this interface prevents tagging an angular speed with a
/// non-angular-speed unit.
/// <para>
/// This is deliberately a dimension of its own, disjoint from <see cref="ISpeedUnit{T}"/> (SI base
/// m/s): an angular speed and a linear speed are different physical quantities, so adding °/s to
/// mm/s — or handing an angular speed to an API that wants a linear one — must not compile.
/// </para>
/// </summary>
public interface IAngularSpeedUnit<T> : IUnit<T> where T : INumber<T>;

/// <summary>
/// SI base unit of angular speed: radians per second. <see cref="ToSi"/>/<see cref="FromSi"/> are identity.
/// </summary>
public readonly struct RadianPerSecond<T> : IAngularSpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value;
    /// <inheritdoc/>
    public static T FromSi(T si) => si;
    /// <inheritdoc/>
    public static string Symbol => "rad/s";
}

/// <summary>
/// Degrees per second. Linear: 1 °/s = π/180 rad/s. The factor is an inline literal so the JIT
/// folds it to an immediate (verified zero-cost vs raw double).
/// </summary>
public readonly struct DegreePerSecond<T> : IAngularSpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value * T.CreateChecked(0.017453292519943295);
    /// <inheritdoc/>
    public static T FromSi(T si) => si / T.CreateChecked(0.017453292519943295);
    /// <inheritdoc/>
    public static string Symbol => "°/s";
}
