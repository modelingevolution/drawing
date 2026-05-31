using FluentAssertions;
using ModelingEvolution.Drawing;
using ProtoBuf;

namespace ModelingEvolution.Drawing.Tests;

public class VoltsTests
{
    private const float Tol = 1e-4f;

    #region Factories and conversions

    [Fact]
    public void From_StoresValueInVolts()
    {
        var a = Volts<float>.From(180f);
        ((float)a).Should().BeApproximately(180f, Tol);
    }

    [Fact]
    public void Zero_IsZeroVolts()
    {
        ((float)Volts<float>.Zero).Should().Be(0f);
    }

    [Fact]
    public void ImplicitConversion_FromT_TreatedAsVolts()
    {
        Volts<float> a = 42f;
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void ExplicitConversion_ToT_ReturnsVolts()
    {
        var a = Volts<float>.From(42f);
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void Addition()
    {
        var a = Volts<float>.From(100f);
        var b = Volts<float>.From(50f);
        ((float)(a + b)).Should().BeApproximately(150f, Tol);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Volts<float>.From(100f);
        var b = Volts<float>.From(30f);
        ((float)(a - b)).Should().BeApproximately(70f, Tol);
    }

    [Fact]
    public void Negation()
    {
        var a = Volts<float>.From(180f);
        ((float)(-a)).Should().BeApproximately(-180f, Tol);
    }

    [Fact]
    public void ScalarMultiplication_RightSide()
    {
        var a = Volts<float>.From(100f);
        ((float)(a * 2f)).Should().BeApproximately(200f, Tol);
    }

    [Fact]
    public void ScalarMultiplication_LeftSide()
    {
        var a = Volts<float>.From(100f);
        ((float)(2f * a)).Should().BeApproximately(200f, Tol);
    }

    [Fact]
    public void Division_ByScalar()
    {
        var a = Volts<float>.From(200f);
        ((float)(a / 2f)).Should().BeApproximately(100f, Tol);
    }

    [Fact]
    public void Abs_OfNegative_IsPositive()
    {
        var a = Volts<float>.From(-150f);
        ((float)a.Abs()).Should().BeApproximately(150f, Tol);
    }

    #endregion

    #region Comparison

#pragma warning disable CS1718 // intentional reflexivity checks
    [Fact]
    public void LessThan()
    {
        (Volts<float>.From(10f) < Volts<float>.From(20f)).Should().BeTrue();
        (Volts<float>.From(20f) < Volts<float>.From(10f)).Should().BeFalse();
        var a = Volts<float>.From(10f);
        (a < a).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan()
    {
        (Volts<float>.From(20f) > Volts<float>.From(10f)).Should().BeTrue();
        var a = Volts<float>.From(10f);
        (a > a).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual()
    {
        var a = Volts<float>.From(10f);
        (a <= a).Should().BeTrue();
        (a <= Volts<float>.From(20f)).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOrEqual()
    {
        var a = Volts<float>.From(10f);
        (a >= a).Should().BeTrue();
        (a >= Volts<float>.From(5f)).Should().BeTrue();
    }
#pragma warning restore CS1718

    [Fact]
    public void CompareTo_Volts_ReturnsExpectedSign()
    {
        Volts<float>.From(10f).CompareTo(Volts<float>.From(20f)).Should().BeLessThan(0);
        Volts<float>.From(20f).CompareTo(Volts<float>.From(10f)).Should().BeGreaterThan(0);
        Volts<float>.From(10f).CompareTo(Volts<float>.From(10f)).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_NullReturnsOne()
    {
        Volts<float>.From(10f).CompareTo(null).Should().Be(1);
    }

    [Fact]
    public void CompareTo_Object_WrongTypeThrows()
    {
        Action act = () => Volts<float>.From(10f).CompareTo("not volts");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Parsing

    [Fact]
    public void Parse_NumericString_ReturnsVolts()
    {
        var a = Volts<float>.Parse("180.5", null);
        ((float)a).Should().BeApproximately(180.5f, Tol);
    }

    [Fact]
    public void TryParse_ValidNumeric_ReturnsTrue()
    {
        Volts<float>.TryParse("42", null, out var a).Should().BeTrue();
        ((float)a).Should().BeApproximately(42f, Tol);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Volts<float>.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse()
    {
        Volts<float>.TryParse("not-a-number", null, out _).Should().BeFalse();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_IncludesValueAndVoltSuffix()
    {
        Volts<float>.From(180f).ToString().Should().Contain("180").And.Contain("V");
    }

    #endregion

    #region ProtoContract

    [Fact]
    public void ProtoBuf_RoundTrip_PreservesValue()
    {
        var original = Volts<float>.From(123.456f);
        var clone = Serializer.DeepClone(original);
        ((float)clone).Should().BeApproximately(123.456f, Tol);
    }

    [Fact]
    public void ProtoBuf_RoundTrip_ZeroValue()
    {
        var clone = Serializer.DeepClone(Volts<float>.Zero);
        ((float)clone).Should().Be(0f);
    }

    #endregion
}
