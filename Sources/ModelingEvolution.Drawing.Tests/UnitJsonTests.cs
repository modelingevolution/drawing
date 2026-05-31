using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// JSON serialization for the quantity types via <see cref="ParsableJsonConverterFactory"/> applied
/// (as a factory attribute) to the OPEN generic types. Values serialize as JSON STRINGS through
/// IParsable's ToString()/Parse() — these tests assert both the exact string form and the
/// round-trip, including BOTH Amps&lt;float&gt; and Amps&lt;double&gt; to prove the open-generic
/// factory closes correctly per T.
/// </summary>
public class UnitJsonTests
{
    // ---- exact serialized string per type (string, not number) ----

    [Fact]
    public void Serialize_EmitsJsonString_PerType()
    {
        JsonSerializer.Serialize(Amps<float>.From(180f)).Should().Be("\"180 A\"");
        JsonSerializer.Serialize(Volts<float>.From(180f)).Should().Be("\"180 V\"");
        JsonSerializer.Serialize(Frequency<float>.FromHertz(180f)).Should().Be("\"180 Hz\"");
        JsonSerializer.Serialize(Speed<float>.From(180f)).Should().Be("\"180 mm/min\"");
        JsonSerializer.Serialize(Radian<float>.FromRadian(180f)).Should().Be("\"180rad\"");
        JsonSerializer.Serialize(Degree<float>.Create(180f)).Should().Be("\"180\\u00B0\"");
    }

    [Fact]
    public void SerializedValue_IsAJsonString_NotANumber()
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(Amps<float>.From(180f)));
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.String);
    }

    // ---- round-trip across representative values, per type ----

    public static TheoryData<float> Values => new() { 0f, -3.5f, 1234.5f };

    [Theory]
    [MemberData(nameof(Values))]
    public void Amps_RoundTrips(float v)
    {
        var original = Amps<float>.From(v);
        JsonSerializer.Deserialize<Amps<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Volts_RoundTrips(float v)
    {
        var original = Volts<float>.From(v);
        JsonSerializer.Deserialize<Volts<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Frequency_RoundTrips(float v)
    {
        var original = Frequency<float>.FromHertz(v);
        JsonSerializer.Deserialize<Frequency<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Speed_RoundTrips(float v)
    {
        var original = Speed<float>.From(v);
        JsonSerializer.Deserialize<Speed<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Radian_RoundTrips(float v)
    {
        var original = Radian<float>.FromRadian(v);
        JsonSerializer.Deserialize<Radian<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Degree_RoundTrips(float v)
    {
        var original = Degree<float>.Create(v);
        JsonSerializer.Deserialize<Degree<float>>(JsonSerializer.Serialize(original)).Should().Be(original);
    }

    // ---- open-generic proof: the SAME factory must close for both float and double ----

    [Fact]
    public void Factory_Closes_For_AmpsFloat()
    {
        var original = Amps<float>.From(42.25f);
        var json = JsonSerializer.Serialize(original);
        json.Should().Be("\"42.25 A\"");
        JsonSerializer.Deserialize<Amps<float>>(json).Should().Be(original);
    }

    [Fact]
    public void Factory_Closes_For_AmpsDouble()
    {
        var original = Amps<double>.From(42.25d);
        var json = JsonSerializer.Serialize(original);
        json.Should().Be("\"42.25 A\"");
        JsonSerializer.Deserialize<Amps<double>>(json).Should().Be(original);
    }

    // ---- containing object ----

    private sealed class WelderReading
    {
        public Amps<float> Current { get; set; }
        public Volts<float> Voltage { get; set; }
        public Speed<float> WireFeed { get; set; }
    }

    [Fact]
    public void ContainingObject_RoundTrips()
    {
        var original = new WelderReading
        {
            Current = Amps<float>.From(184f),
            Voltage = Volts<float>.From(25.3f),
            WireFeed = Speed<float>.From(6.4f),
        };

        var json = JsonSerializer.Serialize(original);
        // properties are emitted as JSON strings
        json.Should().Contain("\"184 A\"").And.Contain("\"25.3 V\"").And.Contain("\"6.4 mm/min\"");

        var clone = JsonSerializer.Deserialize<WelderReading>(json)!;
        clone.Current.Should().Be(original.Current);
        clone.Voltage.Should().Be(original.Voltage);
        clone.WireFeed.Should().Be(original.WireFeed);
    }

    // ---- the factory predicate itself ----

    [Fact]
    public void Factory_CanConvert_AnyIParsableSelf_RejectsNonParsable()
    {
        var f = new ParsableJsonConverterFactory();
        // Our unit types implement IParsable<self> → convertible.
        f.CanConvert(typeof(Amps<float>)).Should().BeTrue();
        f.CanConvert(typeof(Degree<double>)).Should().BeTrue();
        // The factory is intentionally generic over IParsable<self>; int qualifies (BCL IParsable<int>).
        f.CanConvert(typeof(int)).Should().BeTrue();
        // A plain reference type that does not implement IParsable<self> is rejected.
        f.CanConvert(typeof(object)).Should().BeFalse();
    }
}
