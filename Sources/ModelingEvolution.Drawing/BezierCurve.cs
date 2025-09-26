using System.Collections;
using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a cubic Bezier curve defined by four control points.
/// </summary>
/// <typeparam name="T">The numeric type for the coordinates.</typeparam>
public readonly record struct BezierCurve<T> : IEnumerable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the starting point of the Bezier curve.
    /// </summary>
    public Point<T> Start { get; }
    
    /// <summary>
    /// Gets the first control point of the Bezier curve.
    /// </summary>
    public Point<T> C0 { get; }
    
    /// <summary>
    /// Gets the second control point of the Bezier curve.
    /// </summary>
    public Point<T> C1 { get; }
    
    /// <summary>
    /// Gets the ending point of the Bezier curve.
    /// </summary>
    public Point<T> End { get; }

    /// <summary>
    /// Initializes a new instance of the BezierCurve struct.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="c0">The first control point.</param>
    /// <param name="c1">The second control point.</param>
    /// <param name="end">The ending point.</param>
    public BezierCurve(Point<T> start, Point<T> c0, Point<T> c1, Point<T> end)
    {
        Start = start;
        C0 = c0;
        C1 = c1;
        End = end;
    }

    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points.
    /// </summary>
    /// <param name="points">The points to create curves through.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>>)points, T.One / (T.One + T.One));
    }
    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points with a specified coefficient.
    /// </summary>
    /// <param name="coef">The coefficient controlling the curve's smoothness.</param>
    /// <param name="points">The points to create curves through.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(T coef, params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>> )points, coef);
    }
    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points with a specified coefficient.
    /// </summary>
    /// <param name="points">The points to create curves through.</param>
    /// <param name="coef">The coefficient controlling the curve's smoothness.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(IReadOnlyList<Point<T>> points, T coef)
    {
        if (points.Count >= 2)
        {
            var p0 = points[0];
            var p1 = points[1];
            if (points.Count >= 3)
            {
                var p2 = points[2];
                var d = p1 - p0;
                yield return new BezierCurve<T>(p0, 
                    d * coef + p0, 
                    p1 - coef*((p2 - p0).Normalize()) * d.Length, 
                    p1);
            }
            else
            {
                var d = p1 - p0;
                yield return new BezierCurve<T>(p0, p0 + d * coef, p1 - d * coef, p1);
            }

            for (int i = 0; i < points.Count-3; i++)
            {
                p0 = points[i];
                p1 = points[i + 1];
                var p2 = points[i + 2];
                var p3 = points[i + 3];

                var c0 = (p2 - p0).Normalize();
                var c1 = (p3 - p1).Normalize();
                var d = (p2 - p1).Length;
                yield return new BezierCurve<T>(p1, 
                    p1 + c0 * coef * d,
                    p2 - c1 * coef * d, 
                    p2);
            }

            if (points.Count >= 3)
            {
                var p3 = points[^1];
                var p2 = points[^2];
                p1 = points[^3];
                var d = p3 - p2;
                var c0 = (p3 - p1).Normalize();
                yield return new BezierCurve<T>(p2, p2 + c0 * coef * d.Length, p3 - d * coef, p3);
            }
        }
        else yield break;
    }
    /// <summary>
    /// Initializes a new instance of the BezierCurve struct from an array of points.
    /// </summary>
    /// <param name="points">Array containing exactly 4 points: start, control1, control2, end.</param>
    public BezierCurve(params Point<T>[] points) : this(points[0], points[1], points[2], points[3]) { }
    /// <summary>
    /// Transforms the Bezier curve by the specified matrix.
    /// </summary>
    /// <param name="m">The transformation matrix.</param>
    /// <returns>A new transformed Bezier curve.</returns>
    public BezierCurve<T> TransformBy(Matrix<T> m)
    {
        return new BezierCurve<T>(m.Transform(Start), m.Transform(C0), m.Transform(C1), m.Transform(End));
    }
    /// <summary>
    /// Translates a Bezier curve by a vector.
    /// </summary>
    /// <param name="c">The Bezier curve.</param>
    /// <param name="v">The translation vector.</param>
    /// <returns>A new translated Bezier curve.</returns>
    public static BezierCurve<T> operator +(BezierCurve<T> c, Vector<T> v)
    {
        return new BezierCurve<T>(c.Start + v, c.C0 + v, c.C1 + v, c.End + v);
    }
    private static readonly T tree = T.CreateTruncating(3);
    private static readonly T six = T.CreateTruncating(6);
    private static readonly T two = T.CreateTruncating(2);
    /// <summary>
    /// Calculates the extremum points (local minima and maxima) of the Bezier curve.
    /// </summary>
    /// <returns>An array of extremum points.</returns>
    public Point<T>[] CalculateExtremumPoints()
    {
        var D0 = tree * (C0 - Start);
        var D1 = tree * (C1 - C0);
        var D2 = tree * (End - C1);

        // Coefficients for the quadratic equation
        var a = D2 - two * D1 + D0;
        var b = two * (D1 - D0);
        var c = D0;

        var tValuesX = new QuadraticEquation<T>(a.X, b.X, c.X).ZeroPoints();
        var tValuesY = new QuadraticEquation<T>(a.Y, b.Y, c.Y).ZeroPoints();

        var tmp = this;
        return tValuesX
            .Union(tValuesY)
            .Where(x => x >= T.Zero & x <= T.One)
            .Distinct()
            .Select(x => tmp.Evaluate(x))
            .ToArray();

    }

    private readonly static T half = T.CreateTruncating(0.5);
    /// <summary>
    /// Calculates the intersection point of the Bezier curve with a linear equation.
    /// </summary>
    /// <param name="f">The linear equation to intersect with.</param>
    /// <returns>The intersection point.</returns>
    public Point<T> Intersection(LinearEquation<T> f)
    {
        var a = Start.X * f.A - Start.Y - tree * C0.X * f.A + tree * C0.Y + tree * C1.X * f.A - tree * C1.Y - End.X * f.A + End.Y;
        var b = -tree * Start.X * f.A + tree * Start.Y + six * C0.X * f.A - six * C0.Y - tree * C1.X * f.A + tree * C1.Y;
        var c = tree * Start.X * f.A - tree * Start.Y - tree * C0.X * f.A + tree * C0.Y;
        var d = -Start.X * f.A + Start.Y - f.B;
        CubicEquation<T> ex = new CubicEquation<T>(a, b, c, d);
        var zer = ex.FindRoot(half);

        return Evaluate(zer);
    }

    /// <summary>
    /// Evaluates the Bezier curve at the specified parameter value.
    /// </summary>
    /// <param name="t">The parameter value (must be between 0 and 1).</param>
    /// <returns>The point on the curve at parameter t.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when t is not between 0 and 1.</exception>
    public Point<T> Evaluate(T t)
    {
        if (t < T.Zero || t > T.One)
        {
            throw new ArgumentOutOfRangeException(nameof(t), "Parameter t should be between 0 and 1.");
        }

        // Cubic Bézier formula
        T oneMinusT = T.One - t;
        T oneMinusTSquare = oneMinusT * oneMinusT;
        T oneMinusTCube = oneMinusTSquare * oneMinusT;
        T tSquare = t * t;
        T tCube = tSquare * t;

        T x = oneMinusTCube * Start.X +
              tree * oneMinusTSquare * t * C0.X +
              tree * oneMinusT * tSquare * C1.X +
              tCube * End.X;

        T y = oneMinusTCube * Start.Y +
              tree * oneMinusTSquare * t * C0.Y +
              tree * oneMinusT * tSquare * C1.Y +
              tCube * End.Y;

        return new Point<T>(x, y);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the control points of the curve.
    /// </summary>
    /// <returns>An enumerator for the control points.</returns>
    public IEnumerator<Point<T>> GetEnumerator()
    {
        yield return Start;
        yield return C0;
        yield return C1;
        yield return End;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}