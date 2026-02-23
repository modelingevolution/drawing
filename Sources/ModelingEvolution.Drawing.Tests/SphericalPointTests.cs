using FluentAssertions;
using SphericalPointF = ModelingEvolution.Drawing.SphericalPoint<float>;
using CylindricalPointF = ModelingEvolution.Drawing.CylindricalPoint<float>;
using Point3F = ModelingEvolution.Drawing.Point3<float>;

namespace ModelingEvolution.Drawing.Tests;

public class SphericalPointTests
{
    [Fact]
    public void ConstructorSetsProperties()
    {
        var azimuth = Degree<float>.Create(45);
        var inclination = Degree<float>.Create(60);
        var sp = new SphericalPointF(10f, azimuth, inclination);

        sp.Radius.Should().Be(10f);
        sp.Azimuth.Should().Be(azimuth);
        sp.Inclination.Should().Be(inclination);
    }

    [Fact]
    public void Zero_HasAllComponentsZero()
    {
        var z = SphericalPointF.Zero;
        z.Radius.Should().Be(0f);
        ((float)z.Azimuth).Should().Be(0f);
        ((float)z.Inclination).Should().Be(0f);
    }

    #region Static Factories

    [Fact]
    public void OnEquator_HasInclination90()
    {
        var sp = SphericalPointF.OnEquator(5f, Degree<float>.Create(45));

        ((float)sp.Inclination).Should().BeApproximately(90f, 1e-5f);
        ((float)sp.Azimuth).Should().BeApproximately(45f, 1e-5f);
        sp.Radius.Should().Be(5f);

        // Should be on the XY plane
        Point3F p = sp;
        p.Z.Should().BeApproximately(0f, 1e-4f);
    }

    [Fact]
    public void FromElevation_ZeroIsEquator()
    {
        var sp = SphericalPointF.FromElevation(5f, Degree<float>.Create(0), Degree<float>.Create(0));

        ((float)sp.Inclination).Should().BeApproximately(90f, 1e-5f);

        Point3F p = sp;
        p.Z.Should().BeApproximately(0f, 1e-4f);
    }

    [Fact]
    public void FromElevation_90IsNorthPole()
    {
        var sp = SphericalPointF.FromElevation(5f, Degree<float>.Create(0), Degree<float>.Create(90));

        ((float)sp.Inclination).Should().BeApproximately(0f, 1e-5f);

        Point3F p = sp;
        p.X.Should().BeApproximately(0f, 1e-4f);
        p.Y.Should().BeApproximately(0f, 1e-4f);
        p.Z.Should().BeApproximately(5f, 1e-4f);
    }

    [Fact]
    public void FromElevation_Negative90IsSouthPole()
    {
        var sp = SphericalPointF.FromElevation(5f, Degree<float>.Create(0), Degree<float>.Create(-90));

        ((float)sp.Inclination).Should().BeApproximately(180f, 1e-5f);

        Point3F p = sp;
        p.Z.Should().BeApproximately(-5f, 1e-4f);
    }

    [Fact]
    public void FromElevation_45IsAboveEquator()
    {
        var sp = SphericalPointF.FromElevation(10f, Degree<float>.Create(0), Degree<float>.Create(45));

        ((float)sp.Inclination).Should().BeApproximately(45f, 1e-5f);

        Point3F p = sp;
        p.Z.Should().BeApproximately(10f * MathF.Cos(MathF.PI / 4), 1e-4f);
    }

    [Fact]
    public void NorthPole_PointsAlongPlusZ()
    {
        Point3F p = SphericalPointF.NorthPole(7f);

        p.X.Should().BeApproximately(0f, 1e-5f);
        p.Y.Should().BeApproximately(0f, 1e-5f);
        p.Z.Should().BeApproximately(7f, 1e-5f);
    }

    [Fact]
    public void SouthPole_PointsAlongMinusZ()
    {
        Point3F p = SphericalPointF.SouthPole(7f);

        p.X.Should().BeApproximately(0f, 1e-5f);
        p.Y.Should().BeApproximately(0f, 1e-5f);
        p.Z.Should().BeApproximately(-7f, 1e-5f);
    }

