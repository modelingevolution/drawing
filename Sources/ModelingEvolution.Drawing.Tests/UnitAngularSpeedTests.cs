using FluentAssertions;
using ModelingEvolution.Drawing.Units;

namespace ModelingEvolution.Drawing.Tests;

public class UnitAngularSpeedTests
{
    private const double Tol = 1e-9;

    private static AngularSpeed<double, DegreePerSecond<double>> DegPerS(double v) => new(v);
    private static AngularSpeed<double, RadianPerSecond<double>> RadPerS(double v) => new(v);

    [Fact]
    public void Construction_StoresValueInItsOwnUnit()
    {
        DegPerS(12.5).Value.Should().BeApproximately(12.5, Tol);
        AngularSpeed<double, DegreePerSecond<double>>.Zero.Value.Should().Be(0.0);
    }

    [Fact]
    public void ToSi_RadianPerSecond_IsIdentity()
    {
        RadPerS(7.5).ToSi().Should().BeApproximately(7.5, Tol);
    }

    [Fact]
    public void ToSi_DegreePerSecond_ScalesByPiOver180()
    {
        DegPerS(180).ToSi().Should().BeApproximately(Math.PI, Tol);
        DegPerS(90).ToSi().Should().BeApproximately(Math.PI / 2, Tol);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1234.5)]
    [InlineData(-42.0)]
    public void FromSi_RoundTrips_ToSi(double si)
    {
        AngularSpeed<double, DegreePerSecond<double>>.FromSi(si).ToSi().Should().BeApproximately(si, Tol);
    }

    [Fact]
    public void To_ConvertsBetweenUnitsOfSameDimension()
    {
        // 180 °/s == π rad/s
        DegPerS(180).To<RadianPerSecond<double>>().Value.Should().BeApproximately(Math.PI, Tol);
        RadPerS(Math.PI).To<DegreePerSecond<double>>().Value.Should().BeApproximately(180.0, Tol);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    [InlineData(-33.75)]
    [InlineData(360.0)]
    public void To_RoundTripsBackToTheOriginalUnit(double degPerSecond)
    {
        DegPerS(degPerSecond)
            .To<RadianPerSecond<double>>()
            .To<DegreePerSecond<double>>()
            .Value.Should().BeApproximately(degPerSecond, Tol);
    }

    [Fact]
    public void Operator_Add_SameUnit()
    {
        (DegPerS(10) + DegPerS(5)).Value.Should().BeApproximately(15.0, Tol);
    }

    [Fact]
    public void Operator_Subtract_And_Negate()
    {
        (DegPerS(10) - DegPerS(15)).Value.Should().BeApproximately(-5.0, Tol);
        (-DegPerS(10)).Value.Should().BeApproximately(-10.0, Tol);
    }

    [Fact]
    public void Negation_ReversesTheSenseOfRotation()
    {
        // Signed velocity is a contract requirement: negating flips the sense, magnitude is preserved.
        var reversed = -DegPerS(7.5);
        reversed.Value.Should().BeApproximately(-7.5, Tol);
        reversed.ToSi().Should().BeApproximately(-DegPerS(7.5).ToSi(), Tol);
        reversed.Abs().Value.Should().BeApproximately(7.5, Tol);
    }

    [Fact]
    public void Operator_ScalarMultiplyAndDivide()
    {
        (DegPerS(10) * 3.0).Value.Should().BeApproximately(30.0, Tol);
        (2.0 * DegPerS(10)).Value.Should().BeApproximately(20.0, Tol);
        (DegPerS(10) / 4.0).Value.Should().BeApproximately(2.5, Tol);
    }

    [Fact]
    public void Operator_PercentageScaling_BothDirections()
    {
        (DegPerS(10) * new Percentage(50f)).Value.Should().BeApproximately(5.0, 1e-4);
        (new Percentage(50f) * DegPerS(10)).Value.Should().BeApproximately(5.0, 1e-4);
    }

    [Fact]
    public void Operator_PercentageDivide_RecoversTheFullSpeed()
    {
        (DegPerS(5) / new Percentage(50f)).Value.Should().BeApproximately(10.0, 1e-4);
    }

    [Fact]
    public void Operator_AngularSpeedDivideAngularSpeed_IsDimensionlessRatio()
    {
        double ratio = DegPerS(30) / DegPerS(10);
        ratio.Should().BeApproximately(3.0, Tol);
    }

    [Fact]
    public void Add_CrossUnit_NormalisesThroughSi_ResultInThisUnit()
    {
        // π rad/s expressed as 180 °/s, added to 90 °/s => 270 °/s
        DegPerS(90).Add(RadPerS(Math.PI)).Value.Should().BeApproximately(270.0, Tol);
    }

    [Fact]
    public void Subtract_CrossUnit_NormalisesThroughSi_ResultInThisUnit()
    {
        DegPerS(270).Subtract(RadPerS(Math.PI)).Value.Should().BeApproximately(90.0, Tol);
    }

    [Fact]
    public void Comparisons_OrderByValue()
    {
        (DegPerS(5) < DegPerS(6)).Should().BeTrue();
        (DegPerS(6) > DegPerS(5)).Should().BeTrue();
        (DegPerS(5) <= DegPerS(5)).Should().BeTrue();
        (DegPerS(5) >= DegPerS(5)).Should().BeTrue();
        DegPerS(5).CompareTo(DegPerS(6)).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_Object_ThrowsOnForeignType()
    {
        DegPerS(5).CompareTo(null).Should().Be(1);
        var act = () => DegPerS(5).CompareTo("not an angular speed");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsValueAndUnitBased()
    {
        DegPerS(5).Should().Be(DegPerS(5));
        DegPerS(5).Should().NotBe(DegPerS(6));
    }

    [Fact]
    public void ToString_IncludesSymbol()
    {
        DegPerS(12.5).ToString().Should().Be("12.5 °/s");
        RadPerS(12.5).ToString().Should().Be("12.5 rad/s");
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12.5 °/s", 12.5)]
    [InlineData("  -3 °/s ", -3.0)]
    public void Parse_AllowsOptionalTrailingSymbol(string input, double expected)
    {
        var parsed = AngularSpeed<double, DegreePerSecond<double>>.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        parsed.Value.Should().BeApproximately(expected, Tol);
    }

    [Fact]
    public void Parse_RadianPerSecond_AllowsItsOwnSymbol()
    {
        AngularSpeed<double, RadianPerSecond<double>>
            .Parse("1.5 rad/s", System.Globalization.CultureInfo.InvariantCulture)
            .Value.Should().BeApproximately(1.5, Tol);
    }

    [Fact]
    public void TryParse_RoundTripsToString()
    {
        var original = DegPerS(33.0);
        AngularSpeed<double, DegreePerSecond<double>>.TryParse(original.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            .Should().BeTrue();
        parsed.Value.Should().BeApproximately(33.0, Tol);
    }

    [Fact]
    public void TryParse_GarbageReturnsFalse()
    {
        AngularSpeed<double, RadianPerSecond<double>>.TryParse("not-a-number", null, out _).Should().BeFalse();
    }

    [Fact]
    public void AngularSpeedUnits_AreADimensionOfTheirOwn_NotSpeedUnits()
    {
        // The point of the separate dimension: an angular-speed unit must not be usable where a linear
        // speed unit is expected, so Speed<T, DegreePerSecond<T>> cannot be spelled at all.
        // Asserted by reflection rather than BeAssignableTo<T>(): an interface carrying static-abstract
        // members cannot be used as a generic type argument (CS8920).
        typeof(DegreePerSecond<double>).IsAssignableTo(typeof(IAngularSpeedUnit<double>)).Should().BeTrue();
        typeof(RadianPerSecond<double>).IsAssignableTo(typeof(IAngularSpeedUnit<double>)).Should().BeTrue();
        typeof(DegreePerSecond<double>).IsAssignableTo(typeof(ISpeedUnit<double>)).Should().BeFalse();
        typeof(RadianPerSecond<double>).IsAssignableTo(typeof(ISpeedUnit<double>)).Should().BeFalse();
        typeof(MetrePerSecond<double>).IsAssignableTo(typeof(IAngularSpeedUnit<double>)).Should().BeFalse();
    }

    [Fact]
    public void Symbols_AreTheExpectedEngineeringUnits()
    {
        DegreePerSecond<double>.Symbol.Should().Be("°/s");
        RadianPerSecond<double>.Symbol.Should().Be("rad/s");
    }
}
