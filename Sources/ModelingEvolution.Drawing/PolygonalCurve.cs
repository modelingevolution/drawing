using System.Collections;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a polygonal curve composed of connected line segments that can generate smooth Bezier curve segments.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[Svg.SvgExporter(typeof(PolygonalCurveSvgExporterFactory))]
public class PolygonalCurve<T> : IEnumerable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly List<Point<T>> _points;
    /// <summary>
    /// Gets the point at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the point to get.</param>
    /// <returns>The point at the specified index.</returns>
    public Point<T> this[int index] => _points[index];
    
    /// <summary>
    /// Gets the number of points in the curve.
    /// </summary>
    public int Count => _points.Count;
    /// <summary>
    /// Adds points to the curve.
    /// </summary>
    /// <param name="points">The points to add to the curve.</param>
    /// <returns>The current PolygonalCurve instance for method chaining.</returns>
    public PolygonalCurve<T> Add(params Point<T>[] points)
    {
        _points.AddRange(points);
        return this;
    }
    /// <summary>
    /// Creates a new PolygonalCurve from the specified points.
    /// </summary>
    /// <param name="points">The points that define the curve.</param>
    /// <returns>A new PolygonalCurve instance.</returns>
    public static PolygonalCurve<T> From(params Point<T>[] points) => new PolygonalCurve<T>(points);
    /// <summary>
    /// Initializes a new instance of the PolygonalCurve class with the specified points.
    /// </summary>
    /// <param name="points">The points that define the curve.</param>
    public PolygonalCurve(params Point<T>[] points)
    {
        _points = new List<Point<T>>(points);
    }

    private static readonly T t2 = T.CreateTruncating(2);
    /// <summary>
    /// Gets a smooth Bezier curve segment between two consecutive points in the curve.
    /// </summary>
    /// <param name="i0">The index of the first point in the segment.</param>
    /// <returns>A Bezier curve that smoothly connects the specified segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when i0 is not valid for creating a segment.</exception>
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
    /// <summary>
    /// Returns an enumerator that iterates through the points in the curve.
    /// </summary>
    /// <returns>An enumerator for the points in the curve.</returns>
    public IEnumerator<Point<T>> GetEnumerator()
    {
        foreach (var i in _points) yield return i;
    }

    /// <summary>
    /// Returns a non-generic enumerator that iterates through the points in the curve.
    /// </summary>
    /// <returns>A non-generic enumerator for the points in the curve.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}