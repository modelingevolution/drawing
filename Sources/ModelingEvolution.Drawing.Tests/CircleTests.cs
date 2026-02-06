using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class CircleTests
{
    [Fact]
    public void Constructor_SetsCenterAndRadius()
    {
        var c = new Circle<float>(new Point<float>(3, 4), 5f);
        c.Center.Should().Be(new Point<float>(3, 4));
        c.Radius.Should().Be(5f);
    }

    [Fact]
    public void Diameter_ReturnsTwiceRadius()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 7f);
        c.Diameter.Should().Be(14f);
    }

    [Fact]
    public void Area_ReturnsCorrectValue()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 1f);
        c.Area().Should().BeApproximately(MathF.PI, 1e-5f);
    }

    [Fact]
    public void Circumference_ReturnsCorrectValue()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 1f);
        c.Perimeter().Should().BeApproximately(2f * MathF.PI, 1e-5f);
    }

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 10f);
        c.Contains(new Point<float>(3, 4)).Should().BeTrue();
    }

    [Fact]
    public void Contains_PointOnBoundary_ReturnsTrue()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 5f);
        c.Contains(new Point<float>(3, 4)).Should().BeTrue(); // 3²+4²=25=5²
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var c = new Circle<float>(new Point<float>(0, 0), 5f);
        c.Contains(new Point<float>(4, 4)).Should().BeFalse(); // 4²+4²=32>25
    }

    [Fact]
    public void AddVector_TranslatesCenter()
    {
        var c = new Circle<float>(new Point<float>(1, 2), 5f);
        var moved = c + new Vector<float>(10, 20);
        moved.Center.Should().Be(new Point<float>(11, 22));
        moved.Radius.Should().Be(5f);
    }

    [Fact]
    public void SubtractVector_TranslatesCenter()
    {
        var c = new Circle<float>(new Point<float>(11, 22), 5f);
        var moved = c - new Vector<float>(10, 20);
        moved.Center.Should().Be(new Point<float>(1, 2));
        moved.Radius.Should().Be(5f);
    }

    [Fact]
    public void Equation_ReturnsMatchingCircleEquation()
    {
        var c = new Circle<float>(new Point<float>(3, 4), 5f);
        var eq = c.Equation;
        eq.Center.Should().Be(new Point<float>(3, 4));
        eq.Radius.Should().Be(5f);
    }
}
