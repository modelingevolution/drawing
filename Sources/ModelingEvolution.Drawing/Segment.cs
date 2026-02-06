using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a bounded line segment between two points.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
public readonly record struct Segment<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>
    /// The start point of the segment.
    /// </summary>
    [ProtoMember(1)]
    public Point<T> Start { get; init; }

    /// <summary>
    /// The end point of the segment.
    /// </summary>
    [ProtoMember(2)]
    public Point<T> End { get; init; }

    /// <summary>
    /// Initializes a new segment from start to end.
    /// </summary>
    public Segment(Point<T> start, Point<T> end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a segment from two points.
    /// </summary>
    public static Segment<T> From(Point<T> a, Point<T> b) => new(a, b);

    /// <summary>
    /// Gets the direction vector from Start to End.
    /// </summary>
    public Vector<T> Direction => End - Start;

    /// <summary>
    /// Gets the length of the segment.
    /// </summary>
    public T Length => Direction.Length;

    /// <summary>
    /// Gets the midpoint of the segment.
    /// </summary>
    public Point<T> Middle => new((Start.X + End.X) / Two, (Start.Y + End.Y) / Two);

    /// <summary>
    /// Translates the segment by adding a vector.
    /// </summary>
    public static Segment<T> operator +(in Segment<T> s, Vector<T> v) =>
        new(s.Start + v, s.End + v);

    /// <summary>
    /// Translates the segment by subtracting a vector.
    /// </summary>
    public static Segment<T> operator -(in Segment<T> s, Vector<T> v) =>
        new(s.Start - v, s.End - v);

    /// <summary>
    /// Scales both endpoints by a size.
    /// </summary>
    public static Segment<T> operator *(in Segment<T> s, Size<T> sz) =>
        new(s.Start * sz, s.End * sz);

    /// <summary>
    /// Scales both endpoints by dividing by a size.
    /// </summary>
    public static Segment<T> operator /(in Segment<T> s, Size<T> sz) =>
        new(s.Start / sz, s.End / sz);

    /// <summary>
    /// Extends this segment to an infinite line.
    /// </summary>
    public Line<T> ToLine() => Line<T>.From(Start, End);

    /// <summary>
    /// Determines whether a point lies on this segment within floating-point tolerance.
    /// </summary>
    public bool Contains(Point<T> point)
    {
        var d = Direction;
        var ap = point - Start;

        var cross = Vector<T>.CrossProduct(d, ap);
        if (T.Abs(cross) > T.CreateTruncating(1e-9))
            return false;

        var dot = d.X * ap.X + d.Y * ap.Y;
        var lenSq = d.LengthSquared;

        return dot >= T.Zero && dot <= lenSq;
    }

    /// <summary>
    /// Finds the intersection point between this segment and another segment.
    /// Returns null if the segments do not intersect.
    /// </summary>
    public Point<T>? Intersect(in Segment<T> other) => Intersections.Of(this, other);

    /// <summary>
    /// Finds the intersection point between this segment and a line.
    /// Returns null if the line does not intersect the segment.
    /// </summary>
    public Point<T>? Intersect(in Line<T> line) => Intersections.Of(line, this);

    /// <summary>
    /// Returns the portion of this segment that lies inside the circle.
    /// Null if the segment misses or is tangent.
    /// </summary>
    public Segment<T>? Intersect(in Circle<T> circle) => Intersections.Of(this, circle);

    /// <summary>
    /// Returns the point where this segment is tangent to the circle.
    /// Null if the segment misses or is secant.
    /// </summary>
    public Point<T>? TangentPoint(in Circle<T> circle) => Intersections.TangentPoint(this, circle);

    public override string ToString() => $"Segment({Start}, {End})";
}
