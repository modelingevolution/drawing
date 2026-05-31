using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class SiPrefixTests
{
    private const double Tol = 1e-9;

    // Full prefix table, both directions of the scale. Unit "A" used as the carrier.
    [Theory]
    [InlineData("1 TA", 1e12)]
    [InlineData("1 GA", 1e9)]
    [InlineData("1 MA", 1e6)]
    [InlineData("1 kA", 1e3)]
    [InlineData("1 A", 1e0)]
    [InlineData("1 mA", 1e-3)]
    [InlineData("1 µA", 1e-6)]   // U+00B5 micro sign
    [InlineData("1 uA", 1e-6)]   // ASCII alias for micro
    [InlineData("1 nA", 1e-9)]
    [InlineData("1 pA", 1e-12)]
    public void TryParse_EveryPrefix_Scales(string input, double expected)
    {
        SiPrefix.TryParse<double>(input, "A", null, out var v).Should().BeTrue();
        v.Should().BeApproximately(expected, Math.Abs(expected) * 1e-9 + Tol);
    }

    [Theory]
    [InlineData("180", 180)]      // bare number, no unit, exponent 0
    [InlineData("180 A", 180)]    // unit, no prefix
    [InlineData("180A", 180)]     // unit, no space
    [InlineData("1.5 kA", 1500)]
    [InlineData("500 mA", 0.5)]
    [InlineData("-2.5 kA", -2500)]
    public void TryParse_CommonForms(string input, double expected)
    {
        SiPrefix.TryParse<double>(input, "A", null, out var v).Should().BeTrue();
        v.Should().BeApproximately(expected, Tol);
    }

    [Fact]
    public void Prefix_IsCaseSensitive()
    {
        SiPrefix.TryParse<double>("1 mA", "A", null, out var milli).Should().BeTrue();
        SiPrefix.TryParse<double>("1 MA", "A", null, out var mega).Should().BeTrue();
        milli.Should().BeApproximately(1e-3, Tol);
        mega.Should().BeApproximately(1e6, Tol);
        // m (milli) must NOT equal M (mega)
        milli.Should().NotBe(mega);
    }

    [Fact]
    public void Kilo_AcceptsUppercaseK_AsInputAlias()
    {
        // 'K' is a lenient input alias for kilo: "1 KA" == 1000 == "1 kA"
        SiPrefix.TryParse<double>("1 KA", "A", null, out var upperK).Should().BeTrue();
        SiPrefix.TryParse<double>("1 kA", "A", null, out var lowerK).Should().BeTrue();
        upperK.Should().BeApproximately(1000, Tol);
        upperK.Should().Be(lowerK);
    }

    [Fact]
    public void Micro_AcceptsBothSignAndAsciiU()
    {
        SiPrefix.TryParse<double>("1 µA", "A", null, out var sign).Should().BeTrue();
        SiPrefix.TryParse<double>("1 uA", "A", null, out var ascii).Should().BeTrue();
        sign.Should().Be(ascii);
        sign.Should().BeApproximately(1e-6, Tol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NullOrWhitespace_ReturnsFalse(string? input)
        => SiPrefix.TryParse<double>(input, "A", null, out _).Should().BeFalse();

    [Theory]
    [InlineData("180 XA")]   // unknown prefix letter X
    [InlineData("not-a-number")]
    [InlineData("X A")]
    [InlineData("A")]        // unit only, no number
    public void TryParse_Invalid_ReturnsFalse(string input)
        => SiPrefix.TryParse<double>(input, "A", null, out _).Should().BeFalse();

    [Fact]
    public void TryParse_EmptyUnit_StillAcceptsPrefix()
    {
        // unit="" → no unit to strip; a trailing SI prefix is still honoured.
        SiPrefix.TryParse<double>("2 k", "", null, out var v).Should().BeTrue();
        v.Should().BeApproximately(2000, Tol);
    }

    [Fact]
    public void TryParse_EmptyUnit_BareNumber()
    {
        SiPrefix.TryParse<double>("42.5", "", null, out var v).Should().BeTrue();
        v.Should().BeApproximately(42.5, Tol);
    }

    [Fact]
    public void Parse_Invalid_Throws()
    {
        Action act = () => SiPrefix.Parse<double>("180 XA", "A", null);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Action act = () => SiPrefix.Parse<double>(null!, "A", null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryParse_MultiCharUnit_StrippedBeforePrefixCheck()
    {
        // "u/s" must be stripped whole so its leading 'u' is NOT mistaken for the micro prefix.
        SiPrefix.TryParse<double>("5 u/s", "u/s", null, out var plain).Should().BeTrue();
        plain.Should().BeApproximately(5, Tol);

        // and an SI prefix on the value still applies after the unit is removed
        SiPrefix.TryParse<double>("1.5 ku/s", "u/s", null, out var kilo).Should().BeTrue();
        kilo.Should().BeApproximately(1500, Tol);
    }
}
