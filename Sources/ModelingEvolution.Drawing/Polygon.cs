using ProtoBuf;
using System.Collections;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ModelingEvolution.Drawing;
public class PolygonJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Polygon<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(PolygonJsonConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
public class PolygonJsonConverter<T> : JsonConverter<Polygon<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    public override Polygon<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var points = new List<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (!T.TryParse(reader.GetString(), null, out T value))
                throw new JsonException($"Cannot parse value {reader.GetString()} to type {typeof(T)}");

            points.Add(value);
        }

        if (points.Count % 2 != 0)
            throw new JsonException("Array length must be even");

        return new Polygon<T>(points);
    }

    public override void Write(Utf8JsonWriter writer, Polygon<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var point in value.Points)
        {
            writer.WriteStringValue(point.X.ToString());
            writer.WriteStringValue(point.Y.ToString());
        }

        writer.WriteEndArray();
    }
}
[JsonConverter(typeof(PolygonJsonConverterFactory))]
[ProtoContract]
/// <summary>
/// This struct is not immutable, athrough operators are immutable.
/// </summary>
public readonly record struct Polygon<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    [ProtoMember(1)] internal readonly IList<Point<T>> _points;

    public T Area()
    {
        int n = _points.Count;
        if (n < 3)
        {
            throw new ArgumentException("A polygon must have at least 3 points.");
        }

        T area = T.Zero;
        for (int i = 0; i < n; i++)
        {
            var current = _points[i];
            var next = _points[(i + 1) % n];
            area += current.X * next.Y;
            area -= current.Y * next.X;
        }

        area = T.Abs(area) / (T.One + T.One);
        return area;
    }
    // Implement computation of bounding box
    public Rectangle<T> BoundingBox()
    {
        if (_points.Count == 0)
        {
            return new Rectangle<T>(new Point<T>(T.Zero, T.Zero), new Size<T>(T.Zero, T.Zero));
        }

        T minX = _points[0].X;
        T minY = _points[0].Y;
        T maxX = _points[0].X;
        T maxY = _points[0].Y;

        for (int i = 1; i < _points.Count; i++)
        {
            if (_points[i].X < minX)
            {
                minX = _points[i].X;
            }

            if (_points[i].Y < minY)
            {
                minY = _points[i].Y;
            }

            if (_points[i].X > maxX)
            {
                maxX = _points[i].X;
            }

            if (_points[i].Y > maxY)
            {
                maxY = _points[i].Y;
            }
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }
    public bool Contains(Point<T> item)
    {
        return _points.Contains(item);
    }
    [JsonIgnore]
    public bool IsReadOnly => true;

    public static Polygon<T> operator *(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x * f).ToList(a._points.Count));
    }

    public static Polygon<T> operator /(Polygon<T> a, Size<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x / f).ToList(a._points.Count));
    }

    public bool Equals(Polygon<T> other)
    {
        if (object.ReferenceEquals(_points, other._points)) return true;
        return this._points.SequenceEqual(other._points);
    }

    public override int GetHashCode()
    {
        return _points.GetHashCode();
    }

    public Polygon<T> Intersect(Rectangle<T> rect)
    {
        var outputList = this._points.ToList(_points.Count);

        // Clip against each edge of the rectangle
        outputList =
            ClipPolygon(outputList, new Point<T>(rect.Left, rect.Top), new Point<T>(rect.Right, rect.Top)); // Top edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Right, rect.Top),
            new Point<T>(rect.Right, rect.Bottom)); // Right edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Right, rect.Bottom),
            new Point<T>(rect.Left, rect.Bottom)); // Bottom edge
        outputList = ClipPolygon(outputList, new Point<T>(rect.Left, rect.Bottom),
            new Point<T>(rect.Left, rect.Top)); // Left edge

        return new Polygon<T>(outputList.ToArray());
    }

    // Sutherland-Hodgman polygon clipping
    private static List<Point<T>> ClipPolygon(IList<Point<T>> polygon, Point<T> edgeStart, Point<T> edgeEnd)
    {
        List<Point<T>> clippedPolygon = new List<Point<T>>();

        for (int i = 0; i < polygon.Count; i++)
        {
            Point<T> currentPoint = polygon[i];
            Point<T> prevPoint = polygon[(i - 1 + polygon.Count) % polygon.Count];

            bool currentInside = IsInside(currentPoint, edgeStart, edgeEnd);
            bool prevInside = IsInside(prevPoint, edgeStart, edgeEnd);

            if (currentInside && prevInside)
            {
                // Both points are inside, add current point
                clippedPolygon.Add(currentPoint);
            }
            else if (!currentInside && prevInside)
            {
                // Leaving the clip area, add intersection point
                clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
            }
            else if (currentInside && !prevInside)
            {
                // Entering the clip area, add intersection point and current point
                clippedPolygon.Add(GetIntersection(prevPoint, currentPoint, edgeStart, edgeEnd));
                clippedPolygon.Add(currentPoint);
            }
        }

        return clippedPolygon;
    }

    // Helper function to check if a point is inside the clipping edge
    private static bool IsInside(Point<T> p, Point<T> edgeStart, Point<T> edgeEnd)
    {
        return (edgeEnd.X - edgeStart.X) * (p.Y - edgeStart.Y) > (edgeEnd.Y - edgeStart.Y) * (p.X - edgeStart.X);
    }

    // Calculate intersection of line segment (p1, p2) with edge (edgeStart, edgeEnd)
    private static Point<T> GetIntersection(Point<T> p1, Point<T> p2, Point<T> edgeStart, Point<T> edgeEnd)
    {
        T A1 = p2.Y - p1.Y;
        T B1 = p1.X - p2.X;
        T C1 = A1 * p1.X + B1 * p1.Y;

        T A2 = edgeEnd.Y - edgeStart.Y;
        T B2 = edgeStart.X - edgeEnd.X;
        T C2 = A2 * edgeStart.X + B2 * edgeStart.Y;

        T det = A1 * B2 - A2 * B1;

        if (T.Abs(det) <= T.Epsilon) // Lines are parallel
        {
            return new Point<T>(); // No intersection
        }

        T x = (B2 * C1 - B1 * C2) / det;
        T y = (A1 * C2 - A2 * C1) / det;

        return new Point<T>(x, y);
    }

    public static Polygon<T> operator -(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x - f).ToList(a._points.Count));
    }

    public static Polygon<T> operator +(Polygon<T> a, ModelingEvolution.Drawing.Vector<T> f)
    {
        return new Polygon<T>(a.Points.Select(x => x + f).ToList(a._points.Count));
    }

    /// <summary>
    ///  Adds point at the end of the polygon.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Polygon<T> operator +(Polygon<T> a, ModelingEvolution.Drawing.Point<T> f)
    {
        var ret = new Polygon<T>(a.Points.ToList(a._points.Count + 1));
        ret._points.Add(f);
        return ret;
    }


    public void InsertAt(int index, Point<T> point) => _points.Insert(index, point);
    public void Add(int index, Point<T> point) => _points.Add(point);
    public void RemoveAt(int index) => _points.RemoveAt(index);

    public Polygon(IList<Point<T>> points)
    {
        _points = points;
    }

    public Polygon(params Point<T>[] points) : this(points.ToList())
    {
        
    }

    public Polygon(IReadOnlyList<T> points)
    {
        _points = new List<Point<T>>(points.Count / 2);
        for (int i = 0; i < points.Count; i += 2)
        {
            _points.Add(new Point<T>(points[i], points[i + 1]));
        }
    }

    [JsonIgnore]
    public int Count => _points.Count;

    public Point<T> this[int index]
    {
        get { return _points[index]; }
        set => _points[index] = value;  
    }

    public IReadOnlyList<Point<T>> Points => (IReadOnlyList<Point<T>>)_points;
}