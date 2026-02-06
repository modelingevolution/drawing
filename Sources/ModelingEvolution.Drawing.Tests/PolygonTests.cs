using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonTests
{
    private static Polygon<double> Square => new(
        new Point<double>(0, 0),
        new Point<double>(10, 0),
        new Point<double>(10, 10),
        new Point<double>(0, 10));

    [Fact]
    public void ContainsPoint_Inside_ReturnsTrue()
    {
        Square.Contains(new Point<double>(5, 5)).Should().BeTrue();
    }

    [Fact]
    public void ContainsPoint_Outside_ReturnsFalse()
    {
        Square.Contains(new Point<double>(15, 5)).Should().BeFalse();
    }

    [Fact]
    public void ContainsPoint_ConcavePolygon()
    {
        // L-shaped polygon
        var lShape = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 5),
            new Point<double>(5, 5),
            new Point<double>(5, 10),
            new Point<double>(0, 10));
        lShape.Contains(new Point<double>(2, 2)).Should().BeTrue();
        lShape.Contains(new Point<double>(7, 7)).Should().BeFalse();
    }

    [Fact]
    public void Perimeter_Square()
    {
        Square.Perimeter().Should().BeApproximately(40, 1e-9);
    }

    [Fact]
    public void Centroid_Square()
    {
        var c = Square.Centroid();
        c.X.Should().BeApproximately(5, 1e-9);
        c.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void IsConvex_Square_ReturnsTrue()
    {
        Square.IsConvex().Should().BeTrue();
    }

    [Fact]
    public void IsConvex_LShape_ReturnsFalse()
    {
        var lShape = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 5),
            new Point<double>(5, 5),
            new Point<double>(5, 10),
            new Point<double>(0, 10));
        lShape.IsConvex().Should().BeFalse();
    }

    [Fact]
    public void Edges_Square_ReturnsFourEdges()
    {
        var edges = Square.Edges();
        edges.Length.Should().Be(4);
        edges[0].Length.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void ConvexHull_AlreadyConvex_ReturnsSame()
    {
        var hull = Square.ConvexHull();
        hull.Count.Should().Be(4);
        hull.Area().Should().BeApproximately(100, 1e-9);
    }

    [Fact]
    public void ConvexHull_WithInternalPoint_RemovesIt()
    {
        var poly = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(5, 5),
            new Point<double>(10, 10),
            new Point<double>(0, 10));
        var hull = poly.ConvexHull();
        hull.Count.Should().Be(4);
        hull.Area().Should().BeApproximately(100, 1e-9);
    }
}
