using System.Collections.Immutable;
using FluentAssertions;
using ModelingEvolution.Drawing.Kinematics;

namespace ModelingEvolution.Drawing.Tests.Kinematics;

/// <summary>TS-129-03: chain validation.</summary>
public class KinematicChainTests
{
    private static readonly ImmutableArray<Link> TwoLinks = ImmutableArray.Create(new Link("base", -1), new Link("arm", 0));

    private static Joint<double> Hinge(string name = "hinge", Vector3<double>? axis = null, double min = -1, double max = 1, double? maxAcc = null) =>
        new(name, JointType.Revolute, 1, Frame3<double>.Identity, axis ?? Vector3<double>.EZ, min, max, 2, maxAcc);

    private static ImmutableArray<ChainFrame<double>> Frames(params string[] names) =>
        names.Select(n => new ChainFrame<double>(n, 1, Frame3<double>.Identity)).ToImmutableArray();

    [Fact]
    public void Constructor_LinkParentNotBeforeItself_ThrowsNamingTheLink()
    {
        var links = ImmutableArray.Create(new Link("base", -1), new Link("arm", 0), new Link("loop", 2));
        var joints = ImmutableArray.Create(Hinge("j1"), Hinge("j2") with { Child = 2 });

        var act = () => new KinematicChain<double>(links, joints, Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'loop'*");
    }

    [Fact]
    public void Constructor_ParentAfterChild_ThrowsNamingTheLink()
    {
        var links = ImmutableArray.Create(new Link("base", -1), new Link("early", 2), new Link("late", 0));
        var joints = ImmutableArray.Create(Hinge("j1"), Hinge("j2") with { Child = 2 });

        var act = () => new KinematicChain<double>(links, joints, Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'early'*");
    }

    [Fact]
    public void Constructor_ZeroAxis_ThrowsNamingTheJoint()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge("wrist", Vector3<double>.Zero)), Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'wrist'*unit*");
    }

    [Fact]
    public void Constructor_NonUnitAxis_ThrowsNamingTheJoint()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge("wrist", new Vector3<double>(0, 0, 2))), Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'wrist'*unit*");
    }

    [Fact]
    public void Constructor_MinGreaterThanMax_ThrowsNamingTheJoint()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge("elbow", min: 1, max: -1)), Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'elbow'*Min*Max*");
    }

    [Fact]
    public void Constructor_DuplicateFrameNames_ThrowsNamingTheFrame()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge()), Frames("tcp", "flange", "tcp"));

        act.Should().Throw<ArgumentException>().WithMessage("*'tcp'*duplicate*");
    }

    [Fact]
    public void Constructor_LinkWithoutJoint_ThrowsNamingTheLink()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray<Joint<double>>.Empty, Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'arm'*no joint*");
    }

    [Fact]
    public void Constructor_TwoJointsOnOneLink_ThrowsNamingTheJoint()
    {
        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge("first"), Hinge("second")), Frames("tip"));

        act.Should().Throw<ArgumentException>().WithMessage("*'second'*");
    }

    [Fact]
    public void Constructor_FrameOnMissingLink_ThrowsNamingTheFrame()
    {
        var frames = ImmutableArray.Create(new ChainFrame<double>("ghost", 5, Frame3<double>.Identity));

        var act = () => new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge()), frames);

        act.Should().Throw<ArgumentException>().WithMessage("*'ghost'*");
    }

    [Fact]
    public void ValidChain_DofIsTheJointCount()
    {
        TestChains.Arm6().Dof.Should().Be(6);
        TestChains.RailArm6().Dof.Should().Be(7);
        TestChains.Planar2R().Dof.Should().Be(2);
        TestChains.Slider1P().Dof.Should().Be(1);
        var chain = TestChains.Arm6();
        chain.Dof.Should().Be(chain.Joints.Length);
    }

    [Fact]
    public void ValidChain_FrameIndex()
    {
        var chain = TestChains.Arm6();

        chain.FrameIndex("absent").Should().Be(-1);
        chain.FrameIndex("base").Should().Be(0);
        chain.FrameIndex("flange").Should().Be(1);
    }

    [Fact]
    public void ValidChain_MaxAccelerationMayBeNull()
    {
        var chain = new KinematicChain<double>(TwoLinks, ImmutableArray.Create(Hinge(maxAcc: null)), Frames("tip"));

        chain.Joints[0].MaxAcceleration.Should().BeNull();
        TestChains.Arm6().Joints[0].MaxAcceleration.Should().Be(10);
    }
}
