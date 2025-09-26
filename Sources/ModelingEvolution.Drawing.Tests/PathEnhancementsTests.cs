using System.Collections.Immutable;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class PathEnhancementsTests
{
    [Fact]
    public void BoundingBox_EmptyPath_ShouldReturnZeroRectangle()
    {
        // Arrange
        var path = new Path<double>(ImmutableList<BezierCurve<double>>.Empty);

        // Act
        var boundingBox = path.BoundingBox();

        // Assert
        boundingBox.X.Should().Be(0);
        boundingBox.Y.Should().Be(0);
        boundingBox.Width.Should().Be(0);
        boundingBox.Height.Should().Be(0);
    }

    [Fact]
    public void BoundingBox_SingleSegment_ShouldReturnCorrectBounds()
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
        var boundingBox = path.BoundingBox();

        // Assert
        boundingBox.X.Should().BeLessThanOrEqualTo(10);
        boundingBox.Y.Should().BeLessThanOrEqualTo(20);
        boundingBox.Right.Should().BeGreaterThanOrEqualTo(70);
        boundingBox.Bottom.Should().BeGreaterThanOrEqualTo(40);
    }

    [Fact]
    public void TranslateByVector_ShouldMoveAllPoints()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);
        var vector = new Vector<double>(100, 50);

        // Act
        var translatedPath = path + vector;

        // Assert
        var firstSegment = translatedPath.Segments[0];
        firstSegment.Start.X.Should().Be(100);
        firstSegment.Start.Y.Should().Be(50);
        firstSegment.End.X.Should().Be(130);
        firstSegment.End.Y.Should().Be(50);
    }

    [Fact]
    public void SubtractVector_ShouldTranslateInOppositeDirection()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(100, 100),
            new Point<double>(110, 110),
            new Point<double>(120, 110),
            new Point<double>(130, 100)
        );
        var path = Path<double>.FromSegments(curve);
        var vector = new Vector<double>(50, 50);

        // Act
        var translatedPath = path - vector;

        // Assert
        var firstSegment = translatedPath.Segments[0];
        firstSegment.Start.X.Should().Be(50);
        firstSegment.Start.Y.Should().Be(50);
    }

    [Fact]
    public void ScaleBySize_ShouldScaleAllPoints()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(10, 20),
            new Point<double>(30, 40),
            new Point<double>(50, 40),
            new Point<double>(70, 20)
        );
        var path = Path<double>.FromSegments(curve);
        var scale = new Size<double>(2, 3);

        // Act
        var scaledPath = path * scale;

        // Assert
        var firstSegment = scaledPath.Segments[0];
        firstSegment.Start.X.Should().Be(20); // 10 * 2
        firstSegment.Start.Y.Should().Be(60); // 20 * 3
        firstSegment.End.X.Should().Be(140);  // 70 * 2
        firstSegment.End.Y.Should().Be(60);   // 20 * 3
    }

    [Fact]
    public void DivideBySize_ShouldScaleDownAllPoints()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(100, 90),
            new Point<double>(120, 120),
            new Point<double>(140, 120),
            new Point<double>(160, 90)
        );
        var path = Path<double>.FromSegments(curve);
        var scale = new Size<double>(2, 3);

        // Act
        var scaledPath = path / scale;

        // Assert
        var firstSegment = scaledPath.Segments[0];
        firstSegment.Start.X.Should().Be(50); // 100 / 2
        firstSegment.Start.Y.Should().Be(30); // 90 / 3
    }

    [Fact]
    public void Transform_WithMatrix_ShouldApplyTransformation()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(10, 0),
            new Point<double>(20, 10),
            new Point<double>(20, 20),
            new Point<double>(10, 30)
        );
        var path = Path<double>.FromSegments(curve);
        // Create a simple scaling matrix (2x in X, 3x in Y)
        var matrix = new Matrix<double>(2, 0, 0, 3, 0, 0);

        // Act
        var transformedPath = path.Transform(matrix);

        // Assert
        var firstSegment = transformedPath.Segments[0];
        // After scaling: (10,0) -> (20,0)
        firstSegment.Start.X.Should().BeApproximately(20, 0.001);
        firstSegment.Start.Y.Should().BeApproximately(0, 0.001);
        // After scaling: (10,30) -> (20,90)
        firstSegment.End.X.Should().BeApproximately(20, 0.001);
        firstSegment.End.Y.Should().BeApproximately(90, 0.001);
    }

    [Fact]
    public void Reverse_ShouldReversePathDirection()
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
        var reversedPath = path.Reverse();

        // Assert
        reversedPath.Count.Should().Be(2);

        // First segment of reversed path should be the reverse of the last original segment
        var firstReversed = reversedPath.Segments[0];
        firstReversed.Start.X.Should().Be(curve2.End.X);
        firstReversed.Start.Y.Should().Be(curve2.End.Y);
        firstReversed.C0.X.Should().Be(curve2.C1.X);
        firstReversed.C0.Y.Should().Be(curve2.C1.Y);
        firstReversed.C1.X.Should().Be(curve2.C0.X);
        firstReversed.C1.Y.Should().Be(curve2.C0.Y);
        firstReversed.End.X.Should().Be(curve2.Start.X);
        firstReversed.End.Y.Should().Be(curve2.Start.Y);

        // Last segment of reversed path should be the reverse of the first original segment
        var lastReversed = reversedPath.Segments[1];
        lastReversed.Start.X.Should().Be(curve1.End.X);
        lastReversed.Start.Y.Should().Be(curve1.End.Y);
        lastReversed.End.X.Should().Be(curve1.Start.X);
        lastReversed.End.Y.Should().Be(curve1.Start.Y);
    }

    [Fact]
    public void Append_TwoPaths_ShouldCombineSegments()
    {
        // Arrange
        var curve1 = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path1 = Path<double>.FromSegments(curve1);

        var curve2 = new BezierCurve<double>(
            new Point<double>(30, 0),
            new Point<double>(40, -10),
            new Point<double>(50, -10),
            new Point<double>(60, 0)
        );
        var path2 = Path<double>.FromSegments(curve2);

        // Act
        var combinedPath = path1.Append(path2);

        // Assert
        combinedPath.Count.Should().Be(2);
        combinedPath.Segments.Should().HaveCount(2);
        combinedPath.Segments.Should().ContainInOrder(curve1, curve2);
    }

    [Fact]
    public void ConcatenateOperator_ShouldCombinePaths()
    {
        // Arrange
        var path1 = Path<double>.FromPoints(
            new Point<double>(0, 0),
            new Point<double>(50, 50),
            new Point<double>(100, 0)
        );

        var path2 = Path<double>.FromPoints(
            new Point<double>(100, 0),
            new Point<double>(150, -50),
            new Point<double>(200, 0)
        );

        // Act
        var combinedPath = path1 + path2;

        // Assert
        combinedPath.Count.Should().Be(path1.Count + path2.Count);
    }

    [Fact]
    public void Append_EmptyPaths_ShouldHandleCorrectly()
    {
        // Arrange
        var emptyPath = new Path<double>(ImmutableList<BezierCurve<double>>.Empty);
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var nonEmptyPath = Path<double>.FromSegments(curve);

        // Act
        var result1 = emptyPath.Append(nonEmptyPath);
        var result2 = nonEmptyPath.Append(emptyPath);

        // Assert
        result1.Count.Should().Be(1);
        result1.Segments.Should().HaveCount(1);
        result1.Segments.Should().Contain(curve);

        result2.Count.Should().Be(1);
        result2.Segments.Should().HaveCount(1);
        result2.Segments.Should().Contain(curve);
    }

    [Fact]
    public void TranslateByPoint_ShouldTranslateCorrectly()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 10),
            new Point<double>(20, 10),
            new Point<double>(30, 0)
        );
        var path = Path<double>.FromSegments(curve);
        var point = new Point<double>(25, 35);

        // Act
        var translatedPath = path + point;

        // Assert
        var firstSegment = translatedPath.Segments[0];
        firstSegment.Start.X.Should().Be(25);
        firstSegment.Start.Y.Should().Be(35);
    }

    [Fact]
    public void BoundingBox_MultipleSegments_ShouldEncompassAllPoints()
    {
        // Arrange
        var path = Path<double>.FromPoints(
            new Point<double>(-10, -20),
            new Point<double>(50, 100),
            new Point<double>(120, -30),
            new Point<double>(0, 0)
        );

        // Act
        var boundingBox = path.BoundingBox();

        // Assert
        boundingBox.Left.Should().BeLessThanOrEqualTo(-10);
        boundingBox.Top.Should().BeLessThanOrEqualTo(-30);
        boundingBox.Right.Should().BeGreaterThanOrEqualTo(120);
        boundingBox.Bottom.Should().BeGreaterThanOrEqualTo(100);
    }
}