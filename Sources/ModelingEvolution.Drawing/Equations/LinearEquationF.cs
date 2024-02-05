using System.Drawing;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct LinearEquationF
{
    public float A { get; }
    public float B { get;  }
    public LinearEquationF(float a, float b)
    {
        A = a;
        B = b;
    }

    
    public float Compute(float x) => A * x + B;
    public float ComputeZeroPoint()
    {
        if (A == 0)
            throw new InvalidOperationException(
                "The equation does not have a unique x-intercept (zero point) because the slope is zero.");

        return -B / A;
    }
    public PointF Intersect(LinearEquationF other)
    {
        if (MathF.Abs(A - other.A) < float.Epsilon)
        {
            return MathF.Abs(B - other.B) < float.Epsilon ? new PointF(float.PositiveInfinity, float.PositiveInfinity)
                : new PointF(float.NaN, float.NaN);
        }

        var x = (other.B - B) / (A - other.A);
        return new PointF(x, Compute(x));
    }
    public LinearEquationF MirrorByX()
    {
        return new LinearEquationF(-A, -B);
    }

    public LinearEquationF MirrorByY()
    {
        return new LinearEquationF(-A, 2 * B);
    }
    public LinearEquationF Translate(VectorF vector)
    {
        return new LinearEquationF(A, B + vector.Y - A * vector.X);
    }
    public static LinearEquationF From(Point point, VectorF vector)
    {
        float a = vector.Y / vector.X;
        float b = point.Y - a * point.X;
        return new LinearEquationF(a, b);
    }
    public static LinearEquationF From(PointF a, PointF b) => From(a.X, a.Y, b.X, b.Y);
    public static LinearEquationF From(float x1, float y1, float x2, float y2)
    {
        float a = (y2 - y1) / (x2 - x1);
        float b = y1 - a * x1;
        return new LinearEquationF(a, b);
    }
}