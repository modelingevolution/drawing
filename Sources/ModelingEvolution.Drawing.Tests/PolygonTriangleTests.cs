using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonTriangleTests
{
    private static Polygon<float> CreateTriangle(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        return new Polygon<float>(new[]
        {
            new Point<float>(x1, y1),
            new Point<float>(x2, y2),
            new Point<float>(x3, y3)
        });
    }

    [Fact]
    public void SeparateTriangles_Should_NotOverlap()
    {
        // Arrange
        var triangle1 = CreateTriangle(0, 0, 1, 0, 0.5f, 1);
        var triangle2 = CreateTriangle(2, 0, 3, 0, 2.5f, 1);

        // Act & Assert
        triangle1.IsOverlapping(triangle2).Should().BeFalse();
    }

    [Fact]
    public void NestedTriangles_Should_Overlap()
    {
        // Arrange
        var outer = CreateTriangle(0, 0, 2, 0, 1, 2);
        var inner = CreateTriangle(0.5f, 0.5f, 1.5f, 0.5f, 1, 1.5f);

        // Act & Assert
        outer.IsOverlapping(inner).Should().BeTrue();
    }

    [Fact]
    public void SeparateTriangles_Should_HaveNoIntersection()
    {
        // Arrange
        var triangle1 = CreateTriangle(0, 0, 1, 0, 0.5f, 1);
        var triangle2 = CreateTriangle(2, 0, 3, 0, 2.5f, 1);

        // Act
        var intersection = triangle1.Intersect(triangle2);

        // Assert
        intersection.Should().BeEmpty();
    }

    [Fact]
    public void IntersectingTriangles_Should_ReturnIntersectionPolygon()
    {
        // Arrange
        var triangle1 = CreateTriangle(0, 0, 2, 0, 1, 2);
        var triangle2 = CreateTriangle(0.5f, 0, 1.5f, 0, 1, 1);

        // Act
        var intersection = triangle1.Intersect(triangle2);

        // Assert
        intersection.Should().HaveCount(1);
        intersection[0].Points.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}