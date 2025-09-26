using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;
using PointF = Point<float>;

/// <summary>
/// Provides extension methods for working with radial curves and polar point collections.
/// </summary>
public static class RadialCurveF
{
    /// <summary>
    /// Finds the intersection point of a radial curve with a specified angle.
    /// </summary>
    /// <param name="points">The collection of polar points defining the radial curve.</param>
    /// <param name="angle">The angle in radians to intersect with.</param>
    /// <returns>The intersection point, or null if no intersection is found.</returns>
    public static PointF? IntersectWith(this IEnumerable<PolarPoint<float>> points, Radian<double> angle)
    {
        var f = angle.Truncate<float>();
        return points.IntersectWith(f);
    }
    /// <summary>
    /// Finds the intersection point of a radial curve with a specified angle.
    /// </summary>
    /// <typeparam name="T">The numeric type for coordinates.</typeparam>
    /// <param name="points">The collection of polar points defining the radial curve.</param>
    /// <param name="angle">The angle in radians to intersect with.</param>
    /// <returns>The intersection point, or null if no intersection is found.</returns>
    public static Point<T>? IntersectWith<T>(this IEnumerable<PolarPoint<T>> points, Radian<T> angle)
        where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var iterator = points.GetEnumerator();

        if (!iterator.MoveNext())
            return null;
        if (iterator.Current.Angle == angle)
            return iterator.Current;

        var prv = iterator.Current;
        while(iterator.MoveNext())
        {
            var c = iterator.Current;

            if(c.Angle == angle) return c;
            if (prv.Angle < angle && c.Angle > angle) 
                return Approximate(angle, prv, c);
            prv = c;
        }

        return null;
    }

    /// <summary>
    /// Approximates the intersection point between two polar points at a specified angle using linear interpolation.
    /// </summary>
    /// <typeparam name="T">The numeric type for coordinates.</typeparam>
    /// <param name="angle">The angle to intersect at.</param>
    /// <param name="a">The first polar point.</param>
    /// <param name="b">The second polar point.</param>
    /// <returns>The approximated intersection point, or null if no intersection exists.</returns>
    private static Point<T>? Approximate<T>(Radian<T> angle, PolarPoint<T> a, PolarPoint<T> b) 
        where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        LinearEquation<T> e1 = LinearEquation<T>.From(angle);
        
        Point<T> aXY = a;
        Point<T> bXY = b;
        LinearEquation<T> e2 = LinearEquation<T>.From(aXY, bXY);
        return e1.Intersect(e2);
    }
    
}