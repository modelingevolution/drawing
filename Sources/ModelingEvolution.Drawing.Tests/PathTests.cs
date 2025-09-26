using System.Collections.Immutable;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class PathTests
{
    [Fact]
    public void EmptyPath_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var path = new Path<double>(ImmutableList<BezierCurve<double>>.Empty);

        // Assert
        path.IsEmpty.Should().BeTrue();
        path.Count.Should().Be(0);
        path.Segments.Should().BeEmpty();
        path.ToString().Should().BeEmpty();
    }

    [Fact]
    public void FromSegments_WithMultipleCurves_ShouldCreateValidPath()
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

        // Act
        var path = Path<double>.FromSegments(curve1, curve2);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(2);
        path.Segments.Should().HaveCount(2);
        path.Segments.Should().ContainInOrder(curve1, curve2);
    }

    [Fact]
    public void FromPoints_WithThreePoints_ShouldCreateSmoothPath()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(0, 0),
            new Point<double>(50, 100),
            new Point<double>(100, 0)
        };

        // Act
        var path = Path<double>.FromPoints(points);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(2); // Two segments for three points

        // Verify path passes through all points
        path.Segments[0].Start.Should().Be(points[0]);
        path.Segments[0].End.Should().Be(points[1]);
        path.Segments[1].Start.Should().Be(points[1]);
        path.Segments[1].End.Should().Be(points[2]);
    }

    [Fact]
    public void FromPoints_WithCustomSmoothingCoefficient_ShouldCreatePath()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(0, 0),
            new Point<double>(50, 50),
            new Point<double>(100, 0),
            new Point<double>(150, 50)
        };
        var smoothingCoefficient = 0.3;

        // Act
        var path = Path<double>.FromPoints(points, smoothingCoefficient);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(3); // Three segments for four points
    }

    [Fact]
    public void FromPoints_WithSinglePoint_ShouldReturnEmptyPath()
    {
        // Arrange
        var points = new[] { new Point<double>(10, 20) };

        // Act
        var path = Path<double>.FromPoints(points);

        // Assert
        path.IsEmpty.Should().BeTrue();
        path.Count.Should().Be(0);
    }

    [Fact]
    public void ToString_WithSingleCurve_ShouldGenerateValidSvgPath()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(10, 20),
            new Point<double>(30, 40),
            new Point<double>(50, 40),
            new Point<double>(70, 20)
        );
        var path = Path<double>.FromSegments(curve);

        // Act
        var svgPath = path.ToString();

        // Assert
        svgPath.Should().StartWith("M 10 20");
        svgPath.Should().Contain("C 30 40, 50 40, 70 20");
    }

    [Fact]
    public void ToString_WithMultipleCurves_ShouldGenerateValidSvgPath()
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
        var svgPath = path.ToString();

        // Assert
        svgPath.Should().StartWith("M 0 0");
        svgPath.Should().Contain("C 10 10, 20 10, 30 0");
        svgPath.Should().Contain("C 40 -10, 50 -10, 60 0");
    }

    [Fact]
    public void Parse_WithValidSvgPath_ShouldCreatePath()
    {
        // Arrange
        var svgPath = "M 10 20 C 30 40, 50 40, 70 20";

        // Act
        var path = Path<double>.Parse(svgPath, null);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(1);

        var segment = path.Segments[0];
        segment.Start.X.Should().BeApproximately(10, 0.001);
        segment.Start.Y.Should().BeApproximately(20, 0.001);
        segment.C0.X.Should().BeApproximately(30, 0.001);
        segment.C0.Y.Should().BeApproximately(40, 0.001);
        segment.C1.X.Should().BeApproximately(50, 0.001);
        segment.C1.Y.Should().BeApproximately(40, 0.001);
        segment.End.X.Should().BeApproximately(70, 0.001);
        segment.End.Y.Should().BeApproximately(20, 0.001);
    }

    [Fact]
    public void Parse_WithLineCommands_ShouldConvertToBezier()
    {
        // Arrange
        var svgPath = "M 0 0 L 100 100";

        // Act
        var path = Path<double>.Parse(svgPath, null);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(1);

        var segment = path.Segments[0];
        segment.Start.X.Should().BeApproximately(0, 0.001);
        segment.Start.Y.Should().BeApproximately(0, 0.001);
        segment.End.X.Should().BeApproximately(100, 0.001);
        segment.End.Y.Should().BeApproximately(100, 0.001);
    }

    [Fact]
    public void Parse_WithClosePathCommand_ShouldClosePath()
    {
        // Arrange
        var svgPath = "M 0 0 L 100 0 L 100 100 Z";

        // Act
        var path = Path<double>.Parse(svgPath, null);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(3); // Two lines plus closing segment

        // Verify the path closes back to start
        var lastSegment = path.Segments[2];
        lastSegment.End.X.Should().BeApproximately(0, 0.001);
        lastSegment.End.Y.Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void TryParse_WithValidPath_ShouldReturnTrue()
    {
        // Arrange
        var svgPath = "M 10 20 C 30 40, 50 40, 70 20";

        // Act
        var result = Path<double>.TryParse(svgPath, null, out var path);

        // Assert
        result.Should().BeTrue();
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(1);
    }

    [Fact]
    public void TryParse_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        var invalidPath = "M 10 20 C invalid data";

        // Act
        var result = Path<double>.TryParse(invalidPath, null, out var path);

        // Assert
        result.Should().BeFalse();
        path.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TryParse_WithEmptyString_ShouldReturnEmptyPath()
    {
        // Arrange
        var emptyPath = "";

        // Act
        var result = Path<double>.TryParse(emptyPath, null, out var path);

        // Assert
        result.Should().BeTrue();
        path.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithInvalidPath_ShouldThrowFormatException()
    {
        // Arrange
        var invalidPath = "M 10 20 C invalid";

        // Act & Assert
        Assert.Throws<FormatException>(() => Path<double>.Parse(invalidPath, null));
    }

    [Fact]
    public void RoundTrip_PathToStringAndBack_ShouldPreserveData()
    {
        // Arrange
        var originalCurve = new BezierCurve<double>(
            new Point<double>(10.5, 20.7),
            new Point<double>(30.2, 40.9),
            new Point<double>(50.1, 40.3),
            new Point<double>(70.8, 20.5)
        );
        var originalPath = Path<double>.FromSegments(originalCurve);

        // Act
        var svgString = originalPath.ToString();
        var parsedPath = Path<double>.Parse(svgString, null);

        // Assert
        parsedPath.Count.Should().Be(originalPath.Count);

        var parsedSegment = parsedPath.Segments[0];
        var originalSegment = originalPath.Segments[0];

        parsedSegment.Start.X.Should().BeApproximately(originalSegment.Start.X, 0.001);
        parsedSegment.Start.Y.Should().BeApproximately(originalSegment.Start.Y, 0.001);
        parsedSegment.C0.X.Should().BeApproximately(originalSegment.C0.X, 0.001);
        parsedSegment.C0.Y.Should().BeApproximately(originalSegment.C0.Y, 0.001);
        parsedSegment.C1.X.Should().BeApproximately(originalSegment.C1.X, 0.001);
        parsedSegment.C1.Y.Should().BeApproximately(originalSegment.C1.Y, 0.001);
        parsedSegment.End.X.Should().BeApproximately(originalSegment.End.X, 0.001);
        parsedSegment.End.Y.Should().BeApproximately(originalSegment.End.Y, 0.001);
    }

    [Fact]
    public void FromPoints_IntegrationWithBezierCurveCreate_ShouldProduceSamePath()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(0, 0),
            new Point<double>(25, 50),
            new Point<double>(75, 50),
            new Point<double>(100, 0)
        };
        var coefficient = 0.4;

        // Act
        var pathFromPoints = Path<double>.FromPoints(points, coefficient);
        var curvesFromBezier = BezierCurve<double>.Create(points, coefficient).ToList();
        var pathFromCurves = Path<double>.FromSegments(curvesFromBezier);

        // Assert
        pathFromPoints.Count.Should().Be(pathFromCurves.Count);

        for (int i = 0; i < pathFromPoints.Count; i++)
        {
            var seg1 = pathFromPoints.Segments[i];
            var seg2 = pathFromCurves.Segments[i];

            seg1.Start.Should().Be(seg2.Start);
            seg1.C0.Should().Be(seg2.C0);
            seg1.C1.Should().Be(seg2.C1);
            seg1.End.Should().Be(seg2.End);
        }
    }

    [Fact]
    public void Path_WithFloatType_ShouldWork()
    {
        // Arrange
        var points = new[]
        {
            new Point<float>(0f, 0f),
            new Point<float>(50f, 100f),
            new Point<float>(100f, 0f)
        };

        // Act
        var path = Path<float>.FromPoints(points);

        // Assert
        path.IsEmpty.Should().BeFalse();
        path.Count.Should().Be(2);

        var svgPath = path.ToString();
        svgPath.Should().NotBeNullOrEmpty();
    }
}