using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A circular arc in 2D space: a <see cref="Center"/> and <see cref="Radius"/>, swept from
/// <see cref="Start"/> by a signed <see cref="Sweep"/> (CCW positive, CW negative). A full circle is
/// an arc with <c>|Sweep| = 360°</c>. Unlike <see cref="Circle{T}"/> it carries start/sweep, so it maps
/// directly to a CAD <c>ARC</c> entity and to a 3-point circular robot move (start / <see cref="Mid"/> / end).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and angles.</typeparam>
[ProtoContract]
public readonly record struct Arc<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T DegToRad = T.Pi / T.CreateTruncating(180);

    /// <summary>The arc's centre.</summary>
    [ProtoMember(1)] public Point<T> Center { get; init; }

    /// <summary>The arc's radius.</summary>
    [ProtoMember(2)] public T Radius { get; init; }

    /// <summary>The start angle, measured CCW from +X.</summary>
    [ProtoMember(3)] public Degree<T> Start { get; init; }

    /// <summary>The signed swept angle (CCW positive). A full circle is <c>±360°</c>.</summary>
    [ProtoMember(4)] public Degree<T> Sweep { get; init; }

    public Arc(Point<T> center, T radius, Degree<T> start, Degree<T> sweep)
    {
        Center = center;
        Radius = radius;
        Start = start;
        Sweep = sweep;
    }

    /// <summary>The end angle (<see cref="Start"/> + <see cref="Sweep"/>).</summary>
    public Degree<T> End => (Degree<T>)((T)Start + (T)Sweep);

    /// <summary>The mid angle (<see cref="Start"/> + <see cref="Sweep"/>/2).</summary>
    public Degree<T> MidAngle => (Degree<T>)((T)Start + (T)Sweep / Two);

    /// <summary>The point on the arc at the given angle.</summary>
    public Point<T> PointAt(Degree<T> angle)
        => new(Center.X + Radius * Degree.Cos(angle), Center.Y + Radius * Degree.Sin(angle));

    /// <summary>The start point of the arc.</summary>
    public Point<T> StartPoint => PointAt(Start);

    /// <summary>The end point of the arc.</summary>
    public Point<T> EndPoint => PointAt(End);

    /// <summary>The midpoint of the arc (at <see cref="MidAngle"/>) — the natural via-point for a circular move.</summary>
    public Point<T> Mid => PointAt(MidAngle);

    /// <summary>The arc length.</summary>
    public T Length => Radius * T.Abs((T)Sweep) * DegToRad;

    /// <summary>
    /// The unit travel tangent at the given angle, oriented in the sweep direction (CW arcs point the
    /// opposite way to CCW arcs).
    /// </summary>
    public Vector<T> TangentAt(Degree<T> angle)
    {
        var sign = (T)Sweep >= T.Zero ? T.One : -T.One;
        return new Vector<T>(-Degree.Sin(angle) * sign, Degree.Cos(angle) * sign).Normalize();
    }
}
