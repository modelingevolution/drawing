using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PosePath3ExtensionsTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void AdaptToEndpoints_IdenticalEndpoints_LeavesMidsUnchanged()
    {
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(5, 0, 0, 0, 0, 0),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        var run = teach.AdaptToEndpoints(teach[0], teach[^1]);

        run.Count.Should().Be(teach.Count);
        run[1].Position.X.Should().BeApproximately(5.0, Tolerance);
        run[1].Position.Y.Should().BeApproximately(0.0, Tolerance);
        run[1].Position.Z.Should().BeApproximately(0.0, Tolerance);
    }

    [Fact]
    public void AdaptToEndpoints_TranslatedEndpoints_TranslatesMids()
    {
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(5, 0, 0, 0, 0, 0),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        // Translate both endpoints by (100, 50, 25). No rotation, no scale.
        var translation = new Vector3<double>(100, 50, 25);
        var run = teach.AdaptToEndpoints(
            new Pose3<double>(teach[0].Position + translation, teach[0].Rotation),
            new Pose3<double>(teach[^1].Position + translation, teach[^1].Rotation));

        run[1].Position.X.Should().BeApproximately(105.0, Tolerance);
        run[1].Position.Y.Should().BeApproximately(50.0, Tolerance);
        run[1].Position.Z.Should().BeApproximately(25.0, Tolerance);
    }

    [Fact]
    public void AdaptToEndpoints_ScaledEndpoints_ScalesMids()
    {
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(5, 0, 0, 0, 0, 0),    // midway
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        // Adapted: start stays, end doubles → segment 2x length
        var run = teach.AdaptToEndpoints(
            teach[0],
            new Pose3<double>(20, 0, 0, 0, 0, 0));

        // Mid at half-length in teach (5/10) → half-length in run (10/20)
        run[1].Position.X.Should().BeApproximately(10.0, 1e-6);
        run[1].Position.Y.Should().BeApproximately(0.0, 1e-6);
        run[1].Position.Z.Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void AdaptToEndpoints_RotatedEndpoints_RotatesMidPositions()
    {
        // Teach: segment along +X with one mid-point at (5, 0, 0)
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(5, 0, 0, 0, 0, 0),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        // Adapted: rotate segment 90deg around Z so it goes from origin toward +Y
        var run = teach.AdaptToEndpoints(
            teach[0],
            new Pose3<double>(0, 10, 0, 0, 0, 0));

        // Mid should rotate accordingly — was at +X halfway, now at +Y halfway
        run[1].Position.X.Should().BeApproximately(0.0, 1e-6);
        run[1].Position.Y.Should().BeApproximately(5.0, 1e-6);
        run[1].Position.Z.Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void AdaptToEndpoints_TwoPosePath_ReplacesBothEndpoints()
    {
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        var newA = new Pose3<double>(1, 1, 1, 0, 0, 0);
        var newB = new Pose3<double>(11, 1, 1, 0, 0, 0);

        var run = teach.AdaptToEndpoints(newA, newB);

        run.Count.Should().Be(2);
        run[0].Should().Be(newA);
        run[1].Should().Be(newB);
    }

    [Fact]
    public void AdaptToEndpoints_LessThanTwoPoses_Throws()
    {
        var single = new PosePath3<double>(new Pose3<double>(0, 0, 0, 0, 0, 0));
        Action act = () => single.AdaptToEndpoints(
            new Pose3<double>(1, 0, 0, 0, 0, 0),
            new Pose3<double>(2, 0, 0, 0, 0, 0));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdaptToEndpoints_CoincidentTeachEndpoints_Throws()
    {
        // Degenerate teach path: start and end at the same position. No segment direction → cannot derive similarity.
        var teach = new PosePath3<double>(
            new Pose3<double>(5, 5, 5, 0, 0, 0),
            new Pose3<double>(5, 5, 5, 5, 5, 5),    // same position, different rotation
            new Pose3<double>(5, 5, 5, 0, 0, 0));

        Action act = () => teach.AdaptToEndpoints(
            new Pose3<double>(10, 0, 0, 0, 0, 0),
            new Pose3<double>(20, 0, 0, 0, 0, 0));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*endpoints are at the same position*");
    }

    [Fact]
    public void AdaptToEndpoints_RotatedEndpoints_MidRotationPreservedUnchanged()
    {
        // MVP contract: mid-pose rotations are NOT transformed even when the segment rotates.
        // This test pins that contract so a future "fix" doesn't silently change behavior.
        var midTaughtRotation = new Rotation3<double>(15, 30, 45);
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(new Point3<double>(5, 0, 0), midTaughtRotation),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        // Rotate the segment 90° around Z (X axis → Y axis)
        var run = teach.AdaptToEndpoints(
            teach[0],
            new Pose3<double>(0, 10, 0, 0, 0, 0));

        // Mid-pose position has rotated; rotation has NOT.
        run[1].Position.Y.Should().BeApproximately(5.0, 1e-6);
        run[1].Rotation.Should().Be(midTaughtRotation);
    }

    [Fact]
    public void AdaptToEndpoints_PreservesTaughtRotationAtMidPoints()
    {
        // Mid-point has a non-identity rotation; MVP keeps it unchanged.
        var midTaughtRotation = new Rotation3<double>(15, 30, 45);
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(new Point3<double>(5, 0, 0), midTaughtRotation),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        var run = teach.AdaptToEndpoints(
            new Pose3<double>(100, 0, 0, 0, 0, 0),
            new Pose3<double>(110, 0, 0, 0, 0, 0));

        run[1].Rotation.Should().Be(midTaughtRotation);
    }

    [Fact]
    public void AdaptToEndpoints_EndpointsExactlyReplaced()
    {
        var teach = new PosePath3<double>(
            new Pose3<double>(0, 0, 0, 0, 0, 0),
            new Pose3<double>(5, 0, 0, 0, 0, 0),
            new Pose3<double>(10, 0, 0, 0, 0, 0));

        var newA = new Pose3<double>(100, 50, 25, 10, 20, 30);
        var newB = new Pose3<double>(110, 50, 25, 10, 20, 30);

        var run = teach.AdaptToEndpoints(newA, newB);

        run[0].Should().Be(newA);
        run[^1].Should().Be(newB);
    }
}
