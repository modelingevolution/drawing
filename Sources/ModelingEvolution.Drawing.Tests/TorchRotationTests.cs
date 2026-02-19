using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class TorchRotationTests
{
    private const float Tol = 1e-3f;

    /// <summary>
    /// Print what Rotate(EZ) actually produces for various Euler angles.
    /// This tells us the real convention.
    /// </summary>
    [Fact]
    public void PrintForwardDirections()
    {
        var cases = new (string name, float rx, float ry, float rz)[]
        {
            ("Identity",     0,   0,   0),
            ("Rx=90",       90,   0,   0),
            ("Rx=45",       45,   0,   0),
            ("Rx=135",     135,   0,   0),
            ("Ry=90",        0,  90,   0),
            ("Ry=45",        0,  45,   0),
            ("Ry=-45",       0, -45,   0),
            ("Rz=90",        0,   0,  90),
            ("Rx=90,Ry=45", 90,  45,   0),
            ("Rx=45,Ry=90", 45,  90,   0),
            ("Rx=90,Rz=45", 90,   0,  45),
        };

        foreach (var (name, rx, ry, rz) in cases)
        {
            var rot = new Rotation3<float>(rx, ry, rz);
            var fwd = rot.Rotate(Vector3<float>.EZ);
            Console.WriteLine($"  {name,-20} Rotate(EZ) = ({fwd.X:F4}, {fwd.Y:F4}, {fwd.Z:F4})");
        }

        // Also test what axis rotation achieves "45° from vertical toward +X"
        // Desired: fwd = (0.707, -0.707, 0) — down and toward +X
        Console.WriteLine();
        Console.WriteLine("  Searching for 45° tilt toward +X (fwd ≈ (0.707, -0.707, 0))...");
        for (int rx = 0; rx <= 180; rx += 15)
        for (int ry = -90; ry <= 90; ry += 15)
        {
            var rot = new Rotation3<float>(rx, ry, 0);
            var fwd = rot.Rotate(Vector3<float>.EZ);
            if (MathF.Abs(fwd.X - 0.707f) < 0.1f && MathF.Abs(fwd.Y + 0.707f) < 0.1f && MathF.Abs(fwd.Z) < 0.1f)
                Console.WriteLine($"    Rx={rx}, Ry={ry} → ({fwd.X:F3}, {fwd.Y:F3}, {fwd.Z:F3})");
        }

        Console.WriteLine();
        Console.WriteLine("  Searching for 45° tilt toward -X (fwd ≈ (-0.707, -0.707, 0))...");
        for (int rx = 0; rx <= 180; rx += 15)
        for (int ry = -90; ry <= 90; ry += 15)
        {
            var rot = new Rotation3<float>(rx, ry, 0);
            var fwd = rot.Rotate(Vector3<float>.EZ);
            if (MathF.Abs(fwd.X + 0.707f) < 0.1f && MathF.Abs(fwd.Y + 0.707f) < 0.1f && MathF.Abs(fwd.Z) < 0.1f)
                Console.WriteLine($"    Rx={rx}, Ry={ry} → ({fwd.X:F3}, {fwd.Y:F3}, {fwd.Z:F3})");
        }

        Console.WriteLine();
        Console.WriteLine("  Searching for 45° tilt toward -Z (fwd ≈ (0, -0.707, -0.707))...");
        for (int rx = 0; rx <= 180; rx += 15)
        for (int ry = -90; ry <= 90; ry += 15)
        {
            var rot = new Rotation3<float>(rx, ry, 0);
            var fwd = rot.Rotate(Vector3<float>.EZ);
            if (MathF.Abs(fwd.X) < 0.1f && MathF.Abs(fwd.Y + 0.707f) < 0.1f && MathF.Abs(fwd.Z + 0.707f) < 0.1f)
                Console.WriteLine($"    Rx={rx}, Ry={ry} → ({fwd.X:F3}, {fwd.Y:F3}, {fwd.Z:F3})");
        }
    }

    [Fact]
    public void DownRot_ForwardIsNegativeY()
    {
        var rot = new Rotation3<float>(90f, 0f, 0f);
        var fwd = rot.Rotate(Vector3<float>.EZ);
        fwd.X.Should().BeApproximately(0f, Tol);
        fwd.Y.Should().BeApproximately(-1f, Tol);
        fwd.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Edge2_PlusZ_ForwardIs_ZDown()
    {
        var rot = new Rotation3<float>(45f, 0f, 0f);
        var fwd = rot.Rotate(Vector3<float>.EZ);
        fwd.X.Should().BeApproximately(0f, Tol);
        fwd.Y.Should().BeApproximately(-0.707f, Tol);
        fwd.Z.Should().BeApproximately(0.707f, Tol);
    }

    /// <summary>
    /// Build the same L-path trajectory as the Raylib PoC and sample poses at regular intervals.
    /// Writes diagnostic output to D:\source\modelingevolution\temp\trajectory_debug.txt
    /// </summary>
    [Fact]
    public void SampleLPathTrajectory_PrintForwardAtEachPoint()
    {
        var outputPath = @"D:\source\modelingevolution\temp\trajectory_debug.txt";
        using var log = new StreamWriter(outputPath);

        const float workpieceW = 100f;
        const float workpieceH = 60f;
        const float filletRadius = 10f;
        const float approachHeight = 40f;
        const float weldHeight = 0f;
        const float weldSpeed = 30f;
        const float approachSpeed = 60f;

        var speed = Speed<float>.From(weldSpeed);
        var fastSpeed = Speed<float>.From(approachSpeed);
        var corner = Corner<float>.Round(filletRadius);

        float x0 = 0f, x1 = workpieceW;
        float z0 = 0f, z1 = workpieceH;
        float y = weldHeight;
        float d = 2f * filletRadius;

        // Fillet weld from OUTSIDE: body outside chimney, tip toward fillet, 45° from plate
        var edge1Rot = new Rotation3<float>(45f, 0f, 0f);     // +X travel at z=0, body outside (-Z)
        var edge2Rot = new Rotation3<float>(45f, -90f, 0f);   // +Z travel at x=W, body outside (+X)
        var downRot  = new Rotation3<float>(90f, 0f, 0f);     // straight down

        // Verify the raw rotations first
        var e1fwd = edge1Rot.Rotate(Vector3<float>.EZ);
        var e2fwd = edge2Rot.Rotate(Vector3<float>.EZ);
        var dfwd  = downRot.Rotate(Vector3<float>.EZ);
        log.WriteLine($"edge1Rot(45,0,0) fwd=({e1fwd.X:F3}, {e1fwd.Y:F3}, {e1fwd.Z:F3})  // should be (0, -0.707, +0.707)");
        log.WriteLine($"edge2Rot(45,-90,0) fwd=({e2fwd.X:F3}, {e2fwd.Y:F3}, {e2fwd.Z:F3})  // should be (-0.707, -0.707, 0)");
        log.WriteLine($"downRot(90,0,0)   fwd=({dfwd.X:F3}, {dfwd.Y:F3}, {dfwd.Z:F3})");
        log.WriteLine();

        var builder = new Trajectory3Builder<float>();
        builder
            .Add(new Pose3<float>(new Point3<float>(x0, approachHeight, z0), downRot))
            .MoveTo(new Pose3<float>(new Point3<float>(x0, y, z0), edge1Rot), fastSpeed)
            .MoveTo(new Pose3<float>(new Point3<float>(x1 - d, y, z0), edge1Rot), speed)
            .MoveTo(new Pose3<float>(new Point3<float>(x1, y, z0), edge2Rot), speed, corner)
            .MoveTo(new Pose3<float>(new Point3<float>(x1, y, z1), edge2Rot), speed)
            .MoveTo(new Pose3<float>(new Point3<float>(x1, approachHeight, z1), downRot), fastSpeed);

        var trajectory = builder.Build();

        log.WriteLine($"Trajectory: {trajectory.Count} waypoints, duration={trajectory.Duration():F2}s");
        log.WriteLine();

        // Print all waypoints
        log.WriteLine("=== Raw Waypoints ===");
        var span = trajectory.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            var wp = span[i];
            var fwd = wp.Pose.Rotation.Rotate(Vector3<float>.EZ);
            log.WriteLine($"  WP[{i,2}] t={wp.Time:F3}s  pos=({wp.Position.X:F1}, {wp.Position.Y:F1}, {wp.Position.Z:F1})  " +
                          $"rot=({wp.Pose.Rotation.Rx:F1}, {wp.Pose.Rotation.Ry:F1}, {wp.Pose.Rotation.Rz:F1})  " +
                          $"fwd=({fwd.X:F3}, {fwd.Y:F3}, {fwd.Z:F3})");
        }

        log.WriteLine();
        log.WriteLine("=== Sampled Poses (every 0.25s) ===");
        float duration = trajectory.Duration();
        float t0 = span[0].Time;
        for (float t = 0f; t <= duration + 0.01f; t += 0.25f)
        {
            var pose = trajectory.AtTime(t0 + t);
            var fwd = pose.Rotation.Rotate(Vector3<float>.EZ);
            float angleFromVert = MathF.Acos(MathF.Min(1f, MathF.Abs(fwd.Y))) * 180f / MathF.PI;
            log.WriteLine($"  t={t:F2}s  pos=({pose.X:F1}, {pose.Y:F1}, {pose.Z:F1})  " +
                          $"fwd=({fwd.X:F3}, {fwd.Y:F3}, {fwd.Z:F3})  " +
                          $"angle_from_vert={angleFromVert:F1}°");
        }

        log.WriteLine();
        log.WriteLine("=== Edge 1 verification (WP[1] to WP[2]) ===");
        var wp1 = span[1];
        var wp2 = span[2];
        float midEdge1 = (wp1.Time + wp2.Time) / 2f;
        var midPose = trajectory.AtTime(midEdge1);
        var midFwd = midPose.Rotation.Rotate(Vector3<float>.EZ);
        log.WriteLine($"  WP[1].Time={wp1.Time:F3}  WP[2].Time={wp2.Time:F3}  mid={midEdge1:F3}");
        log.WriteLine($"  Mid-edge1 pos=({midPose.X:F1}, {midPose.Y:F1}, {midPose.Z:F1})  " +
                      $"fwd=({midFwd.X:F3}, {midFwd.Y:F3}, {midFwd.Z:F3})");
        log.WriteLine($"  EXPECTED: (0.000, -0.707, +0.707)  ACTUAL: ({midFwd.X:F3}, {midFwd.Y:F3}, {midFwd.Z:F3})");

        // Assert edge 1 forward direction: body→tip points toward +Z and down (-Y) at 45°
        // Body is outside at -Z, tip reaches toward fillet (welding from outside)
        midFwd.X.Should().BeApproximately(0f, 0.01f, "torch should have no X tilt on edge 1");
        midFwd.Y.Should().BeApproximately(-0.707f, 0.01f, "torch should point downward on edge 1");
        midFwd.Z.Should().BeApproximately(0.707f, 0.01f, "torch tip should point toward +Z (body outside at -Z)");

        // Assert no NaN in any waypoint
        for (int i = 0; i < span.Length; i++)
        {
            var wpFwd = span[i].Pose.Rotation.Rotate(Vector3<float>.EZ);
            float.IsNaN(wpFwd.X).Should().BeFalse($"WP[{i}] should not have NaN rotation");
        }

        // Check: is the Slerp between two identical rotations still the same rotation?
        log.WriteLine();
        log.WriteLine("=== Slerp test: edge1Rot to edge1Rot at t=0.5 ===");
        var slerped = Rotation3<float>.Slerp(edge1Rot, edge1Rot, 0.5f);
        var slerpFwd = slerped.Rotate(Vector3<float>.EZ);
        log.WriteLine($"  Slerp result: rot=({slerped.Rx:F1}, {slerped.Ry:F1}, {slerped.Rz:F1})  fwd=({slerpFwd.X:F3}, {slerpFwd.Y:F3}, {slerpFwd.Z:F3})");

        // Check: Slerp between downRot and edge1Rot at various t
        log.WriteLine();
        log.WriteLine("=== Slerp test: downRot(90,0,0) → edge1Rot(45,90,0) ===");
        for (float t = 0f; t <= 1.01f; t += 0.1f)
        {
            var s = Rotation3<float>.Slerp(downRot, edge1Rot, t);
            var sf = s.Rotate(Vector3<float>.EZ);
            log.WriteLine($"  t={t:F1}  rot=({s.Rx:F1}, {s.Ry:F1}, {s.Rz:F1})  fwd=({sf.X:F3}, {sf.Y:F3}, {sf.Z:F3})");
        }

        // Reproduce what ExpandCorner does for the corner at index 3
        log.WriteLine();
        log.WriteLine("=== ExpandCorner reproduction ===");
        // prev = WP[2] edge1Rot, curr = WP[3] edge2Rot, next = WP[4] edge2Rot
        float distIn = 20f;  // distance (80,0,0) to (100,0,0)
        float distOut = 60f; // distance (100,0,0) to (100,0,60)
        float r = 10f;       // corner radius
        float rotFracIn = (distIn - r) / distIn;   // 0.5
        float rotFracOut = r / distOut;              // 0.1667

        log.WriteLine($"  rotFracIn={rotFracIn:F4}, rotFracOut={rotFracOut:F4}");

        var rotP1 = Rotation3<float>.Slerp(edge1Rot, edge2Rot, rotFracIn);
        var rotP2 = Rotation3<float>.Slerp(edge2Rot, edge2Rot, rotFracOut);
        var p1fwd = rotP1.Rotate(Vector3<float>.EZ);
        var p2fwd = rotP2.Rotate(Vector3<float>.EZ);
        log.WriteLine($"  rotP1 = ({rotP1.Rx:F1}, {rotP1.Ry:F1}, {rotP1.Rz:F1})  fwd=({p1fwd.X:F3}, {p1fwd.Y:F3}, {p1fwd.Z:F3})");
        log.WriteLine($"  rotP2 = ({rotP2.Rx:F1}, {rotP2.Ry:F1}, {rotP2.Rz:F1})  fwd=({p2fwd.X:F3}, {p2fwd.Y:F3}, {p2fwd.Z:F3})");

        // Now Slerp across the arc (same as line 410 in ExpandCorner)
        int arcPointCount = 8;
        for (int j = 0; j < arcPointCount; j++)
        {
            float at = (float)j / (arcPointCount - 1);
            var arcRot = Rotation3<float>.Slerp(rotP1, rotP2, at);
            var af = arcRot.Rotate(Vector3<float>.EZ);
            log.WriteLine($"  arc[{j}] t={at:F3}  rot=({arcRot.Rx:F1}, {arcRot.Ry:F1}, {arcRot.Rz:F1})  fwd=({af.X:F3}, {af.Y:F3}, {af.Z:F3})");
        }

        log.Flush();
    }
}
