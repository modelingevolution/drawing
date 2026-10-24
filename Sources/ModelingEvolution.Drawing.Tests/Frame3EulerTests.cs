using FluentAssertions;
using static ModelingEvolution.Drawing.Tests.Frame3TestData;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>TS-129-02: the one Euler rule on the Drawing side (<c>Frame3.FromPose3</c> / <c>ToPose3</c>, <c>Quaternion.FromRpy</c>).</summary>
public class Frame3EulerTests
{
    private static double AngleDiffDegrees(double a, double b)
    {
        var d = (a - b) % 360.0;
        if (d > 180) d -= 360;
        if (d <= -180) d += 360;
        return Math.Abs(d);
    }

    [Fact]
    public void FromPose3_ToPose3_RoundTripsOver10000RandomPoses()
    {
        var rng = new Random(129);
        for (int i = 0; i < 10_000; i++)
        {
            var p = new Pose3<double>(Uniform(rng, 2000), Uniform(rng, 2000), Uniform(rng, 2000),
                Uniform(rng, 180), Uniform(rng, 89.9), Uniform(rng, 180));

            var back = Frame3<double>.FromPose3(p, 0.001).ToPose3(1000);

            Math.Abs(back.X - p.X).Should().BeLessThan(1e-9);
            Math.Abs(back.Y - p.Y).Should().BeLessThan(1e-9);
            Math.Abs(back.Z - p.Z).Should().BeLessThan(1e-9);
            AngleDiffDegrees((double)back.Rx, (double)p.Rx).Should().BeLessThan(1e-9, $"rx of {p}");
            AngleDiffDegrees((double)back.Ry, (double)p.Ry).Should().BeLessThan(1e-9, $"ry of {p}");
            AngleDiffDegrees((double)back.Rz, (double)p.Rz).Should().BeLessThan(1e-9, $"rz of {p}");
        }
    }

    private static double NearGimbalRy(Random rng) => (rng.Next(2) == 0 ? 1 : -1) * (89.9 + rng.NextDouble() * 0.1);

    private static double[,] RotationOf(Pose3<double> p) =>
        Mul(Mul(Rz((double)p.Rz * Math.PI / 180), Ry((double)p.Ry * Math.PI / 180)), Rx((double)p.Rx * Math.PI / 180));

    private static void ShouldBeSameRotation(double[,] actual, double[,] expected, double tol, string because)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                Math.Abs(actual[i, j] - expected[i, j]).Should().BeLessThan(tol, because);
    }

    [Fact]
    public void FromPose3_ToPose3_KeepsTheRotationNearGimbal_10000Random()
    {
        // |ry| in [89.9°, 90°]: the rx/rz split may change, the rotation may not (every element within 1e-8).
        var rng = new Random(129);
        for (int i = 0; i < 10_000; i++)
        {
            var p = new Pose3<double>(Uniform(rng, 2000), Uniform(rng, 2000), Uniform(rng, 2000),
                Uniform(rng, 180), NearGimbalRy(rng), Uniform(rng, 180));

            var back = Frame3<double>.FromPose3(p, 0.001).ToPose3(1000);

            Math.Abs(back.X - p.X).Should().BeLessThan(1e-9);
            Math.Abs(back.Y - p.Y).Should().BeLessThan(1e-9);
            Math.Abs(back.Z - p.Z).Should().BeLessThan(1e-9);
            ShouldBeSameRotation(RotationOf(back), RotationOf(p), 1e-8, $"rotation of {p}");
        }
    }

    [Theory]
    [InlineData(30, 90, 0)]
    [InlineData(30, 90, 45)]
    [InlineData(-120, -90, 0)]
    [InlineData(-120, -90, 70)]
    public void FromPose3_ToPose3_AtExactly90_KeepsTheRotationWithRzZero(double rx, double ry, double rz)
    {
        var p = new Pose3<double>(0, 0, 0, rx, ry, rz);

        var back = Frame3<double>.FromPose3(p, 1).ToPose3(1);

        ((double)back.Ry).Should().BeApproximately(ry, 1e-6);
        ((double)back.Rz).Should().Be(0);
        ShouldBeSameRotation(RotationOf(back), RotationOf(p), 1e-12, $"rotation of {p}");
    }

    [Fact]
    public void FromPose3_ScalesPositionAndKeepsRotation()
    {
        var p = new Pose3<double>(1000, -500, 250, 0, 0, 90);

        var f = Frame3<double>.FromPose3(p, 0.001);

        f.Position.Should().Be(new Point3<double>(1, -0.5, 0.25));
        var x = f.TransformVector(Vector3<double>.EX);
        x.X.Should().BeApproximately(0, 1e-12);
        x.Y.Should().BeApproximately(1, 1e-12);
    }

    [Fact]
    public void FromRpy_EqualsRzRyRxOver10000RandomAngles()
    {
        var rng = new Random(129);
        for (int n = 0; n < 10_000; n++)
        {
            double roll = Uniform(rng, Math.PI), pitch = Uniform(rng, Math.PI), yaw = Uniform(rng, Math.PI);

            var actual = ToMatrix(Quaternion<double>.FromRpy(roll, pitch, yaw));
            var expected = Mul(Mul(Rz(yaw), Ry(pitch)), Rx(roll));

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Math.Abs(actual[i, j] - expected[i, j]).Should().BeLessThan(1e-12);
        }
    }

    [Fact]
    public void FromRpy_IsTheRuleRotation3Uses()
    {
        var rng = new Random(129);
        for (int n = 0; n < 1000; n++)
        {
            var r = new Rotation3<double>(Uniform(rng, 180), Uniform(rng, 90), Uniform(rng, 180));
            var (rx, ry, rz) = r.ToRadians();

            r.ToQuaternion().Should().Be(Quaternion<double>.FromRpy((double)rx, (double)ry, (double)rz));
        }
    }

    private static double[,] Rx(double a) => new[,] { { 1, 0, 0 }, { 0, Math.Cos(a), -Math.Sin(a) }, { 0, Math.Sin(a), Math.Cos(a) } };
    private static double[,] Ry(double a) => new[,] { { Math.Cos(a), 0, Math.Sin(a) }, { 0, 1, 0 }, { -Math.Sin(a), 0, Math.Cos(a) } };
    private static double[,] Rz(double a) => new[,] { { Math.Cos(a), -Math.Sin(a), 0 }, { Math.Sin(a), Math.Cos(a), 0 }, { 0, 0, 1 } };

    private static double[,] Mul(double[,] a, double[,] b)
    {
        var r = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    r[i, j] += a[i, k] * b[k, j];
        return r;
    }
}
