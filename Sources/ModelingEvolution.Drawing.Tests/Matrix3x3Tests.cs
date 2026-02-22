using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Matrix3x3Tests
{
    private const double Tol = 1e-10;
    private const double TolLoose = 1e-6;

    #region Identity & Properties

    [Fact]
    public void Identity_HasCorrectValues()
    {
        var m = Matrix3x3<double>.Identity;
        m.M11.Should().Be(1d); m.M12.Should().Be(0d); m.M13.Should().Be(0d);
        m.M21.Should().Be(0d); m.M22.Should().Be(1d); m.M23.Should().Be(0d);
        m.M31.Should().Be(0d); m.M32.Should().Be(0d); m.M33.Should().Be(1d);
    }

    [Fact]
    public void Zero_HasCorrectValues()
    {
        var m = Matrix3x3<double>.Zero;
        m.M11.Should().Be(0d); m.M12.Should().Be(0d); m.M13.Should().Be(0d);
        m.M21.Should().Be(0d); m.M22.Should().Be(0d); m.M23.Should().Be(0d);
        m.M31.Should().Be(0d); m.M32.Should().Be(0d); m.M33.Should().Be(0d);
    }

    [Fact]
    public void Determinant_Identity_IsOne()
    {
        Matrix3x3<double>.Identity.Determinant.Should().Be(1d);
    }

    [Fact]
    public void Determinant_KnownMatrix()
    {
        // |1 2 3; 0 1 4; 5 6 0| = 1*(0-24) - 2*(0-20) + 3*(0-5) = -24+40-15 = 1
        var m = new Matrix3x3<double>(1, 2, 3, 0, 1, 4, 5, 6, 0);
        m.Determinant.Should().BeApproximately(1d, Tol);
    }

    [Fact]
    public void Trace_KnownMatrix()
    {
        var m = new Matrix3x3<double>(3, 0, 0, 0, 5, 0, 0, 0, 7);
        m.Trace.Should().BeApproximately(15d, Tol);
    }

    #endregion

    #region Transpose

    [Fact]
    public void Transpose_General()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var t = m.Transpose();
        t.M11.Should().Be(1d); t.M12.Should().Be(4d); t.M13.Should().Be(7d);
        t.M21.Should().Be(2d); t.M22.Should().Be(5d); t.M23.Should().Be(8d);
        t.M31.Should().Be(3d); t.M32.Should().Be(6d); t.M33.Should().Be(9d);
    }

    [Fact]
    public void Transpose_RotationMatrix_IsInverse()
    {
        var r = Matrix3x3<double>.RotationX(Degree<double>.Create(30));
        var product = r * r.Transpose();
        AssertIsIdentity(product);
    }

    #endregion

    #region Inverse

    [Fact]
    public void Inverse_Identity()
    {
        var inv = Matrix3x3<double>.Identity.Inverse();
        AssertIsIdentity(inv);
    }

    [Fact]
    public void Inverse_KnownMatrix()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 0, 1, 4, 5, 6, 0);
        var inv = m.Inverse();
        var product = m * inv;
        AssertIsIdentity(product);
    }

    [Fact]
    public void Inverse_Singular_Throws()
    {
        // Third row = first row → det = 0
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 1, 2, 3);
        var act = () => m.Inverse();
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Multiplication

    [Fact]
    public void Multiplication_IdentityIsNeutral()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var r = m * Matrix3x3<double>.Identity;
        AssertMatricesEqual(r, m);
    }

    [Fact]
    public void Multiplication_KnownResult()
    {
        var a = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var b = new Matrix3x3<double>(9, 8, 7, 6, 5, 4, 3, 2, 1);
        var c = a * b;
        // Row 0: (1*9+2*6+3*3, 1*8+2*5+3*2, 1*7+2*4+3*1) = (30, 24, 18)
        c.M11.Should().BeApproximately(30, Tol);
        c.M12.Should().BeApproximately(24, Tol);
        c.M13.Should().BeApproximately(18, Tol);
        // Row 1: (4*9+5*6+6*3, 4*8+5*5+6*2, 4*7+5*4+6*1) = (84, 69, 54)
        c.M21.Should().BeApproximately(84, Tol);
        c.M22.Should().BeApproximately(69, Tol);
        c.M23.Should().BeApproximately(54, Tol);
        // Row 2: (7*9+8*6+9*3, 7*8+8*5+9*2, 7*7+8*4+9*1) = (138, 114, 90)
        c.M31.Should().BeApproximately(138, Tol);
        c.M32.Should().BeApproximately(114, Tol);
        c.M33.Should().BeApproximately(90, Tol);
    }

    [Fact]
    public void Multiplication_Associativity()
    {
        var a = Matrix3x3<double>.RotationX(Degree<double>.Create(30));
        var b = Matrix3x3<double>.RotationY(Degree<double>.Create(45));
        var c = Matrix3x3<double>.RotationZ(Degree<double>.Create(60));
        var ab_c = (a * b) * c;
        var a_bc = a * (b * c);
        AssertMatricesEqual(ab_c, a_bc);
    }

    #endregion

    #region Transform

    [Fact]
    public void Transform_Vector_IdentityPreserves()
    {
        var v = new Vector3<double>(1, 2, 3);
        var r = Matrix3x3<double>.Identity.Transform(v);
        r.X.Should().BeApproximately(1, Tol);
        r.Y.Should().BeApproximately(2, Tol);
        r.Z.Should().BeApproximately(3, Tol);
    }

    [Fact]
    public void Transform_Vector_RotationX90_RotatesBasisVectors()
    {
        var m = Matrix3x3<double>.RotationX(Degree<double>.Create(90));
        // Rotating Y axis by 90° around X → Z axis
        var ey = new Vector3<double>(0, 1, 0);
        var result = m * ey;
        result.X.Should().BeApproximately(0, TolLoose);
        result.Y.Should().BeApproximately(0, TolLoose);
        result.Z.Should().BeApproximately(1, TolLoose);
    }

    [Fact]
    public void Transform_Point_OperatorSyntax()
    {
        var m = Matrix3x3<double>.Scale(2, 3, 4);
        var p = new Point3<double>(1, 2, 3);
        var r = m * p;
        r.X.Should().BeApproximately(2, Tol);
        r.Y.Should().BeApproximately(6, Tol);
        r.Z.Should().BeApproximately(12, Tol);
    }

    #endregion

    #region Rotation Factories

    [Fact]
    public void RotationX_90_RotatesYToZ()
    {
        var m = Matrix3x3<double>.RotationX(Degree<double>.Create(90));
        var v = new Vector3<double>(0, 1, 0);
        var r = m * v;
        r.X.Should().BeApproximately(0, TolLoose);
        r.Y.Should().BeApproximately(0, TolLoose);
        r.Z.Should().BeApproximately(1, TolLoose);
    }

    [Fact]
    public void RotationY_90_RotatesZToX()
    {
        var m = Matrix3x3<double>.RotationY(Degree<double>.Create(90));
        var v = new Vector3<double>(0, 0, 1);
        var r = m * v;
        r.X.Should().BeApproximately(1, TolLoose);
        r.Y.Should().BeApproximately(0, TolLoose);
        r.Z.Should().BeApproximately(0, TolLoose);
    }

    [Fact]
    public void RotationZ_90_RotatesXToY()
    {
        var m = Matrix3x3<double>.RotationZ(Degree<double>.Create(90));
        var v = new Vector3<double>(1, 0, 0);
        var r = m * v;
        r.X.Should().BeApproximately(0, TolLoose);
        r.Y.Should().BeApproximately(1, TolLoose);
        r.Z.Should().BeApproximately(0, TolLoose);
    }

    [Fact]
    public void RotationZYX_MatchesComposedRotations()
    {
        var rx = Degree<double>.Create(30d);
        var ry = Degree<double>.Create(45d);
        var rz = Degree<double>.Create(60d);
        var composed = Matrix3x3<double>.RotationZ(rz) * Matrix3x3<double>.RotationY(ry) * Matrix3x3<double>.RotationX(rx);
        var direct = Matrix3x3<double>.RotationZYX(rx, ry, rz);
        AssertMatricesEqual(composed, direct);
    }

    [Fact]
    public void RotationMatrix_IsOrthogonal()
    {
        var m = Matrix3x3<double>.RotationZYX(Degree<double>.Create(25), Degree<double>.Create(35), Degree<double>.Create(45));
        var product = m * m.Transpose();
        AssertIsIdentity(product);
    }

    [Fact]
    public void RotationMatrix_Determinant_IsOne()
    {
        var m = Matrix3x3<double>.RotationZYX(Degree<double>.Create(10), Degree<double>.Create(20), Degree<double>.Create(30));
        m.Determinant.Should().BeApproximately(1d, TolLoose);
    }

    #endregion

    #region Scale & Construction Factories

    [Fact]
    public void Scale_ScalesVector()
    {
        var m = Matrix3x3<double>.Scale(2, 3, 4);
        var v = new Vector3<double>(1, 1, 1);
        var r = m * v;
        r.X.Should().BeApproximately(2, Tol);
        r.Y.Should().BeApproximately(3, Tol);
        r.Z.Should().BeApproximately(4, Tol);
    }

    [Fact]
    public void FromColumns_BuildsCorrectMatrix()
    {
        var c0 = new Vector3<double>(1, 4, 7);
        var c1 = new Vector3<double>(2, 5, 8);
        var c2 = new Vector3<double>(3, 6, 9);
        var m = Matrix3x3<double>.FromColumns(c0, c1, c2);
        m.M11.Should().Be(1); m.M12.Should().Be(2); m.M13.Should().Be(3);
        m.M21.Should().Be(4); m.M22.Should().Be(5); m.M23.Should().Be(6);
        m.M31.Should().Be(7); m.M32.Should().Be(8); m.M33.Should().Be(9);
    }

    [Fact]
    public void FromRows_BuildsCorrectMatrix()
    {
        var r0 = new Vector3<double>(1, 2, 3);
        var r1 = new Vector3<double>(4, 5, 6);
        var r2 = new Vector3<double>(7, 8, 9);
        var m = Matrix3x3<double>.FromRows(r0, r1, r2);
        m.M11.Should().Be(1); m.M12.Should().Be(2); m.M13.Should().Be(3);
        m.M21.Should().Be(4); m.M22.Should().Be(5); m.M23.Should().Be(6);
        m.M31.Should().Be(7); m.M32.Should().Be(8); m.M33.Should().Be(9);
    }

    #endregion

    #region Euler Round-trip

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(-15, 45, -60)]
    [InlineData(0, 0, 90)]
    [InlineData(180, 0, 0)]
    [InlineData(5, 10, 15)]
    public void RotationZYX_ToEulerZYX_RoundTrip(double rxd, double ryd, double rzd)
    {
        Degree<double> rx = Degree<double>.Create(rxd), ry = Degree<double>.Create(ryd), rz = Degree<double>.Create(rzd);
        var m = Matrix3x3<double>.RotationZYX(rx, ry, rz);
        var (erx, ery, erz) = m.ToEulerZYX();
        // Rebuild matrix from extracted angles and compare
        var m2 = Matrix3x3<double>.RotationZYX(erx, ery, erz);
        AssertMatricesEqual(m, m2);
    }

    [Fact]
    public void ToEulerZYX_GimbalLock_AtPositive90()
    {
        var m = Matrix3x3<double>.RotationZYX(Degree<double>.Create(0), Degree<double>.Create(90), Degree<double>.Create(0));
        var (rx, ry, rz) = m.ToEulerZYX();
        // ry should be ~90°
        ((double)ry).Should().BeApproximately(90, TolLoose);
        // At gimbal lock, rx and rz share DOF; matrix should still reconstruct
        var m2 = Matrix3x3<double>.RotationZYX(rx, ry, rz);
        AssertMatricesEqual(m, m2, 1e-5);
    }

    #endregion

    #region Quaternion Round-trip

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(-30, 45, 60)]
    [InlineData(0, 0, 0)]
    [InlineData(5, -10, 15)]
    public void ToQuaternion_FromQuaternion_RoundTrip(double rxd, double ryd, double rzd)
    {
        var m = Matrix3x3<double>.RotationZYX(Degree<double>.Create(rxd), Degree<double>.Create(ryd), Degree<double>.Create(rzd));
        var q = m.ToQuaternion();
        // Convert quaternion back to rotation, then to matrix
        var rot = Rotation3<double>.FromQuaternion(q);
        var m2 = rot.ToMatrix3x3();
        AssertMatricesEqual(m, m2, 1e-9);
    }

    [Fact]
    public void ToQuaternion_Identity_IsUnitQuaternion()
    {
        var q = Matrix3x3<double>.Identity.ToQuaternion();
        q.W.Should().BeApproximately(1, TolLoose);
        q.X.Should().BeApproximately(0, TolLoose);
        q.Y.Should().BeApproximately(0, TolLoose);
        q.Z.Should().BeApproximately(0, TolLoose);
    }

    #endregion

    #region Rotation3 Integration

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(-15, 45, -60)]
    [InlineData(0, 0, 0)]
    public void Rotation3_ToMatrix3x3_RoundTrip(double rxd, double ryd, double rzd)
    {
        var rot = new Rotation3<double>(rxd, ryd, rzd);
        var m = rot.ToMatrix3x3();
        var rot2 = Rotation3<double>.FromMatrix3x3(m);
        ((double)rot2.Rx).Should().BeApproximately(rxd, TolLoose);
        ((double)rot2.Ry).Should().BeApproximately(ryd, TolLoose);
        ((double)rot2.Rz).Should().BeApproximately(rzd, TolLoose);
    }

    [Fact]
    public void Rotation3_Rotate_MatchesMatrix_Transform()
    {
        var rot = new Rotation3<double>(30, 45, 60);
        var m = rot.ToMatrix3x3();
        var v = new Vector3<double>(1, 2, 3);

        var rotated = rot.Rotate(v);
        var transformed = m.Transform(v);

        rotated.X.Should().BeApproximately(transformed.X, TolLoose);
        rotated.Y.Should().BeApproximately(transformed.Y, TolLoose);
        rotated.Z.Should().BeApproximately(transformed.Z, TolLoose);
    }

    #endregion

    #region Scalar & Element-wise

    [Fact]
    public void ScalarMultiplication()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var r = m * 2d;
        r.M11.Should().Be(2); r.M12.Should().Be(4); r.M13.Should().Be(6);
        r.M21.Should().Be(8); r.M22.Should().Be(10); r.M23.Should().Be(12);
        r.M31.Should().Be(14); r.M32.Should().Be(16); r.M33.Should().Be(18);
    }

    [Fact]
    public void ScalarMultiplication_Commutative()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var r1 = m * 3d;
        var r2 = 3d * m;
        r1.Should().Be(r2);
    }

    [Fact]
    public void Addition()
    {
        var a = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var b = new Matrix3x3<double>(9, 8, 7, 6, 5, 4, 3, 2, 1);
        var c = a + b;
        c.M11.Should().Be(10); c.M12.Should().Be(10); c.M13.Should().Be(10);
        c.M21.Should().Be(10); c.M22.Should().Be(10); c.M23.Should().Be(10);
        c.M31.Should().Be(10); c.M32.Should().Be(10); c.M33.Should().Be(10);
    }

    [Fact]
    public void Subtraction()
    {
        var a = new Matrix3x3<double>(9, 8, 7, 6, 5, 4, 3, 2, 1);
        var b = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var c = a - b;
        c.M11.Should().Be(8); c.M12.Should().Be(6); c.M13.Should().Be(4);
        c.M21.Should().Be(2); c.M22.Should().Be(0); c.M23.Should().Be(-2);
        c.M31.Should().Be(-4); c.M32.Should().Be(-6); c.M33.Should().Be(-8);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void Json_Serialize_Double()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var json = JsonSerializer.Serialize(m);
        json.Should().Be("[[1,2,3],[4,5,6],[7,8,9]]");
    }

    [Fact]
    public void Json_Deserialize_Double()
    {
        var m = JsonSerializer.Deserialize<Matrix3x3<double>>("[[1.5,2.5,3.5],[4.5,5.5,6.5],[7.5,8.5,9.5]]");
        m.M11.Should().Be(1.5); m.M12.Should().Be(2.5); m.M13.Should().Be(3.5);
        m.M21.Should().Be(4.5); m.M22.Should().Be(5.5); m.M23.Should().Be(6.5);
        m.M31.Should().Be(7.5); m.M32.Should().Be(8.5); m.M33.Should().Be(9.5);
    }

    [Fact]
    public void Json_RoundTrip_Double()
    {
        var original = new Matrix3x3<double>(1.23, 4.56, 7.89, 0.12, 3.45, 6.78, 9.01, 2.34, 5.67);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Matrix3x3<double>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Json_RoundTrip_Float()
    {
        var original = new Matrix3x3<float>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Matrix3x3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Json_Identity_RoundTrip()
    {
        var original = Matrix3x3<double>.Identity;
        var json = JsonSerializer.Serialize(original);
        json.Should().Be("[[1,0,0],[0,1,0],[0,0,1]]");
        var deserialized = JsonSerializer.Deserialize<Matrix3x3<double>>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_Format()
    {
        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        m.ToString().Should().Be("[1, 2, 3; 4, 5, 6; 7, 8, 9]");
    }

    #endregion

    #region Helpers

    private static void AssertIsIdentity(Matrix3x3<double> m, double tol = Tol)
    {
        m.M11.Should().BeApproximately(1, tol);
        m.M12.Should().BeApproximately(0, tol);
        m.M13.Should().BeApproximately(0, tol);
        m.M21.Should().BeApproximately(0, tol);
        m.M22.Should().BeApproximately(1, tol);
        m.M23.Should().BeApproximately(0, tol);
        m.M31.Should().BeApproximately(0, tol);
        m.M32.Should().BeApproximately(0, tol);
        m.M33.Should().BeApproximately(1, tol);
    }

    private static void AssertMatricesEqual(Matrix3x3<double> a, Matrix3x3<double> b, double tol = Tol)
    {
        a.M11.Should().BeApproximately(b.M11, tol);
        a.M12.Should().BeApproximately(b.M12, tol);
        a.M13.Should().BeApproximately(b.M13, tol);
        a.M21.Should().BeApproximately(b.M21, tol);
        a.M22.Should().BeApproximately(b.M22, tol);
        a.M23.Should().BeApproximately(b.M23, tol);
        a.M31.Should().BeApproximately(b.M31, tol);
        a.M32.Should().BeApproximately(b.M32, tol);
        a.M33.Should().BeApproximately(b.M33, tol);
    }

    #endregion
}
