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
    /// Splits the curve at parameter t into two curves using De Casteljau's algorithm.
    /// </summary>
    /// <param name="t">The parameter value at which to split (0 to 1).</param>
    /// <returns>A tuple of (Left, Right) curves.</returns>
    public (BezierCurve<T> Left, BezierCurve<T> Right) Split(T t)
    {
        var q0 = LerpPoint(Start, C0, t);
        var q1 = LerpPoint(C0, C1, t);
        var q2 = LerpPoint(C1, End, t);
        var r0 = LerpPoint(q0, q1, t);
        var r1 = LerpPoint(q1, q2, t);
        var s = LerpPoint(r0, r1, t);
        return (new BezierCurve<T>(Start, q0, r0, s), new BezierCurve<T>(s, r1, q2, End));
    }

    /// <summary>
    /// Returns the sub-curve between parameters t0 and t1.
    /// </summary>
    /// <param name="t0">The start parameter (0 to 1).</param>
    /// <param name="t1">The end parameter (t0 to 1).</param>
    /// <returns>The sub-curve between t0 and t1.</returns>
    public BezierCurve<T> SubCurve(T t0, T t1)
    {
        if (t0 == T.Zero && t1 == T.One) return this;
        if (t0 == T.Zero) return Split(t1).Left;
        var (_, right) = Split(t0);
        if (t1 == T.One) return right;
        var remapped = (t1 - t0) / (T.One - t0);
        return right.Split(remapped).Left;
    }

    /// <summary>
    /// Finds all parameter values in (0,1) where this curve crosses the edges of a rectangle.
    /// </summary>
    internal T[] FindEdgeCrossings(Rectangle<T> rect)
    {
        var eps = T.CreateTruncating(1e-9);
        var results = new List<T>();

        // Left edge: x = rect.X
        AddAxisCrossings(Start.X, C0.X, C1.X, End.X, rect.X,
            Start.Y, C0.Y, C1.Y, End.Y, rect.Y, rect.Bottom, results, eps);
        // Right edge: x = rect.Right
        AddAxisCrossings(Start.X, C0.X, C1.X, End.X, rect.Right,
            Start.Y, C0.Y, C1.Y, End.Y, rect.Y, rect.Bottom, results, eps);
        // Top edge: y = rect.Y
        AddAxisCrossings(Start.Y, C0.Y, C1.Y, End.Y, rect.Y,
            Start.X, C0.X, C1.X, End.X, rect.X, rect.Right, results, eps);
        // Bottom edge: y = rect.Bottom
        AddAxisCrossings(Start.Y, C0.Y, C1.Y, End.Y, rect.Bottom,
            Start.X, C0.X, C1.X, End.X, rect.X, rect.Right, results, eps);

        results.Sort();
        // Deduplicate within epsilon
        for (int i = results.Count - 1; i > 0; i--)
            if (results[i] - results[i - 1] < eps)
                results.RemoveAt(i);

        return results.ToArray();
    }

    /// <summary>
    /// Finds t-values where the Bezier polynomial in one axis equals a constant,
    /// and the other axis is within the edge bounds.
    /// </summary>
    private static void AddAxisCrossings(
        T p0, T p1, T p2, T p3, T k,
        T q0, T q1, T q2, T q3, T lo, T hi,
        List<T> results, T eps)
    {
        // Cubic coefficients: B(t) = at³ + bt² + ct + d, solve B(t) = k
        var a = -p0 + tree * p1 - tree * p2 + p3;
        var b = tree * p0 - six * p1 + tree * p2;
        var c = -tree * p0 + tree * p1;
        var d = p0 - k;

        // If all coefficients are ~zero, the component is constant → no crossings
        if (T.Abs(a) < eps && T.Abs(b) < eps && T.Abs(c) < eps)
            return;

        T[] roots;
        try { roots = new CubicEquation<T>(a, b, c, d).ZeroPoints(); }
        catch { return; }

        foreach (var t in roots)
        {
            if (t > -eps && t < T.One + eps)
            {
                var clamped = T.Max(T.Zero, T.Min(T.One, t));
                // Check the other axis is within edge bounds
                var other = EvalComponent(q0, q1, q2, q3, clamped);
                if (other >= lo - eps && other <= hi + eps)
                    results.Add(clamped);
            }
        }
    }

    /// <summary>
    /// Evaluates one component of a cubic Bezier at parameter t.
    /// </summary>
    private static T EvalComponent(T p0, T p1, T p2, T p3, T t)
    {
        T u = T.One - t;
        return u * u * u * p0 + tree * u * u * t * p1 + tree * u * t * t * p2 + t * t * t * p3;
    }

    private static Point<T> LerpPoint(Point<T> a, Point<T> b, T t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

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