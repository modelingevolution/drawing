using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an infinite line in 2D space.
/// Handles both standard (y = Ax + B) and vertical (x = constant) lines.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly record struct Line<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly LinearEquation<T> _equation;
    private readonly bool _isVertical;
    private readonly T _verticalX;

    private Line(LinearEquation<T> equation, bool isVertical, T verticalX)
    {
        _equation = equation;
        _isVertical = isVertical;
        _verticalX = verticalX;
    }

    /// <summary>
    /// Creates a line from two points.
    /// </summary>
    public static Line<T> From(Point<T> a, Point<T> b)
    {
        var dx = b.X - a.X;
        if (T.Abs(dx) < T.CreateTruncating(1e-9))
            return new Line<T>(default, true, a.X);

        return new Line<T>(LinearEquation<T>.From(a, b), false, T.Zero);
    }

    /// <summary>
    /// Creates a line from a point and a direction vector.
    /// </summary>
    public static Line<T> From(Point<T> point, Vector<T> direction)
    {
        if (T.Abs(direction.X) < T.CreateTruncating(1e-9))
            return new Line<T>(default, true, point.X);

        return new Line<T>(LinearEquation<T>.From(point, direction), false, T.Zero);
    }

    /// <summary>
    /// Creates a line from an existing LinearEquation.
    /// </summary>
    public static Line<T> FromEquation(LinearEquation<T> eq) =>
        new(eq, false, T.Zero);

    /// <summary>
    /// Creates a vertical line at the specified x coordinate.
    /// </summary>
    public static Line<T> Vertical(T x) =>
        new(default, true, x);

    /// <summary>
    /// Gets the underlying LinearEquation. Throws if the line is vertical.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the line is vertical.</exception>
    public LinearEquation<T> Equation =>
        !_isVertical ? _equation : throw new InvalidOperationException("Vertical line has no slope-intercept equation.");

    /// <summary>
    /// Gets whether this line is vertical.
    /// </summary>
    public bool IsVertical => _isVertical;

    /// <summary>
    /// Gets the x coordinate for a vertical line.
    /// </summary>
    public T VerticalX => _isVertical ? _verticalX : throw new InvalidOperationException("Non-vertical line has no VerticalX.");

    /// <summary>
    /// Computes the y value for a given x. Throws if the line is vertical.
    /// </summary>
    public T Compute(T x) =>
        !_isVertical ? _equation.Compute(x) : throw new InvalidOperationException("Cannot compute y for a vertical line.");

    /// <summary>
    /// Translates the line by a vector.
    /// </summary>
    public static Line<T> operator +(in Line<T> line, Vector<T> v) =>
        line._isVertical
            ? new Line<T>(default, true, line._verticalX + v.X)
            : new Line<T>(line._equation.Translate(v), false, T.Zero);

    /// <summary>
    /// Translates the line by subtracting a vector.
    /// </summary>
    public static Line<T> operator -(in Line<T> line, Vector<T> v) =>
        line + new Vector<T>(-v.X, -v.Y);

    /// <summary>
    /// Finds the intersection point between this line and another line.
    /// Returns null if the lines are parallel or coincident.
    /// </summary>
    public Point<T>? Intersect(in Line<T> other) => Intersections.Of(this, other);

    /// <summary>
    /// Finds the intersection point between this line and a segment.
    /// Returns null if the line does not intersect the segment.
    /// </summary>
    public Point<T>? Intersect(in Segment<T> segment) => Intersections.Of(this, segment);

    /// <summary>
    /// Returns the chord where this line passes through the circle interior.
    /// Null if the line misses or is tangent.
    /// </summary>
    public Segment<T>? Intersect(in Circle<T> circle) => Intersections.Of(this, circle);

    /// <summary>
    /// Returns the point where this line is tangent to the circle.
    /// Null if the line misses or is secant.
    /// </summary>
    public Point<T>? TangentPoint(in Circle<T> circle) => Intersections.TangentPoint(this, circle);

    /// <summary>
    /// Returns the first chord where this line passes through a triangle.
    /// Zero heap allocation.
    /// </summary>
    public Segment<T>? FirstIntersection(in Triangle<T> triangle) => Intersections.FirstOf(this, triangle);

    public override string ToString() =>
        _isVertical ? $"Line(x = {_verticalX})" : $"Line(y = {_equation.A}x + {_equation.B})";
}
