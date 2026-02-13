using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an infinite line in 2D space.
/// Handles both standard (y = Ax + B) and vertical (x = constant) lines.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[Svg.SvgExporter(typeof(LineSvgExporterFactory))]
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
    /// Creates a horizontal line at the specified y coordinate.
    /// </summary>
    public static Line<T> Horizontal(T y) =>
        new(new LinearEquation<T>(T.Zero, y), false, T.Zero);

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

    /// <summary>
    /// Computes the translation vector that moves this line onto a parallel line.
    /// Returns null if the lines are not parallel.
    /// The vector is perpendicular to both lines and points from this line toward the other.
    /// </summary>
    /// <param name="other">The other line (must be parallel).</param>
    /// <returns>A perpendicular translation vector, or null if not parallel.</returns>
    public Vector<T>? CalculateTranslation(in Line<T> other)
    {
        var eps = T.CreateTruncating(1e-9);

        if (_isVertical && other._isVertical)
        {
            var dx = other._verticalX - _verticalX;
            return new Vector<T>(dx, T.Zero);
        }

        if (_isVertical != other._isVertical)
            return null;

        // Both non-vertical — check slopes are equal
        if (T.Abs(_equation.A - other._equation.A) > eps)
            return null;

        // Perpendicular direction: for y = ax + b, the direction (-a, 1)/len
        // points "upward" (increasing b), so (b2-b1) * (-a, 1) / (a²+1) gives
        // the correct vector from this line to other.
        var a = _equation.A;
        var lenSq = a * a + T.One;
        var db = other._equation.B - _equation.B;
        return new Vector<T>(-a * db / lenSq, db / lenSq);
    }

    /// <summary>
    /// Computes the shortest distance from a point to this line.
    /// </summary>
    /// <param name="point">The point to measure distance from.</param>
    /// <returns>The perpendicular distance from the point to the line.</returns>
    public T DistanceTo(Point<T> point)
    {
        if (_isVertical)
            return T.Abs(point.X - _verticalX);

        // Line: Ax + By + C = 0 where A = _equation.A, B = -1, C = _equation.B
        // Distance = |Ax + By + C| / sqrt(A² + B²)
        var a = _equation.A;
        return T.Abs(a * point.X - point.Y + _equation.B) / T.Sqrt(a * a + T.One);
    }

    /// <summary>
    /// Computes the angle from this line to another line.
    /// Since lines are undirected, the result is in the range (-π/2, π/2].
    /// Positive means counter-clockwise rotation from this line to the other.
    /// </summary>
    /// <param name="other">The other line.</param>
    /// <returns>The signed angle between the lines in radians.</returns>
    public Radian<T> AngleBetween(in Line<T> other)
    {
        var two = T.CreateTruncating(2);
        var halfPi = T.Pi / two;

        var t1 = _isVertical ? halfPi : T.Atan(_equation.A);
        var t2 = other._isVertical ? halfPi : T.Atan(other._equation.A);
        var diff = t2 - t1;

        // Normalize to (-π/2, π/2]
        if (diff > halfPi) diff -= T.Pi;
        if (diff <= -halfPi) diff += T.Pi;

        return Radian<T>.FromRadian(diff);
    }

    /// <summary>
    /// Rotates the line around the specified origin by the given angle.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="origin">The center of rotation. Defaults to the origin (0, 0).</param>
    /// <returns>The rotated line.</returns>
    public Line<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        // Pick two points on the line, rotate them, reconstruct
        Point<T> a, b;
        if (_isVertical)
        {
            a = new Point<T>(_verticalX, T.Zero);
            b = new Point<T>(_verticalX, T.One);
        }
        else
        {
            a = new Point<T>(T.Zero, _equation.Compute(T.Zero));
            b = new Point<T>(T.One, _equation.Compute(T.One));
        }

        return From(a.Rotate(angle, origin), b.Rotate(angle, origin));
    }

    /// <summary>
    /// Rotates the line around the origin by the given angle.
    /// </summary>
    public static Line<T> operator +(in Line<T> line, Degree<T> angle) =>
        line.Rotate(angle);

    /// <summary>
    /// Rotates the line around the origin by the negation of the given angle.
    /// </summary>
    public static Line<T> operator -(in Line<T> line, Degree<T> angle) =>
        line.Rotate(-angle);

    /// <summary>
    /// Determines whether this line is parallel to another line.
    /// </summary>
    public bool IsParallelTo(in Line<T> other)
    {
        if (_isVertical && other._isVertical) return true;
        if (_isVertical != other._isVertical) return false;
        return T.Abs(_equation.A - other._equation.A) < T.CreateTruncating(1e-9);
    }

    /// <summary>
    /// Determines whether this line is perpendicular to another line.
    /// </summary>
    public bool IsPerpendicularTo(in Line<T> other)
    {
        var eps = T.CreateTruncating(1e-9);
        if (_isVertical && other._isVertical) return false;
        if (_isVertical) return T.Abs(other._equation.A) < eps;
        if (other._isVertical) return T.Abs(_equation.A) < eps;
        return T.Abs(_equation.A * other._equation.A + T.One) < eps;
    }

    /// <summary>
    /// Returns the line perpendicular to this one passing through the given point.
    /// </summary>
    public Line<T> PerpendicularAt(Point<T> point)
    {
        if (_isVertical)
            return Horizontal(point.Y);
        return From(point, new Vector<T>(-_equation.A, T.One));
    }

    /// <summary>
    /// Projects a point onto this line (closest point on the line).
    /// </summary>
    public Point<T> ProjectPoint(Point<T> point)
    {
        if (_isVertical)
            return new Point<T>(_verticalX, point.Y);
        var a = _equation.A;
        var px = (point.X + a * (point.Y - _equation.B)) / (a * a + T.One);
        var py = _equation.Compute(px);
        return new Point<T>(px, py);
    }

    /// <summary>
    /// Reflects a point across this line.
    /// </summary>
    public Point<T> Reflect(Point<T> point)
    {
        var proj = ProjectPoint(point);
        return new Point<T>(proj.X + proj.X - point.X, proj.Y + proj.Y - point.Y);
    }

    /// <summary>
    /// Projects a segment onto this line, returning the orthogonal projection as a new segment.
    /// </summary>
    public Segment<T> Project(Segment<T> segment) =>
        new Segment<T>(ProjectPoint(segment.Start), ProjectPoint(segment.End));

    /// <summary>
    /// Returns the portion of this infinite line that lies inside the rectangle, or null if it misses.
    /// </summary>
    public Segment<T>? Intersect(Rectangle<T> rect)
    {
        if (_isVertical)
        {
            if (_verticalX < rect.X || _verticalX > rect.Right) return null;
            return new Segment<T>(
                new Point<T>(_verticalX, rect.Y),
                new Point<T>(_verticalX, rect.Bottom));
        }

        // Create a segment extending well beyond the rectangle, then clip
        var margin = rect.Width + rect.Height;
        var xMin = rect.X - margin;
        var xMax = rect.Right + margin;
        var a = new Point<T>(xMin, _equation.Compute(xMin));
        var b = new Point<T>(xMax, _equation.Compute(xMax));
        return new Segment<T>(a, b).Intersect(rect);
    }

    public override string ToString() =>
        _isVertical ? $"Line(x = {_verticalX})" : $"Line(y = {_equation.A}x + {_equation.B})";
}
