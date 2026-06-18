using FluentAssertions;
using ModelingEvolution.Drawing.Units;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class UnitAccelerationTests
{
    [Fact]
    public void MmPerSecondSquared_ToSi_isMillimetres()
        => new Acceleration<double, MillimetrePerSecondSquared<double>>(1000).ToSi().Should().BeApproximately(1.0, 1e-12);

    [Fact]
    public void MetrePerSecondSquared_ToSi_isIdentity()
        => new Acceleration<double, MetrePerSecondSquared<double>>(9.81).ToSi().Should().BeApproximately(9.81, 1e-12);

    [Fact]
    public void To_convertsBetweenUnits()
    {
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(2000);
        a.To<MetrePerSecondSquared<double>>().Value.Should().BeApproximately(2.0, 1e-12);
    }

    [Fact]
    public void Add_acrossUnits_normalisesThroughSi()
    {
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(500);   // 0.5 m/s²
        var b = new Acceleration<double, MetrePerSecondSquared<double>>(1.0);        // 1.0 m/s²
        a.Add(b).ToSi().Should().BeApproximately(1.5, 1e-12);
    }

    [Fact]
    public void Operators_workWithinUnit()
    {
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(100);
        var b = new Acceleration<double, MillimetrePerSecondSquared<double>>(40);
        (a + b).Value.Should().Be(140);
        (a - b).Value.Should().Be(60);
        (a * 2.0).Value.Should().Be(200);
        (a / 4.0).Value.Should().Be(25);
        (a / b).Should().BeApproximately(2.5, 1e-12);
        (a > b).Should().BeTrue();
    }

    [Theory]
    [InlineData("100", 100)]
    [InlineData("100 mm/s²", 100)]
    public void Parse_roundTrips(string s, double expected)
        => Acceleration<double, MillimetrePerSecondSquared<double>>.Parse(s, null).Value.Should().Be(expected);

    [Fact]
    public void ToString_then_Parse_roundTrips()
    {
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(250);
        a.ToString().Should().Be("250 mm/s²");
        Acceleration<double, MillimetrePerSecondSquared<double>>.Parse(a.ToString(), null).Should().Be(a);
    }
}
