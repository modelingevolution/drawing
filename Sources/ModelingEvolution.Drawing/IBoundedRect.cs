using System.Numerics;

namespace ModelingEvolution.Drawing;

public interface IBoundedRect<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    Rectangle<T> Rect { get; set; }
}