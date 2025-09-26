using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an object that has a rectangular boundary.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and dimensions.</typeparam>
public interface IBoundedRect<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Gets or sets the rectangular boundary of the object.
    /// </summary>
    Rectangle<T> Rect { get; set; }
}