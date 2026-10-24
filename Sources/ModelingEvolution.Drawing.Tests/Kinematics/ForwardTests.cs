using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests.Kinematics;

/// <summary>TS-129-04: <c>Forward</c> against closed-form answers.</summary>
public class ForwardTests
{
    [Fact]
    public void Planar2R_TipMatchesClosedFormAt1000RandomQ()
    {
        var chain = TestChains.Planar2R();
        var tip = chain.FrameIndex("tip");
        var rng = new Random(129);
        Span<double> q = stackalloc double[chain.Dof];
        var links = new Frame3<double>[chain.Links.Length];

        for (int i = 0; i < 1000; i++)
        {
            TestChains.RandomQ(rng, chain, q);
            chain.Forward(q, links);
            var p = chain.FramePose(links, tip).Position;

            p.X.Should().BeApproximately(0.4 * Math.Cos(q[0]) + 0.3 * Math.Cos(q[0] + q[1]), 1e-12);
            p.Y.Should().BeApproximately(0.4 * Math.Sin(q[0]) + 0.3 * Math.Sin(q[0] + q[1]), 1e-12);
            p.Z.Should().BeApproximately(0, 1e-12);
        }
    }

    [Fact]
    public void Slider1P_TipMatchesClosedFormAt1000RandomQ()
    {
        var chain = TestChains.Slider1P();
        var tip = chain.FrameIndex("tip");
        var rng = new Random(129);
        Span<double> q = stackalloc double[chain.Dof];
        var links = new Frame3<double>[chain.Links.Length];

        for (int i = 0; i < 1000; i++)
        {
            TestChains.RandomQ(rng, chain, q);
            chain.Forward(q, links);
            var p = chain.FramePose(links, tip).Position;

            p.X.Should().BeApproximately(q[0], 1e-12);
            p.Y.Should().BeApproximately(0, 1e-12);
            p.Z.Should().BeApproximately(0, 1e-12);
        }
    }

    [Fact]
    public void Forward_AtZero_ComposesTheOrigins()
    {
        var chain = TestChains.Arm6();
        var links = new Frame3<double>[chain.Links.Length];

        chain.Forward(new double[chain.Dof], links);

        var expected = Frame3<double>.Identity;
        for (int c = 1; c < links.Length; c++)
        {
            expected = expected * chain.Joints[c - 1].Origin;
            var (dp, da) = Frame3<double>.Distance(links[c], expected);
            dp.Should().BeLessThan(1e-12);
            da.Should().BeLessThan(1e-9);
        }
    }

    [Fact]
    public void Forward_LinksOfWrongLength_Throws()
    {
        var chain = TestChains.Planar2R();
        var q = new double[chain.Dof];

        var tooShort = () => chain.Forward(q, new Frame3<double>[chain.Links.Length - 1]);
        var tooLong = () => chain.Forward(q, new Frame3<double>[chain.Links.Length + 1]);

        tooShort.Should().Throw<ArgumentException>().WithParameterName("links");
        tooLong.Should().Throw<ArgumentException>().WithParameterName("links");
    }

    [Fact]
    public void Forward_QOfWrongLength_Throws()
    {
        var chain = TestChains.Planar2R();

        var act = () => chain.Forward(new double[chain.Dof + 1], new Frame3<double>[chain.Links.Length]);

        act.Should().Throw<ArgumentException>().WithParameterName("q");
    }

    [Fact]
    public void FramePose_LinksOfWrongLength_Throws()
    {
        var chain = TestChains.Planar2R();

        var act = () => chain.FramePose(new Frame3<double>[1], 0);

        act.Should().Throw<ArgumentException>().WithParameterName("links");
    }

    [Fact]
    public void ForwardAndJacobian_DoNotAllocate()
    {
        var chain = TestChains.RailArm6();
        var flange = chain.FrameIndex("flange");
        var q = new double[chain.Dof];
        var links = new Frame3<double>[chain.Links.Length];
        var j = new double[6 * chain.Dof];
        var rng = new Random(129);
        TestChains.RandomQ(rng, chain, q);
        chain.Forward(q, links);
        chain.Jacobian(links, flange, j);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            chain.Forward(q, links);
            chain.FramePose(links, flange);
            chain.Jacobian(links, flange, j);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
    }
}
