using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides span-level densification methods for point sequences.
/// Points are inserted along edges so that consecutive points are at most 1 unit apart.
/// </summary>
public static class Densification
{
    /// <summary>
    /// Densifies a sequence of points by inserting intermediate points along each edge
    /// so that consecutive points are at most 1 unit apart.
    /// </summary>
    public static ReadOnlyMemory<Point<T>> Densify<T>(ReadOnlySpan<Point<T>> points)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
                  IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
        => Densify(points, T.One);

    /// <summary>
    /// Densifies a sequence of points by inserting intermediate points along each edge
    /// so that consecutive points are at most <paramref name="unit"/> apart.
    /// </summary>
    public static ReadOnlyMemory<Point<T>> Densify<T>(ReadOnlySpan<Point<T>> points, T unit)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
                  IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var u = unit;
        int n = points.Length;
        if (n < 2)
        {
            if (n == 0) return ReadOnlyMemory<Point<T>>.Empty;
            var single = Alloc.Memory<Point<T>>(1);
            single.Span[0] = points[0];
            return single;
        }

        // First pass: count total points
        int total = 0;
        for (int i = 0; i < n - 1; i++)
        {
            var len = points[i].DistanceTo(points[i + 1]);
            total += int.Max(1, int.CreateChecked(T.Ceiling(len / u)));
        }
        total++; // last point

        var mem = Alloc.Memory<Point<T>>(total);
        var dst = mem.Span;
        int idx = 0;
        for (int i = 0; i < n - 1; i++)
        {
            var a = points[i];
            var b = points[i + 1];
            var len = a.DistanceTo(b);
            int steps = int.Max(1, int.CreateChecked(T.Ceiling(len / u)));
            for (int s = 0; s < steps; s++)
            {
                var t = T.CreateChecked(s) / T.CreateChecked(steps);
                dst[idx++] = new Point<T>(
                    a.X + (b.X - a.X) * t,
                    a.Y + (b.Y - a.Y) * t);
            }
        }
        dst[idx] = points[n - 1]; // emit last point

        return mem;
    }
}
