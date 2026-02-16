using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Mutable builder for constructing polygons incrementally.
/// Supports Add, InsertAt, RemoveAt, and indexer set operations.
/// Implicitly converts to an immutable Polygon&lt;T&gt; via Build().
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public record class PolygonBuilder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly List<Point<T>> _points;

    public PolygonBuilder()
    {
        _points = new List<Point<T>>();
    }

    public PolygonBuilder(int capacity)
    {
        _points = new List<Point<T>>(capacity);
    }

    public PolygonBuilder(IEnumerable<Point<T>> points)
    {
        _points = new List<Point<T>>(points);
    }

    public PolygonBuilder(Polygon<T> polygon)
    {
        var span = polygon.AsSpan();
        _points = new List<Point<T>>(span.Length);
        for (int i = 0; i < span.Length; i++)
            _points.Add(span[i]);
    }

    public int Count => _points.Count;

    public Point<T> this[int index]
    {
        get => _points[index];
        set => _points[index] = value;
    }

    public void Add(Point<T> point) => _points.Add(point);

    public void InsertAt(int index, Point<T> point) => _points.Insert(index, point);

    public void RemoveAt(int index) => _points.RemoveAt(index);

    public void Clear() => _points.Clear();

    public Polygon<T> Build() => new(_points.ToArray());

    public static implicit operator Polygon<T>(PolygonBuilder<T> builder) => builder.Build();
}
