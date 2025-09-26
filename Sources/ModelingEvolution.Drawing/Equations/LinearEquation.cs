using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

/// <summary>
/// Represents a linear equation of the form y = Ax + B.
/// </summary>
/// <typeparam name="T">The numeric type used for coefficients.</typeparam>
public readonly record struct LinearEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the slope (coefficient A) of the linear equation.
    /// </summary>
    public T A { get; }
    /// <summary>
    /// Gets the y-intercept (coefficient B) of the linear equation.
    /// </summary>
    public T B { get;  }
    /// <summary>
    /// Initializes a new instance of the LinearEquation struct.
    /// </summary>
    /// <param name="a">The slope of the line.</param>
    /// <param name="b">The y-intercept of the line.</param>
    public LinearEquation(T a, T b)
    {
        A = a;
        B = b;
    }

    /// <summary>
    /// Creates a linear equation with the specified slope that passes through the given point.
    /// </summary>
    /// <param name="a">The slope of the line.</param>
    /// <param name="point">A point that the line passes through.</param>
    /// <returns>A linear equation representing the line.</returns>
    public static LinearEquation<T> From(T a, Point<T> point)
    {
        var b = point.Y - a * point.X;
        return new LinearEquation<T>(a, b);
    }

    /// <summary>
    /// Creates a linear equation perpendicular to this line that passes through the specified point.
    /// </summary>
    /// <param name="point">The point that the perpendicular line passes through.</param>
    /// <returns>A linear equation representing the perpendicular line.</returns>
    public LinearEquation<T> Perpendicular(Point<T> point)
    {
        return From(-T.One / A, point);
    }
    /// <summary>
    /// Creates a linear equation with the specified angle passing through the origin.
    /// </summary>
    /// <param name="angle">The angle of the line in radians.</param>
    /// <returns>A linear equation representing the line.</returns>
    public static LinearEquation<T> From(Radian<T> angle)
    {
        return From(angle, T.Zero);
    }
    /// <summary>
    /// Creates a linear equation with the specified angle and y-intercept.
    /// </summary>
    /// <param name="angle">The angle of the line in radians.</param>
    /// <param name="b">The y-intercept of the line.</param>
    /// <returns>A linear equation representing the line.</returns>
    public static LinearEquation<T> From(Radian<T> angle, T b)
    {
        return new LinearEquation<T>(T.Tan((T)angle), b);
    }

    /// <summary>
    /// Computes the y-value for the given x-value using this linear equation.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <returns>The corresponding y-coordinate.</returns>
    public T Compute(T x) => A * x + B;
    /// <summary>
    /// Computes the x-intercept (zero point) of this linear equation.
    /// </summary>
    /// <returns>The x-coordinate where the line crosses the x-axis.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the line is horizontal (slope is zero).</exception>
    public T ComputeZeroPoint()
    {
        if (A == T.Zero)
            throw new InvalidOperationException(
                "The equation does not have a unique x-intercept (zero point) because the slope is zero.");

        return -B / A;
    }
    /// <summary>
    /// Finds the intersection point between this line and another line.
    /// </summary>
    /// <param name="other">The other linear equation to intersect with.</param>
    /// <returns>The intersection point, or null if the lines are parallel but not coincident.</returns>
    public Point<T>? Intersect(LinearEquation<T> other)
    {
        if (T.Abs(A - other.A) < T.Epsilon)
        {
            return T.Abs(B - other.B) < T.Epsilon ? new Point<T>(T.PositiveInfinity, T.PositiveInfinity)
                : null;
        }

        var x = (other.B - B) / (A - other.A);
        return new Point<T>(x, Compute(x));
    }
    /// <summary>
    /// Creates a linear equation that represents this line mirrored across the x-axis.
    /// </summary>
    /// <returns>A linear equation representing the mirrored line.</returns>
    public LinearEquation<T> MirrorByX()
    {
        return new LinearEquation<T>(-A, -B);
    }

    /// <summary>
    /// Creates a linear equation that represents this line mirrored across the y-axis.
    /// </summary>
    /// <returns>A linear equation representing the mirrored line.</returns>
    public LinearEquation<T> MirrorByY()
    {
        return new LinearEquation<T>(-A, B+B);
    }
    /// <summary>
    /// Creates a linear equation that represents this line translated by the specified vector.
    /// </summary>
    /// <param name="vector">The translation vector.</param>
    /// <returns>A linear equation representing the translated line.</returns>
    public LinearEquation<T> Translate(Drawing.Vector<T> vector)
    {
        return new LinearEquation<T>(A, B + vector.Y - A * vector.X);
    }
    /// <summary>
    /// Creates a linear equation from a point and a direction vector.
    /// </summary>
    /// <param name="point">A point on the line.</param>
    /// <param name="vector">The direction vector of the line.</param>
    /// <returns>A linear equation representing the line.</returns>
    public static LinearEquation<T> From(Point<T> point, Drawing.Vector<T> vector)
    {
        T a = vector.Y / vector.X;
        T b = point.Y - a * point.X;
        return new LinearEquation<T>(a, b);
    }
    /// <summary>
    /// Creates a linear equation that passes through two specified points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>A linear equation representing the line through both points.</returns>
    public static LinearEquation<T> From(Point<T> a, Point<T> b) => From(a.X, a.Y, b.X, b.Y);
    /// <summary>
    /// Creates a linear equation that passes through two points specified by their coordinates.
    /// </summary>
    /// <param name="x1">The x-coordinate of the first point.</param>
    /// <param name="y1">The y-coordinate of the first point.</param>
    /// <param name="x2">The x-coordinate of the second point.</param>
    /// <param name="y2">The y-coordinate of the second point.</param>
    /// <returns>A linear equation representing the line through both points.</returns>
    public static LinearEquation<T> From(T x1, T y1, T x2, T y2)
    {
        T a = (y2 - y1) / (x2 - x1);
        T b = y1 - a * x1;
        return new LinearEquation<T>(a, b);
    }
}