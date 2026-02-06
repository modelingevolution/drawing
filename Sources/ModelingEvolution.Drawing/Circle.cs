using System.Numerics;
using ModelingEvolution.Drawing.Equations;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a circle in 2D space defined by a center point and radius.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
public readonly record struct Circle<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Pi = T.Pi;

    [ProtoMember(1)]
    public Point<T> Center { get; init; }

    [ProtoMember(2)]
    public T Radius { get; init; }

    public Circle(Point<T> center, T radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Gets the underlying CircleEquation.
    /// </summary>
    public CircleEquation<T> Equation => new(Center, Radius);

    /// <summary>
    /// Gets the diameter of the circle.
    /// </summary>
    public T Diameter => Two * Radius;

    /// <summary>
    /// Gets the area of the circle.
    /// </summary>
    public T Area => Pi * Radius * Radius;

    /// <summary>
    /// Gets the circumference of the circle.
    /// </summary>
    public T Circumference => Two * Pi * Radius;

    /// <summary>
    /// Determines whether the specified point lies inside or on the circle.
    /// </summary>
    public bool Contains(Point<T> point)
    {
        var dx = point.X - Center.X;
        var dy = point.Y - Center.Y;
        return dx * dx + dy * dy <= Radius * Radius;
    }

    /// <summary>
    /// Returns the chord where the line passes through this circle.
    /// Null if the line misses or is tangent.
    /// </summary>
    public Segment<T>? Intersect(Line<T> line) => Intersections.Of(line, this);

    /// <summary>
    /// Returns the portion of the segment that lies inside this circle.
    /// Null if the segment misses or is tangent.
    /// </summary>
    public Segment<T>? Intersect(Segment<T> segment) => Intersections.Of(segment, this);

    /// <summary>
    /// Returns the radical chord where this circle and another overlap.
    /// Null if the circles miss or are tangent.
    /// </summary>
    public Segment<T>? Intersect(Circle<T> other) => Intersections.Of(this, other);

    /// <summary>
    /// Returns the point where the line is tangent to this circle.
    /// Null if the line misses or is secant.
    /// </summary>
    public Point<T>? TangentPoint(Line<T> line) => Intersections.TangentPoint(line, this);

    /// <summary>
    /// Returns the point where the segment is tangent to this circle.
    /// Null if the segment misses or is secant.
    /// </summary>
    public Point<T>? TangentPoint(Segment<T> segment) => Intersections.TangentPoint(segment, this);

    /// <summary>
    /// Returns the point where this circle and another are tangent.
    /// Null if the circles miss or have two crossing points.
    /// </summary>
    public Point<T>? TangentPoint(Circle<T> other) => Intersections.TangentPoint(this, other);

    /// <summary>
    /// Translates the circle by adding a vector.
    /// </summary>
    public static Circle<T> operator +(in Circle<T> c, Vector<T> v) =>
        new(c.Center + v, c.Radius);

    /// <summary>
    /// Translates the circle by subtracting a vector.
    /// </summary>
    public static Circle<T> operator -(in Circle<T> c, Vector<T> v) =>
        new(c.Center - v, c.Radius);

    public override string ToString() => $"Circle({Center}, r={Radius})";
}
