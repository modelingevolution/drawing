using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class VectorTests
{
    [Fact]
    public void AngleBetween_Perpendicular_ReturnsHalfPi()
    {
        var v1 = new Vector<double>(1, 0); // →
        var v2 = new Vector<double>(0, 1); // ↑
        var angle = (double)v1.AngleBetween(v2);
        angle.Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void AngleBetween_Opposite_ReturnsPi()
    {
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(-1, 0);
        var angle = (double)v1.AngleBetween(v2);
        Math.Abs(angle).Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void AngleBetween_Clockwise_ReturnsNegative()
    {
        var v1 = new Vector<double>(1, 0); // →
        var v2 = new Vector<double>(0, -1); // ↓
        var angle = (double)v1.AngleBetween(v2);
        angle.Should().BeApproximately(-Math.PI / 2, 1e-9);
    }

    [Fact]
    public void AngleBetween_SameDirection_ReturnsZero()
    {
        var v1 = new Vector<double>(3, 4);
        var v2 = new Vector<double>(6, 8);
        var angle = (double)v1.AngleBetween(v2);
        angle.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void AngleBetween_45Degrees()
    {
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(1, 1);
        var angle = (double)v1.AngleBetween(v2);
        angle.Should().BeApproximately(Math.PI / 4, 1e-9);
    }

    [Fact]
    public void AngleBetween_IsAnticommutative()
    {
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(1, 1);
        var a1 = (double)v1.AngleBetween(v2);
        var a2 = (double)v2.AngleBetween(v1);
        a1.Should().BeApproximately(-a2, 1e-9);
    }

    [Fact]
    public void AngleBetween_ResultInRange_NegPiToPi()
    {
        // -135° should come out as -3π/4, not +5π/4
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(-1, -1);
        var angle = (double)v1.AngleBetween(v2);
        angle.Should().BeApproximately(-3 * Math.PI / 4, 1e-9);
    }
}
