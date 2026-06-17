using FluentAssertions;
using ModelingEvolution.Drawing.Units;

namespace ModelingEvolution.Drawing.Tests;

public class UnitSpeedTests
{
    private const double Tol = 1e-9;

    private static Speed<double, MillimetrePerSecond<double>> MmPerS(double v) => new(v);
    private static Speed<double, MetrePerMinute<double>> MPerMin(double v) => new(v);
    private static Speed<double, MetrePerSecond<double>> MPerS(double v) => new(v);

    [Fact]
    public void ToSi_MetrePerSecond_IsIdentity()
    {
        MPerS(7.5).ToSi().Should().BeApproximately(7.5, Tol);
    }

    [Fact]
    public void ToSi_MillimetrePerSecond_DividesByThousand()
    {
        MmPerS(1000).ToSi().Should().BeApproximately(1.0, Tol);
    }

    [Fact]
    public void ToSi_MetrePerMinute_DividesBySixty()
    {
        MPerMin(60).ToSi().Should().BeApproximately(1.0, Tol);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1234.5)]
    [InlineData(-42.0)]
    public void FromSi_RoundTrips_ToSi(double si)
    {
        Speed<double, MetrePerMinute<double>>.FromSi(si).ToSi().Should().BeApproximately(si, Tol);
    }

    [Fact]
    public void To_ConvertsBetweenUnitsOfSameDimension()
    {
        // 60 m/min == 1 m/s == 1000 mm/s
        var inMs = MPerMin(60).To<MetrePerSecond<double>>();
        inMs.Value.Should().BeApproximately(1.0, Tol);

        var inMm = MPerMin(60).To<MillimetrePerSecond<double>>();
        inMm.Value.Should().BeApproximately(1000.0, Tol);
    }

    [Fact]
    public void Operator_Add_SameUnit()
    {
        (MmPerS(10) + MmPerS(5)).Value.Should().BeApproximately(15.0, Tol);
    }

    [Fact]
    public void Operator_Subtract_And_Negate()
    {
        (MmPerS(10) - MmPerS(15)).Value.Should().BeApproximately(-5.0, Tol);
        (-MmPerS(10)).Value.Should().BeApproximately(-10.0, Tol);
    }

    [Fact]
    public void Operator_ScalarMultiplyAndDivide()
    {
        (MmPerS(10) * 3.0).Value.Should().BeApproximately(30.0, Tol);
        (2.0 * MmPerS(10)).Value.Should().BeApproximately(20.0, Tol);
        (MmPerS(10) / 4.0).Value.Should().BeApproximately(2.5, Tol);
    }

    [Fact]
    public void Operator_SpeedDivideSpeed_IsDimensionlessRatio()
    {
        double ratio = MmPerS(30) / MmPerS(10);
        ratio.Should().BeApproximately(3.0, Tol);
    }

    [Fact]
    public void Add_CrossUnit_NormalisesThroughSi_ResultInThisUnit()
    {
        // 1 m/s expressed as 1000 mm/s, added to 500 mm/s => 1500 mm/s
        var result = MmPerS(500).Add(MPerS(1));
        result.Value.Should().BeApproximately(1500.0, Tol);
    }

    [Fact]
    public void Comparisons_OrderByValue()
    {
        (MmPerS(5) < MmPerS(6)).Should().BeTrue();
        (MmPerS(6) > MmPerS(5)).Should().BeTrue();
        (MmPerS(5) <= MmPerS(5)).Should().BeTrue();
        MmPerS(5).CompareTo(MmPerS(6)).Should().BeNegative();
    }

    [Fact]
    public void Equality_IsValueAndUnitBased()
    {
        MmPerS(5).Should().Be(MmPerS(5));
        MmPerS(5).Should().NotBe(MmPerS(6));
    }

    [Fact]
    public void ToString_IncludesSymbol()
    {
        MPerMin(12.5).ToString().Should().Be("12.5 m/min");
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12.5 mm/s", 12.5)]
    [InlineData("  -3 mm/s ", -3.0)]
    public void Parse_AllowsOptionalTrailingSymbol(string input, double expected)
    {
        var parsed = Speed<double, MillimetrePerSecond<double>>.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        parsed.Value.Should().BeApproximately(expected, Tol);
    }

    [Fact]
    public void TryParse_RoundTripsToString()
    {
        var original = MPerMin(33.0);
        Speed<double, MetrePerMinute<double>>.TryParse(original.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            .Should().BeTrue();
        parsed.Value.Should().BeApproximately(33.0, Tol);
    }

    [Fact]
    public void TryParse_GarbageReturnsFalse()
    {
        Speed<double, MetrePerSecond<double>>.TryParse("not-a-number", null, out _).Should().BeFalse();
    }
}
