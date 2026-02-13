using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a bounded line segment between two points.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
[Svg.SvgExporter(typeof(SegmentSvgExporterFactory))]
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

    /// <summary>
    /// Computes the signed angle from this segment's direction to another segment's direction.
    /// The result is in the range (-π, π].
    /// Positive means counter-clockwise rotation.
    /// </summary>
    /// <param name="other">The other segment.</param>
    /// <returns>The signed angle between the segment directions in radians.</returns>
    public Radian<T> AngleBetween(in Segment<T> other)
    {
        var d1 = Direction;
        var d2 = other.Direction;
        var t1 = T.Atan2(d1.Y, d1.X);
        var t2 = T.Atan2(d2.Y, d2.X);
        var diff = t2 - t1;

        // Normalize to (-π, π]
        var pi = T.Pi;
        if (diff > pi) diff -= pi + pi;
        if (diff <= -pi) diff += pi + pi;

        return Radian<T>.FromRadian(diff);
    }

    /// <summary>
    /// Computes the shortest distance from a point to this segment.
    /// </summary>
    /// <param name="point">The point to measure distance from.</param>
    /// <returns>The shortest distance from the point to the segment.</returns>
    public T DistanceTo(Point<T> point)
    {
        var d = Direction;
        var lenSq = d.LengthSquared;

        if (lenSq < T.CreateTruncating(1e-18))
            return (point - Start).Length;

        // Project point onto the line, clamped to [0, 1]
        var ap = point - Start;
        var t = (ap.X * d.X + ap.Y * d.Y) / lenSq;
        t = T.Max(T.Zero, T.Min(T.One, t));

        var closest = new Point<T>(Start.X + t * d.X, Start.Y + t * d.Y);
        return (point - closest).Length;
    }

    /// <summary>
    /// Rotates the segment around the specified origin by the given angle.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="origin">The center of rotation. Defaults to the origin (0, 0).</param>
    /// <returns>The rotated segment.</returns>
    public Segment<T> Rotate(Degree<T> angle, Point<T> origin = default) =>
        new(Start.Rotate(angle, origin), End.Rotate(angle, origin));

    /// <summary>
    /// Rotates the segment around the origin by the given angle.
    /// </summary>
    public static Segment<T> operator +(in Segment<T> s, Degree<T> angle) =>
        s.Rotate(angle);

    /// <summary>
    /// Rotates the segment around the origin by the negation of the given angle.
    /// </summary>
    public static Segment<T> operator -(in Segment<T> s, Degree<T> angle) =>
        s.Rotate(-angle);

    /// <summary>
    /// Projects a point onto this segment (closest point on the segment).
    /// </summary>
    public Point<T> ProjectPoint(Point<T> point)
    {
        var d = Direction;
        var lenSq = d.LengthSquared;
        if (lenSq < T.CreateTruncating(1e-18))
            return Start;
        var ap = point - Start;
        var t = (ap.X * d.X + ap.Y * d.Y) / lenSq;
        t = T.Max(T.Zero, T.Min(T.One, t));
        return new Point<T>(Start.X + t * d.X, Start.Y + t * d.Y);
    }

    /// <summary>
    /// Returns the point at parametric position t along the segment, where t=0 is Start and t=1 is End.
    /// </summary>
    public Point<T> Lerp(T t) =>
        new Point<T>(Start.X + (End.X - Start.X) * t, Start.Y + (End.Y - Start.Y) * t);

    /// <summary>
    /// Splits the segment at parametric position t, returning two sub-segments.
    /// </summary>
    public (Segment<T> Left, Segment<T> Right) Split(T t)
    {
        var mid = Lerp(t);
        return (new Segment<T>(Start, mid), new Segment<T>(mid, End));
    }

    /// <summary>
    /// Determines whether this segment is parallel to another segment.
    /// </summary>
    public bool IsParallelTo(in Segment<T> other)
    {
        var cross = Vector<T>.CrossProduct(Direction, other.Direction);
        return T.Abs(cross) < T.CreateTruncating(1e-9);
    }

    /// <summary>
    /// Returns a new segment with Start and End swapped.
    /// </summary>
    public Segment<T> Reverse() => new Segment<T>(End, Start);

    /// <summary>
    /// Projects this segment onto the specified line, returning the orthogonal projection.
    /// </summary>
    public Segment<T> ProjectOnto(Line<T> line) => line.Project(this);

    /// <summary>
    /// Returns the portion of this segment that lies inside the rectangle, or null if fully outside.
    /// Uses the Liang-Barsky clipping algorithm.
    /// </summary>
    public Segment<T>? Intersect(Rectangle<T> rect)
    {
        var dx = End.X - Start.X;
        var dy = End.Y - Start.Y;
        var tMin = T.Zero;
        var tMax = T.One;

        if (!ClipEdge(-dx, Start.X - rect.X, ref tMin, ref tMax)) return null;
        if (!ClipEdge(dx, rect.Right - Start.X, ref tMin, ref tMax)) return null;
        if (!ClipEdge(-dy, Start.Y - rect.Y, ref tMin, ref tMax)) return null;
        if (!ClipEdge(dy, rect.Bottom - Start.Y, ref tMin, ref tMax)) return null;

        return new Segment<T>(Lerp(tMin), Lerp(tMax));
    }

    private static bool ClipEdge(T p, T q, ref T tMin, ref T tMax)
    {
        if (p == T.Zero) return q >= T.Zero;
        var r = q / p;
        if (p < T.Zero)
        {
            if (r > tMax) return false;
            tMin = T.Max(tMin, r);
        }
        else
        {
            if (r < tMin) return false;
            tMax = T.Min(tMax, r);
        }
        return true;
    }

    public override string ToString() => $"Segment({Start}, {End})";
}
