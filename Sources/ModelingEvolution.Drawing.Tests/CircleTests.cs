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

    [Fact]
    public void DistanceTo_PointOnCircle_IsZero()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(5, 0)).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointOutside()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(10, 0)).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointInside()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(2, 0)).Should().BeApproximately(3, 1e-9);
    }

    [Fact]
    public void PointAt_ZeroAngle()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var p = c.PointAt(Radian<double>.FromRadian(0));
        p.X.Should().BeApproximately(5, 1e-9);
        p.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void PointAt_90Degrees()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var p = c.PointAt(Radian<double>.FromRadian(Math.PI / 2));
        p.X.Should().BeApproximately(0, 1e-9);
        p.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void PointAt_WithOffset()
    {
        var c = new Circle<double>(new Point<double>(10, 20), 3);
        var p = c.PointAt(Radian<double>.FromRadian(0));
        p.X.Should().BeApproximately(13, 1e-9);
        p.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Rotate_MovesCenter()
    {
        var c = new Circle<double>(new Point<double>(5, 0), 2);
        var rotated = c.Rotate(Degree<double>.Create(90));
        rotated.Center.X.Should().BeApproximately(0, 1e-9);
        rotated.Center.Y.Should().BeApproximately(5, 1e-9);
        rotated.Radius.Should().BeApproximately(2, 1e-9);
    }

    [Fact]
    public void Scale_ByFactor()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var scaled = c.Scale(2.0);
        scaled.Radius.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Scale_BySize()
    {
        var c = new Circle<double>(new Point<double>(1, 2), 5);
        var scaled = c.Scale(new Size<double>(2, 2));
        scaled.Radius.Should().BeApproximately(10, 1e-9);
        scaled.Center.X.Should().BeApproximately(2, 1e-9);
        scaled.Center.Y.Should().BeApproximately(4, 1e-9);
    }

    private static readonly Rectangle<double> Roi = new(0, 0, 10, 10);

    [Fact]
    public void Circle_FullyInside_ReturnsApproximation()
    {
        var c = new Circle<double>(new Point<double>(5, 5), 2);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().BeGreaterThan(0);
        clipped.Area().Should().BeApproximately(c.Area(), 0.5);
    }

    [Fact]
    public void Circle_PartiallyInside_ClipsCorrectly()
    {
        var c = new Circle<double>(new Point<double>(0, 5), 5);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().BeGreaterThan(0);
        clipped.Area().Should().BeLessThan(c.Area());
    }

    [Fact]
    public void Circle_FullyOutside_ReturnsEmpty()
    {
        var c = new Circle<double>(new Point<double>(50, 50), 2);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().Be(0);
    }
}
