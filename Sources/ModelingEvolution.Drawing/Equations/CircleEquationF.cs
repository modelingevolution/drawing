namespace ModelingEvolution.Drawing.Equations;

public readonly record struct CircleEquationF
{
    public PointF Center { get;  }
    public float Radius { get; }

    public CircleEquationF(PointF center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public PointF[] Intersect(LinearEquationF line)
    {
        // Transform the line to the circle's local coordinate system
        var transformedB = line.B - Center.Y + line.A * Center.X;

        // Quadratic coefficients
        var a = 1 + MathF.Pow(line.A, 2);
        var b = 2 * (line.A * transformedB);
        var c = MathF.Pow(transformedB, 2) - MathF.Pow(Radius, 2);

        // Calculate the discriminant
        var discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No real intersection
            return Array.Empty<PointF>();
        }

        // Calculate x coordinates of intersection points
        var x1 = (-b + MathF.Sqrt(discriminant)) / (2 * a);
        var x2 = (-b - MathF.Sqrt(discriminant)) / (2 * a);

        // Calculate y coordinates of intersection points
        var y1 = line.A * x1 + line.B;
        var y2 = line.A * x2 + line.B;

        return discriminant > 0 ? new[] { new PointF(x1 + Center.X, y1), new PointF(x2 + Center.X, y2) } : new[] { new PointF(x1 + Center.X, y1) };
    }
}