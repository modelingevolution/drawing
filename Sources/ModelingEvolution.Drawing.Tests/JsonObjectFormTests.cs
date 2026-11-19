using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>Parse both, write one: the array converters also read the object form; the object converters write it.</summary>
public class JsonObjectFormTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static readonly JsonSerializerOptions ObjectWire = new(JsonSerializerDefaults.Web)
    {
        Converters = { new Pose3ObjectJsonConverter(), new Joints6ObjectJsonConverter(), new Point3ObjectJsonConverter(),
            new Vector3ObjectJsonConverter() }
    };

    [Theory]
    [InlineData("[1,2,3,10,20,30]")]
    [InlineData("""{"x":1,"y":2,"z":3,"rx":10,"ry":20,"rz":30}""")]
    [InlineData("""{"Rz":30,"X":1,"Y":2,"Z":3,"Rx":10,"Ry":20,"extra":true}""")]
    public void Pose3_ReadsTheArrayAndTheObject(string json)
    {
        var expected = new Pose3<double>(1, 2, 3, 10, 20, 30);
        JsonSerializer.Deserialize<Pose3<double>>(json, Web).Should().Be(expected);
        JsonSerializer.Deserialize<Pose3<double>>(json, ObjectWire).Should().Be(expected);
    }

    [Theory]
    [InlineData("[1,2,3,4,5,6]")]
    [InlineData("""{"j1":1,"j2":2,"j3":3,"j4":4,"j5":5,"j6":6}""")]
    public void Joints6_ReadsTheArrayAndTheObject(string json) =>
        JsonSerializer.Deserialize<Joints6<double>>(json, Web).Should().Be(new Joints6<double>(1, 2, 3, 4, 5, 6));

    [Theory]
    [InlineData("[1,2,3]")]
    [InlineData("""{"x":1,"y":2,"z":3}""")]
    public void Point3_And_Vector3_ReadTheArrayAndTheObject(string json)
    {
        JsonSerializer.Deserialize<Point3<double>>(json, Web).Should().Be(new Point3<double>(1, 2, 3));
        JsonSerializer.Deserialize<Vector3<double>>(json, Web).Should().Be(new Vector3<double>(1, 2, 3));
    }

    [Theory]
    [InlineData("""{"x":1,"y":2}""")]
    [InlineData("""{"x":1,"y":2,"z":"3"}""")]
    public void AnObjectMissingOrMistypingAMember_IsRefused(string json) =>
        FluentActions.Invoking(() => JsonSerializer.Deserialize<Point3<double>>(json, Web)).Should().Throw<JsonException>();

    [Fact]
    public void TheArrayStaysTheWrittenForm_TheObjectConvertersWriteTheObject()
    {
        JsonSerializer.Serialize(new Pose3<double>(1, 2, 3, 10, 20, 30), Web).Should().Be("[1,2,3,10,20,30]");
        JsonSerializer.Serialize(new Pose3<double>(1, 2, 3, 10, 20, 30), ObjectWire)
            .Should().Be("""{"x":1,"y":2,"z":3,"rx":10,"ry":20,"rz":30}""");
        JsonSerializer.Serialize(new Joints6<double>(1, 2, 3, 4, 5, 6), ObjectWire)
            .Should().Be("""{"j1":1,"j2":2,"j3":3,"j4":4,"j5":5,"j6":6}""");
        JsonSerializer.Serialize(new Point3<double>(1, 2, 3), ObjectWire).Should().Be("""{"x":1,"y":2,"z":3}""");
        JsonSerializer.Serialize(new Vector3<double>(0, 0, 1), new JsonSerializerOptions { Converters = { new Vector3ObjectJsonConverter() } })
            .Should().Be("""{"X":0,"Y":0,"Z":1}""", "the names follow the options' naming policy");
    }
}
