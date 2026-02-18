using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Trajectory3Tests
{
    private const float Tol = 1e-3f;

    private static Pose3<float> P(float x, float y, float z) => new(x, y, z, 0, 0, 0);
    private static Waypoint3<float> W(float x, float y, float z, float t) => new(P(x, y, z), t);

    // ─────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultCtor_Empty()
    {
        var traj = new Trajectory3<float>();
        traj.Count.Should().Be(0);
    }

    [Fact]
    public void ParamsCtor_StoresWaypoints()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 1));
        traj.Count.Should().Be(2);
        traj[0].Position.Should().Be(new Point3<float>(0, 0, 0));
        traj[0].Time.Should().Be(0f);
        traj[1].Time.Should().Be(1f);
    }

    [Fact]
    public void Start_End()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 5));
        traj.Start.Time.Should().Be(0f);
        traj.End.Time.Should().Be(5f);
    }

    // ─────────────────────────────────────────────
    // Duration & Length
    // ─────────────────────────────────────────────

    [Fact]
    public void Duration_IsLastMinusFirst()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 1), W(10, 0, 0, 4));
        traj.Duration().Should().BeApproximately(3f, Tol);
    }

    [Fact]
    public void Length_PositionalDistance()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 1));
        traj.Length().Should().BeApproximately(10f, Tol);
    }

    // ─────────────────────────────────────────────
    // AtTime — interpolation
    // ─────────────────────────────────────────────

    [Fact]
    public void AtTime_AtStart_ReturnsFirstPose()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 2));
        var pose = traj.AtTime(0f);
        pose.X.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void AtTime_AtEnd_ReturnsLastPose()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 2));
        var pose = traj.AtTime(2f);
        pose.X.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void AtTime_AtMidpoint_InterpolatesPosition()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 2));
        var pose = traj.AtTime(1f);
        pose.X.Should().BeApproximately(5f, Tol);
        pose.Y.Should().BeApproximately(0f, Tol);
        pose.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void AtTime_BeforeStart_ClampsToFirst()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 1), W(10, 0, 0, 3));
        var pose = traj.AtTime(0f);
        pose.X.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void AtTime_AfterEnd_ClampsToLast()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 2));
        var pose = traj.AtTime(100f);
        pose.X.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void AtTime_MultiSegment()
    {
        // 0->10 over [0,2], 10->30 over [2,4]
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 2), W(30, 0, 0, 4));
        traj.AtTime(1f).X.Should().BeApproximately(5f, Tol);   // midway first segment
        traj.AtTime(3f).X.Should().BeApproximately(20f, Tol);  // midway second segment
    }

    // ─────────────────────────────────────────────
    // Reverse
    // ─────────────────────────────────────────────

    [Fact]
    public void Reverse_FlipsOrderPreservesTimingIntervals()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 1), W(30, 0, 0, 3));
        var rev = traj.Reverse();
        rev.Count.Should().Be(3);
        // Original intervals: [0-1], [1-3] → reversed order with same intervals: [0-2], [2-3]
        rev[0].Position.X.Should().BeApproximately(30f, Tol);
        rev[0].Time.Should().BeApproximately(0f, Tol);
        rev[1].Position.X.Should().BeApproximately(10f, Tol);
        rev[1].Time.Should().BeApproximately(2f, Tol);
        rev[2].Position.X.Should().BeApproximately(0f, Tol);
        rev[2].Time.Should().BeApproximately(3f, Tol);
    }

    // ─────────────────────────────────────────────
    // ToPosePath & ToPolyline3
    // ─────────────────────────────────────────────

    [Fact]
    public void ToPosePath_StripsTimestamps()
    {
        var traj = new Trajectory3<float>(W(1, 2, 3, 0), W(4, 5, 6, 1));
        var path = traj.ToPosePath();
        path.Count.Should().Be(2);
        path[0].X.Should().BeApproximately(1f, Tol);
        path[1].X.Should().BeApproximately(4f, Tol);
    }

    [Fact]
    public void ToPolyline3_ExtractsPositions()
    {
        var traj = new Trajectory3<float>(W(1, 2, 3, 0), W(4, 5, 6, 1));
        var poly = traj.ToPolyline3();
        poly.Count.Should().Be(2);
        poly[0].Should().Be(new Point3<float>(1, 2, 3));
    }

    // ─────────────────────────────────────────────
    // Resample
    // ─────────────────────────────────────────────

    [Fact]
    public void Resample_UniformInterval()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 10));
        var resampled = traj.Resample(5f);
        resampled.Count.Should().Be(3); // t=0, t=5, t=10
        resampled[0].Time.Should().BeApproximately(0f, Tol);
        resampled[1].Time.Should().BeApproximately(5f, Tol);
        resampled[2].Time.Should().BeApproximately(10f, Tol);
        resampled[1].Position.X.Should().BeApproximately(5f, Tol);
    }

    // ─────────────────────────────────────────────
    // Operators
    // ─────────────────────────────────────────────

    [Fact]
    public void PlusVector_TranslatesPositions()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(10, 0, 0, 1));
        var moved = traj + new Vector3<float>(1, 2, 3);
        moved[0].Position.X.Should().BeApproximately(1f, Tol);
        moved[0].Position.Y.Should().BeApproximately(2f, Tol);
        moved[0].Time.Should().Be(0f); // time unchanged
    }

    // ─────────────────────────────────────────────
    // Equality
    // ─────────────────────────────────────────────

    [Fact]
    public void Equality_SameWaypoints_AreEqual()
    {
        var a = new Trajectory3<float>(W(1, 2, 3, 0), W(4, 5, 6, 1));
        var b = new Trajectory3<float>(W(1, 2, 3, 0), W(4, 5, 6, 1));
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ─────────────────────────────────────────────
    // JSON round-trip
    // ─────────────────────────────────────────────

    [Fact]
    public void Json_RoundTrip()
    {
        var traj = new Trajectory3<float>(W(1, 2, 3, 0.5f), W(4, 5, 6, 1.5f));
        var json = System.Text.Json.JsonSerializer.Serialize(traj);
        json.Should().Be("[1,2,3,0,0,0,0.5,4,5,6,0,0,0,1.5]");
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Trajectory3<float>>(json);
        deserialized.Should().Be(traj);
    }

    // ─────────────────────────────────────────────
    // ToString
    // ─────────────────────────────────────────────

    [Fact]
    public void ToString_ShowsCount()
    {
        var traj = new Trajectory3<float>(W(0, 0, 0, 0), W(1, 1, 1, 1));
        traj.ToString().Should().Be("Trajectory3(2 waypoints)");
    }
}