    [Fact]
    public void OrbitWithOnEquator_IsClean()
    {
        var center = new Point3F(10f, 50f, 0f);
        var points = new List<Point3F>();
        for (int az = 0; az < 360; az += 30)
            points.Add(center + SphericalPointF.OnEquator(5f, Degree<float>.Create(az)));

        points.Should().HaveCount(12);
        foreach (var p in points)
        {
            p.Z.Should().BeApproximately(0f, 1e-4f); // all on XY plane
            Point3F.Distance(p, center).Should().BeApproximately(5f, 1e-4f);
        }
    }

    #endregion

    [Fact]
    public void ToPoint3_NorthPole()
    {
        // Inclination 0° => point on +Z axis
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(0));
        Point3F p = sp;

        p.X.Should().BeApproximately(0f, 1e-5f);
        p.Y.Should().BeApproximately(0f, 1e-5f);
        p.Z.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void ToPoint3_OnXAxis()
    {
        // Inclination 90°, Azimuth 0° => point on +X axis
        var sp = new SphericalPointF(3f, Degree<float>.Create(0), Degree<float>.Create(90));
        Point3F p = sp;

        p.X.Should().BeApproximately(3f, 1e-5f);
        p.Y.Should().BeApproximately(0f, 1e-5f);
        p.Z.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void ToPoint3_OnYAxis()
    {
        // Inclination 90°, Azimuth 90° => point on +Y axis
        var sp = new SphericalPointF(4f, Degree<float>.Create(90), Degree<float>.Create(90));
        Point3F p = sp;

        p.X.Should().BeApproximately(0f, 1e-5f);
        p.Y.Should().BeApproximately(4f, 1e-5f);
        p.Z.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void ToPoint3_SouthPole()
    {
        // Inclination 180° => point on -Z axis
        var sp = new SphericalPointF(7f, Degree<float>.Create(0), Degree<float>.Create(180));
        Point3F p = sp;

        p.X.Should().BeApproximately(0f, 1e-5f);
        p.Y.Should().BeApproximately(0f, 1e-5f);
        p.Z.Should().BeApproximately(-7f, 1e-5f);
    }

    [Fact]
    public void RoundTrip_Point3ToSphericalAndBack()
    {
        var original = new Point3F(3f, 4f, 5f);
        SphericalPointF spherical = original;
        Point3F restored = spherical;

        restored.X.Should().BeApproximately(original.X, 1e-5f);
        restored.Y.Should().BeApproximately(original.Y, 1e-5f);
        restored.Z.Should().BeApproximately(original.Z, 1e-5f);
    }

    [Fact]
    public void RoundTrip_PreservesRadius()
    {
        var original = new Point3F(1f, 2f, 3f);
        SphericalPointF spherical = original;
        var expectedRadius = MathF.Sqrt(1f + 4f + 9f);

        spherical.Radius.Should().BeApproximately(expectedRadius, 1e-5f);
    }

    [Fact]
    public void FromOrigin_ReturnsZero()
    {
        SphericalPointF spherical = Point3F.Zero;
        spherical.Radius.Should().Be(0f);
    }

    #region Normalized & WithRadius

    [Fact]
    public void Normalized_ReturnsUnitRadius()
    {
        var sp = new SphericalPointF(42f, Degree<float>.Create(30), Degree<float>.Create(60));
        var unit = sp.Normalized();

        unit.Radius.Should().Be(1f);
        unit.Azimuth.Should().Be(sp.Azimuth);
        unit.Inclination.Should().Be(sp.Inclination);
    }

    [Fact]
    public void WithRadius_PreservesAngles()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var scaled = sp.WithRadius(100f);

        scaled.Radius.Should().Be(100f);
        scaled.Azimuth.Should().Be(sp.Azimuth);
        scaled.Inclination.Should().Be(sp.Inclination);
    }

    #endregion

    #region Deconstruct

    [Fact]
    public void Deconstruct_ReturnsComponents()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var (r, az, inc) = sp;

        r.Should().Be(5f);
        az.Should().Be(sp.Azimuth);
        inc.Should().Be(sp.Inclination);
    }

    #endregion

    #region Rotation

