using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PathJsonConverterTests
{
    [Fact]
    public void Should_Serialize_EmptyPath_ToEmptyString()
    {
        // Arrange
        var path = new Path<float>();

        // Act
        var json = JsonSerializer.Serialize(path);

        // Assert
        json.Should().Be("\"\"");
    }

    [Fact]
    public void Should_Serialize_PathWithFloatSegments_ToSvgString()
    {
        // Arrange
        var segment1 = new BezierCurve<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 10),
            new Point<float>(20, 10),
            new Point<float>(30, 0));

        var segment2 = new BezierCurve<float>(
            new Point<float>(30, 0),
            new Point<float>(40, -10),
            new Point<float>(50, -10),
            new Point<float>(60, 0));

        var path = Path<float>.FromSegments(segment1, segment2);

        // Act
        var json = JsonSerializer.Serialize(path);
        var deserialized = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        json.Should().Contain("M 0 0");
        json.Should().Contain("C 10 10, 20 10, 30 0");
        json.Should().Contain("C 40 -10, 50 -10, 60 0");
        deserialized.Segments.Count.Should().Be(2);
        deserialized.Segments.Should().BeEquivalentTo(path.Segments);
    }

    [Fact]
    public void Should_Serialize_PathWithDoubleSegments_ToSvgString()
    {
        // Arrange
        var segment = new BezierCurve<double>(
            new Point<double>(0.5, 1.5),
            new Point<double>(2.5, 3.5),
            new Point<double>(4.5, 5.5),
            new Point<double>(6.5, 7.5));

        var path = Path<double>.FromSegments(segment);

        // Act
        var json = JsonSerializer.Serialize(path);
        var deserialized = JsonSerializer.Deserialize<Path<double>>(json);

        // Assert
        json.Should().Contain("M 0.5 1.5");
        json.Should().Contain("C 2.5 3.5, 4.5 5.5, 6.5 7.5");
        deserialized.Segments.Count.Should().Be(1);
        deserialized.Segments.Should().BeEquivalentTo(path.Segments);
    }

    [Fact]
    public void Should_Deserialize_EmptyString_ToEmptyPath()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var path = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        path.IsEmpty.Should().BeTrue();
        path.Segments.Count.Should().Be(0);
    }

    [Fact]
    public void Should_Deserialize_SvgString_ToPathWithFloatSegments()
    {
        // Arrange
        var svgPath = "M 10 20 C 30 40, 50 60, 70 80";
        var json = $"\"{svgPath}\"";

        // Act
        var path = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        path.Should().NotBeNull();
        path.Segments.Count.Should().Be(1);

        var segment = path.Segments[0];
        segment.Start.Should().Be(new Point<float>(10, 20));
        segment.C0.Should().Be(new Point<float>(30, 40));
        segment.C1.Should().Be(new Point<float>(50, 60));
        segment.End.Should().Be(new Point<float>(70, 80));
    }

    [Fact]
    public void Should_Deserialize_SvgString_ToPathWithDoubleSegments()
    {
        // Arrange
        var svgPath = "M 1.5 2.5 C 3.5 4.5, 5.5 6.5, 7.5 8.5 C 9.5 10.5, 11.5 12.5, 13.5 14.5";
        var json = $"\"{svgPath}\"";

        // Act
        var path = JsonSerializer.Deserialize<Path<double>>(json);

        // Assert
        path.Should().NotBeNull();
        path.Segments.Count.Should().Be(2);

        var segment1 = path.Segments[0];
        segment1.Start.Should().Be(new Point<double>(1.5, 2.5));
        segment1.C0.Should().Be(new Point<double>(3.5, 4.5));
        segment1.C1.Should().Be(new Point<double>(5.5, 6.5));
        segment1.End.Should().Be(new Point<double>(7.5, 8.5));

        var segment2 = path.Segments[1];
        segment2.Start.Should().Be(new Point<double>(7.5, 8.5));
        segment2.C0.Should().Be(new Point<double>(9.5, 10.5));
        segment2.C1.Should().Be(new Point<double>(11.5, 12.5));
        segment2.End.Should().Be(new Point<double>(13.5, 14.5));
    }

    [Fact]
    public void Should_RoundTrip_ComplexPath()
    {
        // Arrange
        var points = new[]
        {
            new Point<float>(0, 0),
            new Point<float>(100, 50),
            new Point<float>(200, 25),
            new Point<float>(300, 75),
            new Point<float>(400, 100)
        };

        var originalPath = Path<float>.FromPoints(points, 0.5f);

        // Act
        var json = JsonSerializer.Serialize(originalPath);
        var deserializedPath = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        deserializedPath.Segments.Count.Should().Be(originalPath.Segments.Count);
        deserializedPath.Segments.Should().BeEquivalentTo(originalPath.Segments);
    }

    [Fact]
    public void Should_HandleLineSegments_InSvgString()
    {
        // Arrange
        var svgPath = "M 0 0 L 10 10 L 20 0";
        var json = $"\"{svgPath}\"";

        // Act
        var path = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        path.Should().NotBeNull();
        path.Segments.Count.Should().Be(2); // Two line segments converted to Bezier curves
    }

    [Fact]
    public void Should_HandleClosePath_InSvgString()
    {
        // Arrange
        var svgPath = "M 0 0 L 10 10 L 10 0 Z";
        var json = $"\"{svgPath}\"";

        // Act
        var path = JsonSerializer.Deserialize<Path<float>>(json);

        // Assert
        path.Should().NotBeNull();
        path.Segments.Count.Should().Be(3); // Three segments including the closing segment

        // Verify the path closes back to start
        var lastSegment = path.Segments[^1];
        lastSegment.End.Should().Be(new Point<float>(0, 0));
    }

    [Fact]
    public void Should_Serialize_PathInComplexObject()
    {
        // Arrange
        var testObject = new TestPathContainer
        {
            Name = "Test",
            Path = Path<float>.FromPoints(
                new Point<float>(0, 0),
                new Point<float>(10, 10),
                new Point<float>(20, 0))
        };

        // Act
        var json = JsonSerializer.Serialize(testObject);
        var deserialized = JsonSerializer.Deserialize<TestPathContainer>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Name.Should().Be("Test");
        deserialized.Path.Segments.Count.Should().Be(testObject.Path.Segments.Count);
        deserialized.Path.Segments.Should().BeEquivalentTo(testObject.Path.Segments);
    }

    [Fact]
    public void Should_ThrowException_ForInvalidJsonType()
    {
        // Arrange
        var json = "123"; // Number instead of string

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<Path<float>>(json);
        act.Should().Throw<JsonException>()
            .WithMessage("*Expected string value for Path*");
    }

    [Fact]
    public void Should_HandleNull_AsEmptyPath()
    {
        // Arrange
        var json = "null";

        // Act
        var path = JsonSerializer.Deserialize<Path<float>?>(json);

        // Assert
        path.Should().BeNull();
    }

    private class TestPathContainer
    {
        public string Name { get; set; } = "";
        public Path<float> Path { get; set; }
    }
}