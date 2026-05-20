using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Collection-expression builder for <see cref="Vector3{T}"/>.
/// Enables <c>Vector3&lt;double&gt; v = [10, 0, 0];</c> syntax.
/// </summary>
public static class Vector3CollectionBuilder
{
    /// <summary>
    /// Constructs a <see cref="Vector3{T}"/> from a span of exactly three components.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the span length is not 3.</exception>
    public static Vector3<T> Create<T>(ReadOnlySpan<T> values)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (values.Length != 3)
            throw new ArgumentException($"Vector3 requires exactly 3 components; got {values.Length}.", nameof(values));
        return new Vector3<T>(values[0], values[1], values[2]);
    }
}
