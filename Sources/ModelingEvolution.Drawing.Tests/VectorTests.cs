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

    [Fact]
    public void Rotate_90Degrees()
    {
        var v = new Vector<double>(1, 0);
        var rotated = v.Rotate(Degree<double>.Create(90));
        rotated.X.Should().BeApproximately(0, 1e-9);
        rotated.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void Rotate_180Degrees()
    {
        var v = new Vector<double>(1, 0);
        var rotated = v.Rotate(Degree<double>.Create(180));
        rotated.X.Should().BeApproximately(-1, 1e-9);
        rotated.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void PerpendicularCW_FromRight()
    {
        var v = new Vector<double>(1, 0);
        var perp = v.PerpendicularCW;
        perp.X.Should().BeApproximately(0, 1e-9);
        perp.Y.Should().BeApproximately(-1, 1e-9);
    }

    [Fact]
    public void PerpendicularCCW_FromRight()
    {
        var v = new Vector<double>(1, 0);
        var perp = v.PerpendicularCCW;
        perp.X.Should().BeApproximately(0, 1e-9);
        perp.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void PerpendicularCW_DotProduct_IsZero()
    {
        var v = new Vector<double>(3, 4);
        var perp = v.PerpendicularCW;
        v.Dot(perp).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossXAxis()
    {
        var v = new Vector<double>(1, 1);
        var normal = new Vector<double>(0, 1);
        var reflected = v.Reflect(normal);
        reflected.X.Should().BeApproximately(1, 1e-9);
        reflected.Y.Should().BeApproximately(-1, 1e-9);
    }

    [Fact]
    public void Dot_Perpendicular_IsZero()
    {
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(0, 1);
        v1.Dot(v2).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Dot_SameDirection()
    {
        var v = new Vector<double>(3, 4);
        v.Dot(v).Should().BeApproximately(25, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf()
    {
        var a = new Vector<double>(0, 0);
        var b = new Vector<double>(10, 20);
        var result = a.Lerp(b, 0.5);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(10, 1e-9);
    }
}
