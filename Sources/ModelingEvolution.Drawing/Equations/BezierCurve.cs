using System.Collections;
using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct BezierCurve<T> : IEnumerable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    public Point<T> Start { get;  }
    public Point<T> C0 { get;  }
    public Point<T> C1 { get;  }
    public Point<T> End { get;  }

    public BezierCurve(Point<T> start, Point<T> c0, Point<T> c1, Point<T> end)
    {
        Start = start;
        C0 = c0;
        C1 = c1;
        End = end;
    }
    
    public BezierCurve(params Point<T>[] points) : this(points[0], points[1], points[2], points[3]) { }
    public BezierCurve<T> TransformBy(Matrix<T> m)
    {
        return new BezierCurve<T>(m.Transform(Start), m.Transform(C0), m.Transform(C1), m.Transform(End));
    }
    public static BezierCurve<T> operator+(BezierCurve<T> c, Drawing.Vector<T> v)
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