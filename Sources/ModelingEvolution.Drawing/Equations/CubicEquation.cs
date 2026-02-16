using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

/// <summary>
/// Represents a cubic equation of the form ax³ + bx² + cx + d = 0.
/// </summary>
/// <typeparam name="T">The numeric type used for coefficients.</typeparam>
public readonly record struct CubicEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// The coefficients of the cubic equation.
    /// </summary>
    private readonly T a, b, c, d;
    /// <summary>
    /// Initializes a new instance of the CubicEquation struct.
    /// </summary>
    /// <param name="a">The coefficient of the x³ term.</param>
    /// <param name="b">The coefficient of the x² term.</param>
    /// <param name="c">The coefficient of the x term.</param>
    /// <param name="d">The constant term.</param>
    public CubicEquation(T a, T b, T c, T d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    /// <summary>
    /// Returns a string representation of this cubic equation.
    /// </summary>
    /// <returns>A string representation in the format "ax^3 + bx^2 + cx + d".</returns>
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
    /// <summary>
    /// Evaluates the cubic equation at the given x value.
    /// </summary>
    /// <param name="x">The x value to evaluate.</param>
    /// <returns>The result of the cubic equation.</returns>
    private T Evaluate(T x)
    {
        return a * x * x * x + b * x * x + c * x + d;
    }

    /// <summary>
    /// Evaluates the derivative of the cubic equation at the given x value.
    /// </summary>
    /// <param name="x">The x value to evaluate.</param>
    /// <returns>The result of the derivative.</returns>
    private T EvaluateDerivative(T x)
    {
        return t3 * a * x * x + t2 * b * x + c;
    }
    /// <summary>
    /// Calculates all real roots of the cubic equation.
    /// </summary>
    /// <returns>An array containing all real roots of the equation.</returns>
    public ReadOnlyMemory<T> ZeroPoints()
    {
        if (a == T.Zero)
        {
            var quadratic = new QuadraticEquation<T>(b, c, d);
            return quadratic.ZeroPoints();
        }

        var bn = b / a;
        var cn = c / a;
        var dn = d / a;

        var delta0 = bn * bn - t3 * cn;
        var delta1 = t2 * bn * bn * bn - t8 * bn * cn + t27 * dn;

        var discriminant = delta1 * delta1 - t4 * delta0 * delta0 * delta0;

        var b3 = -bn / t3;
        if (delta0 == T.Zero && delta1 == T.Zero)
        {
            var mem = Alloc.Memory<T>(1);
            mem.Span[0] = (T)b3;
            return mem;
        }
        else if (discriminant >= T.Zero)
        {
            T C = T.Cbrt((delta1 + T.Sqrt(discriminant)) / t2);
            var mem = Alloc.Memory<T>(1);
            mem.Span[0] = -(bn + C + delta0 / C) / t3;
            return mem;
        }
        else
        {
            var sqrt = delta1 / (t2 * T.Sqrt(delta0 * delta0 * delta0));
            var phi = T.Acos(sqrt);

            var delta2 = t2 * T.Sqrt(delta0) / t3;
            var x0 = b3 - delta2 * T.Cos(phi / t3);
            var x1 = b3 - delta2 * T.Cos((phi - t2 * T.Pi) / t3);
            var x2 = b3 - delta2 * T.Cos((phi - t4 * T.Pi) / t3);

            var mem = Alloc.Memory<T>(3);
            var span = mem.Span;
            span[0] = (T)x0;
            span[1] = (T)x1;
            span[2] = (T)x2;
            return mem;
        }
    }
    /// <summary>
    /// Finds a root of the cubic equation using Newton's method with the specified initial guess.
    /// </summary>
    /// <param name="initialGuess">The initial guess for the root.</param>
    /// <returns>A root of the cubic equation.</returns>
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