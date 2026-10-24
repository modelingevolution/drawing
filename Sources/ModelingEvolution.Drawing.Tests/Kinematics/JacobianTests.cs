using FluentAssertions;
using ModelingEvolution.Drawing.Kinematics;

namespace ModelingEvolution.Drawing.Tests.Kinematics;

/// <summary>TS-129-05: the geometric Jacobian against central differences of <c>Forward</c> (h = 1e-7).</summary>
public class JacobianTests
{
    private const double H = 1e-7;

    private static void AssertMatchesCentralDifferences(KinematicChain<double> chain)
    {
        var flange = chain.FrameIndex("flange");
        int dof = chain.Dof;
        var rng = new Random(129);
        var q = new double[dof];
        var links = new Frame3<double>[chain.Links.Length];
        var j = new double[6 * dof];

        for (int n = 0; n < 1000; n++)
        {
            TestChains.RandomQ(rng, chain, q);
            chain.Forward(q, links);
            chain.Jacobian(links, flange, j);

            for (int k = 0; k < dof; k++)
            {
                var qk = q[k];
                q[k] = qk + H;
                chain.Forward(q, links);
                var plus = chain.FramePose(links, flange);
                q[k] = qk - H;
                chain.Forward(q, links);
                var minus = chain.FramePose(links, flange);
                q[k] = qk;

                var v = (plus.Position - minus.Position) / (2 * H);
                // World-frame rotation between the two samples: dR = R+ · R-^T ≈ exp([ω] 2h).
                var d = plus.Rotation * minus.Rotation.Conjugate();
                var sign = d.W < 0 ? -1.0 : 1.0;
                var w = new Vector3<double>(d.X, d.Y, d.Z) * (sign / H);

                var col = j.AsSpan(6 * k, 6);
                col[0].Should().BeApproximately(v.X, 1e-6, $"vx of joint {k} at sample {n}");
                col[1].Should().BeApproximately(v.Y, 1e-6, $"vy of joint {k} at sample {n}");
                col[2].Should().BeApproximately(v.Z, 1e-6, $"vz of joint {k} at sample {n}");
                col[3].Should().BeApproximately(w.X, 1e-6, $"wx of joint {k} at sample {n}");
                col[4].Should().BeApproximately(w.Y, 1e-6, $"wy of joint {k} at sample {n}");
                col[5].Should().BeApproximately(w.Z, 1e-6, $"wz of joint {k} at sample {n}");
            }
        }
    }

    [Fact]
    public void Arm6_FlangeJacobianMatchesCentralDifferencesAt1000RandomQ() =>
        AssertMatchesCentralDifferences(TestChains.Arm6());

    [Fact]
    public void RailArm6_FlangeJacobianMatchesCentralDifferencesAt1000RandomQ() =>
        AssertMatchesCentralDifferences(TestChains.RailArm6());

    [Fact]
    public void RailArm6_PrismaticColumnIsAxisAndZeroRotation()
    {
        var chain = TestChains.RailArm6();
        var flange = chain.FrameIndex("flange");
        var rng = new Random(129);
        var q = new double[chain.Dof];
        var links = new Frame3<double>[chain.Links.Length];
        var j = new double[6 * chain.Dof];

        for (int n = 0; n < 100; n++)
        {
            TestChains.RandomQ(rng, chain, q);
            chain.Forward(q, links);
            chain.Jacobian(links, flange, j);

            j.AsSpan(0, 6).ToArray().Should().Equal(1.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        }
    }

    [Fact]
    public void Jacobian_OfAFrameOnTheBase_IsZero()
    {
        var chain = TestChains.Arm6();
        var links = new Frame3<double>[chain.Links.Length];
        var j = new double[6 * chain.Dof];
        Array.Fill(j, double.NaN);
        chain.Forward(new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 }, links);

        chain.Jacobian(links, chain.FrameIndex("base"), j);

        j.Should().OnlyContain(x => x == 0);
    }

    [Fact]
    public void Jacobian_OutputOfWrongLength_Throws()
    {
        var chain = TestChains.Arm6();
        var links = new Frame3<double>[chain.Links.Length];
        chain.Forward(new double[chain.Dof], links);

        var act = () => chain.Jacobian(links, 1, new double[6 * chain.Dof - 1]);

        act.Should().Throw<ArgumentException>().WithParameterName("j");
    }

    [Fact]
    public void Jacobian_LinksOfWrongLength_Throws()
    {
        var chain = TestChains.Arm6();

        var act = () => chain.Jacobian(new Frame3<double>[2], 1, new double[6 * chain.Dof]);

        act.Should().Throw<ArgumentException>().WithParameterName("links");
    }
}
