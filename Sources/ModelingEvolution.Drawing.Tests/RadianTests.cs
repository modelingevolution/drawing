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

    #region Parsing (SiPrefix)

    [Fact]
    public void Parse_ToString_RoundTrips()
    {
        // Round-trips against the EXACT default ToString output ("{value}rad", no space).
        var original = Radian<double>.FromRadian(1.5);
        Radian<double>.Parse(original.ToString(), null).Should().Be(original);
    }

    [Fact]
    public void Parse_WithUnitSuffix_ReturnsRadians()
    {
        ((double)Radian<double>.Parse("1.5rad", null)).Should().BeApproximately(1.5, 1e-9);
        ((double)Radian<double>.Parse("1.5 rad", null)).Should().BeApproximately(1.5, 1e-9);
    }

    [Fact]
    public void Parse_WithSiPrefix_Scales()
    {
        // milliradians are a real unit — "1.5 mrad" = 1.5 × 10^-3 = 0.0015
        ((double)Radian<double>.Parse("1.5 mrad", null)).Should().BeApproximately(0.0015, 1e-12);
        ((double)Radian<double>.Parse("500 mrad", null)).Should().BeApproximately(0.5, 1e-9);
        ((double)Radian<double>.Parse("2 krad", null)).Should().BeApproximately(2000, 1e-9);
        // µrad (micro) via both the µ sign and the ASCII 'u' alias
        ((double)Radian<double>.Parse("250 µrad", null)).Should().BeApproximately(0.00025, 1e-12);
        ((double)Radian<double>.Parse("250 urad", null)).Should().BeApproximately(0.00025, 1e-12);
    }

    [Fact]
    public void Parse_BareNumber_ReturnsRadians()
    {
        ((double)Radian<double>.Parse("3.14", null)).Should().BeApproximately(3.14, 1e-9);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Radian<double>.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_UnknownTrailingLetter_ReturnsFalse()
    {
        Radian<double>.TryParse("1.5 Xrad", null, out _).Should().BeFalse();
    }

    #endregion
}
