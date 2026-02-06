using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class IntersectionTests
{
    private const float Tol = 1e-4f;

    // ─────────────────────────────────────────────
    // #1  Line × Line
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXLine_Crossing()
    {
        var l1 = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        var l2 = Line<float>.From(new Point<float>(0, 10), new Point<float>(10, 0));
        var pt = Intersections.Of(l1, l2);
        pt.Should().NotBeNull();
        pt!.Value.X.Should().BeApproximately(5f, Tol);
        pt.Value.Y.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void LineXLine_Parallel_ReturnsNull()
    {
        var l1 = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 0));
        var l2 = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        Intersections.Of(l1, l2).Should().BeNull();
    }

    [Fact]
    public void LineXLine_BothVertical_ReturnsNull()
    {
        Intersections.Of(Line<float>.Vertical(3f), Line<float>.Vertical(5f)).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // #2  Line × Segment
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXSegment_Crossing()
    {
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var seg = new Segment<float>(new Point<float>(5, 0), new Point<float>(5, 10));
        var pt = Intersections.Of(line, seg);
        pt.Should().NotBeNull();
        pt!.Value.X.Should().BeApproximately(5f, Tol);
        pt.Value.Y.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void LineXSegment_Miss()
    {
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var seg = new Segment<float>(new Point<float>(5, 6), new Point<float>(5, 10));
        Intersections.Of(line, seg).Should().BeNull();
    }

    [Fact]
    public void LineXSegment_VerticalLine()
    {
        var line = Line<float>.Vertical(5f);
        var seg = new Segment<float>(new Point<float>(0, 3), new Point<float>(10, 3));
        var pt = Intersections.Of(line, seg);
        pt.Should().NotBeNull();
        pt!.Value.X.Should().BeApproximately(5f, Tol);
        pt.Value.Y.Should().BeApproximately(3f, Tol);
    }

    // ─────────────────────────────────────────────
    // #3  Line × Circle — Intersect + TangentPoint
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXCircle_Secant_ReturnsChord()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.From(new Point<float>(-10, 0), new Point<float>(10, 0)); // y=0
        var chord = Intersections.Of(line, circle);
        chord.Should().NotBeNull();
        chord!.Value.Start.X.Should().BeApproximately(5f, Tol);
        chord.Value.End.X.Should().BeApproximately(-5f, Tol);
        chord.Value.Start.Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void LineXCircle_Miss_ReturnsNull()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.From(new Point<float>(-10, 10), new Point<float>(10, 10));
        Intersections.Of(line, circle).Should().BeNull();
    }

    [Fact]
    public void LineXCircle_Tangent_IntersectReturnsNull_TangentPointReturnsPoint()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.From(new Point<float>(-10, 5), new Point<float>(10, 5)); // y=5, tangent at (0,5)
        Intersections.Of(line, circle).Should().BeNull();
        var tp = Intersections.TangentPoint(line, circle);
        tp.Should().NotBeNull();
        tp!.Value.X.Should().BeApproximately(0f, Tol);
        tp.Value.Y.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void LineXCircle_VerticalSecant()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.Vertical(0f);
        var chord = Intersections.Of(line, circle);
        chord.Should().NotBeNull();
        MathF.Abs(chord!.Value.Start.Y - chord.Value.End.Y).Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void LineXCircle_VerticalTangent()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.Vertical(5f);
        Intersections.Of(line, circle).Should().BeNull();
        var tp = Intersections.TangentPoint(line, circle);
        tp.Should().NotBeNull();
        tp!.Value.X.Should().BeApproximately(5f, Tol);
        tp.Value.Y.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // #4  Line × Triangle
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXTriangle_Through()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5)); // y=5
        var seg = Intersections.Of(line, tri);
        seg.Should().NotBeNull();
        seg!.Value.Length.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void LineXTriangle_Miss()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var line = Line<float>.From(new Point<float>(-5, 20), new Point<float>(15, 20));
        Intersections.Of(line, tri).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // #5  Line × Rectangle
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXRectangle_Through()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        var seg = Intersections.Of(line, rect);
        seg.Should().NotBeNull();
        seg!.Value.Start.X.Should().BeApproximately(0f, Tol);
        seg.Value.End.X.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void LineXRectangle_Miss()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var line = Line<float>.From(new Point<float>(-5, 20), new Point<float>(15, 20));
        Intersections.Of(line, rect).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // #6  Line × Polygon (delegates, tested via PolygonLineIntersectionTests)
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXPolygon_HorizontalThroughSquare()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        var segments = Intersections.Of(line, poly);
        segments.Should().HaveCount(1);
    }

    // ─────────────────────────────────────────────
    // #7  Segment × Segment
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXSegment_Crossing()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        var s2 = new Segment<float>(new Point<float>(0, 10), new Point<float>(10, 0));
        var pt = Intersections.Of(s1, s2);
        pt.Should().NotBeNull();
        pt!.Value.X.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void SegmentXSegment_Parallel_ReturnsNull()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var s2 = new Segment<float>(new Point<float>(0, 1), new Point<float>(10, 1));
        Intersections.Of(s1, s2).Should().BeNull();
    }

    [Fact]
    public void SegmentXSegment_NoOverlap_ReturnsNull()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(1, 0));
        var s2 = new Segment<float>(new Point<float>(5, 5), new Point<float>(6, 5));
        Intersections.Of(s1, s2).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // #8  Segment × Circle — Intersect + TangentPoint
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXCircle_Secant_ReturnsChord()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 3f);
        var seg = new Segment<float>(new Point<float>(0, 5), new Point<float>(10, 5)); // through center
        var chord = Intersections.Of(seg, circle);
        chord.Should().NotBeNull();
        chord!.Value.Length.Should().BeApproximately(6f, Tol); // diameter
    }

    [Fact]
    public void SegmentXCircle_Miss_ReturnsNull()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 3f);
        var seg = new Segment<float>(new Point<float>(0, 20), new Point<float>(10, 20));
        Intersections.Of(seg, circle).Should().BeNull();
    }

    [Fact]
    public void SegmentXCircle_TooShort_ReturnsNull()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 3f);
        var seg = new Segment<float>(new Point<float>(0, 5), new Point<float>(1, 5)); // ends before circle
        Intersections.Of(seg, circle).Should().BeNull();
    }

    [Fact]
    public void SegmentXCircle_Tangent()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 5f);
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0)); // y=0, tangent at (5,0)
        Intersections.Of(seg, circle).Should().BeNull();
        var tp = Intersections.TangentPoint(seg, circle);
        tp.Should().NotBeNull();
        tp!.Value.X.Should().BeApproximately(5f, Tol);
        tp.Value.Y.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // #9  Segment × Triangle
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXTriangle_Crossing()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        var pts = Intersections.Of(seg, tri);
        pts.Should().HaveCount(2);
    }

    [Fact]
    public void SegmentXTriangle_Miss()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var seg = new Segment<float>(new Point<float>(20, 0), new Point<float>(20, 10));
        Intersections.Of(seg, tri).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #10  Segment × Rectangle
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXRectangle_Through()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        var pts = Intersections.Of(seg, rect);
        pts.Should().HaveCount(2);
    }

    [Fact]
    public void SegmentXRectangle_Miss()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var seg = new Segment<float>(new Point<float>(20, 0), new Point<float>(20, 10));
        Intersections.Of(seg, rect).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #11  Segment × Polygon
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXPolygon_Through()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        var pts = Intersections.Of(seg, poly);
        pts.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────
    // #12  Circle × Circle — Intersect + TangentPoint
    // ─────────────────────────────────────────────

    [Fact]
    public void CircleXCircle_Overlapping_ReturnsChord()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(6, 0), 5f);
        var chord = Intersections.Of(c1, c2);
        chord.Should().NotBeNull();
        chord!.Value.Length.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void CircleXCircle_TooFarApart_ReturnsNull()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(20, 0), 5f);
        Intersections.Of(c1, c2).Should().BeNull();
    }

    [Fact]
    public void CircleXCircle_OneInsideOther_ReturnsNull()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 10f);
        var c2 = new Circle<float>(new Point<float>(1, 0), 2f);
        Intersections.Of(c1, c2).Should().BeNull();
    }

    [Fact]
    public void CircleXCircle_ExternallyTangent()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(10, 0), 5f);
        Intersections.Of(c1, c2).Should().BeNull(); // tangent → no chord
        var tp = Intersections.TangentPoint(c1, c2);
        tp.Should().NotBeNull();
        tp!.Value.X.Should().BeApproximately(5f, Tol);
        tp.Value.Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void CircleXCircle_InternallyTangent()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 10f);
        var c2 = new Circle<float>(new Point<float>(5, 0), 5f);
        Intersections.Of(c1, c2).Should().BeNull();
        var tp = Intersections.TangentPoint(c1, c2);
        tp.Should().NotBeNull();
        tp!.Value.X.Should().BeApproximately(10f, Tol);
        tp.Value.Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void CircleXCircle_Concentric_ReturnsNull()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(0, 0), 3f);
        Intersections.Of(c1, c2).Should().BeNull();
        Intersections.TangentPoint(c1, c2).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // #13  Circle × Triangle
    // ─────────────────────────────────────────────

    [Fact]
    public void CircleXTriangle_Crossing()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 4f);
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var pts = Intersections.Of(circle, tri);
        pts.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void CircleXTriangle_NoContact()
    {
        var circle = new Circle<float>(new Point<float>(50, 50), 1f);
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        Intersections.Of(circle, tri).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #14  Circle × Rectangle
    // ─────────────────────────────────────────────

    [Fact]
    public void CircleXRectangle_Crossing()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 6f);
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var pts = Intersections.Of(circle, rect);
        pts.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void CircleXRectangle_NoContact()
    {
        var circle = new Circle<float>(new Point<float>(50, 50), 1f);
        var rect = new Rectangle<float>(0, 0, 10, 10);
        Intersections.Of(circle, rect).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #15  Circle × Polygon
    // ─────────────────────────────────────────────

    [Fact]
    public void CircleXPolygon_Crossing()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 6f);
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var pts = Intersections.Of(circle, poly);
        pts.Should().HaveCountGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // #16  Triangle × Triangle
    // ─────────────────────────────────────────────

    [Fact]
    public void TriangleXTriangle_Overlapping()
    {
        var t1 = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var t2 = new Triangle<float>(new Point<float>(0, 5), new Point<float>(10, 5), new Point<float>(5, -5));
        var pts = Intersections.Of(t1, t2);
        pts.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void TriangleXTriangle_NoOverlap()
    {
        var t1 = new Triangle<float>(new Point<float>(0, 0), new Point<float>(3, 0), new Point<float>(1.5f, 3));
        var t2 = new Triangle<float>(new Point<float>(10, 10), new Point<float>(13, 10), new Point<float>(11.5f, 13));
        Intersections.Of(t1, t2).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #17  Triangle × Rectangle
    // ─────────────────────────────────────────────

    [Fact]
    public void TriangleXRectangle_Overlapping()
    {
        var tri = new Triangle<float>(new Point<float>(5, -1), new Point<float>(5, 11), new Point<float>(15, 5));
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var pts = Intersections.Of(tri, rect);
        pts.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void TriangleXRectangle_NoOverlap()
    {
        var tri = new Triangle<float>(new Point<float>(20, 20), new Point<float>(25, 20), new Point<float>(22, 25));
        var rect = new Rectangle<float>(0, 0, 10, 10);
        Intersections.Of(tri, rect).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #18  Triangle × Polygon
    // ─────────────────────────────────────────────

    [Fact]
    public void TriangleXPolygon_Overlapping()
    {
        var tri = new Triangle<float>(new Point<float>(5, -1), new Point<float>(5, 11), new Point<float>(15, 5));
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var pts = Intersections.Of(tri, poly);
        pts.Should().HaveCountGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // #19  Rectangle × Rectangle
    // ─────────────────────────────────────────────

    [Fact]
    public void RectangleXRectangle_Overlapping()
    {
        var r1 = new Rectangle<float>(0, 0, 10, 10);
        var r2 = new Rectangle<float>(5, 5, 10, 10);
        var pts = Intersections.Of(r1, r2);
        pts.Should().HaveCount(2); // two edge crossing points
    }

    [Fact]
    public void RectangleXRectangle_NoOverlap()
    {
        var r1 = new Rectangle<float>(0, 0, 5, 5);
        var r2 = new Rectangle<float>(10, 10, 5, 5);
        Intersections.Of(r1, r2).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // #20  Rectangle × Polygon
    // ─────────────────────────────────────────────

    [Fact]
    public void RectangleXPolygon_Overlapping()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var poly = new Polygon<float>(
            new Point<float>(5, -1), new Point<float>(15, 5),
            new Point<float>(5, 11));
        var pts = Intersections.Of(rect, poly);
        pts.Should().HaveCountGreaterThan(0);
    }

    // ─────────────────────────────────────────────
    // #21  Polygon × Polygon
    // ─────────────────────────────────────────────

    [Fact]
    public void PolygonXPolygon_Overlapping()
    {
        var p1 = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var p2 = new Polygon<float>(
            new Point<float>(5, 5), new Point<float>(15, 5),
            new Point<float>(15, 15), new Point<float>(5, 15));
        var pts = Intersections.Of(p1, p2);
        pts.Should().HaveCount(2);
    }

    [Fact]
    public void PolygonXPolygon_NoOverlap()
    {
        var p1 = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(5, 0),
            new Point<float>(5, 5), new Point<float>(0, 5));
        var p2 = new Polygon<float>(
            new Point<float>(10, 10), new Point<float>(15, 10),
            new Point<float>(15, 15), new Point<float>(10, 15));
        Intersections.Of(p1, p2).Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // Instance method delegates (spot checks)
    // ─────────────────────────────────────────────

    [Fact]
    public void Circle_Intersect_Line_DelegatesToIntersections()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.From(new Point<float>(-10, 0), new Point<float>(10, 0));
        var chord = circle.Intersect(line);
        chord.Should().NotBeNull();
    }

    [Fact]
    public void Circle_TangentPoint_Line_DelegatesToIntersections()
    {
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        var line = Line<float>.From(new Point<float>(-10, 5), new Point<float>(10, 5));
        circle.TangentPoint(line).Should().NotBeNull();
    }

    [Fact]
    public void Line_Intersect_Circle_DelegatesToIntersections()
    {
        var line = Line<float>.From(new Point<float>(-10, 0), new Point<float>(10, 0));
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        line.Intersect(circle).Should().NotBeNull();
    }

    [Fact]
    public void Line_TangentPoint_Circle_DelegatesToIntersections()
    {
        var line = Line<float>.From(new Point<float>(-10, 5), new Point<float>(10, 5));
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        line.TangentPoint(circle).Should().NotBeNull();
    }

    [Fact]
    public void Segment_Intersect_Circle_DelegatesToIntersections()
    {
        var seg = new Segment<float>(new Point<float>(-10, 0), new Point<float>(10, 0));
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        seg.Intersect(circle).Should().NotBeNull();
    }

    [Fact]
    public void Segment_TangentPoint_Circle_DelegatesToIntersections()
    {
        var seg = new Segment<float>(new Point<float>(-10, 5), new Point<float>(10, 5));
        var circle = new Circle<float>(new Point<float>(0, 0), 5f);
        seg.TangentPoint(circle).Should().NotBeNull();
    }

    [Fact]
    public void Circle_Intersect_Circle_DelegatesToIntersections()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(6, 0), 5f);
        c1.Intersect(c2).Should().NotBeNull();
    }

    [Fact]
    public void Circle_TangentPoint_Circle_DelegatesToIntersections()
    {
        var c1 = new Circle<float>(new Point<float>(0, 0), 5f);
        var c2 = new Circle<float>(new Point<float>(10, 0), 5f);
        c1.TangentPoint(c2).Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    // FirstOf — zero-alloc first-hit variants
    // ─────────────────────────────────────────────

    [Fact]
    public void FirstOf_LineXPolygon_MatchesFullResult()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));

        var full = Intersections.Of(line, poly);
        var first = Intersections.FirstOf(line, poly);

        full.Should().HaveCountGreaterThan(0);
        first.Should().NotBeNull();
        first!.Value.Start.X.Should().BeApproximately(full[0].Start.X, Tol);
        first.Value.End.X.Should().BeApproximately(full[0].End.X, Tol);
    }

    [Fact]
    public void FirstOf_LineXPolygon_Miss_ReturnsNull()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var line = Line<float>.From(new Point<float>(-5, 20), new Point<float>(15, 20));
        Intersections.FirstOf(line, poly).Should().BeNull();
    }

    [Fact]
    public void FirstOf_LineXTriangle_Hit()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        var first = Intersections.FirstOf(line, tri);
        first.Should().NotBeNull();
        first!.Value.Length.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void FirstOf_LineXTriangle_Miss_ReturnsNull()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var line = Line<float>.From(new Point<float>(-5, 20), new Point<float>(15, 20));
        Intersections.FirstOf(line, tri).Should().BeNull();
    }

    [Fact]
    public void FirstOf_LineXRectangle_Hit()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        var first = Intersections.FirstOf(line, rect);
        first.Should().NotBeNull();
        first!.Value.Start.X.Should().BeApproximately(0f, Tol);
        first.Value.End.X.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void FirstOf_LineXRectangle_Miss_ReturnsNull()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var line = Line<float>.From(new Point<float>(-5, 20), new Point<float>(15, 20));
        Intersections.FirstOf(line, rect).Should().BeNull();
    }

    [Fact]
    public void FirstOf_SegmentXTriangle_Hit()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        var first = Intersections.FirstOf(seg, tri);
        first.Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_SegmentXTriangle_Miss_ReturnsNull()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var seg = new Segment<float>(new Point<float>(20, 0), new Point<float>(20, 10));
        Intersections.FirstOf(seg, tri).Should().BeNull();
    }

    [Fact]
    public void FirstOf_SegmentXRectangle_Hit()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        Intersections.FirstOf(seg, rect).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_SegmentXPolygon_Hit()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 11));
        Intersections.FirstOf(seg, poly).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_CircleXTriangle_Hit()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 4f);
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        Intersections.FirstOf(circle, tri).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_CircleXTriangle_Miss_ReturnsNull()
    {
        var circle = new Circle<float>(new Point<float>(50, 50), 1f);
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        Intersections.FirstOf(circle, tri).Should().BeNull();
    }

    [Fact]
    public void FirstOf_CircleXRectangle_Hit()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 6f);
        var rect = new Rectangle<float>(0, 0, 10, 10);
        Intersections.FirstOf(circle, rect).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_CircleXPolygon_Hit()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 6f);
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        Intersections.FirstOf(circle, poly).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_TriangleXTriangle_Hit()
    {
        var t1 = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var t2 = new Triangle<float>(new Point<float>(0, 5), new Point<float>(10, 5), new Point<float>(5, -5));
        Intersections.FirstOf(t1, t2).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_TriangleXTriangle_Miss_ReturnsNull()
    {
        var t1 = new Triangle<float>(new Point<float>(0, 0), new Point<float>(3, 0), new Point<float>(1.5f, 3));
        var t2 = new Triangle<float>(new Point<float>(10, 10), new Point<float>(13, 10), new Point<float>(11.5f, 13));
        Intersections.FirstOf(t1, t2).Should().BeNull();
    }

    [Fact]
    public void FirstOf_TriangleXRectangle_Hit()
    {
        var tri = new Triangle<float>(new Point<float>(5, -1), new Point<float>(5, 11), new Point<float>(15, 5));
        var rect = new Rectangle<float>(0, 0, 10, 10);
        Intersections.FirstOf(tri, rect).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_TriangleXPolygon_Hit()
    {
        var tri = new Triangle<float>(new Point<float>(5, -1), new Point<float>(5, 11), new Point<float>(15, 5));
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        Intersections.FirstOf(tri, poly).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_RectangleXRectangle_Hit()
    {
        var r1 = new Rectangle<float>(0, 0, 10, 10);
        var r2 = new Rectangle<float>(5, 5, 10, 10);
        Intersections.FirstOf(r1, r2).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_RectangleXRectangle_Miss_ReturnsNull()
    {
        var r1 = new Rectangle<float>(0, 0, 5, 5);
        var r2 = new Rectangle<float>(10, 10, 5, 5);
        Intersections.FirstOf(r1, r2).Should().BeNull();
    }

    [Fact]
    public void FirstOf_RectangleXPolygon_Hit()
    {
        var rect = new Rectangle<float>(0, 0, 10, 10);
        var poly = new Polygon<float>(
            new Point<float>(5, -1), new Point<float>(15, 5),
            new Point<float>(5, 11));
        Intersections.FirstOf(rect, poly).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_PolygonXPolygon_Hit()
    {
        var p1 = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var p2 = new Polygon<float>(
            new Point<float>(5, 5), new Point<float>(15, 5),
            new Point<float>(15, 15), new Point<float>(5, 15));
        Intersections.FirstOf(p1, p2).Should().NotBeNull();
    }

    [Fact]
    public void FirstOf_PolygonXPolygon_Miss_ReturnsNull()
    {
        var p1 = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(5, 0),
            new Point<float>(5, 5), new Point<float>(0, 5));
        var p2 = new Polygon<float>(
            new Point<float>(10, 10), new Point<float>(15, 10),
            new Point<float>(15, 15), new Point<float>(10, 15));
        Intersections.FirstOf(p1, p2).Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // Instance method delegates for FirstIntersection
    // ─────────────────────────────────────────────

    [Fact]
    public void Polygon_FirstIntersection_DelegatesToIntersections()
    {
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        poly.FirstIntersection(line).Should().NotBeNull();
    }

    [Fact]
    public void Line_FirstIntersection_Triangle_DelegatesToIntersections()
    {
        var tri = new Triangle<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        line.FirstIntersection(tri).Should().NotBeNull();
    }
}
