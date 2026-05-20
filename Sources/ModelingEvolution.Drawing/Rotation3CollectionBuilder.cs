using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Collection-expression builder for <see cref="Rotation3{T}"/>.
/// Enables <c>Rotation3&lt;double&gt; r = [0, 0, 5];</c> syntax (rx, ry, rz in degrees).
/// </summary>
public static class Rotation3CollectionBuilder
{
    /// <summary>
    /// Constructs a <see cref="Rotation3{T}"/> from a span of exactly three Euler angle components in degrees.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the span length is not 3.</exception>
    public static Rotation3<T> Create<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (values.Length != 3)
            throw new ArgumentException($"Rotation3 requires exactly 3 components (rx, ry, rz in degrees); got {values.Length}.", nameof(values));
        return new Rotation3<T>(values[0], values[1], values[2]);
    }
}
