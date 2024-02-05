namespace ModelingEvolution.Drawing.Equations;

public readonly record struct CubicEquationF
{
    private readonly float a, b, c, d;
    public CubicEquationF(float a, float b, float c, float d)
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
    private float Evaluate(float x)
    {
        return a * x * x * x + b * x * x + c * x + d;
    }

    private float EvaluateDerivative(float x)
    {
        return 3 * a * x * x + 2 * b * x + c;
    }
    public float[] ZeroPoints()
    {
        // Handle the special case where the equation is not actually cubic
        if (a == 0)
        {
            // Handle quadratic case
            var quadratic = new QuadraticEquationF(b, c, d);
            return quadratic.ZeroPoints();
        }

        // Normalize coefficients
        double bn = (double)b / a;
        double cn = (double)c / a;
        double dn = (double)d / a;

        // Calculate discriminant
        double delta0 = bn * bn - 3 * cn;
        double delta1 = 2 * bn * bn * bn - 9 * bn * cn + 27 * dn;

        double discriminant = delta1 * delta1 - 4 * delta0 * delta0 * delta0;

        // Calculate roots


        var b3 = -bn / 3f;
        if (delta0 == 0 && delta1 == 0)
        {
            // All roots are real and equal
            return new float[] { (float)b3 };
        }
        else if (discriminant >= 0)
        {
            double C = Math.Cbrt((delta1 + Math.Sqrt(discriminant)) / 2f);
            return new float[] { (float)(-1f / 3f * bn + (C + delta0 / C) )};
        }
        else
        {
            var sqrt = delta1 / (2 * Math.Sqrt(delta0 * delta0 * delta0));
            double phi = Math.Acos(sqrt);
            

            var delta2 = 2 * Math.Sqrt(delta0);
            double x0 = b3 + delta2 * Math.Cos(phi / 3);
            double x1 = b3 + delta2 * Math.Cos((phi + 2f * Math.PI * 1) / 3f);
            double x2 = b3 + delta2 * Math.Cos((phi + 2f * Math.PI * 2) / 3f);

            return new[] { (float)x0, (float)x1, (float)x2 };
        }


    }
    public float FindRoot(float initialGuess)
    {
        float x = initialGuess;
        float tolerance = 1e-5f; // Tolerance for convergence
        float maxIterations = 100; // Maximum number of iterations
        float h = Evaluate(x) / EvaluateDerivative(x);

        for (int i = 0; i < maxIterations && Math.Abs(h) >= tolerance; i++)
        {
            h = Evaluate(x) / EvaluateDerivative(x);

            // x(i+1) = x(i) - f(x) / f'(x)
            x = x - h;
        }

        return x;
    }
}
public readonly record struct QuadraticEquationF
{
    public float A { get;  }
    public float B { get;  }
    public float C { get;  }

    public QuadraticEquationF(float a, float b, float c)
    {
        A = a;
        B = b;
        C = c;
    }

    public override string ToString()
    {
        return $"y = {A}x^2 + {B}x + {C}";
    }

    public float Discriminant => B * B - 4 * A * C;
    public float[] ZeroPoints()
    {
        if (A == 0.0f) 
            return new []{new LinearEquationF(B, C).ComputeZeroPoint()};

        float discriminant = Discriminant;
        switch (discriminant)
        {
            // No real roots
            case < 0:
                return Array.Empty<float>();
            case 0:
                // One real root
                return new[] { -B / (2 * A) };
            default:
            {
                // Two real roots
                float root1 = (-B + MathF.Sqrt(discriminant)) / (2 * A);
                float root2 = (-B - MathF.Sqrt(discriminant)) / (2 * A);
                return new[] { root1, root2 };
            }
        }
    }

    public PointF? ComputeExtremum()
    {
        if (A == 0) return null;
        var x = Derivative().ComputeZeroPoint();
        return new PointF(x, Compute(x));
    }
    public LinearEquationF Derivative()
    {
        return new LinearEquationF(2 * A, B);
    }

    public float Compute(float x)
    {
        return A * x * x + B * x + C;
    }
    public PointF[] Intersect(LinearEquationF linearEq)
    {
        float a = A;
        float b = B - linearEq.A; // (b - m)
        float c = C - linearEq.B; // (c - n)

        // Calculate the discriminant
        float discriminant = b * b - 4 * a * c;

        switch (discriminant)
        {
            // Solve the quadratic equation
            case < 0:
                // No real roots
                return Array.Empty<PointF>();
            case 0:
                // One real root
                var x = -b / (2 * a);
                return new[] { new PointF(x, Compute(x)) };
            default:
            {
                // Two real roots
                float root1 = (-b + MathF.Sqrt(discriminant)) / (2 * a);
                float root2 = (-b - MathF.Sqrt(discriminant)) / (2 * a);
                return new[] { new PointF(root1, Compute(root1)), new PointF(root2, Compute(root2)) };
            }
        }
    }

}