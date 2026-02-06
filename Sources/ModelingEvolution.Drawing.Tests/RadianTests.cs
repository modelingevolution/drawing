using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class RadianTests
{
    [Fact]
    public void Negate()
    {
        var r = Radian<double>.FromRadian(Math.PI / 4);
        var neg = -r;
        ((double)neg).Should().BeApproximately(-Math.PI / 4, 1e-9);
    }

    [Fact]
    public void Multiply_ByScalar()
    {
        var r = Radian<double>.FromRadian(Math.PI / 4);
        var result = r * 2.0;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Multiply_ScalarByRadian()
    {
        var r = Radian<double>.FromRadian(Math.PI / 6);
        var result = 3.0 * r;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Divide_ByScalar()
    {
        var r = Radian<double>.FromRadian(Math.PI);
        var result = r / 2.0;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Abs_NegativeRadian()
    {
        var r = Radian<double>.FromRadian(-Math.PI / 3);
        ((double)r.Abs()).Should().BeApproximately(Math.PI / 3, 1e-9);
    }

    [Fact]
    public void Normalize_LargePositive()
    {
        var r = Radian<double>.FromRadian(3 * Math.PI);
        ((double)r.Normalize()).Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void Normalize_LargeNegative()
    {
        var r = Radian<double>.FromRadian(-3 * Math.PI);
        ((double)r.Normalize()).Should().BeApproximately(Math.PI, 1e-9);
    }
}
