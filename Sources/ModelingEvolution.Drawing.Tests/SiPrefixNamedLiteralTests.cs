using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.JsonParsableConverter;

namespace ModelingEvolution.Drawing.Tests;

// NaN / +Infinity / -Infinity must round-trip through every SiPrefix-based unit type's own
// ToString/Parse (2026-11-04: Radian<double>.Parse("NaNrad") threw, a JSON cache file was refused on read).
public class SiPrefixNamedLiteralTests
{
    public static TheoryData<double> NamedLiterals => new()
    {
        double.NaN, double.PositiveInfinity, double.NegativeInfinity
    };

    private static void ShouldBeSame(double actual, double expected)
    {
        if (double.IsNaN(expected)) double.IsNaN(actual).Should().BeTrue($"{actual} should be NaN");
        else actual.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NamedLiterals))]
    public void Radian_Parse_RoundTripsOwnToString(double value)
    {
        var text = Radian<double>.FromRadian(value).ToString();

        var parsed = Radian<double>.Parse(text, null);

        ShouldBeSame((double)parsed, value);
    }

    [Theory]
    [MemberData(nameof(NamedLiterals))]
    public void Radian_TryParse_AcceptsOwnToString(double value)
    {
        var text = Radian<double>.FromRadian(value).ToString();

        Radian<double>.TryParse(text, null, out var parsed).Should().BeTrue($"'{text}' was written by Radian");
        ShouldBeSame((double)parsed, value);
    }

    [Theory]
    [MemberData(nameof(NamedLiterals))]
    public void Degree_ParseAndTryParse_RoundTripOwnToString(double value)
    {
        var text = Degree<double>.Create(value).ToString();

        ShouldBeSame((double)Degree<double>.Parse(text, null), value);
        Degree<double>.TryParse(text, null, out var parsed).Should().BeTrue($"'{text}' was written by Degree");
        ShouldBeSame((double)parsed, value);
    }

    public sealed record Holder(Radian<double> Angle);

    [Fact]
    public void Json_RecordWithNaNRadian_RoundTripsThroughTypeConverter()
    {
        var json = JsonSerializer.Serialize(new Holder(Radian<double>.FromRadian(double.NaN)));

        var back = JsonSerializer.Deserialize<Holder>(json)!;

        json.Should().Contain("NaNrad");
        double.IsNaN((double)back.Angle).Should().BeTrue();
    }

    [Fact]
    public void Json_NaNRadian_RoundTripsThroughJsonParsableConverter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonParsableConverter<Radian<double>>());
        var json = JsonSerializer.Serialize(Radian<double>.FromRadian(double.NaN), options);

        var back = JsonSerializer.Deserialize<Radian<double>>(json, options);

        json.Should().Be("\"NaNrad\"");
        double.IsNaN((double)back).Should().BeTrue();
    }

    [Theory]
    [InlineData("NaNx")]       // unknown trailing letter: rule unchanged
    [InlineData("Infinityk")]  // known prefix on a named literal: no unit type writes it
    [InlineData("NaNm")]
    [InlineData("-Infinitym")]
    [InlineData("nanm")]       // lowercase literals carry the same rule
    [InlineData("infinitym")]
    [InlineData("-infinitym")]
    public void TryParse_NamedLiteralWithTrailingLetter_IsRefused(string input)
    {
        SiPrefix.TryParse<double>(input, "", null, out _).Should().BeFalse();
        Radian<double>.TryParse(input + "rad", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_PlusInfinity_AcceptedAsTheNumericTypeDoes()
    {
        SiPrefix.TryParse<double>("+Infinityrad", "rad", null, out var value).Should().BeTrue();
        value.Should().Be(double.PositiveInfinity);
    }

    [Theory]
    [InlineData("nanrad", double.NaN)]
    [InlineData("infinityrad", double.PositiveInfinity)]
    [InlineData("-INFINITYrad", double.NegativeInfinity)]
    public void TryParse_NamedLiteral_AnyCase_Accepted(string input, double expected)
    {
        Radian<double>.TryParse(input, null, out var parsed).Should().BeTrue();
        ShouldBeSame((double)parsed, expected);
    }

    [Fact] // guard: passes on master too
    public void TryParse_IntegerType_RefusesNamedLiteral()
    {
        SiPrefix.TryParse<int>("NaNrad", "rad", null, out _).Should().BeFalse();
    }

    [Fact] // guard: passes on master too
    public void Parse_KiloPrefix_StillScales()
    {
        ((double)Radian<double>.Parse("1.5krad", null)).Should().Be(1500);
    }
}