    [Fact]
    public void RotateAzimuth_AddsToAngle()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var rotated = sp.RotateAzimuth(Degree<float>.Create(15));

        ((float)rotated.Azimuth).Should().BeApproximately(45f, 1e-5f);
        rotated.Radius.Should().Be(5f);
        rotated.Inclination.Should().Be(sp.Inclination);
    }

    [Fact]
    public void RotateAzimuth_MatchesCartesianRotationZ()
    {
        // On the equator (inclination=90°), rotating azimuth by 45° should match RotationZ(45°)
        var sp = new SphericalPointF(10f, Degree<float>.Create(0), Degree<float>.Create(90));
        var rotated = sp.RotateAzimuth(Degree<float>.Create(45));

        var m = Matrix3x3<float>.RotationZ(Degree<float>.Create(45));
        Point3<float> cartesianRotated = m.Transform((Point3<float>)sp);
        Point3<float> sphericalResult = rotated;

        sphericalResult.X.Should().BeApproximately(cartesianRotated.X, 1e-4f);
        sphericalResult.Y.Should().BeApproximately(cartesianRotated.Y, 1e-4f);
        sphericalResult.Z.Should().BeApproximately(cartesianRotated.Z, 1e-4f);
    }

    [Fact]
    public void RotateInclination_AddsToAngle()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(60));
        var rotated = sp.RotateInclination(Degree<float>.Create(30));

        ((float)rotated.Inclination).Should().BeApproximately(90f, 1e-5f);
        rotated.Radius.Should().Be(5f);
        rotated.Azimuth.Should().Be(sp.Azimuth);
    }

    [Fact]
    public void RotateInclination_ClampsToZero()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(10));
        var rotated = sp.RotateInclination(Degree<float>.Create(-50));

        ((float)rotated.Inclination).Should().Be(0f);
    }

    [Fact]
    public void RotateInclination_ClampsTo180()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(170));
        var rotated = sp.RotateInclination(Degree<float>.Create(50));

        ((float)rotated.Inclination).Should().BeApproximately(180f, 1e-5f);
    }

    [Fact]
    public void Rotate_BothAngles()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var rotated = sp.Rotate(Degree<float>.Create(15), Degree<float>.Create(10));

        ((float)rotated.Azimuth).Should().BeApproximately(45f, 1e-5f);
        ((float)rotated.Inclination).Should().BeApproximately(70f, 1e-5f);
        rotated.Radius.Should().Be(5f);
    }

    [Fact]
    public void Rotate_BothAngles_ClampsInclination()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(5));
        var rotated = sp.Rotate(Degree<float>.Create(45), Degree<float>.Create(-100));

        ((float)rotated.Inclination).Should().Be(0f);
        ((float)rotated.Azimuth).Should().BeApproximately(45f, 1e-5f);
    }

    #endregion

    #region Operators & Point3 integration

    [Fact]
    public void Point3PlusSphericalPoint_OffsetsCenter()
    {
        var center = new Point3F(10f, 50f, 0f);
        var offset = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(90));
        // azimuth=0, inclination=90° => offset is along +X axis, r=5

        var result = center + offset;

        result.X.Should().BeApproximately(15f, 1e-4f);
        result.Y.Should().BeApproximately(50f, 1e-4f);
        result.Z.Should().BeApproximately(0f, 1e-4f);
    }

    [Fact]
    public void Point3MinusSphericalPoint_OffsetsCenter()
    {
        var center = new Point3F(10f, 50f, 0f);
        var offset = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(90));

        var result = center - offset;

        result.X.Should().BeApproximately(5f, 1e-4f);
        result.Y.Should().BeApproximately(50f, 1e-4f);
        result.Z.Should().BeApproximately(0f, 1e-4f);
    }

    [Fact]
    public void OrbitAroundCenter_Produces12Points()
    {
        var center = new Point3F(10f, 50f, 0f);
        var r = 5f;
        var step = Degree<float>.Create(30);
        var inclination = Degree<float>.Create(90);

        var points = new List<Point3F>();
        for (int i = 0; i < 12; i++)
        {
            var offset = new SphericalPointF(r, step * i, inclination);
            points.Add(center + offset);
        }

        points.Should().HaveCount(12);

        // First point: azimuth=0° => center + (5,0,0)
        points[0].X.Should().BeApproximately(15f, 1e-4f);
        points[0].Y.Should().BeApproximately(50f, 1e-4f);

        // All points should be at distance r from center
        foreach (var p in points)
            Point3F.Distance(p, center).Should().BeApproximately(r, 1e-4f);
    }

    [Fact]
    public void ExplicitCastToVector3()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(90));
        var v = (Vector3<float>)sp;

        v.X.Should().BeApproximately(5f, 1e-5f);
        v.Y.Should().BeApproximately(0f, 1e-5f);
        v.Z.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void MultiplyByScalar_ScalesRadius()
    {
        var sp = new SphericalPointF(3f, Degree<float>.Create(30), Degree<float>.Create(60));

        var result = sp * 4f;
        result.Radius.Should().Be(12f);
        result.Azimuth.Should().Be(sp.Azimuth);

        var result2 = 4f * sp;
        result2.Should().Be(result);
    }

    [Fact]
    public void DivideByScalar_ScalesRadius()
    {
        var sp = new SphericalPointF(12f, Degree<float>.Create(30), Degree<float>.Create(60));
        var result = sp / 4f;

        result.Radius.Should().Be(3f);
        result.Azimuth.Should().Be(sp.Azimuth);
    }

    #endregion

    #region AngularDistance

    [Fact]
    public void AngularDistance_SameDirection_IsZero()
    {
        var a = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var b = new SphericalPointF(10f, Degree<float>.Create(30), Degree<float>.Create(60));

        var angle = SphericalPointF.AngularDistance(a, b);
        ((float)angle).Should().BeApproximately(0f, 1e-4f);
    }

    [Fact]
    public void AngularDistance_OppositeDirections_Is180()
    {
        // North pole vs south pole
        var north = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(0));
        var south = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(180));

        var angle = SphericalPointF.AngularDistance(north, south);
        ((float)angle).Should().BeApproximately(180f, 1e-3f);
    }

    [Fact]
    public void AngularDistance_Perpendicular_Is90()
    {
        // +Z vs +X
        var zAxis = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(0));
        var xAxis = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(90));

        var angle = SphericalPointF.AngularDistance(zAxis, xAxis);
        ((float)angle).Should().BeApproximately(90f, 1e-3f);
    }

    [Fact]
    public void AngularDistance_IgnoresRadius()
    {
        var a = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(45));
        var b = new SphericalPointF(1000f, Degree<float>.Create(0), Degree<float>.Create(45));

        var angle = SphericalPointF.AngularDistance(a, b);
        ((float)angle).Should().BeApproximately(0f, 1e-4f);
    }

    #endregion

    #region Distance

    [Fact]
    public void Distance_SamePoint_IsZero()
    {
        var sp = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        SphericalPointF.Distance(sp, sp).Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void Distance_MatchesCartesian()
    {
        var a = new Point3F(1f, 2f, 3f);
        var b = new Point3F(4f, 5f, 6f);
        SphericalPointF sa = a;
        SphericalPointF sb = b;

        SphericalPointF.Distance(sa, sb).Should().BeApproximately(Point3F.Distance(a, b), 1e-4f);
    }

    #endregion

    #region Slerp & Lerp

    [Fact]
    public void Slerp_AtZero_ReturnsStart()
    {
        var a = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(0));
        var b = new SphericalPointF(10f, Degree<float>.Create(90), Degree<float>.Create(90));

        Point3F result = SphericalPointF.Slerp(a, b, 0f);
        Point3F expected = a;

        result.X.Should().BeApproximately(expected.X, 1e-5f);
        result.Y.Should().BeApproximately(expected.Y, 1e-5f);
        result.Z.Should().BeApproximately(expected.Z, 1e-5f);
    }

    [Fact]
    public void Slerp_AtOne_ReturnsEnd()
    {
        var a = new SphericalPointF(5f, Degree<float>.Create(0), Degree<float>.Create(0));
        var b = new SphericalPointF(10f, Degree<float>.Create(90), Degree<float>.Create(90));

        Point3F result = SphericalPointF.Slerp(a, b, 1f);
        Point3F expected = b;

        result.X.Should().BeApproximately(expected.X, 1e-4f);
        result.Y.Should().BeApproximately(expected.Y, 1e-4f);
        result.Z.Should().BeApproximately(expected.Z, 1e-4f);
    }

    [Fact]
    public void Slerp_AtHalf_InterpolatesRadius()
    {
        var a = new SphericalPointF(4f, Degree<float>.Create(0), Degree<float>.Create(90));
        var b = new SphericalPointF(8f, Degree<float>.Create(0), Degree<float>.Create(90));

        var result = SphericalPointF.Slerp(a, b, 0.5f);
        result.Radius.Should().BeApproximately(6f, 1e-4f);
    }

    [Fact]
    public void Slerp_MidpointStaysOnSphere()
    {
        // Two points on a unit sphere, 90° apart
        var a = new SphericalPointF(1f, Degree<float>.Create(0), Degree<float>.Create(90));
        var b = new SphericalPointF(1f, Degree<float>.Create(90), Degree<float>.Create(90));

        var mid = SphericalPointF.Slerp(a, b, 0.5f);
        mid.Radius.Should().BeApproximately(1f, 1e-4f);
    }

    [Fact]
    public void Lerp_AtHalf_MatchesCartesianMidpoint()
    {
        var a = new Point3F(1f, 0f, 0f);
        var b = new Point3F(0f, 1f, 0f);

        SphericalPointF sa = a;
        SphericalPointF sb = b;

        Point3F result = SphericalPointF.Lerp(sa, sb, 0.5f);
        var expected = Point3F.Lerp(a, b, 0.5f);

        result.X.Should().BeApproximately(expected.X, 1e-5f);
        result.Y.Should().BeApproximately(expected.Y, 1e-5f);
        result.Z.Should().BeApproximately(expected.Z, 1e-5f);
    }

    #endregion

    #region CylindricalPoint conversions

    [Fact]
    public void ToCylindricalPoint()
    {
        // r=10, azimuth=45°, inclination=60°
        var sp = new SphericalPointF(10f, Degree<float>.Create(45), Degree<float>.Create(60));
        CylindricalPointF cyl = sp;

        cyl.RadialDistance.Should().BeApproximately(10f * MathF.Sin(MathF.PI / 3), 1e-4f);
        cyl.Z.Should().BeApproximately(5f, 1e-4f);
        ((float)cyl.Angle).Should().BeApproximately(MathF.PI / 4, 1e-4f);
    }

    [Fact]
    public void FromCylindricalPoint_RoundTrip()
    {
        var sp = new SphericalPointF(10f, Degree<float>.Create(45), Degree<float>.Create(60));
        CylindricalPointF cyl = sp;
        SphericalPointF restored = cyl;

        restored.Radius.Should().BeApproximately(sp.Radius, 1e-4f);
        ((float)restored.Azimuth).Should().BeApproximately((float)sp.Azimuth, 1e-3f);
        ((float)restored.Inclination).Should().BeApproximately((float)sp.Inclination, 1e-3f);
    }

    [Fact]
    public void FromCylindricalPoint_OnXYPlane()
    {
        // Cylindrical: r=5, angle=0, z=0 => Spherical: r=5, azimuth=0°, inclination=90°
        var cyl = new CylindricalPointF(Radian<float>.FromRadian(0f), 5f, 0f);
        SphericalPointF sp = cyl;

        sp.Radius.Should().BeApproximately(5f, 1e-5f);
        ((float)sp.Azimuth).Should().BeApproximately(0f, 1e-3f);
        ((float)sp.Inclination).Should().BeApproximately(90f, 1e-3f);
    }

    #endregion

    #region ToString & Equality

    [Fact]
    public void ToStringFormat_ContainsDegreeSymbol()
    {
        var sp = new SphericalPointF(1f, Degree<float>.Create(45), Degree<float>.Create(90));
        var str = sp.ToString();
        str.Should().Contain("45");
        str.Should().Contain("90");
        str.Should().Contain("°");
    }

    [Fact]
    public void RecordEquality()
    {
        var a = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var b = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(60));
        var c = new SphericalPointF(5f, Degree<float>.Create(30), Degree<float>.Create(90));

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    #endregion
}
