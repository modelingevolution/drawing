using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Pose3Tests
{
    private readonly ITestOutputHelper _output;
    private const double Tolerance = 1e-10;

    public Pose3Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region JSON Serialization

    [Fact]
    public void Pose3Float_JsonSerialization_RoundTrips()
    {
        var original = new Pose3<float>(100f, 200f, 300f, 10f, 20f, 30f);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[100,200,300,10,20,30]");

        var deserialized = JsonSerializer.Deserialize<Pose3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Pose3Double_JsonSerialization_RoundTrips()
    {
        var original = new Pose3<double>(100.5, 200.5, 300.5, 45.0, 90.0, -30.0);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<Pose3<double>>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Constructors and Properties

    [Fact]
    public void Pose3_Constructor_WithComponents_SetsValues()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        pose.X.Should().Be(1);
        pose.Y.Should().Be(2);
        pose.Z.Should().Be(3);
        pose.Rx.Should().Be(10);
        pose.Ry.Should().Be(20);
        pose.Rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_Constructor_WithPointAndRotation_SetsValues()
    {
        var position = new Point3<double>(1, 2, 3);
        var rotation = new Rotation3<double>(10, 20, 30);

        var pose = new Pose3<double>(position, rotation);

        pose.Position.Should().Be(position);
        pose.Rotation.Should().Be(rotation);
    }

    [Fact]
    public void Pose3_Identity_IsAtOriginWithNoRotation()
    {
        var identity = Pose3<double>.Identity;

        identity.X.Should().Be(0);
        identity.Y.Should().Be(0);
        identity.Z.Should().Be(0);
        identity.Rx.Should().Be(0);
        identity.Ry.Should().Be(0);
        identity.Rz.Should().Be(0);
        identity.IsIdentity.Should().BeTrue();
    }

    #endregion

    #region Operators

    [Fact]
    public void Pose3_Equality_Works()
    {
        var a = new Pose3<double>(1, 2, 3, 10, 20, 30);
        var b = new Pose3<double>(1, 2, 3, 10, 20, 30);
        var c = new Pose3<double>(1, 2, 3, 10, 20, 31);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    #endregion

    #region Conversions

    [Fact]
    public void Pose3_TupleConversion_Works()
    {
        Pose3<double> pose = (1.0, 2.0, 3.0, 10.0, 20.0, 30.0);
        (double x, double y, double z, double rx, double ry, double rz) tuple = pose;

        pose.X.Should().Be(1);
        pose.Y.Should().Be(2);
        pose.Z.Should().Be(3);
        pose.Rx.Should().Be(10);
        pose.Ry.Should().Be(20);
        pose.Rz.Should().Be(30);

        tuple.x.Should().Be(1);
        tuple.rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_Deconstruct_ToPositionAndRotation()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        var (position, rotation) = pose;

        position.X.Should().Be(1);
        rotation.Rx.Should().Be(10);
    }

    [Fact]
    public void Pose3_Deconstruct_ToComponents()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        var (x, y, z, rx, ry, rz) = pose;

        x.Should().Be(1);
        y.Should().Be(2);
        z.Should().Be(3);
        rx.Should().Be(10);
        ry.Should().Be(20);
        rz.Should().Be(30);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Pose3_Parse_Works()
    {
        var pose = Pose3<double>.Parse("100, 200, 300, 45, 90, -30");

        pose.X.Should().Be(100);
        pose.Y.Should().Be(200);
        pose.Z.Should().Be(300);
        pose.Rx.Should().Be(45);
        pose.Ry.Should().Be(90);
        pose.Rz.Should().Be(-30);
    }

    [Fact]
    public void Pose3_TryParse_ReturnsTrue_ForValidInput()
    {
        var success = Pose3<double>.TryParse("1, 2, 3, 10, 20, 30", null, out var pose);

        success.Should().BeTrue();
        pose.X.Should().Be(1);
        pose.Rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_TryParse_ReturnsFalse_ForInvalidInput()
    {
        var success = Pose3<double>.TryParse("1, 2, 3", null, out var pose);

        success.Should().BeFalse();
    }

    #endregion
}
