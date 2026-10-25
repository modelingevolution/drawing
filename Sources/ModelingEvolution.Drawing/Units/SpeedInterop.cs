using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// Bridges the unit-aware <see cref="Speed{T,TUnit}"/> to the legacy unit-less
/// <see cref="ModelingEvolution.Drawing.Speed{T}"/>, whose canonical unit is millimetres per minute (mm/min).
/// Use this at the boundary with APIs that take the legacy type (e.g. <c>Trajectory3Builder.MoveTo</c>) so a
/// mm/s value is not silently read as mm/min — a 60× error.
/// </summary>
public static class SpeedInterop
{
    /// <summary>
    /// Converts any unit-aware speed to the legacy <see cref="ModelingEvolution.Drawing.Speed{T}"/> (mm/min).
    /// </summary>
    public static Speed<T> ToLegacyMmPerMin<T, TUnit>(this Speed<T, TUnit> speed)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
        where TUnit : ISpeedUnit<T>
        => Speed<T>.From(speed.To<MillimetrePerMinute<T>>().Value);
}
