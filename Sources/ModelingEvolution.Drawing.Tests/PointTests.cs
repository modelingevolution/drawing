using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PointTests
{
    [Fact]
    public void DistanceTo_Origin_To_3_4()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(3, 4);
        a.DistanceTo(b).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_SamePoint_IsZero()
    {
        var p = new Point<double>(5, 7);
        p.DistanceTo(p).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 0);
        result.X.Should().BeApproximately(0, 1e-9);
        result.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 1);
        result.X.Should().BeApproximately(10, 1e-9);
        result.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 0.5);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossOrigin()
    {
        var p = new Point<double>(3, 4);
        var center = new Point<double>(0, 0);
        var result = p.Reflect(center);
        result.X.Should().BeApproximately(-3, 1e-9);
        result.Y.Should().BeApproximately(-4, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossCenter()
    {
        var p = new Point<double>(1, 1);
        var center = new Point<double>(3, 3);
        var result = p.Reflect(center);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(5, 1e-9);
    }
}
