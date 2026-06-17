using FluentAssertions;
using ModelingEvolution.Drawing;
using ProtoBuf;

namespace ModelingEvolution.Drawing.Tests;

public class AmpsTests
{
    private const float Tol = 1e-4f;

    #region Factories and conversions

    [Fact]
    public void From_StoresValueInAmperes()
    {
        var a = Amps<float>.From(180f);
        ((float)a).Should().BeApproximately(180f, Tol);
    }

    [Fact]
    public void Zero_IsZeroAmperes()
    {
        ((float)Amps<float>.Zero).Should().Be(0f);
    }

    [Fact]
    public void ImplicitConversion_FromT_TreatedAsAmperes()
    {
        Amps<float> a = 42f;
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void ExplicitConversion_ToT_ReturnsAmperes()
    {
        var a = Amps<float>.From(42f);
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void Addition()
    {
        var a = Amps<float>.From(100f);
        var b = Amps<float>.From(50f);
        ((float)(a + b)).Should().BeApproximately(150f, Tol);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Amps<float>.From(100f);
        var b = Amps<float>.From(30f);
        ((float)(a - b)).Should().BeApproximately(70f, Tol);
    }

    [Fact]
    public void Negation()
    {
        var a = Amps<float>.From(180f);
        ((float)(-a)).Should().BeApproximately(-180f, Tol);
    }

    [Fact]
    public void ScalarMultiplication_RightSide()
    {
        var a = Amps<float>.From(100f);
        ((float)(a * 2f)).Should().BeApproximately(200f, Tol);
    }

    [Fact]
    public void ScalarMultiplication_LeftSide()
    {
        var a = Amps<float>.From(100f);
        ((float)(2f * a)).Should().BeApproximately(200f, Tol);
    }

    [Fact]
    public void Division_ByScalar()
    {
        var a = Amps<float>.From(200f);
        ((float)(a / 2f)).Should().BeApproximately(100f, Tol);
    }

    [Fact]
    public void Abs_OfNegative_IsPositive()
    {
        var a = Amps<float>.From(-150f);
        ((float)a.Abs()).Should().BeApproximately(150f, Tol);
    }

    #endregion

    #region Comparison

#pragma warning disable CS1718 // intentional reflexivity checks
    [Fact]
    public void LessThan()
    {
        (Amps<float>.From(10f) < Amps<float>.From(20f)).Should().BeTrue();
        (Amps<float>.From(20f) < Amps<float>.From(10f)).Should().BeFalse();
        var a = Amps<float>.From(10f);
        (a < a).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan()
    {
        (Amps<float>.From(20f) > Amps<float>.From(10f)).Should().BeTrue();
        var a = Amps<float>.From(10f);
        (a > a).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual()
    {
        var a = Amps<float>.From(10f);
        (a <= a).Should().BeTrue();
        (a <= Amps<float>.From(20f)).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOrEqual()
    {
        var a = Amps<float>.From(10f);
        (a >= a).Should().BeTrue();
        (a >= Amps<float>.From(5f)).Should().BeTrue();
    }
#pragma warning restore CS1718

    [Fact]
    public void CompareTo_Amps_ReturnsExpectedSign()
    {
        Amps<float>.From(10f).CompareTo(Amps<float>.From(20f)).Should().BeLessThan(0);
        Amps<float>.From(20f).CompareTo(Amps<float>.From(10f)).Should().BeGreaterThan(0);
        Amps<float>.From(10f).CompareTo(Amps<float>.From(10f)).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_NullReturnsOne()
    {
        Amps<float>.From(10f).CompareTo(null).Should().Be(1);
    }

    [Fact]
    public void CompareTo_Object_WrongTypeThrows()
    {
        Action act = () => Amps<float>.From(10f).CompareTo("not amps");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Parsing

    [Fact]
    public void Parse_NumericString_ReturnsAmps()
    {
        var a = Amps<float>.Parse("180.5", null);
        ((float)a).Should().BeApproximately(180.5f, Tol);
    }

    [Fact]
    public void TryParse_ValidNumeric_ReturnsTrue()
    {
        Amps<float>.TryParse("42", null, out var a).Should().BeTrue();
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Amps<float>.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse()
    {
        Amps<float>.TryParse("not-a-number", null, out _).Should().BeFalse();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_IncludesValueAndAmpereSuffix()
    {
        Amps<float>.From(180f).ToString().Should().Contain("180").And.Contain("A");
    }

    [Fact]
    public void Parse_ToString_RoundTrips()
    {
        var original = Amps<float>.From(180.5f);
        Amps<float>.Parse(original.ToString(), null).Should().Be(original);
    }

    [Fact]
    public void Parse_WithUnitSuffix_ReturnsAmps()
    {
        ((float)Amps<float>.Parse("180 A", null)).Should().BeApproximately(180f, Tol);
        ((float)Amps<float>.Parse("180A", null)).Should().BeApproximately(180f, Tol);
    }

    [Fact]
    public void Parse_WithSiPrefix_Scales()
    {
        ((float)Amps<float>.Parse("1.5 kA", null)).Should().BeApproximately(1500f, Tol);
        ((float)Amps<float>.Parse("500 mA", null)).Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void Parse_PrefixIsCaseSensitive()
    {
        // m = milli (1e-3), M = mega (1e6) — must differ
        ((float)Amps<float>.Parse("1 mA", null)).Should().BeApproximately(0.001f, Tol);
        ((float)Amps<float>.Parse("1 MA", null)).Should().BeApproximately(1_000_000f, Tol);
        Amps<float>.Parse("1 mA", null).Should().NotBe(Amps<float>.Parse("1 MA", null));
    }

    [Fact]
    public void Parse_KiloAcceptsUppercaseKAlias()
    {
        // "1 KA" == 1000 A == "1 kA" — uppercase K is a lenient input alias for kilo
        ((float)Amps<float>.Parse("1 KA", null)).Should().BeApproximately(1000f, Tol);
        Amps<float>.Parse("1 KA", null).Should().Be(Amps<float>.Parse("1 kA", null));
    }

    [Fact]
    public void TryParse_UnknownTrailingLetter_ReturnsFalse()
    {
        Amps<float>.TryParse("180 XA", null, out _).Should().BeFalse();
    }

    #endregion

    #region ProtoContract

    [Fact]
    public void ProtoBuf_RoundTrip_PreservesValue()
    {
        var original = Amps<float>.From(123.456f);
        var clone = Serializer.DeepClone(original);
        ((float)clone).Should().BeApproximately(123.456f, Tol);
    }

    [Fact]
    public void ProtoBuf_RoundTrip_ZeroValue()
    {
        var clone = Serializer.DeepClone(Amps<float>.Zero);
        ((float)clone).Should().Be(0f);
    }

    #endregion
}
