using FluentAssertions;
using ModelingEvolution.Drawing.Units;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class PercentageTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(12.5f)]
    [InlineData(100f)]
    public void Ctor_acceptsInRange(float v) => new Percentage(v).Value.Should().Be(v);

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(100.1f)]
    [InlineData(float.NaN)]
    public void Ctor_rejectsOutOfRange(float v)
    {
        var act = () => new Percentage(v);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Fraction_isValueOver100() => new Percentage(25f).Fraction.Should().BeApproximately(0.25f, 1e-6f);

    [Fact]
    public void FromFraction_scalesTo100() => Percentage.FromFraction(0.4f).Value.Should().BeApproximately(40f, 1e-4f);

    [Fact]
    public void Clamp_doesNotThrow()
    {
        Percentage.Clamp(150f).Value.Should().Be(100f);
        Percentage.Clamp(-5f).Value.Should().Be(0f);
        Percentage.Clamp(float.NaN).Value.Should().Be(0f);
    }

    [Fact]
    public void Zero_and_Full() { Percentage.Zero.Value.Should().Be(0f); Percentage.Full.Value.Should().Be(100f); }

    [Fact]
    public void Comparison_works() => (new Percentage(20f) < new Percentage(50f)).Should().BeTrue();

    [Theory]
    [InlineData("20", 20f)]
    [InlineData("20%", 20f)]
    [InlineData("12.5%", 12.5f)]
    public void Parse_roundTrips(string s, float expected)
        => Percentage.Parse(s, null).Value.Should().Be(expected);

    [Theory]
    [InlineData("150")]
    [InlineData("-1%")]
    [InlineData("abc")]
    public void TryParse_rejectsBad(string s) => Percentage.TryParse(s, null, out _).Should().BeFalse();

    [Fact]
    public void ToString_appendsPercent() => new Percentage(20f).ToString().Should().Be("20%");

    [Fact]
    public void Multiply_compounds() => (new Percentage(50f) * new Percentage(50f)).Value.Should().BeApproximately(25f, 1e-4f);

    [Fact]
    public void Add_clampsAt100() => (new Percentage(60f) + new Percentage(60f)).Value.Should().Be(100f);

    [Fact]
    public void Subtract_clampsAt0() => (new Percentage(20f) - new Percentage(50f)).Value.Should().Be(0f);

    [Fact]
    public void Speed_scaledByPercentage_bothOrders()
    {
        var max = new Speed<double, MillimetrePerSecond<double>>(80);
        // Tolerance is float-level: Percentage.Fraction is float, so e.g. 0.1f→double is not exact.
        (max * new Percentage(50f)).Value.Should().BeApproximately(40.0, 1e-4);
        (new Percentage(25f) * max).Value.Should().BeApproximately(20.0, 1e-4);
    }

    [Fact]
    public void Acceleration_scaledByPercentage()
    {
        var max = new Acceleration<double, MillimetrePerSecondSquared<double>>(100);
        (max * new Percentage(10f)).Value.Should().BeApproximately(10.0, 1e-4);
    }

    [Fact]
    public void Speed_dividedByPercentage_recoversFull()
    {
        var half = new Speed<double, MillimetrePerSecond<double>>(40);
        (half / new Percentage(50f)).Value.Should().BeApproximately(80.0, 1e-4);   // 40 is 50% of 80
    }
}
