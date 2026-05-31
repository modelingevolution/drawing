using FluentAssertions;
using ModelingEvolution.Drawing;
using ProtoBuf;

namespace ModelingEvolution.Drawing.Tests;

public class FrequencyTests
{
    private const float Tol = 1e-4f;

    #region Factories

    [Fact]
    public void FromHertz_StoresValueInHertz()
    {
        var f = Frequency<float>.FromHertz(100f);
        f.Hertz.Should().BeApproximately(100f, Tol);
    }

    [Fact]
    public void FromKilohertz_ConvertsToHertz()
    {
        var f = Frequency<float>.FromKilohertz(1.5f);
        f.Hertz.Should().BeApproximately(1500f, Tol);
    }

    [Fact]
    public void Zero_IsZeroHertz()
    {
        Frequency<float>.Zero.Hertz.Should().Be(0f);
    }

    [Fact]
    public void ImplicitConversion_FromT_TreatedAsHertz()
    {
        Frequency<float> f = 42f;
        f.Hertz.Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void ExplicitConversion_ToT_ReturnsHertz()
    {
        var f = Frequency<float>.FromHertz(42f);
        ((float)f).Should().BeApproximately(42f, Tol);
    }

    #endregion

    #region Period

    [Fact]
    public void Period_AtTenHertz_IsOneHundredMilliseconds()
    {
        var f = Frequency<float>.FromHertz(10f);
        f.Period.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Period_AtOneThousandHertz_IsOneMillisecond()
    {
        var f = Frequency<float>.FromHertz(1000f);
        f.Period.Should().Be(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Period_AtZeroHertz_IsTimeSpanMaxValue()
    {
        var f = Frequency<float>.FromHertz(0f);
        f.Period.Should().Be(TimeSpan.MaxValue);
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void Addition()
    {
        var a = Frequency<float>.FromHertz(10f);
        var b = Frequency<float>.FromHertz(5f);
        (a + b).Hertz.Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Frequency<float>.FromHertz(10f);
        var b = Frequency<float>.FromHertz(3f);
        (a - b).Hertz.Should().BeApproximately(7f, Tol);
    }

    [Fact]
    public void Negation()
    {
        var f = Frequency<float>.FromHertz(10f);
        (-f).Hertz.Should().BeApproximately(-10f, Tol);
    }

    [Fact]
    public void MultiplyByScalar_BothOrders()
    {
        var f = Frequency<float>.FromHertz(5f);
        (f * 3f).Hertz.Should().BeApproximately(15f, Tol);
        (3f * f).Hertz.Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void DivideByScalar()
    {
        var f = Frequency<float>.FromHertz(15f);
        (f / 3f).Hertz.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void Abs_ReturnsPositive()
    {
        var f = Frequency<float>.FromHertz(-10f);
        f.Abs().Hertz.Should().BeApproximately(10f, Tol);
    }

    #endregion

    #region Comparisons

    [Fact]
    public void Comparisons_RelationalOperators()
    {
        var a = Frequency<float>.FromHertz(5f);
        var b = Frequency<float>.FromHertz(10f);
        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
#pragma warning disable CS1718
        (a <= a).Should().BeTrue();
        (a >= a).Should().BeTrue();
#pragma warning restore CS1718
        (b < a).Should().BeFalse();
        (a > b).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameHertz_AreEqual()
    {
        var a = Frequency<float>.FromHertz(10f);
        var b = Frequency<float>.FromHertz(10f);
        a.Should().Be(b);
    }

    [Fact]
    public void CompareTo_Generic_OrdersByHertz()
    {
        var a = Frequency<float>.FromHertz(5f);
        var b = Frequency<float>.FromHertz(10f);
        a.CompareTo(b).Should().BeLessThan(0);
        b.CompareTo(a).Should().BeGreaterThan(0);
        a.CompareTo(a).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_NullReturnsPositive()
    {
        var a = Frequency<float>.FromHertz(5f);
        a.CompareTo((object?)null).Should().Be(1);
    }

    [Fact]
    public void CompareTo_Object_WrongTypeThrows()
    {
        var a = Frequency<float>.FromHertz(5f);
        Action act = () => a.CompareTo((object)"not a frequency");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Parsing

    [Fact]
    public void TryParse_ValidInteger_Succeeds()
    {
        Frequency<float>.TryParse("100", null, out var result).Should().BeTrue();
        result.Hertz.Should().BeApproximately(100f, Tol);
    }

    [Fact]
    public void Parse_ValidDecimal_RoundTrips()
    {
        var parsed = Frequency<float>.Parse("1.5", null);
        parsed.Hertz.Should().BeApproximately(1.5f, Tol);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Frequency<float>.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse()
    {
        Frequency<float>.TryParse("not-a-number", null, out _).Should().BeFalse();
    }

    [Fact]
    public void Parse_ToString_RoundTrips()
    {
        var original = Frequency<float>.FromHertz(180.5f);
        Frequency<float>.Parse(original.ToString(), null).Should().Be(original);
    }

    [Fact]
    public void Parse_WithUnitSuffix_ReturnsHertz()
    {
        Frequency<float>.Parse("180 Hz", null).Hertz.Should().BeApproximately(180f, Tol);
        Frequency<float>.Parse("180Hz", null).Hertz.Should().BeApproximately(180f, Tol);
    }

    [Fact]
    public void Parse_WithSiPrefix_Scales()
    {
        Frequency<float>.Parse("2 MHz", null).Hertz.Should().BeApproximately(2_000_000f, Tol);
        Frequency<float>.Parse("1.5 kHz", null).Hertz.Should().BeApproximately(1500f, Tol);
        Frequency<float>.Parse("500 mHz", null).Hertz.Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void Parse_PrefixIsCaseSensitive()
    {
        Frequency<float>.Parse("1 mHz", null).Hertz.Should().BeApproximately(0.001f, Tol);
        Frequency<float>.Parse("1 MHz", null).Hertz.Should().BeApproximately(1_000_000f, Tol);
    }

    [Fact]
    public void TryParse_UnknownTrailingLetter_ReturnsFalse()
    {
        Frequency<float>.TryParse("180 XHz", null, out _).Should().BeFalse();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsValueAndHzUnit()
    {
        var f = Frequency<float>.FromHertz(10f);
        var s = f.ToString();
        s.Should().Contain("10");
        s.Should().Contain("Hz");
    }

    #endregion

    #region ProtoContract

    [Fact]
    public void ProtoBuf_DeepClone_PreservesHertz()
    {
        var original = Frequency<float>.FromHertz(123.456f);
        var clone = Serializer.DeepClone(original);
        clone.Hertz.Should().BeApproximately(123.456f, Tol);
    }

    [Fact]
    public void ProtoBuf_DeepClone_PreservesZero()
    {
        var original = Frequency<float>.Zero;
        var clone = Serializer.DeepClone(original);
        clone.Hertz.Should().Be(0f);
    }

    #endregion
}
