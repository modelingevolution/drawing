using System.Numerics;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Central static class that owns ALL intersection logic between geometric types.
/// <para>
/// <b>Of(...)</b> — returns the interior intersection (Segment for secant, null for miss/tangent).<br/>
/// <b>TangentPoint(...)</b> — returns the single touch point when shapes are tangent (null otherwise).
/// </para>
/// </summary>
public static class Intersections
{
    private static readonly double Epsilon = 1e-9;
    private static readonly double TangentEpsilon = 1e-14;
    private static readonly double DuplicateEpsilon = 1e-7;

    // ────────────────────────────────────────────────
    // #1  Line × Line → Point?
    // ────────────────────────────────────────────────
    public static Point<T>? Of<T>(in Line<T> a, in Line<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (a.IsVertical && b.IsVertical)
            return null;

        if (a.IsVertical)
            return new Point<T>(a.VerticalX, b.Equation.Compute(a.VerticalX));

        if (b.IsVertical)
            return new Point<T>(b.VerticalX, a.Equation.Compute(b.VerticalX));

        return a.Equation.Intersect(b.Equation);
    }

    // ────────────────────────────────────────────────
    // #2  Line × Segment → Point?
    // ────────────────────────────────────────────────
    public static Point<T>? Of<T>(in Line<T> line, in Segment<T> segment)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var eps = T.CreateTruncating(Epsilon);
        var d = segment.Direction;

        if (line.IsVertical)
        {
            var vx = line.VerticalX;
            if (T.Abs(d.X) < eps)
                return null;

            var t = (vx - segment.Start.X) / d.X;
            if (t < T.Zero || t > T.One) return null;
            return new Point<T>(vx, segment.Start.Y + t * d.Y);
        }

        var eq = line.Equation;
        var denom = d.Y - eq.A * d.X;
        if (T.Abs(denom) < eps)
            return null;

        var t2 = (eq.A * segment.Start.X + eq.B - segment.Start.Y) / denom;
        if (t2 < T.Zero || t2 > T.One) return null;

