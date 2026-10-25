using FluentAssertions;
using ModelingEvolution.Drawing.Units;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// Epic 128 §1.2 additive primitives, rebased onto the epic-129 quaternion <see cref="Frame3{T}"/> model:
/// <see cref="Frame3{T}.FromAxes"/> (basis-from-directions for tool placement), <see cref="Arc{T}"/>, and the
/// mm/s→mm/min <see cref="Speed{T}"/> bridge. These are the shared geometry the ModelingEvolution.ToolPath
/// module builds on. (The old Rotation3.FromAxes / Pose3.FromFrame were dropped: the basis→rotation now lives
/// on <see cref="Quaternion{T}.FromAxes"/>, which this builds on.)
/// </summary>
public class Epic128PrimitivesTests
{
    private const double Eps = 1e-9;

    private static void BeClose(Vector3<double> actual, Vector3<double> expected)
    {
        actual.X.Should().BeApproximately(expected.X, Eps);
        actual.Y.Should().BeApproximately(expected.Y, Eps);
        actual.Z.Should().BeApproximately(expected.Z, Eps);
    }

    [Fact]
    public void FromAxes_aligned_hints_give_an_axis_aligned_frame()
    {
        // forward = +X, approach = +Z ⇒ local X=+X, Z=+Z, Y = Z×X = +Y (identity orientation).
        var f = Frame3<double>.FromAxes(new Point3<double>(1, 2, 3), Vector3<double>.EX, Vector3<double>.EZ);

        f.Position.X.Should().BeApproximately(1, Eps);
        f.Position.Y.Should().BeApproximately(2, Eps);
        f.Position.Z.Should().BeApproximately(3, Eps);
        BeClose(f.TransformVector(Vector3<double>.EX), Vector3<double>.EX);
        BeClose(f.TransformVector(Vector3<double>.EY), Vector3<double>.EY);
        BeClose(f.TransformVector(Vector3<double>.EZ), Vector3<double>.EZ);
    }

    [Fact]
    public void FromAxes_reorthogonalizes_forward_against_approach()
    {
        // forward is not orthogonal to approach — its approach-component must be projected out, so local X
        // comes back as the clean +X while local Z stays +Z.
        var f = Frame3<double>.FromAxes(Point3<double>.Zero, new Vector3<double>(1, 0, 0.5), Vector3<double>.EZ);

        BeClose(f.TransformVector(Vector3<double>.EZ), Vector3<double>.EZ);   // local Z == approach
        BeClose(f.TransformVector(Vector3<double>.EX), Vector3<double>.EX);   // local X == forward, de-Z'd
    }

    [Fact]
    public void FromAxes_90deg_about_Z_maps_the_basis()
    {
        // forward +Y, approach +Z ⇒ local X=+Y, Z=+Z, Y = Z×X = -X (a +90° turn about Z).
        var f = Frame3<double>.FromAxes(Point3<double>.Zero, Vector3<double>.EY, Vector3<double>.EZ);

        BeClose(f.TransformVector(Vector3<double>.EX), Vector3<double>.EY);
        BeClose(f.TransformVector(Vector3<double>.EY), new Vector3<double>(-1, 0, 0));
        BeClose(f.TransformVector(Vector3<double>.EZ), Vector3<double>.EZ);
    }

    [Fact]
    public void FromAxes_throws_when_forward_parallel_to_approach()
    {
        var act = () => Frame3<double>.FromAxes(Point3<double>.Zero, Vector3<double>.EZ, Vector3<double>.EZ);
        act.Should().Throw<System.ArgumentException>();
    }

    [Fact]
    public void FromAxes_throws_when_approach_is_degenerate()
    {
        var act = () => Frame3<double>.FromAxes(Point3<double>.Zero, Vector3<double>.EX, Vector3<double>.Zero);
        act.Should().Throw<System.ArgumentException>();
    }

    [Fact]
    public void Arc_points_and_length_for_quarter_circle()
    {
        var arc = new Arc<double>(new Point<double>(0, 0), 1.0, 0.0, 90.0);   // CCW quarter from +X
        arc.StartPoint.X.Should().BeApproximately(1, Eps);
        arc.StartPoint.Y.Should().BeApproximately(0, Eps);
        arc.EndPoint.X.Should().BeApproximately(0, Eps);
        arc.EndPoint.Y.Should().BeApproximately(1, Eps);
        arc.Mid.X.Should().BeApproximately(System.Math.Sqrt(0.5), 1e-9);
        arc.Mid.Y.Should().BeApproximately(System.Math.Sqrt(0.5), 1e-9);
        arc.Length.Should().BeApproximately(System.Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Arc_tangent_follows_sweep_sign()
    {
        var ccw = new Arc<double>(new Point<double>(0, 0), 1.0, 0.0, 90.0);
        var cw = new Arc<double>(new Point<double>(0, 0), 1.0, 0.0, -90.0);
        var tCcw = ccw.TangentAt(0.0);
        var tCw = cw.TangentAt(0.0);
        tCcw.X.Should().BeApproximately(0, Eps);
        tCcw.Y.Should().BeApproximately(1, Eps);     // CCW at +X travels +Y
        tCw.X.Should().BeApproximately(0, Eps);
        tCw.Y.Should().BeApproximately(-1, Eps);     // CW reverses
    }

    [Fact]
    public void Speed_bridge_converts_mm_per_second_to_mm_per_minute()
    {
        var mmPerS = new Speed<double, MillimetrePerSecond<double>>(10);  // 10 mm/s
        var legacy = mmPerS.ToLegacyMmPerMin();                            // expect 600 mm/min
        ((double)legacy).Should().BeApproximately(600, 1e-9);
    }
}
