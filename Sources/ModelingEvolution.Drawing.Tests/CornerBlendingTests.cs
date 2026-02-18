using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class CornerBlendingTests
{
    private const float Tol = 1e-2f;

    private static Pose3<float> P(float x, float y, float z) => new(x, y, z, 0, 0, 0);

    // ─────────────────────────────────────────────
    // Basic corner blending
    // ─────────────────────────────────────────────

    [Fact]
    public void Sharp_Corner_PassesThroughWaypoint()
    {
        // A→B→C with sharp corner at B: trajectory passes through B exactly
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Sharp)
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        // Should have exactly 3 waypoints (no expansion)
        traj.Count.Should().Be(3);
        traj[1].Position.X.Should().BeApproximately(10f, Tol);
        traj[1].Position.Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Round_Corner_ExpandsToArcPoints()
    {
        // A→B→C with round corner at B: more than 3 waypoints
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        traj.Count.Should().BeGreaterThan(3);
    }

    [Fact]
    public void Round_Corner_NeverPassesThroughExactWaypoint()
    {
        // The original waypoint (10,0,0) should NOT appear in the trajectory
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            // No point should be exactly at (10, 0, 0)
            var pos = span[i].Position;
            var isExactCorner = MathF.Abs(pos.X - 10f) < 0.001f && MathF.Abs(pos.Y) < 0.001f;
            isExactCorner.Should().BeFalse($"waypoint {i} at ({pos.X},{pos.Y},{pos.Z}) should not be at the exact corner");
        }
    }

    [Fact]
    public void Round_Corner_90Degree_ArcIsSmooth()
    {
        // 90-degree turn: (0,0,0)→(10,0,0)→(10,10,0)
        // Blend radius 3: arc starts at (8,0,0) and ends at (10,3,0)
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();

        // First point should be (0,0,0)
        span[0].Position.X.Should().BeApproximately(0f, Tol);

        // Second point should be near P1 = (10-3, 0, 0) = (7, 0, 0)
        // (it's the blend start on the incoming segment, 3 units from the corner)
        span[1].Position.X.Should().BeApproximately(7f, Tol);
        span[1].Position.Y.Should().BeApproximately(0f, Tol);

        // Last arc point should be near P2 = (10, 3, 0)
        // (blend end on the outgoing segment, 3 units from the corner)
        var lastArcIdx = span.Length - 2; // second-to-last is last arc point
        span[lastArcIdx].Position.X.Should().BeApproximately(10f, Tol);
        span[lastArcIdx].Position.Y.Should().BeApproximately(3f, Tol);

        // Last point should be (10, 10, 0)
        span[^1].Position.X.Should().BeApproximately(10f, Tol);
        span[^1].Position.Y.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Round_Corner_AllPointsEquidistantFromArcCenter()
    {
        // For a 90-degree turn, the arc should be a true circular arc
        // Arc center for (0,0)→(10,0)→(10,10) with r=3:
        // bisector from (10,0) toward inside: direction (-1,1)/√2
        // centerDist = r / cos(θ/2) = 3 / cos(π/4) = 3√2
        // arcCenter = (10,0) + (-1/√2, 1/√2) * 3√2 = (10-3, 3) = (7, 3)
        // arcRadius = r * tan(θ/2) = 3 * tan(π/4) = 3

        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();
        var arcCenterX = 7f;
        var arcCenterY = 3f;
        var expectedRadius = 3f;

        // Skip first and last waypoints (not part of the arc)
        for (int i = 1; i < span.Length - 1; i++)
        {
            var dx = span[i].Position.X - arcCenterX;
            var dy = span[i].Position.Y - arcCenterY;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            dist.Should().BeApproximately(expectedRadius, 0.1f,
                $"arc point {i} at ({span[i].Position.X:F3},{span[i].Position.Y:F3}) should be {expectedRadius} from arc center ({arcCenterX},{arcCenterY})");
        }
    }

    // ─────────────────────────────────────────────
    // Timestamps
    // ─────────────────────────────────────────────

    [Fact]
    public void Round_Corner_TimestampsAreMonotonic()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();
        for (int i = 1; i < span.Length; i++)
        {
            span[i].Time.Should().BeGreaterThan(span[i - 1].Time,
                $"timestamp at index {i} should be greater than at {i - 1}");
        }
    }

    [Fact]
    public void Round_Corner_PreservesEndpoints()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();
        // First and last waypoints unchanged
        span[0].Position.X.Should().BeApproximately(0f, Tol);
        span[0].Time.Should().BeApproximately(0f, Tol);
        span[^1].Position.X.Should().BeApproximately(10f, Tol);
        span[^1].Position.Y.Should().BeApproximately(10f, Tol);
    }

    // ─────────────────────────────────────────────
    // Corner speed
    // ─────────────────────────────────────────────

    [Fact]
    public void CornerSpeed_AffectsArcDuration()
    {
        // Build two trajectories with same geometry but different corner speeds
        var trajFast = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f), Speed<float>.From(20f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var trajSlow = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f), Speed<float>.From(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        // Slow corner should take longer overall
        trajSlow.Duration().Should().BeGreaterThan(trajFast.Duration());
    }

    [Fact]
    public void CornerSpeed_SlowCorner_IncreasesDuration()
    {
        // Without corner speed (uses natural timing)
        var trajNatural = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        // With very slow corner speed
        var trajSlow = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f), Speed<float>.From(0.5f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        trajSlow.Duration().Should().BeGreaterThan(trajNatural.Duration());
    }

    // ─────────────────────────────────────────────
    // Multiple corners
    // ─────────────────────────────────────────────

    [Fact]
    public void Multiple_Corners_AllExpanded()
    {
        // Square path: 4 corners
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(0, 10, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(0, 0, 0), Speed<float>.From(10f))
            .Build();

        // Should have many more points than the 5 original waypoints
        traj.Count.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Multiple_Corners_TimestampsMonotonic()
    {
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(0, 10, 0), Speed<float>.From(10f))
            .Build();

        var span = traj.AsSpan();
        for (int i = 1; i < span.Length; i++)
        {
            span[i].Time.Should().BeGreaterThan(span[i - 1].Time,
                $"timestamp at index {i} should be > index {i - 1}");
        }
    }

    // ─────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────

    [Fact]
    public void Radius_LargerThanSegment_IsClamped()
    {
        // Segment length is 10, radius is 20 — should clamp to 5 (half segment)
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(20f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        // Should still produce valid output with clamped radius
        traj.Count.Should().BeGreaterThan(3);
        var span = traj.AsSpan();
        for (int i = 1; i < span.Length; i++)
            span[i].Time.Should().BeGreaterThan(span[i - 1].Time);
    }

    [Fact]
    public void Corner_OnFirstWaypoint_IsIgnored()
    {
        // First waypoint can't have a corner (no incoming segment)
        // The builder's Add for first waypoint uses Add(pose), not MoveTo
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f))
            .Build();

        traj.Count.Should().Be(2);
    }

    [Fact]
    public void Corner_OnLastWaypoint_IsIgnored()
    {
        // Last waypoint corner is ignored (no outgoing segment)
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .Build();

        // Last waypoint has a corner but it's the last → ignored
        traj.Count.Should().Be(3);
    }

    [Fact]
    public void NearStraight_Path_NoBlending()
    {
        // Three collinear points — nearly straight, no corner to blend
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(2f))
            .MoveTo(P(20, 0, 0), Speed<float>.From(10f))
            .Build();

        // Nearly straight: corner blending should be skipped
        traj.Count.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    // 3D corners
    // ─────────────────────────────────────────────

    [Fact]
    public void Corner_In3D_ProducesArc()
    {
        // Turn in 3D: X-axis to Z-axis
        var traj = new Trajectory3Builder<float>()
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 0, 10), Speed<float>.From(10f))
            .Build();

        traj.Count.Should().BeGreaterThan(3);

        // All arc points should be in the XZ plane (Y=0)
        var span = traj.AsSpan();
        for (int i = 0; i < span.Length; i++)
            span[i].Position.Y.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // Fluent API
    // ─────────────────────────────────────────────

    [Fact]
    public void FluentChaining_WithCorner_ReturnsThis()
    {
        var builder = new Trajectory3Builder<float>();
        var same = builder
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(5f), Corner<float>.Round(2f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(5f), Corner<float>.Round(1f), Speed<float>.From(3f))
            .MoveTo(P(0, 10, 0), Speed<float>.From(5f));
        same.Should().BeSameAs(builder);
    }

    [Fact]
    public void ArcResolution_AffectsPointCount()
    {
        var builderLow = new Trajectory3Builder<float> { ArcResolution = 4 };
        var trajLow = builderLow
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        var builderHigh = new Trajectory3Builder<float> { ArcResolution = 16 };
        var trajHigh = builderHigh
            .Add(P(0, 0, 0))
            .MoveTo(P(10, 0, 0), Speed<float>.From(10f), Corner<float>.Round(3f))
            .MoveTo(P(10, 10, 0), Speed<float>.From(10f))
            .Build();

        trajHigh.Count.Should().BeGreaterThan(trajLow.Count);
    }
}
