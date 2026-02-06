using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonLineIntersectionTests
{
    private static Polygon<float> UnitSquare() =>
        new(new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));

    [Fact]
    public void HorizontalLineThroughSquare_ReturnsOneSegment()
    {
        var polygon = UnitSquare();
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        segments[0].Start.X.Should().BeApproximately(0f, 1e-5f);
        segments[0].Start.Y.Should().BeApproximately(5f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(10f, 1e-5f);
        segments[0].End.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void VerticalLineThroughSquare_ReturnsOneSegment()
    {
        var polygon = UnitSquare();
        var line = Line<float>.Vertical(5f);
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        segments[0].Start.X.Should().BeApproximately(5f, 1e-5f);
        segments[0].Start.Y.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(5f, 1e-5f);
        segments[0].End.Y.Should().BeApproximately(10f, 1e-5f);
    }

    [Fact]
    public void DiagonalLineThroughSquare_ReturnsOneSegment()
    {
        var polygon = UnitSquare();
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        // Should intersect at two corners: (0,0) and (10,10)
        segments[0].Start.X.Should().BeApproximately(0f, 1e-5f);
        segments[0].Start.Y.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(10f, 1e-5f);
        segments[0].End.Y.Should().BeApproximately(10f, 1e-5f);
    }

    [Fact]
    public void LineMissingPolygon_ReturnsEmpty()
    {
        var polygon = UnitSquare();
        var line = Line<float>.From(new Point<float>(0, 20), new Point<float>(10, 20));
        var segments = polygon.Intersections(line).ToList();

        segments.Should().BeEmpty();
    }

    [Fact]
    public void LineThroughConcavePolygon_ReturnsMultipleSegments()
    {
        // L-shaped polygon
        var polygon = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 5), new Point<float>(5, 5),
            new Point<float>(5, 10), new Point<float>(0, 10));

        // Horizontal line at y=7 cuts through the narrow part only (x=0 to x=5)
        var line = Line<float>.From(new Point<float>(0, 7), new Point<float>(10, 7));
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        segments[0].Start.X.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void LineThroughConcavePolygon_MultipleIntersectionSegments()
    {
        // U-shaped polygon: cut by horizontal line yields 2 segments
        var polygon = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(8, 10),
            new Point<float>(8, 3), new Point<float>(2, 3),
            new Point<float>(2, 10), new Point<float>(0, 10));

        // Horizontal line at y=5 should intersect the two vertical arms
        var line = Line<float>.From(new Point<float>(-5, 5), new Point<float>(15, 5));
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(2);
        // First segment: left arm (x=0 to x=2)
        segments[0].Start.X.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(2f, 1e-5f);
        // Second segment: right arm (x=8 to x=10)
        segments[1].Start.X.Should().BeApproximately(8f, 1e-5f);
        segments[1].End.X.Should().BeApproximately(10f, 1e-5f);
    }

    [Fact]
    public void FirstIntersection_ReturnsFirstSegment()
    {
        var polygon = UnitSquare();
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        var result = polygon.FirstIntersection(line);

        result.Should().NotBeNull();
        result!.Value.Start.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void FirstIntersection_LineMissing_ReturnsNull()
    {
        var polygon = UnitSquare();
        var line = Line<float>.From(new Point<float>(0, 20), new Point<float>(10, 20));
        var result = polygon.FirstIntersection(line);

        result.Should().BeNull();
    }

    [Fact]
    public void VerticalLineMissingPolygon_ReturnsEmpty()
    {
        var polygon = UnitSquare();
        var line = Line<float>.Vertical(20f);
        var segments = polygon.Intersections(line).ToList();

        segments.Should().BeEmpty();
    }

    [Fact]
    public void VerticalLineAtEdge_ReturnsSegment()
    {
        var polygon = UnitSquare();
        var line = Line<float>.Vertical(0f);
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        segments[0].Start.X.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.X.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void LineThroughVertex_ReturnsSegment()
    {
        // Triangle
        var polygon = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(5, 10));

        // Line through the apex vertex (5,10) going downward
        var line = Line<float>.Vertical(5f);
        var segments = polygon.Intersections(line).ToList();

        segments.Should().HaveCount(1);
        segments[0].Start.Y.Should().BeApproximately(0f, 1e-5f);
        segments[0].End.Y.Should().BeApproximately(10f, 1e-5f);
    }
}
