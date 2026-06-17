using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Collection-expression builder for <see cref="Point3{T}"/>.
/// Enables <c>Point3&lt;double&gt; p = [10, 20, 30];</c> syntax.
/// </summary>
public static class Point3CollectionBuilder
{
    /// <summary>
    /// Constructs a <see cref="Point3{T}"/> from a span of exactly three components.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the span length is not 3.</exception>
    public static Point3<T> Create<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (values.Length != 3)
            throw new ArgumentException($"Point3 requires exactly 3 components; got {values.Length}.", nameof(values));
        return new Point3<T>(values[0], values[1], values[2]);
    }
}
