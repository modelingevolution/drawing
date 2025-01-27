using System;

using System.Drawing;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ModelingEvolution.Drawing;
using Xunit;


using Polygon = ModelingEvolution.Drawing.Polygon<float>;
using PointF = ModelingEvolution.Drawing.Point<float>;
using Xunit;
using FluentAssertions;


public class PolygonTest
{

    [Fact]
    public void Union_WithHoleFilling_ShouldProduceSinglePolygon()
    {
        // Arrange
        // Create a C-shaped polygon
        var cShape = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0),   // Top-left
            new(100, 0), // Top-right
            new(100, 100), // Bottom-right
            new(80, 100),  // Inner bottom-right
            new(80, 20),   // Inner top-right
            new(20, 20),   // Inner top-left
            new(20, 100),  // Inner bottom-left
            new(0, 100),   // Bottom-left
        });

        // Create a rectangle that bridges the gap
        var bridge = new Polygon<float>(new List<Point<float>>
        {
            new(10, 40),
            new(90, 40),
            new(90, 60),
            new(10, 60),
        });

        // Act
        // Try union without hole filling
        var resultWithHoles = Polygon<float>.Union([cShape,bridge]);
        var isOberlapping = cShape.IsOverlapping(bridge);
        // Try union with hole filling
        var withoutHoles = Polygon<float>.Union([cShape, bridge], true);

        // Assert
        // Without hole filling, we expect multiple polygons
        Assert.True(resultWithHoles.Count > 1,
            "Union without hole filling should produce multiple polygons");
        Assert.True(isOberlapping);
        // With hole filling, we expect a single polygon
        Assert.True(withoutHoles.Count == 1,
            "Union with hole filling should produce a single polygon");

        // Optional: Verify the area is larger with hole filling
        float areaWithoutHoles = resultWithHoles[0].Area() - resultWithHoles[1].Area();
        float areaWithHoles = withoutHoles[0].Area();
        Assert.True(areaWithHoles > areaWithoutHoles,
            "Area with hole filling should be larger than without");
    }


    [Fact]
    public void Indexer()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var polygon2 = polygon;
        polygon2[0] = new Point<float>(1, 1);

        polygon[0].Should().Be(polygon2[0]);
    }

    [Fact]
    public void Area_ValidPolygon_ReturnsCorrectArea()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var area = polygon.Area();
        Assert.Equal(6, area);
    }

    [Fact]
    public void Area_LessThanThreePoints_ThrowsArgumentException()
    {
        var points = new float[] { 0, 0, 4, 0 };
        var polygon = new Polygon(points);
        Assert.Throws<ArgumentException>(() => polygon.Area());
    }

    [Fact]
    public void Render_ValidDimensions_ReturnsCorrectPoints()
    {
        var points = new float[] { 0, 0, 1, 0, 1, 1 };
        var polygon = new Polygon(points);
        var renderedPoints = (polygon * new Size<float>(100, 100)).Points.Select(x => (Point)x).ToList();
        Assert.Equal(new Point(0, 0), renderedPoints[0]);
        Assert.Equal(new Point(100, 0), renderedPoints[1]);
        Assert.Equal(new Point(100, 100), renderedPoints[2]);
    }

    [Fact]
    public void OperatorDivide_ValidSize_ReturnsScaledPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var size = new Size<float>(2, 2);
        var scaledPolygon = polygon / size;
        Assert.Equal(new Point<float>(0, 0), scaledPolygon[0]);
        Assert.Equal(new Point<float>(2, 0), scaledPolygon[1]);
        Assert.Equal(new Point<float>(2, 1.5f), scaledPolygon[2]);
    }
    [Fact]
    public void Intersect_NoIntersection_ReturnsEmptyPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(5, 5, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Empty(intersectedPolygon.Points);
    }
    [Fact]
    public void Intersect_RectangleFullyInsidePolygon_ReturnsRectangleAsPolygon()
    {
        var points = new float[] { 0, 0, 6, 0, 6, 6, 0, 6 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(2, 2, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon.Points);
    }
    [Fact]
    public void Intersect_PolygonFullyInsideRectangle_ReturnsOriginalPolygon()
    {
        var points = new float[] { 2, 2, 4, 2, 4, 4, 2, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(0, 0, 6, 6);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon.Points);
    }
    [Fact]
    public void Intersect_RectangleTouchesPolygonEdge_ReturnsCorrectIntersection()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(3, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(4, 1), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 3), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(3, 1), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(3, 3), intersectedPolygon.Points);
    }

    [Fact]
    public void Intersect_ValidRectangle_ReturnsIntersectedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(1, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
    }

    [Fact]
    public void OperatorSubtract_ValidVector_ReturnsTranslatedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var vector = new ModelingEvolution.Drawing.Vector<float>(1, 1);
        var translatedPolygon = polygon - vector;
        Assert.Equal(new Point<float>(-1, -1), translatedPolygon[0]);
        Assert.Equal(new Point<float>(3, -1), translatedPolygon[1]);
        Assert.Equal(new Point<float>(3, 2), translatedPolygon[2]);
    }

    [Fact]
    public void OperatorAdd_ValidVector_ReturnsTranslatedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var vector = new ModelingEvolution.Drawing.Vector<float>(1, 1);
        var translatedPolygon = polygon + vector;
        Assert.Equal(new Point<float>(1, 1), translatedPolygon[0]);
        Assert.Equal(new Point<float>(5, 1), translatedPolygon[1]);
        Assert.Equal(new Point<float>(5, 4), translatedPolygon[2]);
    }

    [Fact]
    public void Indexer_ValidIndex_ReturnsCorrectPoint()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        Assert.Equal(new Point<float>(4, 0), polygon[1]);
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfPoints()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        Assert.Equal(3, polygon.Count);
    }

    [Fact]
    public void GetEnumerator_ReturnsAllPoints()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var enumerator = polygon.Points.GetEnumerator();
        int count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }

        Assert.Equal(3, count);
    }

    [Fact]
    public void Union_TouchingPolygons_ReturnsSinglePolygon()
    {
        // Arrange
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(0, 0),
            new Point<float>(1, 0),
            new Point<float>(1, 1),
            new Point<float>(0, 1)
        });

        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(1, 0),
            new Point<float>(2, 0),
            new Point<float>(2, 1),
            new Point<float>(1, 1)
        });

        // Act
        var result = polygon1 | polygon2;

        // Assert
        Assert.Equal(4, result.Points.Count); // Should be a single rectangle
        Assert.Equal(2.0f, result.Area(), 0.0001f); // Area should be 2 units
    }
    [Fact]
    public void Union_DisconnectedPolygons_ThrowsInvalidOperationException()
    {
        // Arrange
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(0, 0),
            new Point<float>(1, 0),
            new Point<float>(1, 1),
            new Point<float>(0, 1)
        });

        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(3, 3),
            new Point<float>(4, 3),
            new Point<float>(4, 4),
            new Point<float>(3, 4)
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => polygon1 | polygon2);
    }
    [Fact]
    public void Intersection_TwoOverlappingSquares_ReturnsSinglePolygon()
    {
        // Arrange
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(0, 0),
            new Point<float>(2, 0),
            new Point<float>(2, 2),
            new Point<float>(0, 2)
        });

        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(1, 1),
            new Point<float>(3, 1),
            new Point<float>(3, 3),
            new Point<float>(1, 3)
        });

        // Act
        var result = polygon1 & polygon2;

        // Assert
        Assert.Equal(4, result.Points.Count); // Union of overlapping squares should have 8 vertices
        var exPoints = new PointF[]
        {
            new PointF(2, 2),
            new PointF(1, 2),
            new PointF(1, 1),
            new PointF(2, 1)
        };
        for (int i = 0; i < result.Points.Count; i++)
        {
            var expectedPoint = exPoints[i];
            Assert.True(result.Points[i].Equals(expectedPoint));
        }
        Assert.True(polygon1.IsOverlapping(polygon2));
    }

    [Fact]
    public void Union_TwoOverlappingSquares_ReturnsSinglePolygon()
    {
        // Arrange
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(0, 0),
            new Point<float>(2, 0),
            new Point<float>(2, 2),
            new Point<float>(0, 2)
        });

        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(1, 1),
            new Point<float>(3, 1),
            new Point<float>(3, 3),
            new Point<float>(1, 3)
        });

        // Act
        var result = polygon1 | polygon2;

        // Assert
        Assert.Equal(8, result.Points.Count); // Union of overlapping squares should have 8 vertices
        Assert.True(result.Area() > polygon1.Area()); // Union should be larger than either input
        Assert.True(result.Area() > polygon2.Area());
    }

    [Fact]
    public void Intersection_PartiallyOverlappingTriangles_ReturnsSinglePolygon()
    {
        // Arrange
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(0, 0),
            new Point<float>(2, 0),
            new Point<float>(1, 2)
        });

        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new Point<float>(1, 0),
            new Point<float>(3, 0),
            new Point<float>(2, 2)
        });

        // Act
        var result = polygon1 & polygon2;

        // Assert
        Assert.True(result.Points.Count > 2); // Should be a polygon
        Assert.True(result.Area() > 0); // Should have positive area
    }
}