using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a bounded line segment between two points in 3D space.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
public readonly record struct Segment3<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>
    /// The start point of the segment.
    /// </summary>
    [ProtoMember(1)]
    public Point3<T> Start { get; init; }

    /// <summary>
    /// The end point of the segment.
    /// </summary>
    [ProtoMember(2)]
    public Point3<T> End { get; init; }

    /// <summary>
    /// Initializes a new segment from start to end.
    /// </summary>
    public Segment3(Point3<T> start, Point3<T> end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a segment from two points.
    /// </summary>
    public static Segment3<T> From(Point3<T> a, Point3<T> b) => new(a, b);

    /// <summary>
    /// Gets the direction vector from Start to End.
    /// </summary>
    public Vector3<T> Direction => (Vector3<T>)(End - Start);

    /// <summary>
    /// Gets the length of the segment.
    /// </summary>
    public T Length => Point3<T>.Distance(Start, End);

    /// <summary>
    /// Gets the midpoint of the segment.
    /// </summary>
    public Point3<T> Middle => Point3<T>.Middle(Start, End);

    /// <summary>
    /// Translates the segment by adding a vector.
    /// </summary>
    public static Segment3<T> operator +(in Segment3<T> s, Vector3<T> v) =>
        new(s.Start + v, s.End + v);

    /// <summary>
    /// Translates the segment by subtracting a vector.
    /// </summary>
    public static Segment3<T> operator -(in Segment3<T> s, Vector3<T> v) =>
        new(s.Start - v, s.End - v);

    /// <summary>
    /// Scales both endpoints by a scalar.
    /// </summary>
    public static Segment3<T> operator *(in Segment3<T> s, T scalar) =>
        new(s.Start * scalar, s.End * scalar);

    /// <summary>
    /// Computes the shortest distance from a point to this segment.
    /// </summary>
    public T DistanceTo(Point3<T> point)
    {
        var d = Direction;
        var lenSq = d.LengthSquared;

        if (lenSq < T.CreateTruncating(1e-18))
            return Point3<T>.Distance(point, Start);

        // Project point onto the line, clamped to [0, 1]
        var ap = (Vector3<T>)(point - Start);
        var t = Vector3<T>.Dot(ap, d) / lenSq;
        t = T.Max(T.Zero, T.Min(T.One, t));

        var closest = Start + d * t;
        return Point3<T>.Distance(point, closest);
    }

    /// <summary>
    /// Returns the point at parametric position t along the segment, where t=0 is Start and t=1 is End.
    /// </summary>
    public Point3<T> Lerp(T t) => Point3<T>.Lerp(Start, End, t);

    /// <summary>
    /// Splits the segment at parametric position t, returning two sub-segments.
    /// </summary>
    public (Segment3<T> Left, Segment3<T> Right) Split(T t)
    {
        var mid = Lerp(t);
        return (new Segment3<T>(Start, mid), new Segment3<T>(mid, End));
    }

    /// <summary>
    /// Returns a new segment with Start and End swapped.
    /// </summary>
    public Segment3<T> Reverse() => new(End, Start);

    /// <summary>
    /// Projects a point onto this segment (closest point on the segment).
    /// </summary>
    public Point3<T> ProjectPoint(Point3<T> point)
    {
        var d = Direction;
        var lenSq = d.LengthSquared;
        if (lenSq < T.CreateTruncating(1e-18))
            return Start;
        var ap = (Vector3<T>)(point - Start);
        var t = Vector3<T>.Dot(ap, d) / lenSq;
        t = T.Max(T.Zero, T.Min(T.One, t));
        return Start + d * t;
    }

    public override string ToString() => $"Segment3({Start}, {End})";
}
