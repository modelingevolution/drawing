using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct QuadraticEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    public T A { get;  }
    public T B { get;  }
    public T C { get;  }

    public QuadraticEquation(T a, T b, T c)
    {
        A = a;
        B = b;
        C = c;
    }

    public override string ToString()
    {
        return $"y = {A}x^2 + {B}x + {C}";
    }

    public T Discriminant => B * B - T.CreateTruncating(4) * A * C;
    
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

    public Point<T>? ComputeExtremum()
    {
        if (A == T.Zero) return null;
        var x = Derivative().ComputeZeroPoint();
        return new Point<T>(x, Compute(x));
    }
    public LinearEquation<T> Derivative()
    {
        return new LinearEquation<T>(A+A, B);
    }

    public T Compute(T x)
    {
        return A * x * x + B * x + C;
    }
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