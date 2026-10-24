using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PosePath3BuilderTests
{
    [Fact]
    public void Build_Empty_ReturnsEmptyPath()
    {
        var path = new PosePath3Builder<double>().Build();
        path.Count.Should().Be(0);
    }

    [Fact]
    public void StartAt_OnlyOnce_BuildsOnePose()
    {
        var start = new Pose3<double>(1, 2, 3, 0, 0, 0);
        var path = new PosePath3Builder<double>().StartAt(start).Build();

        path.Count.Should().Be(1);
        path[0].Should().Be(start);
    }

    [Fact]
    public void StartAt_CalledTwice_Throws()
    {
        var b = new PosePath3Builder<double>().StartAt(new Pose3<double>(0, 0, 0, 0, 0, 0));
        Action act = () => b.StartAt(new Pose3<double>(1, 1, 1, 0, 0, 0));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Step_BeforeStartAt_Throws()
    {
        var b = new PosePath3Builder<double>();
        Action act = () => b.Step(new Frame3<double>(new Point3<double>(1, 0, 0), Quaternion<double>.Identity));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Step_ComposesInThePreviousPoseFrame()
    {
        // Start rotated 90° about Z: a +X step in the pose's frame moves +Y in the world, and the rotations compose.
        var step = new Frame3<double>(new Point3<double>(10, 0, 0), Quaternion<double>.FromAxisAngle(Vector3<double>.EZ, Math.PI / 2));
        var path = new PosePath3Builder<double>()
            .StartAt(new Pose3<double>(100, 50, 200, 0, 0, 90))
            .Step(step)
            .Build();

        path.Count.Should().Be(2);
        path[1].X.Should().BeApproximately(100, 1e-9);
        path[1].Y.Should().BeApproximately(60, 1e-9);
        path[1].Z.Should().BeApproximately(200, 1e-9);
        ((double)path[1].Rz).Should().BeApproximately(180, 1e-9);
    }

    [Fact]
    public void Step_Chain_FromZeroCapacityAndLiteralStart_Grows()
    {
        // Regression guard: doubling from 0 would never grow; constructor must floor at 1.
        var step = new Frame3<double>(new Point3<double>(10, 0, 0), Quaternion<double>.Identity);
        var path = new PosePath3Builder<double>(0)
            .StartAt([120, 50, 200, 180, 0, 0])
            .Step(step)
            .Step(step)
            .Build();

        path.Count.Should().Be(3);
        path[0].Position.Should().Be(new Point3<double>(120, 50, 200));
        path[^1].X.Should().BeApproximately(140, 1e-9);
        path[^1].Y.Should().BeApproximately(50, 1e-9);
        path[^1].Z.Should().BeApproximately(200, 1e-9);
    }
}
