using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Vector3Tests
{
    private readonly ITestOutputHelper _output;

    public Vector3Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region JSON Serialization

    [Fact]
    public void Vector3Float_JsonSerialization_RoundTrips()
    {
        var original = new Vector3<float>(1.5f, 2.5f, 3.5f);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[1.5,2.5,3.5]");

        var deserialized = JsonSerializer.Deserialize<Vector3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Vector3Double_JsonSerialization_RoundTrips()
    {
        var original = new Vector3<double>(100.5, 200.5, 300.5);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<Vector3<double>>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Basic Operations

    [Fact]
    public void Vector3_Add_Works()
    {
        var a = new Vector3<double>(1, 2, 3);
        var b = new Vector3<double>(4, 5, 6);

        var result = a + b;

        result.X.Should().Be(5);
        result.Y.Should().Be(7);
        result.Z.Should().Be(9);
    }

    [Fact]
    public void Vector3_Subtract_Works()
    {
        var a = new Vector3<double>(10, 20, 30);
        var b = new Vector3<double>(1, 2, 3);

        var result = a - b;

        result.X.Should().Be(9);
        result.Y.Should().Be(18);
        result.Z.Should().Be(27);
    }

    [Fact]
    public void Vector3_ScalarMultiply_Works()
    {
        var v = new Vector3<double>(1, 2, 3);

        var result = v * 2;

        result.X.Should().Be(2);
        result.Y.Should().Be(4);
        result.Z.Should().Be(6);
    }

    [Fact]
    public void Vector3_ScalarDivide_Works()
    {
        var v = new Vector3<double>(10, 20, 30);

        var result = v / 2;

        result.X.Should().Be(5);
        result.Y.Should().Be(10);
        result.Z.Should().Be(15);
    }

    [Fact]
    public void Vector3_Negate_Works()
    {
        var v = new Vector3<double>(1, 2, 3);

        var result = -v;

        result.X.Should().Be(-1);
        result.Y.Should().Be(-2);
        result.Z.Should().Be(-3);
    }

    #endregion

    #region Length and Normalization

    [Fact]
    public void Vector3_Length_CalculatesCorrectly()
    {
        var v = new Vector3<double>(3, 4, 0);

        v.Length.Should().Be(5);
    }

    [Fact]
    public void Vector3_Length3D_CalculatesCorrectly()
    {
        var v = new Vector3<double>(1, 2, 2);

        v.Length.Should().Be(3);
    }

    [Fact]
    public void Vector3_LengthSquared_CalculatesCorrectly()
    {
        var v = new Vector3<double>(1, 2, 2);

        v.LengthSquared.Should().Be(9);
    }

    [Fact]
    public void Vector3_Normalize_ReturnsUnitVector()
    {
        var v = new Vector3<double>(3, 4, 0);

        var normalized = v.Normalize();

        normalized.Length.Should().BeApproximately(1.0, 1e-10);
        normalized.X.Should().BeApproximately(0.6, 1e-10);
        normalized.Y.Should().BeApproximately(0.8, 1e-10);
        normalized.Z.Should().Be(0);
    }

    [Fact]
    public void Vector3_Normalize_ThrowsForZeroVector()
    {
        var v = Vector3<double>.Zero;

        var act = () => v.Normalize();

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Dot and Cross Products

    [Fact]
    public void Vector3_DotProduct_CalculatesCorrectly()
    {
        var a = new Vector3<double>(1, 2, 3);
        var b = new Vector3<double>(4, 5, 6);

        var dot = Vector3<double>.Dot(a, b);

        dot.Should().Be(32); // 1*4 + 2*5 + 3*6 = 4 + 10 + 18 = 32
    }

    [Fact]
    public void Vector3_DotProduct_Operator_Works()
    {
        var a = new Vector3<double>(1, 0, 0);
        var b = new Vector3<double>(0, 1, 0);

        var dot = a * b;

        dot.Should().Be(0); // Perpendicular vectors
    }

    [Fact]
    public void Vector3_CrossProduct_CalculatesCorrectly()
    {
        var x = Vector3<double>.EX;
        var y = Vector3<double>.EY;

        var cross = Vector3<double>.Cross(x, y);

        cross.X.Should().Be(0);
        cross.Y.Should().Be(0);
        cross.Z.Should().Be(1); // X × Y = Z
    }

    [Fact]
    public void Vector3_CrossProduct_AntiCommutative()
    {
        var a = new Vector3<double>(1, 2, 3);
        var b = new Vector3<double>(4, 5, 6);

        var ab = Vector3<double>.Cross(a, b);
        var ba = Vector3<double>.Cross(b, a);

        ab.X.Should().Be(-ba.X);
        ab.Y.Should().Be(-ba.Y);
        ab.Z.Should().Be(-ba.Z);
    }

    #endregion

    #region Angle and Projection

    [Fact]
    public void Vector3_AngleBetween_CalculatesCorrectly()
    {
        var x = Vector3<double>.EX;
        var y = Vector3<double>.EY;

        var angle = Vector3<double>.AngleBetween(x, y);

        angle.Should().BeApproximately(Math.PI / 2, 1e-10); // 90 degrees
    }

    [Fact]
    public void Vector3_AngleBetween_ParallelVectors_IsZero()
    {
        var a = new Vector3<double>(1, 0, 0);
        var b = new Vector3<double>(2, 0, 0);

        var angle = Vector3<double>.AngleBetween(a, b);

        angle.Should().BeApproximately(0, 1e-10);
    }

    [Fact]
    public void Vector3_ProjectOnto_Works()
    {
        var v = new Vector3<double>(3, 4, 0);
        var direction = Vector3<double>.EX;

        var projection = v.ProjectOnto(direction);

        projection.X.Should().Be(3);
        projection.Y.Should().Be(0);
        projection.Z.Should().Be(0);
    }

    #endregion

    #region RotationTo

    [Fact]
    public void Vector3_RotationTo_EZtoEX_Rotates90DegreesAroundY()
    {
        var rotation = Vector3<double>.EZ.RotationTo(Vector3<double>.EX);

        var result = rotation.Rotate(Vector3<double>.EZ);

        result.X.Should().BeApproximately(1, 1e-6);
        result.Y.Should().BeApproximately(0, 1e-6);
        result.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Vector3_RotationTo_EZtoEY_Rotates90DegreesAroundX()
    {
        var rotation = Vector3<double>.EZ.RotationTo(Vector3<double>.EY);

        var result = rotation.Rotate(Vector3<double>.EZ);

        result.X.Should().BeApproximately(0, 1e-6);
        result.Y.Should().BeApproximately(1, 1e-6);
        result.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Vector3_RotationTo_ParallelVectors_ReturnsIdentity()
    {
        var rotation = Vector3<double>.EZ.RotationTo(Vector3<double>.EZ);

        rotation.Should().Be(Rotation3<double>.Identity);
    }

    [Fact]
    public void Vector3_RotationTo_AntiParallelVectors_Rotates180Degrees()
    {
        var rotation = Vector3<double>.EZ.RotationTo(-Vector3<double>.EZ);

        var result = rotation.Rotate(Vector3<double>.EZ);

        result.X.Should().BeApproximately(0, 1e-6);
        result.Y.Should().BeApproximately(0, 1e-6);
        result.Z.Should().BeApproximately(-1, 1e-6);
    }

    [Fact]
    public void Vector3_RotationTo_ArbitraryVectors_AlignsCorrectly()
    {
        var from = new Vector3<double>(1, 2, 3).Normalize();
        var to = new Vector3<double>(-2, 1, 0.5).Normalize();

        var rotation = from.RotationTo(to);
        var result = rotation.Rotate(from);

        result.X.Should().BeApproximately(to.X, 1e-6);
        result.Y.Should().BeApproximately(to.Y, 1e-6);
        result.Z.Should().BeApproximately(to.Z, 1e-6);
    }

    [Fact]
    public void Vector3_RotationTo_NonUnitVectors_NormalizesFirst()
    {
        var from = new Vector3<double>(0, 0, 5); // length 5
        var to = new Vector3<double>(10, 0, 0);  // length 10

        var rotation = from.RotationTo(to);
        var result = rotation.Rotate(Vector3<double>.EZ);

        result.X.Should().BeApproximately(1, 1e-6);
        result.Y.Should().BeApproximately(0, 1e-6);
        result.Z.Should().BeApproximately(0, 1e-6);
    }

    #endregion

    #region Interpolation

    [Fact]
    public void Vector3_Lerp_InterpolatesCorrectly()
    {
        var a = new Vector3<double>(0, 0, 0);
        var b = new Vector3<double>(10, 20, 30);

        var half = Vector3<double>.Lerp(a, b, 0.5);

        half.X.Should().Be(5);
        half.Y.Should().Be(10);
        half.Z.Should().Be(15);
    }

    #endregion

    #region Unit Vectors

    [Fact]
    public void Vector3_UnitVectors_AreCorrect()
    {
        Vector3<double>.EX.Should().Be(new Vector3<double>(1, 0, 0));
        Vector3<double>.EY.Should().Be(new Vector3<double>(0, 1, 0));
        Vector3<double>.EZ.Should().Be(new Vector3<double>(0, 0, 1));
        Vector3<double>.Zero.Should().Be(new Vector3<double>(0, 0, 0));
    }

    #endregion

    #region Pose3 Operators

    [Fact]
    public void Vector3_AddRotation_ReturnsPose3()
    {
        var vector = new Vector3<double>(10, 20, 30);
        var rotation = new Rotation3<double>(45, 90, -30);

        Pose3<double> pose = vector + rotation;

        pose.X.Should().Be(10);
        pose.Y.Should().Be(20);
        pose.Z.Should().Be(30);
        pose.Rx.Should().Be(45);
        pose.Ry.Should().Be(90);
        pose.Rz.Should().Be(-30);
    }

    #endregion

    #region Vector3 to Rotation3 Conversion

    [Fact]
    public void Vector3_ToRotation3_ZAxisDirection_ReturnsZeroRotation()
    {
        // +Z direction should result in identity rotation (no rotation needed)
        var z = Vector3<double>.EZ;

        var rotation = (Rotation3<double>)z;

        // When pointing in +Z, yaw can be any value (atan2(0,0)) but pitch should be 0
        // The result should rotate +Z to +Z (identity-like)
        var resultDir = (Vector3<double>)rotation;
        resultDir.Z.Should().BeApproximately(1, 1e-10);
    }

    [Fact]
    public void Vector3_ToRotation3_XAxisDirection()
    {
        // +X direction
        var x = Vector3<double>.EX;

        var rotation = (Rotation3<double>)x;

        // Converting back should give +X direction
        var resultDir = (Vector3<double>)rotation;
        resultDir.X.Should().BeApproximately(1, 1e-6);
        resultDir.Y.Should().BeApproximately(0, 1e-6);
        resultDir.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Vector3_ToRotation3_YAxisDirection()
    {
        // +Y direction
        var y = Vector3<double>.EY;

        var rotation = (Rotation3<double>)y;

        // Converting back should give +Y direction
        var resultDir = (Vector3<double>)rotation;
        resultDir.X.Should().BeApproximately(0, 1e-6);
        resultDir.Y.Should().BeApproximately(1, 1e-6);
        resultDir.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Vector3_ToRotation3_ArbitraryDirection_RoundTrips()
    {
        // Arbitrary normalized direction
        var direction = new Vector3<double>(1, 2, 3).Normalize();

        var rotation = (Rotation3<double>)direction;
        var backToVector = (Vector3<double>)rotation;

        // Should round-trip back to the same direction
        backToVector.X.Should().BeApproximately(direction.X, 1e-6);
        backToVector.Y.Should().BeApproximately(direction.Y, 1e-6);
        backToVector.Z.Should().BeApproximately(direction.Z, 1e-6);
    }

    [Fact]
    public void Vector3_ToRotation3_NonUnitVector_NormalizesFirst()
    {
        // Non-unit vector should be normalized before conversion
        var v = new Vector3<double>(3, 4, 0); // length = 5

        var rotation = (Rotation3<double>)v;
        var resultDir = (Vector3<double>)rotation;

        // Result should be normalized direction
        resultDir.Length.Should().BeApproximately(1, 1e-10);
        resultDir.X.Should().BeApproximately(0.6, 1e-6);
        resultDir.Y.Should().BeApproximately(0.8, 1e-6);
        resultDir.Z.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Vector3_ToRotation3_ZeroVector_ReturnsIdentity()
    {
        var zero = Vector3<double>.Zero;

        var rotation = (Rotation3<double>)zero;

        rotation.Should().Be(Rotation3<double>.Identity);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Vector3_Parse_Works()
    {
        var v = Vector3<double>.Parse("1.5, 2.5, 3.5");

        v.X.Should().Be(1.5);
        v.Y.Should().Be(2.5);
        v.Z.Should().Be(3.5);
    }

    [Fact]
    public void Vector3_TryParse_ReturnsTrue_ForValidInput()
    {
        var success = Vector3<double>.TryParse("1, 2, 3", null, out var v);

        success.Should().BeTrue();
        v.X.Should().Be(1);
        v.Y.Should().Be(2);
        v.Z.Should().Be(3);
    }

    #endregion
}
