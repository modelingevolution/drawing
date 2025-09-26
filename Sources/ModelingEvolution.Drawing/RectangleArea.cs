using System.Numerics;

namespace ModelingEvolution.Drawing;
using VectorF = Vector<float>;

/// <summary>
/// Represents a rectangle area that can accumulate multiple rectangles and compute their bounding area.
/// This structure supports floating-point numeric types and provides area calculations and bounds operations.
/// </summary>
/// <typeparam name="T">The floating-point numeric type used for coordinates and dimensions.</typeparam>
public readonly record struct RectangleArea<T>
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Rectangle<T> _result;
    private readonly bool _isEmpty = true;

    /// <summary>
    /// Initializes a new instance of the RectangleArea struct representing an empty area.
    /// </summary>
    public RectangleArea()
    {
        _isEmpty = true;
        _result = Rectangle<T>.Empty;
    }

    private RectangleArea(Rectangle<T> result)
    {
        _isEmpty = false;
        _result = result;
    }

    /// <summary>
    /// Gets the offset (top-left location) of the accumulated rectangle area.
    /// </summary>
    public Vector<T> Offset => _result.Location;
    /// <summary>
    /// Gets the area value (height × width) of the accumulated rectangle.
    /// </summary>
    public T Value => _result.Height * _result.Width;
    /// <summary>
    /// Implicitly converts a RectangleArea to its size dimensions.
    /// </summary>
    /// <param name="rectangleAreaF">The rectangle area to convert.</param>
    /// <returns>The size of the rectangle area.</returns>
    public static implicit operator Size<T>(RectangleArea<T> rectangleAreaF) => rectangleAreaF._result.Size;
    /// <summary>
    /// Implicitly converts a RectangleArea to its area value.
    /// </summary>
    /// <param name="rectangleAreaF">The rectangle area to convert.</param>
    /// <returns>The area value (height × width).</returns>
    public static implicit operator T(RectangleArea<T> rectangleAreaF) => rectangleAreaF.Value;
    /// <summary>
    /// Explicitly converts a RectangleArea to its underlying Rectangle.
    /// </summary>
    /// <param name="rectangleAreaF">The rectangle area to convert.</param>
    /// <returns>The underlying rectangle representing the accumulated bounds.</returns>
    public static explicit operator Rectangle<T>(RectangleArea<T> rectangleAreaF) => rectangleAreaF._result;
    /// <summary>
    /// Adds a rectangle to the rectangle area, expanding the bounds to include the new rectangle.
    /// </summary>
    /// <param name="left">The current rectangle area.</param>
    /// <param name="right">The rectangle to add to the area.</param>
    /// <returns>A new RectangleArea that encompasses both the original area and the new rectangle.</returns>
    public static RectangleArea<T> operator +(RectangleArea<T> left, Rectangle<T> right)
    {
        if (right.Size == Size<T>.Empty) return left;

        return left._isEmpty ? new RectangleArea<T>(right) : 
            new RectangleArea<T>(Rectangle<T>.Bounds(right, left._result));
    }

    /// <summary>
    /// Returns a string representation of the rectangle area.
    /// </summary>
    /// <returns>A string representing the rectangle area, or "[-]" if empty.</returns>
    public override string ToString()
    {
        return _isEmpty ? "[-]": _result.ToString();
    }
}