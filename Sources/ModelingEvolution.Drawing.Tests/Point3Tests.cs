using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Point3Tests
{
    private readonly ITestOutputHelper _output;

    public Point3Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region JSON Serialization

    record FooPoint3(Point3<float> Value);
    record FooPoint3D(Point3<double> Value);

    [Fact]
    public void Point3Float_JsonSerialization_RoundTrips()
    {
        var original = new Point3<float>(1.5f, 2.5f, 3.5f);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[1.5,2.5,3.5]");

        var deserialized = JsonSerializer.Deserialize<Point3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Point3Double_JsonSerialization_RoundTrips()
    {
        var original = new Point3<double>(100.123, 200.456, 300.789);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<Point3<double>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Point3_InRecord_JsonSerialization_RoundTrips()
    {
        var original = new FooPoint3(new Point3<float>(1f, 2f, 3f));
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<FooPoint3>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Operators

    [Fact]
    public void Point3_SubtractPoints_ReturnsVector3()
    {
        var a = new Point3<double>(10, 20, 30);
        var b = new Point3<double>(1, 2, 3);

        var result = a - b;

        result.Should().BeOfType<Vector3<double>>();
        result.X.Should().Be(9);
        result.Y.Should().Be(18);
        result.Z.Should().Be(27);
    }

    [Fact]
    public void Point3_AddVector_ReturnsPoint()
    {
        var point = new Point3<double>(10, 20, 30);
        var vector = new Vector3<double>(1, 2, 3);

        var result = point + vector;

        result.X.Should().Be(11);
        result.Y.Should().Be(22);
        result.Z.Should().Be(33);
    }

    [Fact]
    public void Point3_SubtractVector_ReturnsPoint()
    {
        var point = new Point3<double>(10, 20, 30);
        var vector = new Vector3<double>(1, 2, 3);

        var result = point - vector;

        result.X.Should().Be(9);
        result.Y.Should().Be(18);
        result.Z.Should().Be(27);
    }

    [Fact]
    public void Point3_Equality_Works()
    {
        var a = new Point3<double>(1, 2, 3);
        var b = new Point3<double>(1, 2, 3);
        var c = new Point3<double>(1, 2, 4);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Point3_AddRotation_ReturnsPose3()
    {
        var point = new Point3<double>(10, 20, 30);
        var rotation = new Rotation3<double>(45, 90, -30);

        Pose3<double> pose = point + rotation;

        pose.Position.Should().Be(point);
        pose.Rotation.Should().Be(rotation);
        pose.X.Should().Be(10);
        pose.Y.Should().Be(20);
        pose.Z.Should().Be(30);
        pose.Rx.Should().Be(45);
        pose.Ry.Should().Be(90);
        pose.Rz.Should().Be(-30);
    }

    #endregion

    #region Methods

    [Fact]
    public void Point3_Distance_CalculatesCorrectly()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(3, 4, 0);

        var distance = Point3<double>.Distance(a, b);

        distance.Should().Be(5);
    }

    [Fact]
    public void Point3_Distance3D_CalculatesCorrectly()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 2, 2);

        var distance = Point3<double>.Distance(a, b);

        distance.Should().Be(3);
    }

    [Fact]
    public void Point3_Middle_CalculatesCorrectly()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(10, 20, 30);

        var middle = Point3<double>.Middle(a, b);

        middle.X.Should().Be(5);
        middle.Y.Should().Be(10);
        middle.Z.Should().Be(15);
    }

    [Fact]
    public void Point3_Lerp_InterpolatesCorrectly()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(10, 20, 30);

        var quarter = Point3<double>.Lerp(a, b, 0.25);
        var half = Point3<double>.Lerp(a, b, 0.5);

        quarter.X.Should().Be(2.5);
        quarter.Y.Should().Be(5);
        quarter.Z.Should().Be(7.5);

        half.X.Should().Be(5);
        half.Y.Should().Be(10);
        half.Z.Should().Be(15);
    }

    #endregion

    #region Conversions

    [Fact]
    public void Point3_TupleConversion_Works()
    {
        Point3<double> point = (1.0, 2.0, 3.0);
        (double x, double y, double z) tuple = point;

        point.X.Should().Be(1);
        point.Y.Should().Be(2);
        point.Z.Should().Be(3);

        tuple.x.Should().Be(1);
        tuple.y.Should().Be(2);
        tuple.z.Should().Be(3);
    }

    [Fact]
    public void Point3_ToVector3_Works()
    {
        var point = new Point3<double>(1, 2, 3);
        Vector3<double> vector = point;

        vector.X.Should().Be(1);
        vector.Y.Should().Be(2);
        vector.Z.Should().Be(3);
    }

    [Fact]
    public void Point3_Truncating_ConvertsType()
    {
        var pointD = new Point3<double>(1.9, 2.9, 3.9);
        var pointF = pointD.Truncating<float>();

        pointF.X.Should().Be(1.9f);
        pointF.Y.Should().Be(2.9f);
        pointF.Z.Should().Be(3.9f);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Point3_Parse_Works()
    {
        var point = Point3<double>.Parse("1.5, 2.5, 3.5");

        point.X.Should().Be(1.5);
        point.Y.Should().Be(2.5);
        point.Z.Should().Be(3.5);
    }

    [Fact]
    public void Point3_TryParse_ReturnsTrue_ForValidInput()
    {
        var success = Point3<double>.TryParse("1, 2, 3", null, out var point);

        success.Should().BeTrue();
        point.X.Should().Be(1);
        point.Y.Should().Be(2);
        point.Z.Should().Be(3);
    }

    [Fact]
    public void Point3_TryParse_ReturnsFalse_ForInvalidInput()
    {
        var success = Point3<double>.TryParse("invalid", null, out var point);

        success.Should().BeFalse();
        point.Should().Be(Point3<double>.Zero);
    }

    #endregion
}
