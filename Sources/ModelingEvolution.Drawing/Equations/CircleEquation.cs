﻿using System.Numerics;

namespace ModelingEvolution.Drawing.Equations;

public readonly record struct CircleEquation<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    public Point<T> Center { get;  }
    public T Radius { get; }

    public CircleEquation(Point<T> center, T radius)
    {
        Center = center;
        Radius = radius;
    }

    private static readonly T t2 = T.CreateTruncating(2);
    private static readonly T t4 = T.CreateTruncating(4);
    
    public Point<T>[] Intersect(LinearEquation<T> line)
    {
        // Transform the line to the circle's local coordinate system
        var transformedB = line.B - Center.Y + line.A * Center.X;

        // Quadratic coefficients
        var a = T.One + T.Pow(line.A, t2);
        var b = t2 * (line.A * transformedB);
        var c = T.Pow(transformedB, t2) - T.Pow(Radius, t2);

        // Calculate the discriminant
        var discriminant = b * b - t4 * a * c;

        if (discriminant < T.Zero)
        {
            // No real intersection
            return Array.Empty<Point<T>>();
        }

        // Calculate x coordinates of intersection points
        var x1 = (-b + T.Sqrt(discriminant)) / (t2 * a);
        var x2 = (-b - T.Sqrt(discriminant)) / (t2 * a);

        // Calculate y coordinates of intersection points
        var y1 = line.A * x1 + line.B;
        var y2 = line.A * x2 + line.B;

        return discriminant > T.Zero ? new[] { new Point<T>(x1 + Center.X, y1), new Point<T>(x2 + Center.X, y2) } : new[] { new Point<T>(x1 + Center.X, y1) };
    }
}