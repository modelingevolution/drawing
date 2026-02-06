using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides area calculation for a 2D shape.
/// </summary>
public interface IArea<T> where T : IFloatingPoint<T>
{
    T Area();
}

/// <summary>
/// Provides perimeter calculation for a 2D shape.
/// </summary>
public interface IPerimeter<T> where T : IFloatingPoint<T>
{
    T Perimeter();
}

/// <summary>
/// Provides centroid (center of mass) calculation for a 2D shape.
/// </summary>
public interface ICentroid<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    Point<T> Centroid();
}

/// <summary>
/// Provides axis-aligned bounding box calculation for a 2D shape.
/// </summary>
public interface IBoundingBox<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    Rectangle<T> BoundingBox();
}

/// <summary>
/// Provides rotation around a point for a 2D shape.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
/// <typeparam name="TSelf">The type returned after rotation.</typeparam>
public interface IRotatable<T, TSelf>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    TSelf Rotate(Degree<T> angle, Point<T> origin = default);
}

/// <summary>
/// Provides uniform scaling for a 2D shape.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
/// <typeparam name="TSelf">The type returned after scaling.</typeparam>
public interface IScalable<T, TSelf> where T : IFloatingPoint<T>
{
    TSelf Scale(T factor);
}

/// <summary>
/// Defines a closed 2D shape with area, perimeter, centroid, bounding box,
/// point containment, rotation and scaling.
/// </summary>
public interface IShape<T, TSelf> : IArea<T>, IPerimeter<T>, ICentroid<T>, IBoundingBox<T>, IRotatable<T, TSelf>, IScalable<T, TSelf>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
    where TSelf : IShape<T, TSelf>
{
    bool Contains(Point<T> point);
}
