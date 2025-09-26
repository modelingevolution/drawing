using FluentAssertions;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonSvgExporterTests
{
    [Fact]
    public void Export_SimpleTriangle_ReturnsCorrectSvgPath()
    {
        // Arrange
        var triangle = new Polygon<float>(
            new Point<float>(0, 0),
            new Point<float>(100, 0),
            new Point<float>(50, 100)
        );
        var paint = new SvgPaint(Colors.Red, Colors.Black, 2f);

        // Act
        var svg = SvgExporter.Export(triangle, 200, 200, paint);

        // Assert
        svg.Should().StartWith("<svg xmlns=\"http://www.w3.org/2000/svg\"")
            .And.Contain("width=\"200\" height=\"200\"")
            .And.Contain("viewBox=\"0 0 200 200\"")
            .And.Contain("<path ")
            .And.Contain($"fill=\"{Colors.Red}\"")
            .And.Contain($"stroke=\"{Colors.Black}\"")
            .And.Contain("stroke-width=\"2\"")
            .And.Contain("d=\"M0,0 L100,0 L50,100 Z\"")
            .And.EndWith("</svg>");
    }

    [Fact]
    public void Export_EmptyPolygon_ReturnsEmptySvg()
    {
        var empty = new Polygon<float>();
        var svg = SvgExporter.Export(empty, 100, 100, Colors.Blue);

        svg.Should().Be("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\"></svg>");
    }

    [Fact]
    public void Export_SinglePoint_ReturnsEmptySvg()
    {
        var point = new Polygon<float>(new Point<float>(10, 10));
        var svg = SvgExporter.Export(point, 100, 100, Colors.Blue);

        svg.Should().Be("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\"></svg>");
    }
}