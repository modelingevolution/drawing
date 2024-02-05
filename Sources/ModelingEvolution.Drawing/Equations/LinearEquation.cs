using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct LinearEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    public T A { get; }
    public T B { get;  }
    public LinearEquation(T a, T b)
    {
        A = a;
        B = b;
    }
    public static LinearEquation<T> From(Radian<T> angle)
    {
        return From(angle, T.Zero);
    }
    public static LinearEquation<T> From(Radian<T> angle, T b)
    {
        return new LinearEquation<T>(T.Tan((T)angle), b);
    }

    public T Compute(T x) => A * x + B;
    public T ComputeZeroPoint()
    {
        if (A == T.Zero)
            throw new InvalidOperationException(
                "The equation does not have a unique x-intercept (zero point) because the slope is zero.");

        return -B / A;
    }
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
    public LinearEquation<T> MirrorByX()
    {
        return new LinearEquation<T>(-A, -B);
    }

    public LinearEquation<T> MirrorByY()
    {
        return new LinearEquation<T>(-A, B+B);
    }
    public LinearEquation<T> Translate(Drawing.Vector<T> vector)
    {
        return new LinearEquation<T>(A, B + vector.Y - A * vector.X);
    }
    public static LinearEquation<T> From(Point<T> point, Drawing.Vector<T> vector)
    {
        T a = vector.Y / vector.X;
        T b = point.Y - a * point.X;
        return new LinearEquation<T>(a, b);
    }
    public static LinearEquation<T> From(Point<T> a, Point<T> b) => From(a.X, a.Y, b.X, b.Y);
    public static LinearEquation<T> From(T x1, T y1, T x2, T y2)
    {
        T a = (y2 - y1) / (x2 - x1);
        T b = y1 - a * x1;
        return new LinearEquation<T>(a, b);
    }
}