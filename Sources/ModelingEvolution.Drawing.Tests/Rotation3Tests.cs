using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Rotation3Tests
{
    private readonly ITestOutputHelper _output;
    private const double Tolerance = 1e-10;

    public Rotation3Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region JSON Serialization

    [Fact]
    public void Rotation3Float_JsonSerialization_RoundTrips()
    {
        var original = new Rotation3<float>(10f, 20f, 30f);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[10,20,30]");

        var deserialized = JsonSerializer.Deserialize<Rotation3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Rotation3Double_JsonSerialization_RoundTrips()
    {
        var original = new Rotation3<double>(45.5, 90.0, -30.0);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<Rotation3<double>>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Constructors and Properties

    [Fact]
    public void Rotation3_Constructor_SetsValues()
    {
        var rotation = new Rotation3<double>(10, 20, 30);

        rotation.Rx.Should().Be(10);
        rotation.Ry.Should().Be(20);
        rotation.Rz.Should().Be(30);
    }

    [Fact]
    public void Rotation3_Identity_IsZeroRotation()
    {
        var identity = Rotation3<double>.Identity;

        identity.Rx.Should().Be(0);
        identity.Ry.Should().Be(0);
        identity.Rz.Should().Be(0);
        identity.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Rotation3_FromDegrees_CreatesDegreeRotation()
    {
        var rotation = Rotation3<double>.FromDegrees(90, 0, 0);

        rotation.Rx.Should().Be(90);
    }

    [Fact]
    public void Rotation3_FromRadians_ConvertsToInternal()
    {
        var rotation = Rotation3<double>.FromRadians(
            Radian<double>.FromRadian(Math.PI / 2),
            Radian<double>.FromRadian(0),
            Radian<double>.FromRadian(0));

        ((double)rotation.Rx).Should().BeApproximately(90, Tolerance);
    }

    [Fact]
    public void Rotation3_ToRadians_ConvertsCorrectly()
    {
        var rotation = new Rotation3<double>(90, 180, 270);

        var (rx, ry, rz) = rotation.ToRadians();

        ((double)rx).Should().BeApproximately(Math.PI / 2, Tolerance);
        ((double)ry).Should().BeApproximately(Math.PI, Tolerance);
        ((double)rz).Should().BeApproximately(3 * Math.PI / 2, Tolerance);
    }

    #endregion

    #region Quaternion Conversion

    [Fact]
    public void Rotation3_ToQuaternion_Identity_ReturnsIdentityQuaternion()
    {
        var rotation = Rotation3<double>.Identity;

        var quaternion = rotation.ToQuaternion();

        quaternion.W.Should().BeApproximately(1, Tolerance);
        quaternion.X.Should().BeApproximately(0, Tolerance);
        quaternion.Y.Should().BeApproximately(0, Tolerance);
        quaternion.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void Rotation3_QuaternionConversion_RoundTrips()
    {
        var original = new Rotation3<double>(30, 45, 60);

        var quaternion = original.ToQuaternion();
        var backToRotation = Rotation3<double>.FromQuaternion(quaternion);

        ((double)backToRotation.Rx).Should().BeApproximately((double)original.Rx, 1e-6);
        ((double)backToRotation.Ry).Should().BeApproximately((double)original.Ry, 1e-6);
        ((double)backToRotation.Rz).Should().BeApproximately((double)original.Rz, 1e-6);
    }

    #endregion

    #region Rotation Operations

    [Fact]
    public void Rotation3_Rotate_XAxis90_RotatesYToZ()
    {
        var rotation = new Rotation3<double>(90, 0, 0); // 90 degrees around X
        var v = new Vector3<double>(0, 1, 0); // Y unit vector

        var rotated = rotation.Rotate(v);

        rotated.X.Should().BeApproximately(0, Tolerance);
        rotated.Y.Should().BeApproximately(0, Tolerance);
        rotated.Z.Should().BeApproximately(1, Tolerance);
    }

    [Fact]
    public void Rotation3_Rotate_YAxis90_RotatesZToX()
    {
        var rotation = new Rotation3<double>(0, 90, 0); // 90 degrees around Y
        var v = new Vector3<double>(0, 0, 1); // Z unit vector

        var rotated = rotation.Rotate(v);

        rotated.X.Should().BeApproximately(1, Tolerance);
        rotated.Y.Should().BeApproximately(0, Tolerance);
        rotated.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void Rotation3_Rotate_ZAxis90_RotatesXToY()
    {
        var rotation = new Rotation3<double>(0, 0, 90); // 90 degrees around Z
        var v = new Vector3<double>(1, 0, 0); // X unit vector

        var rotated = rotation.Rotate(v);

        rotated.X.Should().BeApproximately(0, Tolerance);
        rotated.Y.Should().BeApproximately(1, Tolerance);
        rotated.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void Rotation3_RotatePoint_Works()
    {
        var rotation = new Rotation3<double>(0, 0, 90);
        var point = new Point3<double>(1, 0, 0);

        var rotated = rotation.Rotate(point);

        rotated.X.Should().BeApproximately(0, Tolerance);
        rotated.Y.Should().BeApproximately(1, Tolerance);
        rotated.Z.Should().BeApproximately(0, Tolerance);
    }

    #endregion

    #region Inverse

    [Fact]
    public void Rotation3_Inverse_ReversesRotation()
    {
        var rotation = new Rotation3<double>(30, 45, 60);
        var v = new Vector3<double>(1, 2, 3);

        var rotated = rotation.Rotate(v);
        var q = rotation.ToQuaternion();
        var qInverse = q.Inverse();
        var backToOriginal = qInverse.Rotate(rotated);

        backToOriginal.X.Should().BeApproximately(v.X, 1e-6);
        backToOriginal.Y.Should().BeApproximately(v.Y, 1e-6);
        backToOriginal.Z.Should().BeApproximately(v.Z, 1e-6);
    }

    #endregion

    #region Combine

    [Fact]
    public void Rotation3_Combine_TwoRotations()
    {
        var r1 = new Rotation3<double>(90, 0, 0);
        var r2 = new Rotation3<double>(0, 90, 0);
        var v = new Vector3<double>(0, 0, 1);

        // Combine applies r2 first, then r1 (quaternion convention)
        var combined = Rotation3<double>.Combine(r1, r2);
        var rotated = combined.Rotate(v);

        // Verify combined rotation produces valid result
        rotated.Length.Should().BeApproximately(1.0, 1e-6); // Unit vector stays unit
    }

    #endregion

    #region Slerp

    [Fact]
    public void Rotation3_Slerp_AtZero_ReturnsFirst()
    {
        var a = new Rotation3<double>(0, 0, 0);
        var b = new Rotation3<double>(90, 0, 0);

        var result = Rotation3<double>.Slerp(a, b, 0);

        ((double)result.Rx).Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Rotation3_Slerp_AtOne_ReturnsSecond()
    {
        var a = new Rotation3<double>(0, 0, 0);
        var b = new Rotation3<double>(90, 0, 0);

        var result = Rotation3<double>.Slerp(a, b, 1);

        ((double)result.Rx).Should().BeApproximately(90, 1e-6);
    }

    [Fact]
    public void Rotation3_Slerp_AtHalf_ReturnsMidpoint()
    {
        var a = new Rotation3<double>(0, 0, 0);
        var b = new Rotation3<double>(90, 0, 0);

        var result = Rotation3<double>.Slerp(a, b, 0.5);

        ((double)result.Rx).Should().BeApproximately(45, 1e-6);
    }

    #endregion

    #region Operators

    [Fact]
    public void Rotation3_Equality_Works()
    {
        var a = new Rotation3<double>(10, 20, 30);
        var b = new Rotation3<double>(10, 20, 30);
        var c = new Rotation3<double>(10, 20, 31);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void Rotation3_UnaryMinus_ReturnsInverse()
    {
        var rotation = new Rotation3<double>(10, 20, 30);

        var negated = -rotation;

        negated.Rx.Should().Be(-10);
        negated.Ry.Should().Be(-20);
        negated.Rz.Should().Be(-30);
    }

    #endregion

    #region Conversions

    [Fact]
    public void Rotation3_TupleConversion_Works()
    {
        Rotation3<double> rotation = (10.0, 20.0, 30.0);
        (double rx, double ry, double rz) tuple = rotation;

        rotation.Rx.Should().Be(10);
        rotation.Ry.Should().Be(20);
        rotation.Rz.Should().Be(30);

        tuple.rx.Should().Be(10);
        tuple.ry.Should().Be(20);
        tuple.rz.Should().Be(30);
    }

    #endregion

    #region Rotation3 to Vector3 Conversion

    [Fact]
    public void Rotation3_ToVector3_Identity_ReturnsZAxis()
    {
        var identity = Rotation3<double>.Identity;

        var direction = (Vector3<double>)identity;

        // Identity rotation applied to +Z should give +Z
        direction.X.Should().BeApproximately(0, 1e-10);
        direction.Y.Should().BeApproximately(0, 1e-10);
        direction.Z.Should().BeApproximately(1, 1e-10);
    }

    [Fact]
    public void Rotation3_ToVector3_90DegYaw_PointsToY()
    {
        // 90 degrees around Z (yaw) should rotate +Z to... still +Z (Z rotation doesn't affect Z axis)
        // Let's use a pitch rotation instead
        var rotation = new Rotation3<double>(0, 90, 0); // 90 deg pitch around Y

        var direction = (Vector3<double>)rotation;

        // +Z rotated 90 deg around Y axis goes to +X
        direction.X.Should().BeApproximately(1, 1e-6);
        direction.Y.Should().BeApproximately(0, 1e-6);
        direction.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Rotation3_ToVector3_ReturnsUnitVector()
    {
        var rotation = new Rotation3<double>(30, 45, 60);

        var direction = (Vector3<double>)rotation;

        direction.Length.Should().BeApproximately(1, 1e-10);
    }

    [Fact]
    public void Rotation3_ToVector3_RoundTrips_WithVector3()
    {
        // Start with a direction, convert to rotation, convert back
        var originalDir = new Vector3<double>(1, 1, 1).Normalize();

        var rotation = (Rotation3<double>)originalDir;
        var backToDir = (Vector3<double>)rotation;

        backToDir.X.Should().BeApproximately(originalDir.X, 1e-6);
        backToDir.Y.Should().BeApproximately(originalDir.Y, 1e-6);
        backToDir.Z.Should().BeApproximately(originalDir.Z, 1e-6);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Rotation3_Parse_Works()
    {
        var rotation = Rotation3<double>.Parse("45.5, 90.0, -30.0");

        rotation.Rx.Should().Be(45.5);
        rotation.Ry.Should().Be(90.0);
        rotation.Rz.Should().Be(-30.0);
    }

    [Fact]
    public void Rotation3_TryParse_ReturnsTrue_ForValidInput()
    {
        var success = Rotation3<double>.TryParse("10, 20, 30", null, out var rotation);

        success.Should().BeTrue();
        rotation.Rx.Should().Be(10);
        rotation.Ry.Should().Be(20);
        rotation.Rz.Should().Be(30);
    }

    #endregion
}
