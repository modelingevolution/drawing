using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// Marker for units of the speed dimension (SI base: metres per second). Constraining
/// <see cref="Speed{T,TUnit}"/> to this interface prevents tagging a speed with a non-speed unit.
/// </summary>
public interface ISpeedUnit<T> : IUnit<T> where T : INumber<T>;

/// <summary>
/// SI base unit of speed: metres per second. <see cref="ToSi"/>/<see cref="FromSi"/> are identity.
/// </summary>
public readonly struct MetrePerSecond<T> : ISpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value;
    /// <inheritdoc/>
    public static T FromSi(T si) => si;
    /// <inheritdoc/>
    public static string Symbol => "m/s";
}

/// <summary>
/// Millimetres per second. Linear: 1 mm/s = 1e-3 m/s. The factor is an inline literal so the JIT
/// folds it to an immediate (verified zero-cost vs raw double).
/// </summary>
public readonly struct MillimetrePerSecond<T> : ISpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value * T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static T FromSi(T si) => si / T.CreateChecked(0.001);
    /// <inheritdoc/>
    public static string Symbol => "mm/s";
}

/// <summary>
/// Metres per minute. Linear: 1 m/min = 1/60 m/s. The factor is an inline literal so the JIT
/// folds it to an immediate (verified zero-cost vs raw double).
/// </summary>
public readonly struct MetrePerMinute<T> : ISpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value / T.CreateChecked(60.0);
    /// <inheritdoc/>
    public static T FromSi(T si) => si * T.CreateChecked(60.0);
    /// <inheritdoc/>
    public static string Symbol => "m/min";
}

/// <summary>
/// Millimetres per minute. 1 mm/min = 1e-3 m / 60 s. This is the unit of the legacy unit-less
/// <see cref="ModelingEvolution.Drawing.Speed{T}"/>, so it is the bridge target for interop
/// (see <see cref="SpeedInterop.ToLegacyMmPerMin{T, TUnit}"/>).
/// </summary>
public readonly struct MillimetrePerMinute<T> : ISpeedUnit<T> where T : INumber<T>
{
    /// <inheritdoc/>
    public static T ToSi(T value) => value / T.CreateChecked(60000.0);
    /// <inheritdoc/>
    public static T FromSi(T si) => si * T.CreateChecked(60000.0);
    /// <inheritdoc/>
    public static string Symbol => "mm/min";
}
