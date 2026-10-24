using System.Collections.Immutable;
using ModelingEvolution.Drawing.Kinematics;

namespace ModelingEvolution.Drawing.Tests.Kinematics;

/// <summary>
/// Test data for the kinematic chain scenarios (epic-129 TS-129-03..05). Metres and radians.
/// <c>test.arm6</c> is a synthetic 6R arm with non-trivial joint origins and axes; Matrix.Fixtures copies this
/// definition later, so a change here is a change of the fixture.
/// </summary>
internal static class TestChains
{
    public static Frame3<double> Origin(double x, double y, double z, double roll, double pitch, double yaw) =>
        new(new Point3<double>(x, y, z), Quaternion<double>.FromRpy(roll, pitch, yaw));

    public static Vector3<double> Unit(double x, double y, double z) => new Vector3<double>(x, y, z).Normalize();

    private static Joint<double> R(string name, int child, Frame3<double> origin, Vector3<double> axis, double min, double max, double? maxAcc = null) =>
        new(name, JointType.Revolute, child, origin, axis, min, max, 3.14, maxAcc);

    /// <summary>The six revolute joints of test.arm6, child links 1..6 shifted by <paramref name="linkOffset"/>.</summary>
    private static IEnumerable<Joint<double>> Arm6Joints(int linkOffset, Frame3<double> j1Origin) =>
    [
        R("j1", 1 + linkOffset, j1Origin,                                             Unit(0, 0, 1),     -3.05, 3.05, 10),
        R("j2", 2 + linkOffset, Origin(0.050, -0.030, 0.120, Math.PI / 2, 0, 0.1),    Unit(0, 0, 1),     -2.60, 2.60, 10),
        R("j3", 3 + linkOffset, Origin(0.420, 0.010, 0.000, 0, 0, 0.2),               Unit(0, 0, -1),    -2.80, 2.80, 10),
        R("j4", 4 + linkOffset, Origin(0.380, -0.020, 0.090, -Math.PI / 2, 0.05, 0), Unit(0, 0.6, 0.8), -3.05, 3.05),
        R("j5", 5 + linkOffset, Origin(0.010, 0.110, 0.030, Math.PI / 2, 0, 0),      Unit(1, 0, 0),     -2.10, 2.10),
        R("j6", 6 + linkOffset, Origin(0.000, 0.020, 0.095, 0, -Math.PI / 2, 0.3),    Unit(0, 0, 1),     -6.28, 6.28),
    ];

    /// <summary>test.arm6: links base, link1..link6; frames base (on base) and flange (on link6).</summary>
    public static KinematicChain<double> Arm6()
    {
        var links = ImmutableArray.Create(
            new Link("base", -1), new Link("link1", 0), new Link("link2", 1), new Link("link3", 2),
            new Link("link4", 3), new Link("link5", 4), new Link("link6", 5));
        var joints = Arm6Joints(0, Origin(0, 0, 0.180, 0, 0, 0)).ToImmutableArray();
        var frames = ImmutableArray.Create(
            new ChainFrame<double>("base", 0, Frame3<double>.Identity),
            new ChainFrame<double>("flange", 6, Origin(0, 0, 0.045, Math.PI, 0, 0)));
        return new KinematicChain<double>(links, joints, frames);
    }

    /// <summary>
    /// A 1P+6R chain: a rail (prismatic along +X, 0..3 m) whose carriage carries test.arm6
    /// (arm base 0.10 m off the carriage in Y, 0.05 m up).
    /// </summary>
    public static KinematicChain<double> RailArm6()
    {
        var links = ImmutableArray.Create(
            new Link("base", -1), new Link("carriage", 0), new Link("link1", 1), new Link("link2", 2), new Link("link3", 3),
            new Link("link4", 4), new Link("link5", 5), new Link("link6", 6));
        var rail = new Joint<double>("rail", JointType.Prismatic, 1, Origin(0, 0, 0.25, 0, 0, 0), Unit(1, 0, 0), 0, 3, 1.0, null);
        var joints = ImmutableArray.Create(rail).AddRange(Arm6Joints(1, Origin(0, 0.10, 0.230, 0, 0, 0)));
        var frames = ImmutableArray.Create(
            new ChainFrame<double>("base", 0, Frame3<double>.Identity),
            new ChainFrame<double>("flange", 7, Origin(0, 0, 0.045, Math.PI, 0, 0)));
        return new KinematicChain<double>(links, joints, frames);
    }

    /// <summary>A planar 2R chain: link lengths 0.4 m and 0.3 m, axes +Z; frame <c>tip</c> at the end of link 2.</summary>
    public static KinematicChain<double> Planar2R()
    {
        var links = ImmutableArray.Create(new Link("base", -1), new Link("upper", 0), new Link("lower", 1));
        var joints = ImmutableArray.Create(
            new Joint<double>("shoulder", JointType.Revolute, 1, Frame3<double>.Identity, Vector3<double>.EZ, -Math.PI, Math.PI, 2, null),
            new Joint<double>("elbow", JointType.Revolute, 2, Origin(0.4, 0, 0, 0, 0, 0), Vector3<double>.EZ, -Math.PI, Math.PI, 2, null));
        var frames = ImmutableArray.Create(new ChainFrame<double>("tip", 2, Origin(0.3, 0, 0, 0, 0, 0)));
        return new KinematicChain<double>(links, joints, frames);
    }

    /// <summary>A 1P chain: a slider along +X, range 0..3 m; frame <c>tip</c> on the slider.</summary>
    public static KinematicChain<double> Slider1P()
    {
        var links = ImmutableArray.Create(new Link("base", -1), new Link("slider", 0));
        var joints = ImmutableArray.Create(
            new Joint<double>("slide", JointType.Prismatic, 1, Frame3<double>.Identity, Vector3<double>.EX, 0, 3, 1, 2));
        var frames = ImmutableArray.Create(new ChainFrame<double>("tip", 1, Frame3<double>.Identity));
        return new KinematicChain<double>(links, joints, frames);
    }

    /// <summary>Fills <paramref name="q"/> uniformly within each joint's limits.</summary>
    public static void RandomQ(Random rng, KinematicChain<double> chain, Span<double> q)
    {
        for (int i = 0; i < q.Length; i++)
        {
            var j = chain.Joints[i];
            q[i] = j.Min + rng.NextDouble() * (j.Max - j.Min);
        }
    }
}
