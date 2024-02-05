using System.Collections;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public class PolygonalCurve<T> : IEnumerable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    private readonly List<Point<T>> _points;
    public Point<T> this[int index] => _points[index];
    public int Count => _points.Count;
    public PolygonalCurve<T> Add(params Point<T>[] points)
    {
        _points.AddRange(points);
        return this;
    }
    public static PolygonalCurve<T> From(params Point<T>[] points) => new PolygonalCurve<T>(points);
    public PolygonalCurve(params Point<T>[] points)
    {
        _points = new List<Point<T>>(points);
    }

    private static readonly T t2 = T.CreateTruncating(2);
    public BezierCurve<T> GetSmoothSegment(int i0)
    {
        int i1 = i0 + 1;
        if (i1 >= _points.Count)
            throw new ArgumentOutOfRangeException("Number must be 1 smaller than number of points in curve");

        var p0 = _points[i0];
        var p1 = _points[i0 + 1];
        var d = p1 - p0;
        var controlPointLength = d.Length / t2;

        var a = i0 == 0 ? d / t2 : (p0 - _points[i0 - 1]).Normalize() * controlPointLength;
        var b = i1 == _points.Count - 1 ? -d / t2 : (p1 - _points[i1 + 1]).Normalize() * controlPointLength;

        return new BezierCurve<T>(p0, p0 + a, p1 + b, p1);
    }
    public IEnumerator<Point<T>> GetEnumerator()
    {
        foreach (var i in _points) yield return i;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}