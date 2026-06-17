using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class DegreeTests
{
    [Fact]
    public void Multiply_ByScalar()
    {
        var d = Degree<double>.Create(45);
        var result = d * 2.0;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Multiply_ScalarByDegree()
    {
        var d = Degree<double>.Create(30);
        var result = 3.0 * d;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Divide_ByScalar()
    {
        var d = Degree<double>.Create(180);
        var result = d / 2.0;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Abs_NegativeDegree()
    {
        var d = Degree<double>.Create(-45);
        ((double)d.Abs()).Should().BeApproximately(45, 1e-9);
    }

    [Fact]
    public void Normalize_LargePositive()
    {
        var d = Degree<double>.Create(270);
        ((double)d.Normalize()).Should().BeApproximately(-90, 1e-9);
    }

    [Fact]
    public void Normalize_LargeNegative()
    {
        var d = Degree<double>.Create(-270);
        ((double)d.Normalize()).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Normalize_180_StaysAt180()
    {
        var d = Degree<double>.Create(180);
        ((double)d.Normalize()).Should().BeApproximately(180, 1e-9);
    }

    [Fact]
    public void Normalize_AlreadyInRange()
    {
        var d = Degree<double>.Create(45);
        ((double)d.Normalize()).Should().BeApproximately(45, 1e-9);
    }

    #region Parsing (SiPrefix)

    [Fact]
    public void Parse_ToString_RoundTrips()
    {
        // Round-trips against the EXACT default ToString output ("{value}°", U+00B0, no space).
        var original = Degree<double>.Create(90);
        Degree<double>.Parse(original.ToString(), null).Should().Be(original);
    }

    [Fact]
    public void Parse_WithUnitSuffix_ReturnsDegrees()
    {
        ((double)Degree<double>.Parse("90°", null)).Should().BeApproximately(90, 1e-9);
        ((double)Degree<double>.Parse("90 °", null)).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Parse_WithSiPrefix_Scales()
    {
        ((double)Degree<double>.Parse("500 m°", null)).Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void Parse_BareNumber_ReturnsDegrees()
    {
        ((double)Degree<double>.Parse("45", null)).Should().BeApproximately(45, 1e-9);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Degree<double>.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_UnknownTrailingLetter_ReturnsFalse()
    {
        Degree<double>.TryParse("90 X°", null, out _).Should().BeFalse();
    }

    #endregion
}
