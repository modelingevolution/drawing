using FluentAssertions;
using static ModelingEvolution.Drawing.Tests.Frame3TestData;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>TS-129-01: the <see cref="Frame3{T}"/> algebra (epic-129 design § "Drawing: the kinematic chain").</summary>
public class Frame3Tests
{
    private const int Samples = 10_000;
    private const double PositionTolerance = 1e-12;
    private const double AngleTolerance = 1e-9;

    [Fact]
    public void Multiply_ByInverse_IsIdentityOver10000RandomFrames()
    {
        var rng = new Random(129);
        for (int i = 0; i < Samples; i++)
        {
            var a = RandomFrame(rng);

            var (dp, da) = Frame3<double>.Distance(a * a.Inverse(), Frame3<double>.Identity);
            var (dp2, da2) = Frame3<double>.Distance(a.Inverse() * a, Frame3<double>.Identity);

            dp.Should().BeLessThan(PositionTolerance);
            da.Should().BeLessThan(AngleTolerance);
            dp2.Should().BeLessThan(PositionTolerance);
            da2.Should().BeLessThan(AngleTolerance);
        }
    }

    [Fact]
    public void Multiply_IsAssociativeOver10000RandomFrames()
    {
        var rng = new Random(129);
        for (int i = 0; i < Samples; i++)
        {
            var a = RandomFrame(rng);
            var b = RandomFrame(rng);
            var c = RandomFrame(rng);

            var (dp, da) = Frame3<double>.Distance((a * b) * c, a * (b * c));

            dp.Should().BeLessThan(PositionTolerance);
            da.Should().BeLessThan(AngleTolerance);
        }
    }

    [Fact]
    public void TransformPoint_EqualsRotatePlusPositionOver10000RandomFrames()
    {
        var rng = new Random(129);
        for (int i = 0; i < Samples; i++)
        {
            var a = RandomFrame(rng);
            var p = new Point3<double>(Uniform(rng, 2), Uniform(rng, 2), Uniform(rng, 2));

            var expected = (Point3<double>)a.Rotation.Rotate(p) + (Vector3<double>)a.Position;

            Point3<double>.Distance(a.TransformPoint(p), expected).Should().BeLessThan(PositionTolerance);
        }
    }

    [Fact]
    public void Multiply_TransformsLikeComposition()
    {
        var rng = new Random(129);
        for (int i = 0; i < 1000; i++)
        {
            var a = RandomFrame(rng);
            var b = RandomFrame(rng);
            var p = new Point3<double>(Uniform(rng, 2), Uniform(rng, 2), Uniform(rng, 2));

            Point3<double>.Distance((a * b).TransformPoint(p), a.TransformPoint(b.TransformPoint(p)))
                .Should().BeLessThan(PositionTolerance);
            Point3<double>.Distance(a.Inverse().TransformPoint(a.TransformPoint(p)), p)
                .Should().BeLessThan(PositionTolerance);
        }
    }

    [Fact]
    public void Multiply_MethodAndOperatorAgree()
    {
        var rng = new Random(129);
        var a = RandomFrame(rng);
        var b = RandomFrame(rng);

        a.Multiply(b).Should().Be(a * b);
    }

    [Fact]
    public void Identity_IsNeutral()
    {
        var a = RandomFrame(new Random(129));

        (Frame3<double>.Identity * a).Should().Be(a);
        (a * Frame3<double>.Identity).Should().Be(a);
    }

    [Fact]
    public void TransformPoint_WithTranslationOnly()
    {
        var frame = new Frame3<double>(new Point3<double>(10, 20, 30), Quaternion<double>.Identity);

        frame.TransformPoint(new Point3<double>(1, 2, 3)).Should().Be(new Point3<double>(11, 22, 33));
    }

