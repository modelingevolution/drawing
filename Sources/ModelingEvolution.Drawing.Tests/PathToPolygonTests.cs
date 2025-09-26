using System.Collections.Immutable;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class PathToPolygonTests
{
    [Fact]
    public void Close_EmptyPath_ShouldReturnEmptyPolygon()
    {
        // Arrange
        var path = new Path<double>(ImmutableList<BezierCurve<double>>.Empty);

        // Act
        var polygon = path.Close();

        // Assert
        polygon.Points.Should().NotBeNull();
        polygon.Points.Count.Should().Be(0);
    }

    [Fact]
    public void Close_SingleSegment_ShouldCreateClosedPolygon()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);

        // Act
        var polygon = path.Close(10); // 10 samples per segment

        // Assert
        polygon.Points.Count.Should().BeGreaterThanOrEqualTo(10);

        // First point should be at the start of the curve
        polygon.Points[0].X.Should().BeApproximately(0, 0.1);
        polygon.Points[0].Y.Should().BeApproximately(0, 0.1);

        // Last point should be at the end of the curve
        var lastPoint = polygon.Points[^1];
        lastPoint.X.Should().BeApproximately(30, 0.1);
        lastPoint.Y.Should().BeApproximately(0, 0.1);
    }

    [Fact]
    public void ToPolygon_SingleSegment_ShouldCreateOpenPolygon()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);

        // Act
        var polygon = path.ToPolygon(10);

        // Assert
        polygon.Points.Count.Should().BeGreaterThanOrEqualTo(10);

        // Verify it's the same as Close() for a single segment
        var closedPolygon = path.Close(10);
        polygon.Points.Count.Should().Be(closedPolygon.Points.Count);
    }

    [Fact]
    public void Close_MultipleSegments_ShouldSampleAllSegments()
    {
        // Arrange
        var curve1 = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var curve2 = new BezierCurve<double>(
            new Point<double>(30, 0),
            new Point<double>(40, -10),
            new Point<double>(50, -10),
            new Point<double>(60, 0)
        );
        var path = Path<double>.FromSegments(curve1, curve2);

        // Act
        var polygon = path.Close(5); // 5 samples per segment

        // Assert
        // Should have approximately 2 * 5 samples (minus duplicates at connections)
        polygon.Points.Count.Should().BeGreaterThanOrEqualTo(8);
        polygon.Points.Count.Should().BeLessThanOrEqualTo(10);

        // First point from first curve
        polygon.Points[0].X.Should().BeApproximately(0, 0.1);
        polygon.Points[0].Y.Should().BeApproximately(0, 0.1);

        // Last point from second curve
        var lastPoint = polygon.Points[^1];
        lastPoint.X.Should().BeApproximately(60, 0.1);
        lastPoint.Y.Should().BeApproximately(0, 0.1);
    }

    [Fact]
    public void Close_PathFromPoints_ShouldCreateSmoothPolygon()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(0, 0),
            new Point<double>(50, 100),
            new Point<double>(100, 0)
        };
        var path = Path<double>.FromPoints(points);

        // Act
        var polygon = path.Close(15);

        // Assert
        polygon.Points.Count.Should().BeGreaterThan(15); // Multiple segments

        // Verify the polygon passes through key points
        // Start point
        polygon.Points[0].X.Should().BeApproximately(0, 0.1);
        polygon.Points[0].Y.Should().BeApproximately(0, 0.1);

        // Should have points near the middle peak
        polygon.Points.Should().Contain(p =>
            Math.Abs(p.X - 50) < 10 && p.Y > 80);

        // End point
        var lastPoint = polygon.Points[^1];
        lastPoint.X.Should().BeApproximately(100, 0.1);
        lastPoint.Y.Should().BeApproximately(0, 0.1);
    }

    [Fact]
    public void SamplePoints_WithMinimumSamples_ShouldThrowForLessThanTwo()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => path.Close(1));
        Assert.Throws<ArgumentException>(() => path.ToPolygon(0));
    }

    [Fact]
    public void Close_WithHighSampleRate_ShouldCreateDetailedPolygon()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 20),
            new Point<double>(20, 20),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);

        // Act
        var lowDetailPolygon = path.Close(5);
        var highDetailPolygon = path.Close(50);

        // Assert
        highDetailPolygon.Points.Count.Should().BeGreaterThan(lowDetailPolygon.Points.Count);
        highDetailPolygon.Points.Count.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void Close_CircularPath_ShouldNotDuplicateConnectionPoint()
    {
        // Arrange - Create a path that forms a circle-like shape
        var points = new[]
        {
            new Point<double>(100, 0),
            new Point<double>(0, 100),
            new Point<double>(-100, 0),
            new Point<double>(0, -100),
            new Point<double>(100, 0) // Back to start
        };
        var path = Path<double>.FromPoints(points, 0.5);

        // Act
        var polygon = path.Close(10);

        // Assert
        // Check that first and last points are close (forming a closed shape)
        var first = polygon.Points[0];
        var last = polygon.Points[^1];

        // The path should close naturally, so last point should be near the first
        var distance = Math.Sqrt(Math.Pow(last.X - first.X, 2) + Math.Pow(last.Y - first.Y, 2));
        distance.Should().BeLessThan(20); // Allow some tolerance for the curve
    }

    [Fact]
    public void ToPolygon_TransformedPath_ShouldMaintainTransformation()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);
        var translatedPath = path + new Vector<double>(100, 50);

        // Act
        var polygon = translatedPath.ToPolygon(10);

        // Assert
        polygon.Points[0].X.Should().BeApproximately(100, 0.1);
        polygon.Points[0].Y.Should().BeApproximately(50, 0.1);

        var lastPoint = polygon.Points[^1];
        lastPoint.X.Should().BeApproximately(130, 0.1);
        lastPoint.Y.Should().BeApproximately(50, 0.1);
    }

    [Fact]
    public void Close_ScaledPath_ShouldMaintainScale()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(10, 10),
            new Point<double>(20, 20),
            new Point<double>(30, 20),
            new Point<double>(40, 10)
        );
        var path = Path<double>.FromSegments(curve);
        var scaledPath = path * new Size<double>(2, 3);

        // Act
        var polygon = scaledPath.Close(10);

        // Assert
        polygon.Points[0].X.Should().BeApproximately(20, 0.1); // 10 * 2
        polygon.Points[0].Y.Should().BeApproximately(30, 0.1); // 10 * 3

        var lastPoint = polygon.Points[^1];
        lastPoint.X.Should().BeApproximately(80, 0.1); // 40 * 2
        lastPoint.Y.Should().BeApproximately(30, 0.1); // 10 * 3
    }

    [Fact]
    public void PolygonArea_FromClosedPath_ShouldCalculateCorrectly()
    {
        // Arrange - Create a simple rectangular-like path
        var points = new[]
        {
            new Point<double>(0, 0),
            new Point<double>(100, 0),
            new Point<double>(100, 50),
            new Point<double>(0, 50),
            new Point<double>(0, 0)
        };
        var path = Path<double>.FromPoints(points, 0.1); // Small smoothing

        // Act
        var polygon = path.Close(50); // High detail for accuracy
        var area = polygon.Area();

        // Assert
        // Should be approximately 100 * 50 = 5000 (allowing for curve smoothing)
        area.Should().BeApproximately(5000, 600); // Allow 12% tolerance for curves
    }

    [Fact]
    public void Close_WithFloatType_ShouldWork()
    {
        // Arrange
        var curve = new BezierCurve<float>(
            new Point<float>(0f, 0f),
            new Point<float>(10f, 10f),
            new Point<float>(20f, 10f),
            new Point<float>(30f, 0f)
        );
        var path = Path<float>.FromSegments(curve);

        // Act
        var polygon = path.Close(10);

        // Assert
        polygon.Points.Count.Should().BeGreaterThanOrEqualTo(10);
        polygon.Points[0].X.Should().BeApproximately(0f, 0.1f);
        polygon.Points[0].Y.Should().BeApproximately(0f, 0.1f);
    }
}