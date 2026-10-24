using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Matrix3x3SimilarityTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void Identity_WhenSourceEqualsTarget()
    {
        var v = new Vector3<double>(1, 0, 0);
        var M = Matrix3x3<double>.SimilarityFromVectors(v, v);

        var result = M.Transform(v);
        result.X.Should().BeApproximately(v.X, Tolerance);
        result.Y.Should().BeApproximately(v.Y, Tolerance);
        result.Z.Should().BeApproximately(v.Z, Tolerance);
    }

    [Fact]
    public void PureScale_DoublesAlongSameAxis()
    {
        var source = new Vector3<double>(1, 0, 0);
        var target = new Vector3<double>(2, 0, 0);

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, Tolerance);
        result.Y.Should().BeApproximately(target.Y, Tolerance);
        result.Z.Should().BeApproximately(target.Z, Tolerance);
    }

    [Fact]
    public void PureRotation_NinetyDegreesXtoY()
    {
        var source = new Vector3<double>(1, 0, 0);
        var target = new Vector3<double>(0, 1, 0);

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, Tolerance);
        result.Y.Should().BeApproximately(target.Y, Tolerance);
        result.Z.Should().BeApproximately(target.Z, Tolerance);
    }

    [Fact]
    public void Combined_RotationAndScale()
    {
        var source = new Vector3<double>(1, 0, 0);
        var target = new Vector3<double>(0, 2, 0);   // 90deg + scale 2x

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, Tolerance);
        result.Y.Should().BeApproximately(target.Y, Tolerance);
        result.Z.Should().BeApproximately(target.Z, Tolerance);
    }

    [Fact]
    public void ZeroSource_Throws()
    {
        var source = Vector3<double>.Zero;
        var target = new Vector3<double>(1, 0, 0);
        Action act = () => Matrix3x3<double>.SimilarityFromVectors(source, target);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ZeroTarget_Throws()
    {
        var source = new Vector3<double>(1, 0, 0);
        Action act = () => Matrix3x3<double>.SimilarityFromVectors(source, Vector3<double>.Zero);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0.3, -0.4, 0.5)]
    public void AntiParallel_IsAHalfTurnMappingSourceToTarget(double x, double y, double z)
    {
        // Opposite vectors: the rotation is Quaternion.FromTo's half-turn. Its axis is normalize(EX × source), or
        // normalize(EY × source) when source is parallel to EX (so +X → -X turns about -Z). The test asserts only
        // what every choice satisfies: source maps to target and the rotation is a half-turn (trace = -1).
        var source = new Vector3<double>(x, y, z);
        var target = -2 * source;

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, 1e-12);
        result.Y.Should().BeApproximately(target.Y, 1e-12);
        result.Z.Should().BeApproximately(target.Z, 1e-12);
        var R = 0.5 * M;
        (R.M11 + R.M22 + R.M33).Should().BeApproximately(-1, 1e-12);
        R.Determinant.Should().BeApproximately(1, 1e-12);
    }

    [Fact]
    public void Gimbal_NinetyDegreesAroundY_XtoZ()
    {
        // 90-degree rotation around Y axis is at the Euler ZYX gimbal singularity (pitch ±90°).
        // The intermediate Euler representation can lose a DOF here — verify the matrix still
        // round-trips the source to the target correctly.
        var source = new Vector3<double>(1, 0, 0);
        var target = new Vector3<double>(0, 0, 1);

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, 1e-6);
        result.Y.Should().BeApproximately(target.Y, 1e-6);
        result.Z.Should().BeApproximately(target.Z, 1e-6);
    }

    [Fact]
    public void NonAxisAligned_VectorsAlign()
    {
        var source = new Vector3<double>(1, 1, 0);
        var target = new Vector3<double>(2, 0, 2);

        var M = Matrix3x3<double>.SimilarityFromVectors(source, target);
        var result = M.Transform(source);

        result.X.Should().BeApproximately(target.X, 1e-6);
        result.Y.Should().BeApproximately(target.Y, 1e-6);
        result.Z.Should().BeApproximately(target.Z, 1e-6);
    }
}
