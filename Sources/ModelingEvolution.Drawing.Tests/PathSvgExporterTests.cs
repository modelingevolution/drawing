using System.Collections.Immutable;
using FluentAssertions;
using ModelingEvolution.Drawing.Svg;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class PathSvgExporterTests
{
    [Fact]
    public void Export_EmptyPath_ShouldReturnEmptyString()
    {
        // Arrange
        var path = new Path<double>(ImmutableList<BezierCurve<double>>.Empty);
        var exporter = new PathSvgExporter<double>();
        var paint = new SvgPaint(Colors.Blue, Colors.Black, 2f);

        // Act
        var svg = exporter.Export(path, paint);

        // Assert
        svg.Should().BeEmpty();
    }

    [Fact]
    public void Export_SingleSegmentPath_ShouldGenerateValidSvg()
    {
        // Arrange
        var curve = new BezierCurve<double>(
            new Point<double>(10, 20),
            new Point<double>(30, 40),
            new Point<double>(50, 40),
            new Point<double>(70, 20)
        );
        var path = Path<double>.FromSegments(curve);
        var exporter = new PathSvgExporter<double>();
        var paint = new SvgPaint(Colors.Red, Colors.Black, 1.5f);

        // Act
        var svg = exporter.Export(path, paint);

        // Assert
        svg.Should().StartWith("<path ");
        svg.Should().Contain("fill=\"" + Colors.Red.ToString() + "\"");
        svg.Should().Contain("stroke=\"" + Colors.Black.ToString() + "\"");
        svg.Should().Contain("stroke-width=\"1.5\"");
        svg.Should().Contain("d=\"M 10 20 C 30 40, 50 40, 70 20\"");
        svg.Should().EndWith("/>");
    }

    [Fact]
    public void Export_MultipleSegmentPath_ShouldGenerateValidSvg()
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
        var exporter = new PathSvgExporter<double>();
        var paint = SvgPaint.WithStroke(Colors.Green, 3f);

        // Act
        var svg = exporter.Export(path, paint);

        // Assert
        svg.Should().StartWith("<path ");
        svg.Should().Contain("fill=\"" + Colors.Transparent.ToString() + "\"");
        svg.Should().Contain("stroke=\"" + Colors.Green.ToString() + "\"");
        svg.Should().Contain("stroke-width=\"3\"");
        svg.Should().Contain("M 0 0");
        svg.Should().Contain("C 10 10, 20 10, 30 0");
        svg.Should().Contain("C 40 -10, 50 -10, 60 0");
    }

    [Fact]
    public void PathSvgExporterFactory_ShouldCreateCorrectExporter()
    {
        // Arrange
        var factory = new PathSvgExporterFactory();
        var pathType = typeof(Path<double>);

        // Act
        var exporter = factory.Create(pathType);

        // Assert
        exporter.Should().NotBeNull();
        exporter.Should().BeOfType<PathSvgExporter<double>>();
    }

    [Fact]
    public void SvgExporter_Export_WithPath_ShouldGenerateCompleteSvg()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(10, 10),
            new Point<double>(50, 80),
            new Point<double>(90, 10)
        };
        var path = Path<double>.FromPoints(points);

        // Act
        var svg = SvgExporter.Export(path, 100, 100, Colors.Blue);

        // Assert
        svg.Should().StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\"");
        svg.Should().Contain("width=\"100\"");
        svg.Should().Contain("height=\"100\"");
        svg.Should().Contain("viewBox=\"0 0 100 100\"");
        svg.Should().Contain("<path ");
        svg.Should().Contain("fill=\"" + Colors.Blue.ToString() + "\"");
        svg.Should().EndWith("</svg>");
    }

    [Fact]
    public void SvgExporter_Export_WithMultiplePaths_ShouldCombinePaths()
    {
        // Arrange
        var path1 = Path<double>.FromPoints(
            new Point<double>(10, 10),
            new Point<double>(30, 30),
            new Point<double>(50, 10)
        );

        var path2 = Path<double>.FromPoints(
            new Point<double>(60, 10),
            new Point<double>(80, 30),
            new Point<double>(100, 10)
        );

        var paths = new[] { path1, path2 };
        var paint = new SvgPaint(Colors.Yellow, Colors.Black, 2f);

        // Act
        var svg = SvgExporter.Export(paths, 120, 50, paint);

        // Assert
        svg.Should().StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\"");
        svg.Should().Contain("width=\"120\"");
        svg.Should().Contain("height=\"50\"");

        // Should contain two path elements
        var pathCount = svg.Split(new[] { "<path " }, StringSplitOptions.None).Length - 1;
        pathCount.Should().Be(2);
    }

    [Fact]
    public void SvgExporter_Export_WithPaintSelector_ShouldApplyDifferentStyles()
    {
        // Arrange
        var paths = new[]
        {
            Path<double>.FromPoints(
                new Point<double>(10, 10),
                new Point<double>(30, 30)
            ),
            Path<double>.FromPoints(
                new Point<double>(40, 10),
                new Point<double>(60, 30)
            )
        };

        var paintSelector = new Func<object, SvgPaint>(obj =>
        {
            var index = Array.IndexOf(paths, obj);
            return index == 0
                ? SvgPaint.WithFill(Colors.Red)
                : SvgPaint.WithFill(Colors.Blue);
        });

        // Act
        var svg = SvgExporter.Export(
            paths.Cast<object>().ToList(),
            100, 50,
            paintSelector
        );

        // Assert
        svg.Should().Contain(Colors.Red.ToString());
        svg.Should().Contain(Colors.Blue.ToString());
    }

    [Fact]
    public void PathWithFloatType_ShouldExportCorrectly()
    {
        // Arrange
        var curve = new BezierCurve<float>(
            new Point<float>(10f, 20f),
            new Point<float>(30f, 40f),
            new Point<float>(50f, 40f),
            new Point<float>(70f, 20f)
        );
        var path = Path<float>.FromSegments(curve);
        var exporter = new PathSvgExporter<float>();
        var paint = SvgPaint.WithFill(Colors.Purple);

        // Act
        var svg = exporter.Export(path, paint);

        // Assert
        svg.Should().StartWith("<path ");
        svg.Should().Contain("fill=\"" + Colors.Purple.ToString() + "\"");
        svg.Should().Contain("d=\"M 10 20");
    }

    [Fact]
    public void PathFromPoints_ExportedSvg_ShouldCreateSmoothCurves()
    {
        // Arrange
        var points = new[]
        {
            new Point<double>(0, 50),
            new Point<double>(25, 0),
            new Point<double>(50, 50),
            new Point<double>(75, 0),
            new Point<double>(100, 50)
        };
        var path = Path<double>.FromPoints(points, 0.4);

        // Act
        var svg = SvgExporter.Export(path, 100, 50, Colors.Cyan, Colors.Black, 1f);

        // Assert
        svg.Should().Contain("<svg ");
        svg.Should().Contain("<path ");
        svg.Should().Contain("fill=\"" + Colors.Cyan.ToString() + "\"");
        svg.Should().Contain("stroke=\"" + Colors.Black.ToString() + "\"");

        // Should have 4 cubic Bezier curves for 5 points
        var curveCount = svg.Split(new[] { " C " }, StringSplitOptions.None).Length - 1;
        curveCount.Should().Be(4);
    }
}