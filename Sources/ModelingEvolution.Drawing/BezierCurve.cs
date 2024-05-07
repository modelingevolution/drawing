using System.Collections;
using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;


public readonly record struct BezierCurve<T> : IEnumerable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    public Point<T> Start { get; }
    public Point<T> C0 { get; }
    public Point<T> C1 { get; }
    public Point<T> End { get; }

    public BezierCurve(Point<T> start, Point<T> c0, Point<T> c1, Point<T> end)
    {
        Start = start;
        C0 = c0;
        C1 = c1;
        End = end;
    }

    
    public static IEnumerable<BezierCurve<T>> Create(params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>>)points, T.One / (T.One + T.One));
    }
    public static IEnumerable<BezierCurve<T>> Create(T coef, params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>> )points, coef);
    }
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
    public BezierCurve(params Point<T>[] points) : this(points[0], points[1], points[2], points[3]) { }
    public BezierCurve<T> TransformBy(Matrix<T> m)
    {
        return new BezierCurve<T>(m.Transform(Start), m.Transform(C0), m.Transform(C1), m.Transform(End));
    }
    public static BezierCurve<T> operator +(BezierCurve<T> c, Vector<T> v)
    {
        return new BezierCurve<T>(c.Start + v, c.C0 + v, c.C1 + v, c.End + v);
    }
    private static readonly T tree = T.CreateTruncating(3);
    private static readonly T six = T.CreateTruncating(6);
    private static readonly T two = T.CreateTruncating(2);
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