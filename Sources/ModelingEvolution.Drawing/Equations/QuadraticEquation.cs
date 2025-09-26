using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

/// <summary>
/// Represents a quadratic equation of the form y = Ax² + Bx + C.
/// </summary>
/// <typeparam name="T">The numeric type used for coefficients.</typeparam>
public readonly record struct QuadraticEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the coefficient of the x² term.
    /// </summary>
    public T A { get;  }
    /// <summary>
    /// Gets the coefficient of the x term.
    /// </summary>
    public T B { get;  }
    /// <summary>
    /// Gets the constant term.
    /// </summary>
    public T C { get;  }

    /// <summary>
    /// Initializes a new instance of the QuadraticEquation struct.
    /// </summary>
    /// <param name="a">The coefficient of the x² term.</param>
    /// <param name="b">The coefficient of the x term.</param>
    /// <param name="c">The constant term.</param>
    public QuadraticEquation(T a, T b, T c)
    {
        A = a;
        B = b;
        C = c;
    }

    /// <summary>
    /// Returns a string representation of this quadratic equation.
    /// </summary>
    /// <returns>A string representation in the format "y = Ax^2 + Bx + C".</returns>
    public override string ToString()
    {
        return $"y = {A}x^2 + {B}x + {C}";
    }

    /// <summary>
    /// Gets the discriminant of the quadratic equation (B² - 4AC).
    /// </summary>
    public T Discriminant => B * B - T.CreateTruncating(4) * A * C;
    
    /// <summary>
    /// Calculates the x-intercepts (zeros) of this quadratic equation.
    /// </summary>
    /// <returns>An array of x-values where the equation equals zero.</returns>
    public T[] ZeroPoints()
    {
        if (A == T.Zero) 
            return new []{new LinearEquation<T>(B, C).ComputeZeroPoint()};

        T discriminant = Discriminant;
        switch (discriminant)
        {
            // No real roots
            case < 0:
                return Array.Empty<T>();
            case 0:
                // One real root
                return new[] { -B / (A+A) };
            default:
            {
                // Two real roots
                T root1 = (-B + T.Sqrt(discriminant)) / (A+A);
                T root2 = (-B - T.Sqrt(discriminant)) / (A+A);
                return new[] { root1, root2 };
            }
        }
    }

    /// <summary>
    /// Computes the extremum (vertex) point of the quadratic function.
    /// </summary>
    /// <returns>The vertex point, or null if this is not a quadratic equation (A = 0).</returns>
    public Point<T>? ComputeExtremum()
    {
        if (A == T.Zero) return null;
        var x = Derivative().ComputeZeroPoint();
        return new Point<T>(x, Compute(x));
    }
    /// <summary>
    /// Computes the derivative of this quadratic equation as a linear equation.
    /// </summary>
    /// <returns>The derivative as a linear equation (2Ax + B).</returns>
    public LinearEquation<T> Derivative()
    {
        return new LinearEquation<T>(A+A, B);
    }

    /// <summary>
    /// Computes the y-value for the given x-value using this quadratic equation.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <returns>The corresponding y-coordinate.</returns>
    public T Compute(T x)
    {
        return A * x * x + B * x + C;
    }
    /// <summary>
    /// Finds the intersection points between this quadratic equation and a linear equation.
    /// </summary>
    /// <param name="linearEq">The linear equation to intersect with.</param>
    /// <returns>An array of intersection points (0, 1, or 2 points).</returns>
    public Point<T>[] Intersect(LinearEquation<T> linearEq)
    {
        T a = A;
        T b = B - linearEq.A; // (b - m)
        T c = C - linearEq.B; // (c - n)

        // Calculate the discriminant
        T discriminant = b * b - T.CreateTruncating(4) * a * c;

        switch (discriminant)
        {
            // Solve the quadratic equation
            case < 0:
                // No real roots
                return Array.Empty<Point<T>>();
            case 0:
                // One real root
                var x = -b / (a+a);
                return new[] { new Point<T>(x, Compute(x)) };
            default:
            {
                // Two real roots
                T root1 = (-b + T.Sqrt(discriminant)) / (a+a);
                T root2 = (-b - T.Sqrt(discriminant)) / (a+a);
                return new[] { new Point<T>(root1, Compute(root1)), new Point<T>(root2, Compute(root2)) };
            }
        }
    }

}