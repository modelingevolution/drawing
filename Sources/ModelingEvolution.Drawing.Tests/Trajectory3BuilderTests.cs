using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Trajectory3BuilderTests
{
    private const float Tol = 1e-3f;

    private static Pose3<float> P(float x, float y, float z) => new(x, y, z, 0, 0, 0);

    [Fact]
    public void Build_Empty_ReturnsEmptyTrajectory()
    {
        var traj = new Trajectory3Builder<float>().Build();
        traj.Count.Should().Be(0);
    }

    [Fact]
    public void Add_Pose_FirstAtTimeZero()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .Build();
        traj.Count.Should().Be(1);
        traj[0].Time.Should().Be(0f);
    }

    [Fact]
    public void Add_PoseWithTime_ExplicitTimestamp()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0), 1f)
            .Add(P(10, 0, 0), 5f)
            .Build();
        traj.Count.Should().Be(2);
        traj[0].Time.Should().Be(1f);
        traj[1].Time.Should().Be(5f);
    }

    [Fact]
    public void Add_RawValues()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(1f, 2f, 3f, 0f, 0f, 0f, 0.5f)
            .Build();
        traj.Count.Should().Be(1);
        traj[0].Position.X.Should().BeApproximately(1f, Tol);
        traj[0].Time.Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void MoveTo_ComputesTimeFromSpeed()
    {
        // Distance from (0,0,0) to (10,0,0) = 10 units
        // Speed = 5 u/s → dt = 10/5 = 2s
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(5f))
            .Build();

        traj.Count.Should().Be(2);
        traj[0].Time.Should().BeApproximately(0f, Tol);
        traj[1].Time.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void MoveTo_ChainsCorrectly()
    {
        // 0→10 at speed 10 (dt=1), then 10→30 at speed 5 (dt=4)
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f))
            .MoveTo(P(30, 0, 0), Speed<float>.From(5f))
            .Build();

        traj.Count.Should().Be(3);
        traj[0].Time.Should().BeApproximately(0f, Tol);
        traj[1].Time.Should().BeApproximately(1f, Tol);   // 10/10
        traj[2].Time.Should().BeApproximately(5f, Tol);   // 1 + 20/5
    }

    [Fact]
    public void MoveTo_3DDiagonal()
    {
        // Distance from origin to (3,4,0) = 5
        // Speed = 2.5 → dt = 2
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(3, 4, 0), Speed<float>.From(2.5f))
            .Build();

        traj[1].Time.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void MoveTo_AsFirstWaypoint_AddsAtTimeZero()
    {
        var traj = new Trajectory3Builder<float>()
            .MoveTo(P(10, 0, 0), Speed<float>.From(5f))
            .Build();

        traj.Count.Should().Be(1);
        traj[0].Time.Should().Be(0f);
    }

    [Fact]
    public void AddTrajectory_ShiftsTimestamps()
    {
        var existing = new Trajectory3<float>(
            new Waypoint3<float>(P(0, 0, 0), 0f),
            new Waypoint3<float>(P(10, 0, 0), 2f));

        var traj = new Trajectory3Builder<float>()
            .Add(P(-10, 0, 0), 0f)
            .Add(P(0, 0, 0), 3f)  // current time = 3
            .AddTrajectory(existing)
            .Build();

        traj.Count.Should().Be(4);
        traj[2].Time.Should().BeApproximately(3f, Tol);  // shifted: 0+3
        traj[3].Time.Should().BeApproximately(5f, Tol);  // shifted: 2+3
    }

    [Fact]
    public void AddPath_UniformTimestamps()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(5, 0, 0), P(10, 0, 0));
        var traj = new Trajectory3Builder<float>()
            .AddPath(path, 0f, 4f)
            .Build();

        traj.Count.Should().Be(3);
        traj[0].Time.Should().BeApproximately(0f, Tol);
        traj[1].Time.Should().BeApproximately(2f, Tol);
        traj[2].Time.Should().BeApproximately(4f, Tol);
    }

    [Fact]
    public void FluentChaining_AllMethodsReturnThis()
    {
        var builder = new Trajectory3Builder<float>();
        var same = builder
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(5f))
            .Add(P(20, 0, 0), 10f);
        same.Should().BeSameAs(builder);
    }

    [Fact]
    public void MoveTo_WithPositionAndRotation()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(new Point3<float>(10, 0, 0), Rotation3<float>.Identity, Speed<float>.From(5f))
            .Build();

        traj[1].Time.Should().BeApproximately(2f, Tol);
    }
}