    [Fact]
    public void TransformPoint_WithRotationOnly()
    {
        var frame = new Frame3<double>(Point3<double>.Zero, Quaternion<double>.FromAxisAngle(Vector3<double>.EZ, Math.PI / 2));

        var p = frame.TransformPoint(new Point3<double>(1, 0, 0));

        p.X.Should().BeApproximately(0, PositionTolerance);
        p.Y.Should().BeApproximately(1, PositionTolerance);
        p.Z.Should().BeApproximately(0, PositionTolerance);
    }

    [Fact]
    public void TransformVector_RotatesButDoesNotTranslate()
    {
        var frame = new Frame3<double>(new Point3<double>(100, 200, 300), Quaternion<double>.FromAxisAngle(Vector3<double>.EZ, Math.PI / 2));

        var v = frame.TransformVector(Vector3<double>.EX);

        v.X.Should().BeApproximately(0, PositionTolerance);
        v.Y.Should().BeApproximately(1, PositionTolerance);
        v.Z.Should().BeApproximately(0, PositionTolerance);
    }

    [Fact]
    public void Interpolate_AtZeroAndOne_ReturnsTheEnds()
    {
        var rng = new Random(129);
        for (int i = 0; i < 1000; i++)
        {
            var a = RandomFrame(rng);
            var b = RandomFrame(rng);

            var at0 = Frame3<double>.Interpolate(a, b, 0);
            var at1 = Frame3<double>.Interpolate(a, b, 1);

            Point3<double>.Distance(at0.Position, a.Position).Should().BeLessThan(PositionTolerance);
            QuaternionError(at0.Rotation, a.Rotation).Should().BeLessThan(1e-12);
            Point3<double>.Distance(at1.Position, b.Position).Should().BeLessThan(PositionTolerance);
            QuaternionError(at1.Rotation, b.Rotation).Should().BeLessThan(1e-12);
        }
    }

    [Fact]
    public void Interpolate_AtHalf_GivesMidpointAndHalfAngle()
    {
        var rng = new Random(129);
        for (int i = 0; i < 1000; i++)
        {
            var a = RandomFrame(rng);
            var b = RandomFrame(rng);

            var mid = Frame3<double>.Interpolate(a, b, 0.5);

            var expected = new Point3<double>((a.Position.X + b.Position.X) / 2, (a.Position.Y + b.Position.Y) / 2, (a.Position.Z + b.Position.Z) / 2);
            Point3<double>.Distance(mid.Position, expected).Should().BeLessThan(PositionTolerance);
            var whole = Frame3<double>.Distance(a, b).Angle;
            Frame3<double>.Distance(a, mid).Angle.Should().BeApproximately(whole / 2, 1e-12);
            Frame3<double>.Distance(mid, b).Angle.Should().BeApproximately(whole / 2, 1e-12);
        }
    }

    [Fact]
    public void Distance_GivesPositionAndAngleError()
    {
        var rng = new Random(129);
        for (int i = 0; i < 1000; i++)
        {
            var a = RandomFrame(rng);
            var offset = new Vector3<double>(Uniform(rng, 1), Uniform(rng, 1), Uniform(rng, 1));
            var axis = new Vector3<double>(Uniform(rng, 1) + 2, Uniform(rng, 1), Uniform(rng, 1)).Normalize();
            var angle = 0.01 + rng.NextDouble() * (Math.PI - 0.02);
            var b = a * new Frame3<double>((Point3<double>)offset, Quaternion<double>.FromAxisAngle(axis, angle));

            var (position, rotation) = Frame3<double>.Distance(a, b);

            position.Should().BeApproximately(offset.Length, 1e-12);
            rotation.Should().BeApproximately(angle, 1e-12);
            Frame3<double>.Distance(b, a).Angle.Should().BeApproximately(angle, 1e-12);
        }
    }

    [Fact]
    public void Distance_PositionPart_IsEuclidean()
    {
        var a = new Frame3<double>(Point3<double>.Zero, Quaternion<double>.Identity);
        var b = new Frame3<double>(new Point3<double>(3, 4, 0), Quaternion<double>.FromRpy(1, 0.5, 0.3));

        Frame3<double>.Distance(a, b).Position.Should().Be(5);
    }
}
