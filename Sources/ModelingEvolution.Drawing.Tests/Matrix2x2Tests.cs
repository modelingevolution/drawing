using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Matrix2x2Tests
{
    private const float Tol = 1e-5f;

    [Fact]
    public void Identity_HasCorrectValues()
    {
        var m = Matrix2x2<float>.Identity;
        m.M11.Should().Be(1f);
        m.M12.Should().Be(0f);
        m.M21.Should().Be(0f);
        m.M22.Should().Be(1f);
    }

    [Fact]
    public void Determinant_Identity_IsOne()
    {
        Matrix2x2<float>.Identity.Determinant.Should().Be(1f);
    }

    [Fact]
    public void Determinant_KnownMatrix()
    {
        // |3 8; 4 6| = 3*6 - 8*4 = -14
        var m = new Matrix2x2<float>(3, 8, 4, 6);
        m.Determinant.Should().BeApproximately(-14f, Tol);
    }

    [Fact]
    public void Trace_KnownMatrix()
    {
        var m = new Matrix2x2<float>(3, 8, 4, 6);
        m.Trace.Should().BeApproximately(9f, Tol);
    }

    [Fact]
    public void Transpose()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        var t = m.Transpose();
        t.M11.Should().Be(1f);
        t.M12.Should().Be(3f);
        t.M21.Should().Be(2f);
        t.M22.Should().Be(4f);
    }

    [Fact]
    public void Addition()
    {
        var a = new Matrix2x2<float>(1, 2, 3, 4);
        var b = new Matrix2x2<float>(5, 6, 7, 8);
        var c = a + b;
        c.Should().Be(new Matrix2x2<float>(6, 8, 10, 12));
    }

    [Fact]
    public void Subtraction()
    {
        var a = new Matrix2x2<float>(5, 6, 7, 8);
        var b = new Matrix2x2<float>(1, 2, 3, 4);
        var c = a - b;
        c.Should().Be(new Matrix2x2<float>(4, 4, 4, 4));
    }

    [Fact]
    public void Multiplication_IdentityIsNeutral()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        var r = m * Matrix2x2<float>.Identity;
        r.Should().Be(m);
    }

    [Fact]
    public void Multiplication_KnownResult()
    {
        // [1 2; 3 4] * [5 6; 7 8] = [1*5+2*7, 1*6+2*8; 3*5+4*7, 3*6+4*8] = [19 22; 43 50]
        var a = new Matrix2x2<float>(1, 2, 3, 4);
        var b = new Matrix2x2<float>(5, 6, 7, 8);
        var c = a * b;
        c.M11.Should().BeApproximately(19f, Tol);
        c.M12.Should().BeApproximately(22f, Tol);
        c.M21.Should().BeApproximately(43f, Tol);
        c.M22.Should().BeApproximately(50f, Tol);
    }

    [Fact]
    public void ScalarMultiplication()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        var r = m * 2f;
        r.Should().Be(new Matrix2x2<float>(2, 4, 6, 8));

        var r2 = 3f * m;
        r2.Should().Be(new Matrix2x2<float>(3, 6, 9, 12));
    }

    [Fact]
    public void TransformVector()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        var v = new Vector<float>(5, 6);
        var r = m.Transform(v);
        // (1*5+2*6, 3*5+4*6) = (17, 39)
        r.X.Should().BeApproximately(17f, Tol);
        r.Y.Should().BeApproximately(39f, Tol);
    }

    [Fact]
    public void TransformVector_OperatorSyntax()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        var v = new Vector<float>(5, 6);
        var r = m * v;
        r.X.Should().BeApproximately(17f, Tol);
        r.Y.Should().BeApproximately(39f, Tol);
    }

    [Fact]
    public void TransformPoint()
    {
        var m = new Matrix2x2<float>(2, 0, 0, 3);
        var p = new Point<float>(4, 5);
        var r = m.Transform(p);
        r.X.Should().BeApproximately(8f, Tol);
        r.Y.Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Inverse_Identity()
    {
        var inv = Matrix2x2<float>.Identity.Inverse();
        inv.Should().Be(Matrix2x2<float>.Identity);
    }

    [Fact]
    public void Inverse_KnownMatrix()
    {
        var m = new Matrix2x2<float>(4, 7, 2, 6);
        var inv = m.Inverse();
        // Verify M * M^-1 = I
        var product = m * inv;
        product.M11.Should().BeApproximately(1f, Tol);
        product.M12.Should().BeApproximately(0f, Tol);
        product.M21.Should().BeApproximately(0f, Tol);
        product.M22.Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Inverse_Singular_Throws()
    {
        var m = new Matrix2x2<float>(1, 2, 2, 4); // det = 0
        var act = () => m.Inverse();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rotation_90Degrees()
    {
        var m = Matrix2x2<float>.Rotation(Radian<float>.FromRadian(MathF.PI / 2f));
        var v = new Vector<float>(1, 0);
        var r = m * v;
        r.X.Should().BeApproximately(0f, Tol);
        r.Y.Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Rotation_45Degrees_PreservesLength()
    {
        var m = Matrix2x2<float>.Rotation(Radian<float>.FromRadian(MathF.PI / 4f));
        var v = new Vector<float>(3, 4);
        var r = m * v;
        // Rotation preserves length
        r.Length.Should().BeApproximately(v.Length, Tol);
    }

    [Fact]
    public void Scale()
    {
        var m = Matrix2x2<float>.Scale(2, 3);
        var v = new Vector<float>(4, 5);
        var r = m * v;
        r.X.Should().BeApproximately(8f, Tol);
        r.Y.Should().BeApproximately(15f, Tol);
    }

    // ── Eigendecomposition ──

    [Fact]
    public void Eigenvalues_DiagonalMatrix()
    {
        // Eigenvalues of diagonal matrix are the diagonal entries
        var m = new Matrix2x2<float>(5, 0, 0, 3);
        var (l1, l2) = m.Eigenvalues();
        l1.Should().BeApproximately(5f, Tol);
        l2.Should().BeApproximately(3f, Tol);
    }

    [Fact]
    public void Eigenvalues_IdentityMatrix()
    {
        var (l1, l2) = Matrix2x2<float>.Identity.Eigenvalues();
        l1.Should().BeApproximately(1f, Tol);
        l2.Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Eigenvalues_SymmetricMatrix()
    {
        // [2 1; 1 2] has eigenvalues 3 and 1
        var m = new Matrix2x2<float>(2, 1, 1, 2);
        var (l1, l2) = m.Eigenvalues();
        l1.Should().BeApproximately(3f, Tol);
        l2.Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Eigenvectors_DiagonalMatrix()
    {
        var m = new Matrix2x2<float>(5, 0, 0, 3);
        var (v1, v2) = m.Eigenvectors();

        // v1 for λ=5 should be (1, 0), v2 for λ=3 should be (0, 1)
        MathF.Abs(v1.X).Should().BeApproximately(1f, Tol);
        MathF.Abs(v1.Y).Should().BeApproximately(0f, Tol);
        MathF.Abs(v2.X).Should().BeApproximately(0f, Tol);
        MathF.Abs(v2.Y).Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Eigenvectors_SymmetricMatrix_AreOrthogonal()
    {
        var m = new Matrix2x2<float>(2, 1, 1, 2);
        var (v1, v2) = m.Eigenvectors();

        // Should be unit vectors
        v1.Length.Should().BeApproximately(1f, Tol);
        v2.Length.Should().BeApproximately(1f, Tol);

        // Should be orthogonal: dot product ≈ 0
        var dot = v1.X * v2.X + v1.Y * v2.Y;
        dot.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Eigen_VerifyAv_Equals_LambdaV()
    {
        // For a symmetric matrix, A·v = λ·v
        var m = new Matrix2x2<float>(4, 2, 2, 3);
        var (l1, v1, l2, v2) = m.Eigen();

        var av1 = m * v1;
        av1.X.Should().BeApproximately(l1 * v1.X, Tol);
        av1.Y.Should().BeApproximately(l1 * v1.Y, Tol);

        var av2 = m * v2;
        av2.X.Should().BeApproximately(l2 * v2.X, Tol);
        av2.Y.Should().BeApproximately(l2 * v2.Y, Tol);
    }

    [Fact]
    public void Eigen_RepeatedEigenvalue()
    {
        // Identity has repeated eigenvalue 1
        var (l1, v1, l2, v2) = Matrix2x2<float>.Identity.Eigen();
        l1.Should().BeApproximately(1f, Tol);
        l2.Should().BeApproximately(1f, Tol);
        v1.Length.Should().BeApproximately(1f, Tol);
        v2.Length.Should().BeApproximately(1f, Tol);
    }

    // ── Covariance ──

    [Fact]
    public void CovarianceMatrix_PointsOnXAxis()
    {
        // Points spread along X only → high cxx, zero cxy, zero cyy
        var pts = new[]
        {
            new Point<float>(-2, 0),
            new Point<float>(-1, 0),
            new Point<float>(0, 0),
            new Point<float>(1, 0),
            new Point<float>(2, 0),
        };
        var cov = Matrix2x2<float>.CovarianceMatrix(pts);
        cov.M11.Should().BeGreaterThan(0f); // variance in X
        cov.M12.Should().BeApproximately(0f, Tol); // no covariance
        cov.M21.Should().BeApproximately(0f, Tol);
        cov.M22.Should().BeApproximately(0f, Tol); // no variance in Y
    }

    [Fact]
    public void CovarianceMatrix_IsSymmetric()
    {
        var rng = new Random(42);
        var pts = new Point<float>[50];
        for (int i = 0; i < 50; i++)
            pts[i] = new Point<float>((float)rng.NextDouble() * 100, (float)rng.NextDouble() * 100);

        var cov = Matrix2x2<float>.CovarianceMatrix(pts);
        cov.M12.Should().BeApproximately(cov.M21, Tol);
    }

    [Fact]
    public void CovarianceMatrix_PrincipalAxisAlignedWithSpread()
    {
        // Points along the line y = x → principal eigenvector should be ~(1/√2, 1/√2)
        var pts = new[]
        {
            new Point<float>(-3, -3),
            new Point<float>(-1, -1),
            new Point<float>(0, 0),
            new Point<float>(1, 1),
            new Point<float>(3, 3),
        };
        var cov = Matrix2x2<float>.CovarianceMatrix(pts);
        var (_, v1, _, _) = cov.Eigen();

        // Principal eigenvector should point along (1,1) direction
        var expected = 1f / MathF.Sqrt(2f);
        MathF.Abs(v1.X).Should().BeApproximately(expected, 0.01f);
        MathF.Abs(v1.Y).Should().BeApproximately(expected, 0.01f);
    }

    [Fact]
    public void CovarianceMatrix_WithKnownCentroid()
    {
        var pts = new[]
        {
            new Point<float>(1, 1),
            new Point<float>(3, 1),
            new Point<float>(1, 3),
            new Point<float>(3, 3),
        };
        var centroid = new Point<float>(2, 2);
        var cov = Matrix2x2<float>.CovarianceMatrix(pts, centroid);

        // Should equal the auto-centroid version
        var covAuto = Matrix2x2<float>.CovarianceMatrix(pts);
        cov.M11.Should().BeApproximately(covAuto.M11, Tol);
        cov.M12.Should().BeApproximately(covAuto.M12, Tol);
        cov.M22.Should().BeApproximately(covAuto.M22, Tol);
    }

    [Fact]
    public void CovarianceMatrix_Empty_ReturnsZero()
    {
        var cov = Matrix2x2<float>.CovarianceMatrix(ReadOnlySpan<Point<float>>.Empty);
        cov.Should().Be(Matrix2x2<float>.Zero);
    }

    [Fact]
    public void Eigen_WithDoubles()
    {
        var m = new Matrix2x2<double>(4, 2, 2, 3);
        var (l1, v1, l2, v2) = m.Eigen();

        // Verify A·v = λ·v
        var av1 = m * v1;
        av1.X.Should().BeApproximately(l1 * v1.X, 1e-10);
        av1.Y.Should().BeApproximately(l1 * v1.Y, 1e-10);

        var av2 = m * v2;
        av2.X.Should().BeApproximately(l2 * v2.X, 1e-10);
        av2.Y.Should().BeApproximately(l2 * v2.Y, 1e-10);
    }

    [Fact]
    public void ToString_Format()
    {
        var m = new Matrix2x2<float>(1, 2, 3, 4);
        m.ToString().Should().Be("[1, 2; 3, 4]");
    }

    // ── JSON serialization ──

    [Fact]
    public void Json_Serialize_Float()
    {
        var m = new Matrix2x2<float>(1.5f, 2.5f, 3.5f, 4.5f);
        var json = JsonSerializer.Serialize(m);
        json.Should().Be("[[1.5,2.5],[3.5,4.5]]");
    }

    [Fact]
    public void Json_Deserialize_Float()
    {
        var m = JsonSerializer.Deserialize<Matrix2x2<float>>("[[1.5,2.5],[3.5,4.5]]");
        m.M11.Should().Be(1.5f);
        m.M12.Should().Be(2.5f);
        m.M21.Should().Be(3.5f);
        m.M22.Should().Be(4.5f);
    }

    [Fact]
    public void Json_RoundTrip_Float()
    {
        var original = new Matrix2x2<float>(1, 2, 3, 4);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Matrix2x2<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Json_RoundTrip_Double()
    {
        var original = new Matrix2x2<double>(1.23456789, 2.34567891, 3.45678912, 4.56789123);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Matrix2x2<double>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Json_Identity_RoundTrip()
    {
        var original = Matrix2x2<float>.Identity;
        var json = JsonSerializer.Serialize(original);
        json.Should().Be("[[1,0],[0,1]]");
        var deserialized = JsonSerializer.Deserialize<Matrix2x2<float>>(json);
        deserialized.Should().Be(original);
    }
}
