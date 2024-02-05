using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;
using PointF = Point<float>;

public static class RadialCurveF
{
    public static PointF? IntersectWith(this IEnumerable<PolarPoint<float>> points, Radian<double> angle)
    {
        var f = angle.Truncate<float>();
        return points.IntersectWith(f);
    }
    public static Point<T>? IntersectWith<T>(this IEnumerable<PolarPoint<T>> points, Radian<T> angle)
        where T : IFloatingPointIeee754<T>
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

    private static Point<T>? Approximate<T>(Radian<T> angle, PolarPoint<T> a, PolarPoint<T> b) 
        where T : IFloatingPointIeee754<T>
    {
        LinearEquation<T> e1 = LinearEquation<T>.From(angle);
        
        Point<T> aXY = a;
        Point<T> bXY = b;
        LinearEquation<T> e2 = LinearEquation<T>.From(aXY, bXY);
        return e1.Intersect(e2);
    }
    
}