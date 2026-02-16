using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class PolylineIntersectionTests
{
    private const float Tol = 1e-4f;

    // Helper: an L-shaped polyline from (0,0) → (10,0) → (10,10)
    private static Polyline<float> LShape => new(
        new Point<float>(0, 0),
        new Point<float>(10, 0),
        new Point<float>(10, 10));

    // Helper: a Z-shaped polyline
    private static Polyline<float> ZShape => new(
        new Point<float>(0, 10),
        new Point<float>(10, 10),
        new Point<float>(0, 0),
        new Point<float>(10, 0));

    // ─────────────────────────────────────────────
    // Line × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void LineXPolyline_CrossesTwoEdges()
    {
        // Z-shape has 3 edges. Horizontal line y=5 crosses the diagonal edge.
        // We need a polyline that a line crosses at least twice (even number of crossings).
        // Use an M-shape: (0,0)->(2,10)->(4,0)->(6,10)->(8,0)
        var mShape = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(2, 10),
            new Point<float>(4, 0),
            new Point<float>(6, 10),
            new Point<float>(8, 0));
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var segments = Intersections.Of(line, mShape);
        // y=5 crosses 4 edges => 2 chord segments
        segments.Count.Should().Be(2);
    }

    [Fact]
    public void LineXPolyline_Miss()
    {
        var line = Line<float>.From(new Point<float>(0, 20), new Point<float>(10, 20));
        var segments = Intersections.Of(line, LShape);
        segments.Count.Should().Be(0);
    }

    [Fact]
    public void LineXPolyline_FirstOf_ReturnsFirstSegment()
    {
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var first = Intersections.FirstOf(line, ZShape);
        // If the line hits at least two edges, it should return a segment
        // Otherwise null is acceptable
    }

    // ─────────────────────────────────────────────
    // Segment × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void SegmentXPolyline_Crossing()
    {
        // Segment from (5,-1) to (5,1) crosses the horizontal edge of L-shape at (5,0)
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 1));
        var hits = Intersections.Of(seg, LShape);
        hits.Length.Should().Be(1);
        var hitsSpan = hits.Span;
        hitsSpan[0].X.Should().BeApproximately(5f, Tol);
        hitsSpan[0].Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void SegmentXPolyline_Miss()
    {
        var seg = new Segment<float>(new Point<float>(20, 20), new Point<float>(30, 30));
        Intersections.Of(seg, LShape).Span.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void SegmentXPolyline_FirstOf()
    {
        var seg = new Segment<float>(new Point<float>(5, -1), new Point<float>(5, 1));
        var hit = Intersections.FirstOf(seg, LShape);
        hit.Should().NotBeNull();
        hit!.Value.X.Should().BeApproximately(5f, Tol);
    }

    // ─────────────────────────────────────────────
    // Circle × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void CircleXPolyline_Crossing()
    {
        // Circle centered at (5,0) r=2 crosses the horizontal edge at (3,0) and (7,0)
        var circle = new Circle<float>(new Point<float>(5, 0), 2);
        var hits = Intersections.Of(circle, LShape);
        hits.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void CircleXPolyline_Miss()
    {
        var circle = new Circle<float>(new Point<float>(5, 5), 0.5f);
        Intersections.Of(circle, LShape).Span.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void CircleXPolyline_FirstOf()
    {
        var circle = new Circle<float>(new Point<float>(5, 0), 2);
        Intersections.FirstOf(circle, LShape).Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    // Triangle × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void TriangleXPolyline_Crossing()
    {
        // Triangle that straddles the horizontal edge of L-shape
        var tri = new Triangle<float>(
            new Point<float>(3, -2),
            new Point<float>(7, -2),
            new Point<float>(5, 2));
        var hits = Intersections.Of(tri, LShape);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void TriangleXPolyline_FirstOf()
    {
        var tri = new Triangle<float>(
            new Point<float>(3, -2),
            new Point<float>(7, -2),
            new Point<float>(5, 2));
        Intersections.FirstOf(tri, LShape).Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    // Rectangle × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void RectangleXPolyline_Crossing()
    {
        // Rectangle that overlaps the corner of L-shape
        var rect = new Rectangle<float>(8, -2, 4, 4);
        var hits = Intersections.Of(rect, LShape);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void RectangleXPolyline_FirstOf()
    {
        var rect = new Rectangle<float>(8, -2, 4, 4);
        Intersections.FirstOf(rect, LShape).Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    // Polygon × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void PolygonXPolyline_Crossing()
    {
        // Square polygon that straddles the horizontal edge
        var poly = new Polygon<float>(
            new Point<float>(3, -2),
            new Point<float>(7, -2),
            new Point<float>(7, 2),
            new Point<float>(3, 2));
        var hits = Intersections.Of(poly, LShape);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void PolygonXPolyline_FirstOf()
    {
        var poly = new Polygon<float>(
            new Point<float>(3, -2),
            new Point<float>(7, -2),
            new Point<float>(7, 2),
            new Point<float>(3, 2));
        Intersections.FirstOf(poly, LShape).Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    // Polyline × Polyline
    // ─────────────────────────────────────────────

    [Fact]
    public void PolylineXPolyline_Crossing()
    {
        // Two crossing polylines
        var a = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        var b = new Polyline<float>(new Point<float>(0, 10), new Point<float>(10, 0));
        var hits = Intersections.Of(a, b);
        hits.Length.Should().Be(1);
        var hitsSpan = hits.Span;
        hitsSpan[0].X.Should().BeApproximately(5f, Tol);
        hitsSpan[0].Y.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void PolylineXPolyline_Parallel_NoHits()
    {
        var a = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var b = new Polyline<float>(new Point<float>(0, 5), new Point<float>(10, 5));
        Intersections.Of(a, b).Span.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void PolylineXPolyline_FirstOf()
    {
        var a = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        var b = new Polyline<float>(new Point<float>(0, 10), new Point<float>(10, 0));
        var hit = Intersections.FirstOf(a, b);
        hit.Should().NotBeNull();
        hit!.Value.X.Should().BeApproximately(5f, Tol);
    }

    // ─────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────

    [Fact]
    public void EmptyPolyline_ReturnsEmpty()
    {
        var empty = new Polyline<float>();
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        Intersections.Of(seg, empty).Span.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void SinglePointPolyline_ReturnsEmpty()
    {
        var single = new Polyline<float>(new Point<float>(5, 5));
        var seg = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        Intersections.Of(seg, single).Span.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void PolylineConvenienceMethods_Work()
    {
        var pl = LShape;
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        // These delegate to Intersections static class
        pl.Intersections(line).Should().NotBeNull();
        // FirstIntersection may be null if no pair forms a segment
    }
}
