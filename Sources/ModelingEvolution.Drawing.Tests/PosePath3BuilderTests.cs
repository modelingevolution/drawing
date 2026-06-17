using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class PosePath3BuilderTests
{
    [Fact]
    public void Build_Empty_ReturnsEmptyPath()
    {
        var path = new PosePath3Builder<double>().Build();
        path.Count.Should().Be(0);
    }

    [Fact]
    public void StartAt_OnlyOnce_BuildsOnePose()
    {
        var start = new Pose3<double>(1, 2, 3, 0, 0, 0);
        var path = new PosePath3Builder<double>().StartAt(start).Build();

        path.Count.Should().Be(1);
        path[0].Should().Be(start);
    }

    [Fact]
    public void StartAt_CalledTwice_Throws()
    {
        var b = new PosePath3Builder<double>().StartAt(new Pose3<double>(0, 0, 0, 0, 0, 0));
        Action act = () => b.StartAt(new Pose3<double>(1, 1, 1, 0, 0, 0));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Delta_BeforeStartAt_Throws()
    {
        var b = new PosePath3Builder<double>();
        Action act = () => b.Delta(new Vector3<double>(1, 0, 0), Rotation3<double>.Identity);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Delta_AppendsTranslationToPreviousPosition()
    {
        var start = new Pose3<double>(100, 50, 200, 0, 0, 0);
        var path = new PosePath3Builder<double>()
            .StartAt(start)
            .Delta(new Vector3<double>(10, 0, 0), Rotation3<double>.Identity)
            .Build();

        path.Count.Should().Be(2);
        path[1].X.Should().Be(110);
        path[1].Y.Should().Be(50);
        path[1].Z.Should().Be(200);
    }

    [Fact]
    public void Delta_AppendsRotationToPreviousRotation()
    {
        var start = new Pose3<double>(0, 0, 0, 10, 20, 30);
        var path = new PosePath3Builder<double>()
            .StartAt(start)
            .Delta(Vector3<double>.Zero, new Rotation3<double>(1, 2, 3))
            .Build();

        path[1].Rx.Should().Be((Degree<double>)11.0);
        path[1].Ry.Should().Be((Degree<double>)22.0);
        path[1].Rz.Should().Be((Degree<double>)33.0);
    }

    [Fact]
    public void Chain_OfMultipleDeltas_AccumulatesCorrectly()
    {
        var path = new PosePath3Builder<double>()
            .StartAt(new Pose3<double>(0, 0, 0, 0, 0, 0))
            .Delta([10, 0, 0], [0, 0, 0])
            .Delta([10, 0, 0], [0, 0, 0])
            .Delta([10, 0, 0], [0, 0, 0])
            .Build();

        path.Count.Should().Be(4);
        path[0].Position.Should().Be(new Point3<double>(0, 0, 0));
        path[1].Position.Should().Be(new Point3<double>(10, 0, 0));
        path[2].Position.Should().Be(new Point3<double>(20, 0, 0));
        path[3].Position.Should().Be(new Point3<double>(30, 0, 0));
    }

    [Fact]
    public void Construct_WithZeroInitialCapacity_DoesNotInfiniteLoop()
    {
        // Regression guard: doubling from 0 would never grow; constructor must floor at 1.
        var builder = new PosePath3Builder<double>(0);
        var path = builder
            .StartAt(new Pose3<double>(0, 0, 0, 0, 0, 0))
            .Delta(new Vector3<double>(1, 0, 0), Rotation3<double>.Identity)
            .Delta(new Vector3<double>(1, 0, 0), Rotation3<double>.Identity)
            .Build();

        path.Count.Should().Be(3);
    }

    [Fact]
    public void CollectionLiteralSyntax_CompilesAndProducesCorrectResult()
    {
        // The headline use case: code-generated programs use [x,y,z] literals
        var path = new PosePath3Builder<double>()
            .StartAt([120, 50, 200, 180, 0, 0])
            .Delta([10, 0, 0], [0, 0, 0])
            .Delta([10, 0, 0], [0, 0, 0])
            .Build();

        path.Count.Should().Be(3);
        path[0].Position.Should().Be(new Point3<double>(120, 50, 200));
        path[^1].Position.Should().Be(new Point3<double>(140, 50, 200));
    }
}