        var px = segment.Start.X + t2 * d.X;
        return new Point<T>(px, eq.Compute(px));
    }

    // ────────────────────────────────────────────────
    // #3  Line × Circle → Segment? (chord)
    // ────────────────────────────────────────────────
    /// <summary>
    /// Returns the chord where the line passes through the circle interior.
    /// Null if the line misses or is tangent to the circle.
    /// </summary>
    public static Segment<T>? Of<T>(in Line<T> line, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = LineCirclePoints(line, circle);
        return pts.Length == 2 ? new Segment<T>(pts[0], pts[1]) : null;
    }

    /// <summary>
    /// Returns the tangent point where the line just touches the circle.
    /// Null if the line misses or is secant to the circle.
    /// </summary>
    public static Point<T>? TangentPoint<T>(in Line<T> line, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = LineCirclePoints(line, circle);
        return pts.Length == 1 ? pts[0] : null;
    }

    // ────────────────────────────────────────────────
    // #4  Line × Triangle → Segment?
    // ────────────────────────────────────────────────
    public static Segment<T>? Of<T>(in Line<T> line, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        var segments = LineThroughEdges(line, vertices);
        return segments.Count > 0 ? segments[0] : null;
    }

    // ────────────────────────────────────────────────
    // #5  Line × Rectangle → Segment?
    // ────────────────────────────────────────────────
    public static Segment<T>? Of<T>(in Line<T> line, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        var segments = LineThroughEdges(line, RectVerticesCw(rect));
        return segments.Count > 0 ? segments[0] : null;
    }

    // ────────────────────────────────────────────────
    // #6  Line × Polygon → IReadOnlyList<Segment>
    // ────────────────────────────────────────────────
    public static IReadOnlyList<Segment<T>> Of<T>(in Line<T> line, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return LineThroughEdges(line, polygon.Span);
    }

    // ────────────────────────────────────────────────
    // #7  Segment × Segment → Point?
    // ────────────────────────────────────────────────
    public static Point<T>? Of<T>(in Segment<T> a, in Segment<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var eps = T.CreateTruncating(Epsilon);
        var d1 = a.Direction;
        var d2 = b.Direction;

        var cross = Vector<T>.CrossProduct(d1, d2);
        if (T.Abs(cross) < eps)
            return null;

        var diff = b.Start - a.Start;
        var t = Vector<T>.CrossProduct(diff, d2) / cross;
        var u = Vector<T>.CrossProduct(diff, d1) / cross;

        if (t >= T.Zero && t <= T.One && u >= T.Zero && u <= T.One)
            return new Point<T>(a.Start.X + t * d1.X, a.Start.Y + t * d1.Y);

        return null;
    }

    // ────────────────────────────────────────────────
    // #8  Segment × Circle → Segment? (chord clipped to segment)
    // ────────────────────────────────────────────────
    /// <summary>
    /// Returns the portion of the segment that lies inside the circle.
    /// Null if the segment misses, is tangent, or only one endpoint touches.
    /// </summary>
    public static Segment<T>? Of<T>(in Segment<T> segment, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = SegmentCirclePoints(segment, circle);
        return pts.Length == 2 ? new Segment<T>(pts[0], pts[1]) : null;
    }

    /// <summary>
    /// Returns the tangent point where the segment just touches the circle.
    /// Null if the segment misses or is secant to the circle.
    /// </summary>
    public static Point<T>? TangentPoint<T>(in Segment<T> segment, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = SegmentCirclePoints(segment, circle);
        return pts.Length == 1 ? pts[0] : null;
    }

    // ────────────────────────────────────────────────
    // #9  Segment × Triangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Segment<T> segment, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        return SegmentVsEdges(segment, vertices);
    }

    // ────────────────────────────────────────────────
    // #10  Segment × Rectangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Segment<T> segment, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return SegmentVsEdges(segment, RectVerticesCw(rect));
    }

    // ────────────────────────────────────────────────
    // #11  Segment × Polygon → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Segment<T> segment, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return SegmentVsEdges(segment, polygon.Span);
    }

    // ────────────────────────────────────────────────
    // #12  Circle × Circle → Segment? (radical chord)
    // ────────────────────────────────────────────────
    /// <summary>
    /// Returns the radical chord where two circles overlap.
    /// Null if circles miss, are tangent, or one contains the other.
    /// </summary>
    public static Segment<T>? Of<T>(in Circle<T> a, in Circle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = CircleCirclePoints(a, b);
        return pts.Length == 2 ? new Segment<T>(pts[0], pts[1]) : null;
    }

    /// <summary>
    /// Returns the tangent point where two circles just touch.
    /// Null if circles miss or have two intersection points.
    /// </summary>
    public static Point<T>? TangentPoint<T>(in Circle<T> a, in Circle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var pts = CircleCirclePoints(a, b);
        return pts.Length == 1 ? pts[0] : null;
    }

    // ────────────────────────────────────────────────
    // #13  Circle × Triangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Circle<T> circle, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        return CircleVsEdges(circle, vertices);
    }

    // ────────────────────────────────────────────────
    // #14  Circle × Rectangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Circle<T> circle, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return CircleVsEdges(circle, RectVerticesCw(rect));
    }

    // ────────────────────────────────────────────────
    // #15  Circle × Polygon → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Circle<T> circle, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return CircleVsEdges(circle, polygon.Span);
    }

    // ────────────────────────────────────────────────
    // #16  Triangle × Triangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Triangle<T> a, in Triangle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] v1 = [a.A, a.B, a.C];
        Point<T>[] v2 = [b.A, b.B, b.C];
        return EdgesVsEdges(v1, v2);
    }

    // ────────────────────────────────────────────────
    // #17  Triangle × Rectangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Triangle<T> triangle, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        Point<T>[] v1 = [triangle.A, triangle.B, triangle.C];
        return EdgesVsEdges(v1, RectVerticesCw(rect));
    }

    // ────────────────────────────────────────────────
    // #18  Triangle × Polygon → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Triangle<T> triangle, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        Point<T>[] v1 = [triangle.A, triangle.B, triangle.C];
        return EdgesVsEdges(v1, polygon.Span);
    }

    // ────────────────────────────────────────────────
    // #19  Rectangle × Rectangle → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Rectangle<T> a, in Rectangle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return EdgesVsEdges(RectVerticesCw(a), RectVerticesCw(b));
    }

    // ────────────────────────────────────────────────
    // #20  Rectangle × Polygon → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Rectangle<T> rect, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return EdgesVsEdges(RectVerticesCw(rect), polygon.Span);
    }

    // ────────────────────────────────────────────────
    // #21  Polygon × Polygon → Point[]
    // ────────────────────────────────────────────────
    public static Point<T>[] Of<T>(in Polygon<T> a, in Polygon<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return EdgesVsEdges(a.Span, b.Span);
    }

    // ════════════════════════════════════════════════
    //  Raw point helpers (shared by Of + TangentPoint)
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns 0, 1, or 2 raw crossing points between a line and a circle.
    /// 0 = miss, 1 = tangent, 2 = secant.
    /// </summary>
    internal static Point<T>[] LineCirclePoints<T>(in Line<T> line, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        if (line.IsVertical)
        {
            var dx = line.VerticalX - circle.Center.X;
            var disc = circle.Radius * circle.Radius - dx * dx;
            if (disc < T.Zero)
                return [];

            var sqrtDisc = T.Sqrt(disc);
            if (disc < T.CreateTruncating(TangentEpsilon))
                return [new Point<T>(line.VerticalX, circle.Center.Y)];

            return
            [
                new Point<T>(line.VerticalX, circle.Center.Y + sqrtDisc),
                new Point<T>(line.VerticalX, circle.Center.Y - sqrtDisc)
            ];
        }

        return circle.Equation.Intersect(line.Equation);
    }

    /// <summary>
    /// Returns 0, 1, or 2 raw crossing points between a segment and a circle.
    /// Line-circle hits filtered to segment parameter [0, 1].
    /// </summary>
    internal static Point<T>[] SegmentCirclePoints<T>(in Segment<T> segment, in Circle<T> circle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var eps = T.CreateTruncating(Epsilon);
        var line = segment.ToLine();
        var allHits = LineCirclePoints(line, circle);
        if (allHits.Length == 0)
            return allHits;

        var d = segment.Direction;
        var lenSq = d.X * d.X + d.Y * d.Y;
        if (lenSq < eps)
            return [];

        var result = new List<Point<T>>(2);
        foreach (var pt in allHits)
        {
            var ap = pt - segment.Start;
            var t = (ap.X * d.X + ap.Y * d.Y) / lenSq;
            if (t >= T.Zero - eps && t <= T.One + eps)
                result.Add(pt);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Returns 0, 1, or 2 raw crossing points between two circles.
    /// 0 = miss/concentric, 1 = tangent, 2 = overlapping.
    /// </summary>
    internal static Point<T>[] CircleCirclePoints<T>(in Circle<T> a, in Circle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var two = T.CreateTruncating(2);
        var eps = T.CreateTruncating(Epsilon);

        var dx = b.Center.X - a.Center.X;
        var dy = b.Center.Y - a.Center.Y;
        var d = T.Sqrt(dx * dx + dy * dy);

        if (d > a.Radius + b.Radius + eps)
            return [];
        if (d < T.Abs(a.Radius - b.Radius) - eps)
            return [];
        if (d < eps)
            return []; // concentric

        var aSq = a.Radius * a.Radius;
        var bSq = b.Radius * b.Radius;
        var dSq = d * d;
        var h2 = (dSq + aSq - bSq) / (two * d);

        var disc = aSq - h2 * h2;
        if (disc < T.Zero)
            disc = T.Zero;

        var px = a.Center.X + h2 * dx / d;
        var py = a.Center.Y + h2 * dy / d;

        if (disc < T.CreateTruncating(TangentEpsilon))
            return [new Point<T>(px, py)];

        var h = T.Sqrt(disc);
        var rx = -dy * h / d;
        var ry = dx * h / d;

        return
        [
            new Point<T>(px + rx, py + ry),
            new Point<T>(px - rx, py - ry)
        ];
    }

    // ════════════════════════════════════════════════
    //  Edge-based helpers
    // ════════════════════════════════════════════════

    /// <summary>
    /// Rectangle vertices in clockwise winding: TL, TR, BR, BL.
    /// </summary>
    private static Point<T>[] RectVerticesCw<T>(in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return
        [
            new(rect.Left, rect.Top),
            new(rect.Right, rect.Top),
            new(rect.Right, rect.Bottom),
            new(rect.Left, rect.Bottom)
        ];
    }

    /// <summary>
    /// Finds all segments where a line passes through a closed polygon defined by vertices.
    /// Pairs intersection points as entry/exit chords.
    /// </summary>
    internal static List<Segment<T>> LineThroughEdges<T>(in Line<T> line, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        if (n < 2) return [];

        var eps = T.CreateTruncating(Epsilon);
        var dupEps = T.CreateTruncating(DuplicateEpsilon);
        var hits = new List<(Point<T> point, T param)>();

        for (int i = 0; i < n; i++)
        {
            var edgeStart = vertices[i];
            var edgeEnd = vertices[(i + 1) % n];
            var edgeDir = edgeEnd - edgeStart;

            Point<T>? hit;
            if (line.IsVertical)
            {
                var vx = line.VerticalX;
                if (T.Abs(edgeDir.X) < eps)
                    continue;

                var t = (vx - edgeStart.X) / edgeDir.X;
                if (t < T.Zero || t > T.One) continue;
                hit = new Point<T>(vx, edgeStart.Y + t * edgeDir.Y);
            }
            else
            {
                var eq = line.Equation;
                var denom = edgeDir.Y - eq.A * edgeDir.X;
                if (T.Abs(denom) < eps)
                    continue;

                var t = (eq.A * edgeStart.X + eq.B - edgeStart.Y) / denom;
                if (t < T.Zero || t > T.One) continue;

                var px = edgeStart.X + t * edgeDir.X;
                hit = new Point<T>(px, eq.Compute(px));
            }

            T param = line.IsVertical ? hit.Value.Y : hit.Value.X;

            if (!IsDuplicate(hits, hit.Value, dupEps))
                hits.Add((hit.Value, param));
        }

        if (hits.Count < 2) return [];

        hits.Sort((a, b) => a.param.CompareTo(b.param));

        var result = new List<Segment<T>>(hits.Count / 2);
        for (int i = 0; i + 1 < hits.Count; i += 2)
            result.Add(new Segment<T>(hits[i].point, hits[i + 1].point));

        return result;
    }

    /// <summary>
    /// Tests a segment against each edge of a closed polygon. Returns all crossing points.
    /// </summary>
    internal static Point<T>[] SegmentVsEdges<T>(in Segment<T> segment, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        if (n < 2) return [];

        var dupEps = T.CreateTruncating(DuplicateEpsilon);
        var result = new List<Point<T>>();

        for (int i = 0; i < n; i++)
        {
            var edge = new Segment<T>(vertices[i], vertices[(i + 1) % n]);
            var hit = Of(segment, edge);
            if (hit != null && !IsDuplicate(result, hit.Value, dupEps))
                result.Add(hit.Value);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Tests a circle against each edge of a closed polygon. Returns all crossing points.
    /// </summary>
    internal static Point<T>[] CircleVsEdges<T>(in Circle<T> circle, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        if (n < 2) return [];

        var dupEps = T.CreateTruncating(DuplicateEpsilon);
        var result = new List<Point<T>>();

        for (int i = 0; i < n; i++)
        {
            var edge = new Segment<T>(vertices[i], vertices[(i + 1) % n]);
            var hits = SegmentCirclePoints(edge, circle);
            foreach (var hit in hits)
            {
                if (!IsDuplicate(result, hit, dupEps))
                    result.Add(hit);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Tests each edge of polygon v1 against each edge of polygon v2. Returns all crossing points.
    /// </summary>
    internal static Point<T>[] EdgesVsEdges<T>(ReadOnlySpan<Point<T>> v1, ReadOnlySpan<Point<T>> v2)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n1 = v1.Length;
        int n2 = v2.Length;
        if (n1 < 2 || n2 < 2) return [];

        var dupEps = T.CreateTruncating(DuplicateEpsilon);
        var result = new List<Point<T>>();

        for (int i = 0; i < n1; i++)
        {
            var edge1 = new Segment<T>(v1[i], v1[(i + 1) % n1]);
            for (int j = 0; j < n2; j++)
            {
                var edge2 = new Segment<T>(v2[j], v2[(j + 1) % n2]);
                var hit = Of(edge1, edge2);
                if (hit != null && !IsDuplicate(result, hit.Value, dupEps))
                    result.Add(hit.Value);
            }
        }

        return result.ToArray();
    }

    // ════════════════════════════════════════════════
    //  FirstOf — zero-alloc first-hit variants
    // ════════════════════════════════════════════════

    // #6'  Line × Polygon → first Segment?
    public static Segment<T>? FirstOf<T>(in Line<T> line, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstLineThroughEdges(line, polygon.Span);
    }

    // #4'  Line × Triangle → first Segment? (already at most 1, but zero-alloc)
    public static Segment<T>? FirstOf<T>(in Line<T> line, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        return FirstLineThroughEdges(line, vertices);
    }

    // #5'  Line × Rectangle → first Segment?
    public static Segment<T>? FirstOf<T>(in Line<T> line, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstLineThroughEdges(line, RectVerticesCw(rect));
    }

    // #9'  Segment × Triangle → first Point?
    public static Point<T>? FirstOf<T>(in Segment<T> segment, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        return FirstSegmentVsEdges(segment, vertices);
    }

    // #10' Segment × Rectangle → first Point?
    public static Point<T>? FirstOf<T>(in Segment<T> segment, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstSegmentVsEdges(segment, RectVerticesCw(rect));
    }

    // #11' Segment × Polygon → first Point?
    public static Point<T>? FirstOf<T>(in Segment<T> segment, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstSegmentVsEdges(segment, polygon.Span);
    }

    // #13' Circle × Triangle → first Point?
    public static Point<T>? FirstOf<T>(in Circle<T> circle, in Triangle<T> triangle)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] vertices = [triangle.A, triangle.B, triangle.C];
        return FirstCircleVsEdges(circle, vertices);
    }

    // #14' Circle × Rectangle → first Point?
    public static Point<T>? FirstOf<T>(in Circle<T> circle, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstCircleVsEdges(circle, RectVerticesCw(rect));
    }

    // #15' Circle × Polygon → first Point?
    public static Point<T>? FirstOf<T>(in Circle<T> circle, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstCircleVsEdges(circle, polygon.Span);
    }

    // #16' Triangle × Triangle → first Point?
    public static Point<T>? FirstOf<T>(in Triangle<T> a, in Triangle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Point<T>[] v1 = [a.A, a.B, a.C];
        Point<T>[] v2 = [b.A, b.B, b.C];
        return FirstEdgesVsEdges(v1, v2);
    }

    // #17' Triangle × Rectangle → first Point?
    public static Point<T>? FirstOf<T>(in Triangle<T> triangle, in Rectangle<T> rect)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        Point<T>[] v1 = [triangle.A, triangle.B, triangle.C];
        return FirstEdgesVsEdges(v1, RectVerticesCw(rect));
    }

    // #18' Triangle × Polygon → first Point?
    public static Point<T>? FirstOf<T>(in Triangle<T> triangle, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        Point<T>[] v1 = [triangle.A, triangle.B, triangle.C];
        return FirstEdgesVsEdges(v1, polygon.Span);
    }

    // #19' Rectangle × Rectangle → first Point?
    public static Point<T>? FirstOf<T>(in Rectangle<T> a, in Rectangle<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstEdgesVsEdges(RectVerticesCw(a), RectVerticesCw(b));
    }

    // #20' Rectangle × Polygon → first Point?
    public static Point<T>? FirstOf<T>(in Rectangle<T> rect, in Polygon<T> polygon)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstEdgesVsEdges(RectVerticesCw(rect), polygon.Span);
    }

    // #21' Polygon × Polygon → first Point?
    public static Point<T>? FirstOf<T>(in Polygon<T> a, in Polygon<T> b)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        return FirstEdgesVsEdges(a.Span, b.Span);
    }

    // ════════════════════════════════════════════════
    //  Zero-alloc first-hit internal helpers
    // ════════════════════════════════════════════════

    /// <summary>
    /// Finds the first entry/exit segment where a line passes through edges.
    /// Tracks only the two smallest-parameter hits — O(n) time, O(1) heap.
    /// </summary>
    internal static Segment<T>? FirstLineThroughEdges<T>(in Line<T> line, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        if (n < 2) return null;

        var eps = T.CreateTruncating(Epsilon);
        var dupEps = T.CreateTruncating(DuplicateEpsilon);

        // Track the two smallest-parameter hits with no heap allocation
        bool has1 = false, has2 = false;
        Point<T> p1 = default, p2 = default;
        T par1 = T.Zero, par2 = T.Zero;

        for (int i = 0; i < n; i++)
        {
            var edgeStart = vertices[i];
            var edgeEnd = vertices[(i + 1) % n];
            var edgeDir = edgeEnd - edgeStart;

            Point<T> hit;
            if (line.IsVertical)
            {
                var vx = line.VerticalX;
                if (T.Abs(edgeDir.X) < eps) continue;
                var t = (vx - edgeStart.X) / edgeDir.X;
                if (t < T.Zero || t > T.One) continue;
                hit = new Point<T>(vx, edgeStart.Y + t * edgeDir.Y);
            }
            else
            {
                var eq = line.Equation;
                var denom = edgeDir.Y - eq.A * edgeDir.X;
                if (T.Abs(denom) < eps) continue;
                var t = (eq.A * edgeStart.X + eq.B - edgeStart.Y) / denom;
                if (t < T.Zero || t > T.One) continue;
                var px = edgeStart.X + t * edgeDir.X;
                hit = new Point<T>(px, eq.Compute(px));
            }

            T param = line.IsVertical ? hit.Y : hit.X;

            // Check duplicate against the 1-2 hits we already have
            if (has1 && T.Abs(p1.X - hit.X) < dupEps && T.Abs(p1.Y - hit.Y) < dupEps) continue;
            if (has2 && T.Abs(p2.X - hit.X) < dupEps && T.Abs(p2.Y - hit.Y) < dupEps) continue;

            // Insert into the two-slot min-tracker
            if (!has1)
            {
                p1 = hit; par1 = param; has1 = true;
            }
            else if (!has2)
            {
                if (param < par1)
                {
                    p2 = p1; par2 = par1;
                    p1 = hit; par1 = param;
                }
                else
                {
                    p2 = hit; par2 = param;
                }
                has2 = true;
            }
            else if (param < par2)
            {
                if (param < par1)
                {
                    p2 = p1; par2 = par1;
                    p1 = hit; par1 = param;
                }
                else
                {
                    p2 = hit; par2 = param;
                }
            }
        }

        if (!has2) return null;
        return new Segment<T>(p1, p2);
    }

    /// <summary>
    /// Returns the first crossing point of a segment against polygon edges.
    /// Stops at first hit — O(n) time, O(1) heap.
    /// </summary>
    internal static Point<T>? FirstSegmentVsEdges<T>(in Segment<T> segment, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        for (int i = 0; i < n; i++)
        {
            var edge = new Segment<T>(vertices[i], vertices[(i + 1) % n]);
            var hit = Of(segment, edge);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>
    /// Returns the first crossing point of a circle against polygon edges.
    /// Stops at first hit — O(n) time, O(1) heap.
    /// </summary>
    internal static Point<T>? FirstCircleVsEdges<T>(in Circle<T> circle, ReadOnlySpan<Point<T>> vertices)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n = vertices.Length;
        for (int i = 0; i < n; i++)
        {
            var edge = new Segment<T>(vertices[i], vertices[(i + 1) % n]);
            var pts = SegmentCirclePoints(edge, circle);
            if (pts.Length > 0) return pts[0];
        }
        return null;
    }

    /// <summary>
    /// Returns the first crossing point between edges of two polygons.
    /// Stops at first hit — O(n*m) worst case, O(1) heap.
    /// </summary>
    internal static Point<T>? FirstEdgesVsEdges<T>(ReadOnlySpan<Point<T>> v1, ReadOnlySpan<Point<T>> v2)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int n1 = v1.Length;
        int n2 = v2.Length;
        for (int i = 0; i < n1; i++)
        {
            var edge1 = new Segment<T>(v1[i], v1[(i + 1) % n1]);
            for (int j = 0; j < n2; j++)
            {
                var edge2 = new Segment<T>(v2[j], v2[(j + 1) % n2]);
                var hit = Of(edge1, edge2);
                if (hit != null) return hit;
            }
        }
        return null;
    }

    // ════════════════════════════════════════════════
    //  Dedup helper
    // ════════════════════════════════════════════════

    private static bool IsDuplicate<T>(List<Point<T>> points, Point<T> candidate, T eps)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (T.Abs(points[i].X - candidate.X) < eps &&
                T.Abs(points[i].Y - candidate.Y) < eps)
                return true;
        }
        return false;
    }

    private static bool IsDuplicate<T>(List<(Point<T> point, T param)> hits, Point<T> candidate, T eps)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
        IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        for (int i = 0; i < hits.Count; i++)
        {
            if (T.Abs(hits[i].point.X - candidate.X) < eps &&
                T.Abs(hits[i].point.Y - candidate.Y) < eps)
                return true;
        }
        return false;
    }
}
