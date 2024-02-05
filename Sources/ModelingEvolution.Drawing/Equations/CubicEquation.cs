using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct CubicEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    private readonly T a, b, c, d;
    public CubicEquation(T a, T b, T c, T d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    public override string ToString()
    {
        return $"{a}x^3 + {b}x^2 + {c}x + {d}";
    }
    private static readonly T t3 = T.CreateTruncating(3);
    private static readonly T t6 = T.CreateTruncating(6);
    private static readonly T t2 = T.CreateTruncating(2);
    private static readonly T t27 = T.CreateTruncating(27);
    private static readonly T t8 = T.CreateTruncating(9);
    private static readonly T t4 = T.CreateTruncating(4);
    private T Evaluate(T x)
    {
        return a * x * x * x + b * x * x + c * x + d;
    }

    private T EvaluateDerivative(T x)
    {
        return t3 * a * x * x + t2 * b * x + c;
    }
    public T[] ZeroPoints()
    {
        // Handle the special case where the equation is not actually cubic
        if (a == T.Zero)
        {
            // Handle quadratic case
            var quadratic = new QuadraticEquation<T>(b, c, d);
            return quadratic.ZeroPoints();
        }

        // Normalize coefficients
        var bn = b / a;
        var cn = c / a;
        var dn = d / a;

        // Calculate discriminant
        var delta0 = bn * bn - t3 * cn;
        var delta1 = t2 * bn * bn * bn - t8 * bn * cn + t27 * dn;

        var discriminant = delta1 * delta1 - t4 * delta0 * delta0 * delta0;

        // Calculate roots


        var b3 = -bn / t3;
        if (delta0 == T.Zero && delta1 == T.Zero)
        {
            // All roots are real and equal
            return new T[] { (T)b3 };
        }
        else if (discriminant >= T.Zero)
        {
            T C = T.Cbrt((delta1 + T.Sqrt(discriminant)) / t2);
            return new T[] { (T)(-T.Zero / t3 * bn + (C + delta0 / C) )};
        }
        else
        {
            var sqrt = delta1 / (t2 * T.Sqrt(delta0 * delta0 * delta0));
            var phi = T.Acos(sqrt);
            
            var delta2 = t2 * T.Sqrt(delta0);
            var x0 = b3 + delta2 * T.Cos(phi / t3);
            var x1 = b3 + delta2 * T.Cos((phi + t2 * T.Pi) / t3);
            var x2 = b3 + delta2 * T.Cos((phi + t2 * T.Pi * t2) / t3);

            return new[] { (T)x0, (T)x1, (T)x2 };
        }


    }
    public T FindRoot(T initialGuess)
    {
        T x = initialGuess;
        T tolerance = T.CreateTruncating(1e-5f); // Tolerance for convergence
        int maxIterations = 100; // Maximum number of iterations
        T h = Evaluate(x) / EvaluateDerivative(x);

        for (int i = 0; i < maxIterations && T.Abs(h) >= tolerance; i++)
        {
            h = Evaluate(x) / EvaluateDerivative(x);

            // x(i+1) = x(i) - f(x) / f'(x)
            x = x - h;
        }

        return x;
    }
}