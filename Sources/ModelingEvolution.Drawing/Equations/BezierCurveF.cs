using System.Collections;
using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;

namespace ModelingEvolution.Drawing.Equations;

public class PolygonalCurveF : IEnumerable<PointF>
{
    private readonly List<PointF> _points;
    public PointF this[int index] => _points[index]; 
    public int Count => _points.Count;
    public PolygonalCurveF Add(params PointF[] points)
    {
        _points.AddRange(points);
        return this;
    }
    public static PolygonalCurveF From(params PointF[] points) => new PolygonalCurveF(points);
    public PolygonalCurveF(params PointF[] points)
    {
        _points = new List<PointF>(points);
    }

    public BezierCurveF GetSmoothSegment(int i0)
    {
        int i1 = i0 + 1;
        if (i1 >= _points.Count)
            throw new ArgumentOutOfRangeException("Number must be 1 smaller than number of points in curve");

        var p0 = _points[i0];
        var p1 = _points[i0+1];
        VectorF d = p1 - p0;
        var controlPointLength = d.Length / 2;

        VectorF a = i0 == 0 ? d/2 : (p0 - _points[i0-1]).Normalize()*controlPointLength;
        VectorF b = i1 == _points.Count-1 ? -d/2 : (p1- _points[i1 + 1]).Normalize() * controlPointLength;
        
        return new BezierCurveF(p0, p0 + a, p1 + b, p1);
                }
    public IEnumerator<PointF> GetEnumerator()
    {
        foreach (var i in _points) yield return i;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}



public readonly record struct BezierCurveF : IEnumerable<PointF>
{
    public PointF Start { get;  }
    public PointF C0 { get;  }
    public PointF C1 { get;  }
    public PointF End { get;  }

    public BezierCurveF(PointF start, PointF c0, PointF c1, PointF end)
    {
        Start = start;
        C0 = c0;
        C1 = c1;
        End = end;
    }
    
    public BezierCurveF(params PointF[] points) : this(points[0], points[1], points[2], points[3]) { }
    public BezierCurveF TransformBy(MatrixF m)
    {
        return new BezierCurveF(m.Transform(Start), m.Transform(C0), m.Transform(C1), m.Transform(End));
    }
    public static BezierCurveF operator+(BezierCurveF c, VectorF v)
    {
        return new BezierCurveF(c.Start + v, c.C0 + v, c.C1 + v, c.End + v);
    }
    public PointF[] CalculateExtremumPoints()
    {
        var D0 = 3 * (C0 - Start);
        var D1 = 3 * (C1 - C0);
        var D2 = 3 * (End - C1);

        // Coefficients for the quadratic equation
        var a = D2 - 2 * D1 + D0;
        var b = 2 * (D1 - D0);
        var c = D0;
        
        float[] tValuesX = new QuadraticEquationF(a.X, b.X, c.X).ZeroPoints();
        float[] tValuesY = new QuadraticEquationF(a.Y, b.Y, c.Y).ZeroPoints();

        var tmp = this;
        return tValuesX
            .Union(tValuesY)
            .Where(x => x >= 0 & x <= 1)
            .Distinct()
            .Select(x => tmp.Evaluate(x))
            .ToArray();

    }

    public PointF Intersection(LinearEquationF f)
    {
        var a = Start.X * f.A - Start.Y - 3 * C0.X * f.A + 3 * C0.Y + 3 * C1.X * f.A - 3 * C1.Y - End.X * f.A + End.Y;
        var b = -3 * Start.X * f.A + 3 * Start.Y + 6 * C0.X * f.A - 6 * C0.Y - 3 * C1.X * f.A + 3 * C1.Y;
        var c = 3 * Start.X * f.A - 3 * Start.Y - 3 * C0.X * f.A + 3 * C0.Y;
        var d = -Start.X * f.A + Start.Y - f.B;
        CubicEquationF ex = new CubicEquationF(a, b, c, d);
        var zer = ex.FindRoot(0.5f);

        return Evaluate(zer);
    }

    public PointF Evaluate(float t)
    {
        if (t < 0 || t > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(t), "Parameter t should be between 0 and 1.");
        }

        // Cubic Bézier formula
        float oneMinusT = 1 - t;
        float oneMinusTSquare = oneMinusT * oneMinusT;
        float oneMinusTCube = oneMinusTSquare * oneMinusT;
        float tSquare = t * t;
        float tCube = tSquare * t;

        float x = oneMinusTCube * Start.X +
                  3 * oneMinusTSquare * t * C0.X +
                  3 * oneMinusT * tSquare * C1.X +
                  tCube * End.X;

        float y = oneMinusTCube * Start.Y +
                  3 * oneMinusTSquare * t * C0.Y +
                  3 * oneMinusT * tSquare * C1.Y +
                  tCube * End.Y;

        return new PointF(x, y);
    }

    public IEnumerator<PointF> GetEnumerator()
    {
        yield return Start;
        yield return C0;
        yield return C1;
        yield return End;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}