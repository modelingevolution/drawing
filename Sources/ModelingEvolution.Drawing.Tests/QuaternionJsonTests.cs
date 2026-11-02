using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// Quaternion{T} records only its four components as [w, x, y, z]; the old object shape is refused.
/// </summary>
public class QuaternionJsonTests
{
    [Fact]
    public void Serialize_Double_WritesFourNumberArray()
    {
        var q = new Quaternion<double>(1, 0, 0, 0);

        JsonSerializer.Serialize(q).Should().Be("[1,0,0,0]");
    }

    [Fact]
    public void Serialize_UnitQuaternion_RecordsNoDerivedMembers()
    {
        var q = Quaternion<double>.FromAxisAngle(new Vector3<double>(0, 0, 1), Math.PI / 3);

        var json = JsonSerializer.Serialize(q);

        json.Should().NotContain("IsNormalized").And.NotContain("Length").And.NotContain("LengthSquared");
        json.Should().StartWith("[").And.EndWith("]");
    }

    [Fact]
    public void RoundTrip_Double_PreservesExactDigits()
    {
        var q = new Quaternion<double>(0.1 + 0.2, Math.PI / 7, -1.0 / 3.0, 1e-17);

        var json = JsonSerializer.Serialize(q);
        var back = JsonSerializer.Deserialize<Quaternion<double>>(json);

        json.Should().Be($"[{(0.1 + 0.2).ToString("R", System.Globalization.CultureInfo.InvariantCulture)}," +
                         $"{(Math.PI / 7).ToString("R", System.Globalization.CultureInfo.InvariantCulture)}," +
                         $"{(-1.0 / 3.0).ToString("R", System.Globalization.CultureInfo.InvariantCulture)},1E-17]");
        back.W.Should().Be(q.W);
        back.X.Should().Be(q.X);
        back.Y.Should().Be(q.Y);
        back.Z.Should().Be(q.Z);
    }

    [Fact]
    public void RoundTrip_Float_PreservesExactDigits()
    {
        var q = new Quaternion<float>(0.70710677f, 0f, 0.70710677f, 1f / 3f);

        var back = JsonSerializer.Deserialize<Quaternion<float>>(JsonSerializer.Serialize(q));

        back.Should().Be(q);
    }

    [Fact]
    public void Deserialize_OldObjectShape_IsRefused()
    {
        const string old = "{\"W\":1,\"X\":0,\"Y\":0,\"Z\":0,\"Length\":1,\"LengthSquared\":1,\"IsNormalized\":false}";

        var act = () => JsonSerializer.Deserialize<Quaternion<double>>(old);

        act.Should().Throw<JsonException>().WithMessage("*array [w, x, y, z]*");
    }

    [Theory]
    [InlineData("[1,0,0]")]
    [InlineData("[1,0,0,0,0]")]
    [InlineData("[1,0,\"0\",0]")]
    public void Deserialize_WrongArityOrNonNumber_IsRefused(string json)
    {
        var act = () => JsonSerializer.Deserialize<Quaternion<double>>(json);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Frame3_RecordsPositionAndRotationArraysOnly()
    {
        var frame = new Frame3<double>(new Point3<double>(1, 2, 3), Quaternion<double>.Identity);

        var json = JsonSerializer.Serialize(frame);
        var back = JsonSerializer.Deserialize<Frame3<double>>(json);

        json.Should().Be("{\"Position\":[1,2,3],\"Rotation\":[1,0,0,0]}");
        back.Should().Be(frame);
    }
}
