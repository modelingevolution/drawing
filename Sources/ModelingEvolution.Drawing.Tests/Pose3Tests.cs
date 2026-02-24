using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Pose3Tests
{
    private readonly ITestOutputHelper _output;
    private const double Tolerance = 1e-10;

    public Pose3Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region JSON Serialization

    [Fact]
    public void Pose3Float_JsonSerialization_RoundTrips()
    {
        var original = new Pose3<float>(100f, 200f, 300f, 10f, 20f, 30f);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[100,200,300,10,20,30]");

        var deserialized = JsonSerializer.Deserialize<Pose3<float>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Pose3Double_JsonSerialization_RoundTrips()
    {
        var original = new Pose3<double>(100.5, 200.5, 300.5, 45.0, 90.0, -30.0);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<Pose3<double>>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Constructors and Properties

    [Fact]
    public void Pose3_Constructor_WithComponents_SetsValues()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        pose.X.Should().Be(1);
        pose.Y.Should().Be(2);
        pose.Z.Should().Be(3);
        pose.Rx.Should().Be(10);
        pose.Ry.Should().Be(20);
        pose.Rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_Constructor_WithPointAndRotation_SetsValues()
    {
        var position = new Point3<double>(1, 2, 3);
        var rotation = new Rotation3<double>(10, 20, 30);

        var pose = new Pose3<double>(position, rotation);

        pose.Position.Should().Be(position);
        pose.Rotation.Should().Be(rotation);
    }

    [Fact]
    public void Pose3_Identity_IsAtOriginWithNoRotation()
    {
        var identity = Pose3<double>.Identity;

        identity.X.Should().Be(0);
        identity.Y.Should().Be(0);
        identity.Z.Should().Be(0);
        identity.Rx.Should().Be(0);
        identity.Ry.Should().Be(0);
        identity.Rz.Should().Be(0);
        identity.IsIdentity.Should().BeTrue();
    }

    #endregion

    #region Transform Operations

    [Fact]
    public void Pose3_TransformPoint_WithTranslationOnly()
    {
        var pose = new Pose3<double>(10, 20, 30, 0, 0, 0);
        var localPoint = new Point3<double>(1, 2, 3);

        var worldPoint = pose.TransformPoint(localPoint);

        worldPoint.X.Should().Be(11);
        worldPoint.Y.Should().Be(22);
        worldPoint.Z.Should().Be(33);
    }

    [Fact]
    public void Pose3_TransformPoint_WithRotationOnly()
    {
        var pose = new Pose3<double>(0, 0, 0, 0, 0, 90); // 90 deg around Z
        var localPoint = new Point3<double>(1, 0, 0);

        var worldPoint = pose.TransformPoint(localPoint);

        worldPoint.X.Should().BeApproximately(0, Tolerance);
        worldPoint.Y.Should().BeApproximately(1, Tolerance);
        worldPoint.Z.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void Pose3_TransformVector_RotatesButDoesNotTranslate()
    {
        var pose = new Pose3<double>(100, 200, 300, 0, 0, 90);
        var localVector = new Vector3<double>(1, 0, 0);

        var worldVector = pose.TransformVector(localVector);

        worldVector.X.Should().BeApproximately(0, Tolerance);
        worldVector.Y.Should().BeApproximately(1, Tolerance);
        worldVector.Z.Should().BeApproximately(0, Tolerance);
    }

    #endregion

    #region Lerp

    [Fact]
    public void Pose3_Lerp_AtZero_ReturnsFirst()
    {
        var a = new Pose3<double>(0, 0, 0, 0, 0, 0);
        var b = new Pose3<double>(100, 200, 300, 90, 0, 0);

        var result = Pose3<double>.Lerp(a, b, 0);

        result.X.Should().Be(0);
        result.Y.Should().Be(0);
        result.Z.Should().Be(0);
    }

    [Fact]
    public void Pose3_Lerp_AtOne_ReturnsSecond()
    {
        var a = new Pose3<double>(0, 0, 0, 0, 0, 0);
        var b = new Pose3<double>(100, 200, 300, 90, 0, 0);

        var result = Pose3<double>.Lerp(a, b, 1);

        result.X.Should().Be(100);
        result.Y.Should().Be(200);
        result.Z.Should().Be(300);
    }

    [Fact]
    public void Pose3_Lerp_AtHalf_InterpolatesPosition()
    {
        var a = new Pose3<double>(0, 0, 0, 0, 0, 0);
        var b = new Pose3<double>(100, 200, 300, 0, 0, 0);

        var result = Pose3<double>.Lerp(a, b, 0.5);

        result.X.Should().Be(50);
        result.Y.Should().Be(100);
        result.Z.Should().Be(150);
    }

    #endregion

    #region Distance

    [Fact]
    public void Pose3_Distance_CalculatesPositionDistance()
    {
        var a = new Pose3<double>(0, 0, 0, 0, 0, 0);
        var b = new Pose3<double>(3, 4, 0, 90, 45, 30);

        var distance = Pose3<double>.Distance(a, b);

        distance.Should().Be(5);
    }

    #endregion

    #region Operators

    [Fact]
    public void Pose3_Equality_Works()
    {
        var a = new Pose3<double>(1, 2, 3, 10, 20, 30);
        var b = new Pose3<double>(1, 2, 3, 10, 20, 30);
        var c = new Pose3<double>(1, 2, 3, 10, 20, 31);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
    }

    #endregion

    #region Conversions

    [Fact]
    public void Pose3_TupleConversion_Works()
    {
        Pose3<double> pose = (1.0, 2.0, 3.0, 10.0, 20.0, 30.0);
        (double x, double y, double z, double rx, double ry, double rz) tuple = pose;

        pose.X.Should().Be(1);
        pose.Y.Should().Be(2);
        pose.Z.Should().Be(3);
        pose.Rx.Should().Be(10);
        pose.Ry.Should().Be(20);
        pose.Rz.Should().Be(30);

        tuple.x.Should().Be(1);
        tuple.rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_Deconstruct_ToPositionAndRotation()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        var (position, rotation) = pose;

        position.X.Should().Be(1);
        rotation.Rx.Should().Be(10);
    }

    [Fact]
    public void Pose3_Deconstruct_ToComponents()
    {
        var pose = new Pose3<double>(1, 2, 3, 10, 20, 30);

        var (x, y, z, rx, ry, rz) = pose;

        x.Should().Be(1);
        y.Should().Be(2);
        z.Should().Be(3);
        rx.Should().Be(10);
        ry.Should().Be(20);
        rz.Should().Be(30);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Pose3_Parse_Works()
    {
        var pose = Pose3<double>.Parse("100, 200, 300, 45, 90, -30");

        pose.X.Should().Be(100);
        pose.Y.Should().Be(200);
        pose.Z.Should().Be(300);
        pose.Rx.Should().Be(45);
        pose.Ry.Should().Be(90);
        pose.Rz.Should().Be(-30);
    }

    [Fact]
    public void Pose3_TryParse_ReturnsTrue_ForValidInput()
    {
        var success = Pose3<double>.TryParse("1, 2, 3, 10, 20, 30", null, out var pose);

        success.Should().BeTrue();
        pose.X.Should().Be(1);
        pose.Rz.Should().Be(30);
    }

    [Fact]
    public void Pose3_TryParse_ReturnsFalse_ForInvalidInput()
    {
        var success = Pose3<double>.TryParse("1, 2, 3", null, out var pose);

        success.Should().BeFalse();
    }

    #endregion

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane
        pose.X.Should().BeApproximately(0.5, Tolerance);
        pose.Y.Should().BeApproximately(0.5, Tolerance);
        pose.Z.Should().BeApproximately(0, Tolerance);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane
        pose.X.Should().BeApproximately(0.5, Tolerance);
        pose.Y.Should().BeApproximately(0.5, Tolerance);
        pose.Z.Should().BeApproximately(0, Tolerance);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

        // Origin should be projection of h onto plane (Y=0)
        pose.X.Should().BeApproximately(0.5, Tolerance);
        pose.Y.Should().BeApproximately(0, Tolerance);
        pose.Z.Should().BeApproximately(0.5, Tolerance);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

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

        var act = () => Pose3<double>.FromSurface(a, b, c, h);

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

        var act = () => Pose3<double>.FromSurface(a, b, c, h);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

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

        var pose = Pose3<double>.FromSurface(a, b, c, h);

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

        var pose = Pose3<double>.FromSurface(a, b, c);

        pose.X.Should().Be(10);
        pose.Y.Should().Be(20);
        pose.Z.Should().Be(30);
    }

    [Fact]
    public void FromSurface_ThreePoints_XAxisAlongAB()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(5, 0, 0);
        var c = new Point3<double>(0, 5, 0);

        var pose = Pose3<double>.FromSurface(a, b, c);

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

        var pose = Pose3<double>.FromSurface(a, b, c);

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

        var pose = Pose3<double>.FromSurface(a, b, c);

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

        var pose = Pose3<double>.FromSurface(a, b, c);

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

        var act = () => Pose3<double>.FromSurface(a, b, c);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*collinear*");
    }

    [Fact]
    public void FromSurface_ThreePoints_PointsOnSurface_HaveZeroLocalZ()
    {
        var a = new Point3<double>(10, 20, 5);
        var b = new Point3<double>(15, 20, 5);
        var c = new Point3<double>(10, 25, 5);

        var pose = Pose3<double>.FromSurface(a, b, c);
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

        var pose = Pose3<double>.FromSurface(points);

        pose.X.Should().BeApproximately(5, 0.0001);
        pose.Y.Should().BeApproximately(5, 0.0001);
        pose.Z.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void FromSurface_NPoints_HorizontalPlane_ZPointsUp()
    {
        // Counter-clockwise in XY plane → right-hand rule → Z points up
        var points = new Point3<double>[]
        {
            new(0, 0, 0), new(10, 0, 0), new(10, 10, 0), new(0, 10, 0)
        };

        var pose = Pose3<double>.FromSurface(points);
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

        var pose = Pose3<double>.FromSurface(points);
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

        var pose = Pose3<double>.FromSurface(points);
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

        var pose = Pose3<double>.FromSurface(points);
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

        var pose = Pose3<double>.FromSurface(points);
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

        var pose3 = Pose3<double>.FromSurface(a, b, c);
        var poseN = Pose3<double>.FromSurface([a, b, c, d]);

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

        var act = () => Pose3<double>.FromSurface(points);

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

        var act = () => Pose3<double>.FromSurface(points);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromSurface_NPoints_DelegatesToThreePointFor3()
    {
        var a = new Point3<double>(10, 20, 30);
        var b = new Point3<double>(11, 20, 30);
        var c = new Point3<double>(10, 21, 30);

        var pose3 = Pose3<double>.FromSurface(a, b, c);
        var poseN = Pose3<double>.FromSurface([a, b, c]);

        // Exact same result — delegates to 3-point overload
        pose3.X.Should().Be(poseN.X);
        pose3.Y.Should().Be(poseN.Y);
        pose3.Z.Should().Be(poseN.Z);
    }

    #endregion
}
