using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Collection-expression builder for <see cref="Pose3{T}"/>.
/// Enables <c>Pose3&lt;double&gt; p = [10, 20, 30, 0, 0, 0];</c> syntax (x, y, z, rx, ry, rz; rotation in degrees).
/// </summary>
public static class Pose3CollectionBuilder
{
    /// <summary>
    /// Constructs a <see cref="Pose3{T}"/> from a span of exactly six components: x, y, z, rx, ry, rz.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the span length is not 6.</exception>
    public static Pose3<T> Create<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (values.Length != 6)
            throw new ArgumentException($"Pose3 requires exactly 6 components (x, y, z, rx, ry, rz); got {values.Length}.", nameof(values));
        return new Pose3<T>(values[0], values[1], values[2], values[3], values[4], values[5]);
    }
}
