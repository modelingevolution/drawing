using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// <c>Frame3.FromSurface</c> (moved from the pose type in epic-129 S3, design § "Pose3 is a wire and edit format only").
/// </summary>
public class Frame3SurfaceTests
{
    private const double Tolerance = 1e-10;

    #region FromSurface

    [Fact]
    public void FromSurface_HorizontalPlane_HintAbove_ZPointsUp()
    {
        // Horizontal plane at Z=0 (XY plane)
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 1, 0);
        // Hint point above the plane
        var h = new Point3<double>(0.5, 0.5, 10);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane
        pose.Position.X.Should().BeApproximately(0.5, Tolerance);
        pose.Position.Y.Should().BeApproximately(0.5, Tolerance);
        pose.Position.Z.Should().BeApproximately(0, Tolerance);

        // Z-axis should point up (toward h)
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(0, Tolerance);
        zDir.Z.Should().BeApproximately(1, Tolerance);
    }

    [Fact]
    public void FromSurface_HorizontalPlane_HintBelow_ZPointsDown()
    {
        // Horizontal plane at Z=0
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 1, 0);
        // Hint point below the plane
        var h = new Point3<double>(0.5, 0.5, -10);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane
        pose.Position.X.Should().BeApproximately(0.5, Tolerance);
        pose.Position.Y.Should().BeApproximately(0.5, Tolerance);
        pose.Position.Z.Should().BeApproximately(0, Tolerance);

        // Z-axis should point down (toward h)
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(0, Tolerance);
        zDir.Z.Should().BeApproximately(-1, Tolerance);
    }

    [Fact]
    public void FromSurface_VerticalPlane_XZ_HintInFront()
    {
        // Vertical plane (XZ plane, Y=0)
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 0, 1);
        // Hint point in front (positive Y)
        var h = new Point3<double>(0.5, 5, 0.5);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane (Y=0)
        pose.Position.X.Should().BeApproximately(0.5, Tolerance);
        pose.Position.Y.Should().BeApproximately(0, Tolerance);
        pose.Position.Z.Should().BeApproximately(0.5, Tolerance);

        // Z-axis should point toward positive Y
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(1, Tolerance);
        zDir.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void FromSurface_XAxisAlongEdgeAB()
    {
        // Horizontal plane
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(2, 0, 0);  // X direction
        var c = new Point3<double>(0, 2, 0);
        var h = new Point3<double>(1, 1, 5);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // X-axis should be along a→b direction (positive X)
        var xDir = pose.Rotation.Rotate(Vector3<double>.EX);
        xDir.X.Should().BeApproximately(1, Tolerance);
        xDir.Y.Should().BeApproximately(0, Tolerance);
        xDir.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void FromSurface_TiltedPlane()
    {
        // Plane tilted 45° around X axis (normal points in Y-Z direction)
        // Plane equation: z = y (passes through origin with normal (0, -1, 1))
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 1, 1);  // 45° tilt
        // Hint point OFF the plane (z ≠ y, so z - y ≠ 0)
        var h = new Point3<double>(0.5, 2, 5);  // z=5, y=2, so h is above the plane

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Z should point toward h - normal is (0, -1/√2, 1/√2)
        // Since h has z > y, h is on the +normal side, so Z = +normal
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        // Y and Z components should have same magnitude (45° angle from Y-Z plane)
        Math.Abs(zDir.Y).Should().BeApproximately(Math.Abs(zDir.Z), 0.01);
        // Z component should be positive (normal points toward +Z more than -Y)
        zDir.Z.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromSurface_ThrowsWhenHintOnPlane()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 1, 0);
        // Hint point ON the plane
        var h = new Point3<double>(0.5, 0.5, 0);

        var act = () => Frame3<double>.FromSurface(a, b, c, h);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*lies on the surface*");
    }

    [Fact]
    public void FromSurface_ThrowsWhenPointsCollinear()
    {
        // All three points on a line
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(2, 0, 0);
        var h = new Point3<double>(0, 1, 0);

        var act = () => Frame3<double>.FromSurface(a, b, c, h);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*collinear*");
    }

    [Fact]
    public void FromSurface_TransformPointOnSurface_HasZeroZ()
    {
        // Create any surface
        var a = new Point3<double>(10, 20, 5);
        var b = new Point3<double>(15, 20, 5);
        var c = new Point3<double>(10, 25, 8);
        var h = new Point3<double>(12, 22, 20);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Point 'a' is on the surface, so in local coordinates its Z should be 0
        var inverse = pose.Inverse();
        var aLocal = inverse.TransformPoint(a);

        aLocal.Z.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void FromSurface_HintPointTransforms_ToPositiveZ()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(0, 1, 0);
        var h = new Point3<double>(0.5, 0.5, 10);

        var pose = Frame3<double>.FromSurface(a, b, c, h);

        // Transform h to local coordinates - should have positive Z
        var inverse = pose.Inverse();
        var hLocal = inverse.TransformPoint(h);

        hLocal.Z.Should().BeGreaterThan(0);
        // Local X and Y should be near 0 (h is directly above origin)
        hLocal.X.Should().BeApproximately(0, 0.0001);
        hLocal.Y.Should().BeApproximately(0, 0.0001);
    }

    #endregion

    #region FromSurface (3-point, right-hand rule)

    [Fact]
    public void FromSurface_ThreePoints_OriginAtA()
    {
        var a = new Point3<double>(10, 20, 30);
        var b = new Point3<double>(11, 20, 30);
        var c = new Point3<double>(10, 21, 30);

        var pose = Frame3<double>.FromSurface(a, b, c);

        pose.Position.X.Should().Be(10);
        pose.Position.Y.Should().Be(20);
        pose.Position.Z.Should().Be(30);
    }

    [Fact]
    public void FromSurface_ThreePoints_XAxisAlongAB()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(5, 0, 0);
        var c = new Point3<double>(0, 5, 0);

        var pose = Frame3<double>.FromSurface(a, b, c);

        var xDir = pose.Rotation.Rotate(Vector3<double>.EX);
        xDir.X.Should().BeApproximately(1, Tolerance);
        xDir.Y.Should().BeApproximately(0, Tolerance);
        xDir.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void FromSurface_ThreePoints_RightHandRule_ZPointsUp()
    {
        // Counter-clockwise in XY plane when viewed from above
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);  // +X
        var c = new Point3<double>(0, 1, 0);  // +Y
        // Right-hand rule: X×Y = +Z

        var pose = Frame3<double>.FromSurface(a, b, c);

        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(0, Tolerance);
        zDir.Z.Should().BeApproximately(1, Tolerance);
    }

    [Fact]
    public void FromSurface_ThreePoints_ReversedOrder_ZPointsDown()
    {
        // Reversed order: c, b, a (clockwise when viewed from above)
        var a = new Point3<double>(0, 1, 0);  // was c
        var b = new Point3<double>(1, 0, 0);  // same
        var c = new Point3<double>(0, 0, 0);  // was a
        // Right-hand rule with reversed winding: Z points down

        var pose = Frame3<double>.FromSurface(a, b, c);

        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.Z.Should().BeLessThan(0);
    }

    [Fact]
    public void FromSurface_ThreePoints_VerticalPlane()
    {
        // XZ plane (vertical wall)
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);  // +X
        var c = new Point3<double>(0, 0, 1);  // +Z
        // Right-hand rule: X×Z = -Y

        var pose = Frame3<double>.FromSurface(a, b, c);

        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);
        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(-1, Tolerance);
        zDir.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void FromSurface_ThreePoints_ThrowsWhenCollinear()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(1, 0, 0);
        var c = new Point3<double>(2, 0, 0);

        var act = () => Frame3<double>.FromSurface(a, b, c);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*collinear*");
    }

    [Fact]
    public void FromSurface_ThreePoints_PointsOnSurface_HaveZeroLocalZ()
    {
        var a = new Point3<double>(10, 20, 5);
        var b = new Point3<double>(15, 20, 5);
        var c = new Point3<double>(10, 25, 5);

        var pose = Frame3<double>.FromSurface(a, b, c);
        var inverse = pose.Inverse();

        // All three points should have Z=0 in local coordinates
        inverse.TransformPoint(a).Z.Should().BeApproximately(0, 0.0001);
        inverse.TransformPoint(b).Z.Should().BeApproximately(0, 0.0001);
        inverse.TransformPoint(c).Z.Should().BeApproximately(0, 0.0001);
    }

    #endregion

    #region FromSurface (N-point, best-fit plane)

    [Fact]
    public void FromSurface_NPoints_OriginIsCentroid()
    {
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(10, 0, 0), new(10, 10, 0), new(0, 10, 0)
        };

        var pose = Frame3<double>.FromSurface(points);

        pose.Position.X.Should().BeApproximately(5, 0.0001);
        pose.Position.Y.Should().BeApproximately(5, 0.0001);
        pose.Position.Z.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void FromSurface_NPoints_HorizontalPlane_ZPointsUp()
    {
        // Counter-clockwise in XY plane → right-hand rule → Z points up
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(10, 0, 0), new(10, 10, 0), new(0, 10, 0)
        };

        var pose = Frame3<double>.FromSurface(points);
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);

        zDir.X.Should().BeApproximately(0, Tolerance);
        zDir.Y.Should().BeApproximately(0, Tolerance);
        zDir.Z.Should().BeApproximately(1, Tolerance);
    }

    [Fact]
    public void FromSurface_NPoints_XAxisAlongFirstEdge()
    {
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(5, 0, 0), new(5, 5, 0), new(0, 5, 0)
        };

        var pose = Frame3<double>.FromSurface(points);
        var xDir = pose.Rotation.Rotate(Vector3<double>.EX);

        // X-axis should be along points[0]→points[1] projected onto plane = +X
        xDir.X.Should().BeApproximately(1, Tolerance);
        xDir.Y.Should().BeApproximately(0, Tolerance);
        xDir.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void FromSurface_NPoints_AllPointsHaveNearZeroLocalZ()
    {
        // Coplanar points: all at Z=5
        var points = new Point3<double>[]
        {
            new(0, 0, 5), new(10, 0, 5), new(10, 8, 5),
            new(5, 12, 5), new(0, 8, 5)
        };

        var pose = Frame3<double>.FromSurface(points);
        var inverse = pose.Inverse();

        for (int i = 0; i < points.Length; i++)
        {
            var local = inverse.TransformPoint(points[i]);
            local.Z.Should().BeApproximately(0, 0.0001, $"Point {i} should have Z≈0 in local frame");
        }
    }

    [Fact]
    public void FromSurface_NPoints_NoisyPlane_NormalIsClose()
    {
        // Points approximately on Z=0 plane with small noise
        var points = new Point3<double>[]
        {
            new(0, 0, 0.01), new(10, 0, -0.01), new(10, 10, 0.02),
            new(0, 10, -0.02), new(5, 5, 0.005), new(3, 7, -0.01)
        };

        var pose = Frame3<double>.FromSurface(points);
        var zDir = pose.Rotation.Rotate(Vector3<double>.EZ);

        // Normal should be approximately +Z
        Math.Abs(zDir.Z).Should().BeGreaterThan(0.999);
    }

    [Fact]
    public void FromSurface_NPoints_TiltedPlane()
    {
        // Points on the plane z = x (45° tilt around Y-axis)
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(1, 0, 1), new(1, 1, 1),
            new(0, 1, 0), new(0.5, 0.5, 0.5)
        };

        var pose = Frame3<double>.FromSurface(points);
        var inverse = pose.Inverse();

        for (int i = 0; i < points.Length; i++)
        {
            var local = inverse.TransformPoint(points[i]);
            local.Z.Should().BeApproximately(0, 0.001, $"Point {i} should lie on the surface");
        }
    }

    [Fact]
    public void FromSurface_NPoints_MatchesThreePointForExactSquare()
    {
        // 4 points: first 3 define the same plane
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(10, 0, 0);
        var c = new Point3<double>(10, 10, 0);
        var d = new Point3<double>(0, 10, 0);

        var pose3 = Frame3<double>.FromSurface(a, b, c);
        var poseN = Frame3<double>.FromSurface([a, b, c, d]);

        // Normal directions should match
        var z3 = pose3.Rotation.Rotate(Vector3<double>.EZ);
        var zN = poseN.Rotation.Rotate(Vector3<double>.EZ);
        z3.X.Should().BeApproximately(zN.X, 0.001);
        z3.Y.Should().BeApproximately(zN.Y, 0.001);
        z3.Z.Should().BeApproximately(zN.Z, 0.001);

        // X-axis directions should match
        var x3 = pose3.Rotation.Rotate(Vector3<double>.EX);
        var xN = poseN.Rotation.Rotate(Vector3<double>.EX);
        x3.X.Should().BeApproximately(xN.X, 0.001);
        x3.Y.Should().BeApproximately(xN.Y, 0.001);
        x3.Z.Should().BeApproximately(xN.Z, 0.001);
    }

    [Fact]
    public void FromSurface_NPoints_ThrowsForFewerThan3()
    {
        var points = new Point3<double>[] { new(0, 0, 0), new(1, 0, 0) };

        var act = () => Frame3<double>.FromSurface(points);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*3 points*");
    }

    [Fact]
    public void FromSurface_NPoints_ThrowsForCollinearPoints()
    {
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0)
        };

        var act = () => Frame3<double>.FromSurface(points);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromSurface_NPoints_DelegatesToThreePointFor3()
    {
        var a = new Point3<double>(10, 20, 30);
        var b = new Point3<double>(11, 20, 30);
        var c = new Point3<double>(10, 21, 30);

        var pose3 = Frame3<double>.FromSurface(a, b, c);
        var poseN = Frame3<double>.FromSurface([a, b, c]);

        // Exact same result — delegates to 3-point overload
        pose3.Position.X.Should().Be(poseN.Position.X);
        pose3.Position.Y.Should().Be(poseN.Position.Y);
        pose3.Position.Z.Should().Be(poseN.Position.Z);
    }

    #endregion

    #region Same frame as before the move (TS-129-01)

    // The pose each overload returned on Drawing 1.13.0.71 (master 3c711a8), printed with round-trip precision.
    // The moved method must give the same frame within 1e-12 as that pose converted through FromPose3.
    private static void ShouldMatchOldResult(Frame3<double> actual, Pose3<double> old)
    {
        var expected = Frame3<double>.FromPose3(old, 1.0);
        var (dp, da) = Frame3<double>.Distance(actual, expected);
        dp.Should().BeLessThan(1e-12);
        da.Should().BeLessThan(1e-12);
    }

    [Fact]
    public void FromSurface_ThreePoints_SameFrameAsBeforeTheMove()
    {
        var frame = Frame3<double>.FromSurface(new Point3<double>(10, 20, 5), new Point3<double>(15, 21, 6), new Point3<double>(9, 25, 8));

        ShouldMatchOldResult(frame, new Pose3<double>(10, 20, 5, 30.000000000000004, -11.095803283136393, 11.309932474020215));
    }

    [Fact]
    public void FromSurface_HintPoint_SameFrameAsBeforeTheMove()
    {
        var frame = Frame3<double>.FromSurface(new Point3<double>(10, 20, 5), new Point3<double>(15, 20, 5),
            new Point3<double>(10, 25, 8), new Point3<double>(12, 22, 20));

        ShouldMatchOldResult(frame, new Pose3<double>(12, 28.08823529411765, 9.852941176470589, 30.963756532073525, -0.0, 0));
    }

    [Fact]
    public void FromSurface_NPoints_SameFrameAsBeforeTheMove()
    {
        var frame = Frame3<double>.FromSurface(new Point3<double>[]
        {
            new(0, 0, 0), new(1, 0, 1), new(1, 1, 1.1), new(0, 1, 0.05), new(0.5, 0.5, 0.52)
        });

        ShouldMatchOldResult(frame, new Pose3<double>(0.5, 0.5, 0.534, 3.037244313390312, -45.71745485306115, -0.05383375807035245));
    }

    #endregion
}
