using FluentAssertions;
using static ModelingEvolution.Drawing.Tests.Frame3TestData;

namespace ModelingEvolution.Drawing.Tests;

/// <summary><c>Quaternion.FromTo</c> and <c>Quaternion.FromAxes</c> (epic-129 design § "Drawing: the kinematic chain").</summary>
public class QuaternionFactoryTests
{
    private static Vector3<double> RandomVector(Random rng) => new(Uniform(rng, 1), Uniform(rng, 1), Uniform(rng, 1));

    private static void ShouldBeClose(Vector3<double> a, Vector3<double> b, double tol)
    {
        a.X.Should().BeApproximately(b.X, tol);
        a.Y.Should().BeApproximately(b.Y, tol);
        a.Z.Should().BeApproximately(b.Z, tol);
    }

    [Fact]
    public void FromTo_TurnsFromIntoTo()
    {
        var rng = new Random(129);
        for (int i = 0; i < 1000; i++)
        {
            var from = RandomVector(rng);
            var to = RandomVector(rng);

            var q = Quaternion<double>.FromTo(from, to);

            q.Length.Should().BeApproximately(1, 1e-12);
            ShouldBeClose(q.Rotate(from.Normalize()), to.Normalize(), 1e-12);
        }
    }

    [Fact]
    public void FromTo_IsTheShortestArc()
    {
        var q = Quaternion<double>.FromTo(Vector3<double>.EX, Vector3<double>.EY);

        // 90° about +Z
        QuaternionError(q, Quaternion<double>.FromAxisAngle(Vector3<double>.EZ, Math.PI / 2)).Should().BeLessThan(1e-12);
    }

    [Fact]
    public void FromTo_SameDirection_IsIdentity()
    {
        var q = Quaternion<double>.FromTo(new Vector3<double>(0, 0, 2), Vector3<double>.EZ);

        QuaternionError(q, Quaternion<double>.Identity).Should().BeLessThan(1e-12);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0.3, -0.4, 0.5)]
    public void FromTo_OppositeDirections_TurnsByPi(double x, double y, double z)
    {
        var from = new Vector3<double>(x, y, z);

        var q = Quaternion<double>.FromTo(from, -from);

        ShouldBeClose(q.Rotate(from.Normalize()), -from.Normalize(), 1e-12);
        q.W.Should().BeApproximately(0, 1e-12);
    }

    [Fact]
    public void FromTo_ZeroVector_Throws()
    {
        var act = () => Quaternion<double>.FromTo(Vector3<double>.Zero, Vector3<double>.EX);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromAxes_RecoversTheRotationOfItsAxes()
    {
        var rng = new Random(129);
        for (int i = 0; i < 10_000; i++)
        {
            var q = RandomRotation(rng);

            var back = Quaternion<double>.FromAxes(q.Rotate(Vector3<double>.EX), q.Rotate(Vector3<double>.EY), q.Rotate(Vector3<double>.EZ));

            QuaternionError(back, q).Should().BeLessThan(1e-12);
        }
    }

    [Fact]
    public void FromAxes_Identity()
    {
        QuaternionError(Quaternion<double>.FromAxes(Vector3<double>.EX, Vector3<double>.EY, Vector3<double>.EZ), Quaternion<double>.Identity)
            .Should().BeLessThan(1e-15);
    }
}
