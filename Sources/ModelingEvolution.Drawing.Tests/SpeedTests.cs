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
        ((float)s).Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Zero_IsZero()
    {
        ((float)Speed<float>.Zero).Should().Be(0f);
    }

    [Fact]
    public void ImplicitConversion_FromT()
    {
        Speed<float> s = 42f;
        ((float)s).Should().BeApproximately(42f, Tol);
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
        ((float)(a + b)).Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Speed<float>.From(10f);
        var b = Speed<float>.From(3f);
        ((float)(a - b)).Should().BeApproximately(7f, Tol);
    }

    [Fact]
    public void Negation()
    {
        var s = Speed<float>.From(10f);
        ((float)(-s)).Should().BeApproximately(-10f, Tol);
    }

    [Fact]
    public void MultiplyByScalar()
    {
        var s = Speed<float>.From(5f);
        ((float)(s * 3f)).Should().BeApproximately(15f, Tol);
        ((float)(3f * s)).Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void DivideByScalar()
    {
        var s = Speed<float>.From(15f);
        ((float)(s / 3f)).Should().BeApproximately(5f, Tol);
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
        ((float)s.Abs()).Should().BeApproximately(10f, Tol);
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
        ((float)parsed).Should().BeApproximately(42.5f, Tol);
    }

    [Fact]
    public void ToString_Format()
    {
        var s = Speed<float>.From(10f);
        s.ToString().Should().Contain("10");
    }

    [Fact]
    public void Parse_ToString_RoundTrips()
    {
        var original = Speed<float>.From(42.5f);
        Speed<float>.Parse(original.ToString(), null).Should().Be(original);
    }

    [Fact]
    public void Parse_WithUnitSuffix_ReturnsSpeed()
    {
        ((float)Speed<float>.Parse("42.5 u/s", null)).Should().BeApproximately(42.5f, Tol);
    }

    [Fact]
    public void Parse_WithSiPrefix_Scales()
    {
        // strip the "u/s" unit first, then the SI prefix on the value
        ((float)Speed<float>.Parse("1.5 ku/s", null)).Should().BeApproximately(1500f, Tol);
        ((float)Speed<float>.Parse("500 mu/s", null)).Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void Parse_PrefixIsCaseSensitive()
    {
        ((float)Speed<float>.Parse("1 mu/s", null)).Should().BeApproximately(0.001f, Tol);
        ((float)Speed<float>.Parse("1 Mu/s", null)).Should().BeApproximately(1_000_000f, Tol);
    }

    [Fact]
    public void TryParse_UnknownTrailingLetter_ReturnsFalse()
    {
        Speed<float>.TryParse("42 Xu/s", null, out _).Should().BeFalse();
    }
}
