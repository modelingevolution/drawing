using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class SpeedTests
{
    private const float Tol = 1e-4f;

    [Fact]
    public void From_CreatesSpeed()
    {
        var s = Speed<float>.From(10f);
        s.Value.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Zero_IsZero()
    {
        Speed<float>.Zero.Value.Should().Be(0f);
    }

    [Fact]
    public void ImplicitConversion_FromT()
    {
        Speed<float> s = 42f;
        s.Value.Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void ExplicitConversion_ToT()
    {
        var s = Speed<float>.From(42f);
        ((float)s).Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void Addition()
    {
        var a = Speed<float>.From(10f);
        var b = Speed<float>.From(5f);
        (a + b).Value.Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Speed<float>.From(10f);
        var b = Speed<float>.From(3f);
        (a - b).Value.Should().BeApproximately(7f, Tol);
    }

    [Fact]
    public void Negation()
    {
        var s = Speed<float>.From(10f);
        (-s).Value.Should().BeApproximately(-10f, Tol);
    }

    [Fact]
    public void MultiplyByScalar()
    {
        var s = Speed<float>.From(5f);
        (s * 3f).Value.Should().BeApproximately(15f, Tol);
        (3f * s).Value.Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void DivideByScalar()
    {
        var s = Speed<float>.From(15f);
        (s / 3f).Value.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void TimeFor_DistanceDividedBySpeed()
    {
        var s = Speed<float>.From(10f);
        s.TimeFor(100f).Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void DistanceIn_SpeedTimesTime()
    {
        var s = Speed<float>.From(10f);
        s.DistanceIn(5f).Should().BeApproximately(50f, Tol);
    }

    [Fact]
    public void Abs_ReturnsPositive()
    {
        var s = Speed<float>.From(-10f);
        s.Abs().Value.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Comparisons()
    {
        var a = Speed<float>.From(5f);
        var b = Speed<float>.From(10f);
        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
        (a <= a).Should().BeTrue();
        (a >= a).Should().BeTrue();
    }

    [Fact]
    public void Equality()
    {
        var a = Speed<float>.From(10f);
        var b = Speed<float>.From(10f);
        a.Should().Be(b);
    }

    [Fact]
    public void Parse_RoundTrip()
    {
        var s = Speed<float>.From(42.5f);
        Speed<float>.TryParse("42.5", null, out var parsed).Should().BeTrue();
        parsed.Value.Should().BeApproximately(42.5f, Tol);
    }

    [Fact]
    public void ToString_Format()
    {
        var s = Speed<float>.From(10f);
        s.ToString().Should().Contain("10");
    }
}
