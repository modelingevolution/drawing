using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Segment3Tests
{
    private const float Tol = 1e-4f;

    [Fact]
    public void Ctor_StoresStartEnd()
    {
        var s = new Segment3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        s.Start.Should().Be(new Point3<float>(1, 2, 3));
        s.End.Should().Be(new Point3<float>(4, 5, 6));
    }

    [Fact]
    public void Length_AlongAxis()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        s.Length.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Length_Diagonal3D()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(1, 1, 1));
        s.Length.Should().BeApproximately(MathF.Sqrt(3f), Tol);
    }

    [Fact]
    public void Middle_IsMidpoint()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 6, 4));
        var m = s.Middle;
        m.X.Should().BeApproximately(5f, Tol);
        m.Y.Should().BeApproximately(3f, Tol);
        m.Z.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void Direction_IsEndMinusStart()
    {
        var s = new Segment3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 6, 8));
        var d = s.Direction;
        d.X.Should().BeApproximately(3f, Tol);
        d.Y.Should().BeApproximately(4f, Tol);
        d.Z.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void PlusVector_Translates()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(1, 0, 0));
        var moved = s + new Vector3<float>(5, 3, 1);
        moved.Start.Should().Be(new Point3<float>(5, 3, 1));
        moved.End.Should().Be(new Point3<float>(6, 3, 1));
    }

    [Fact]
    public void Reverse_SwapsStartEnd()
    {
        var s = new Segment3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        var r = s.Reverse();
        r.Start.Should().Be(new Point3<float>(4, 5, 6));
        r.End.Should().Be(new Point3<float>(1, 2, 3));
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 10, 10));
        var p = s.Lerp(0f);
        p.Should().Be(new Point3<float>(0, 0, 0));
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 10, 10));
        var p = s.Lerp(1f);
        p.X.Should().BeApproximately(10f, Tol);
        p.Y.Should().BeApproximately(10f, Tol);
        p.Z.Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 10, 10));
        var p = s.Lerp(0.5f);
        p.X.Should().BeApproximately(5f, Tol);
        p.Y.Should().BeApproximately(5f, Tol);
        p.Z.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void DistanceTo_PointOnSegment_IsZero()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        s.DistanceTo(new Point3<float>(5, 0, 0)).Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void DistanceTo_PointAbove()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        s.DistanceTo(new Point3<float>(5, 0, 3)).Should().BeApproximately(3f, Tol);
    }

    [Fact]
    public void DistanceTo_PointBeyondEnd()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        // Past the end: closest point is the end
        s.DistanceTo(new Point3<float>(15, 0, 0)).Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void ProjectPoint_OntoSegment()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        var p = s.ProjectPoint(new Point3<float>(5, 7, 3));
        p.X.Should().BeApproximately(5f, Tol);
        p.Y.Should().BeApproximately(0f, Tol);
        p.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Split_AtHalf()
    {
        var s = new Segment3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        var (left, right) = s.Split(0.5f);
        left.Start.Should().Be(new Point3<float>(0, 0, 0));
        left.End.X.Should().BeApproximately(5f, Tol);
        right.Start.X.Should().BeApproximately(5f, Tol);
        right.End.Should().Be(new Point3<float>(10, 0, 0));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Segment3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        var b = new Segment3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        a.Should().Be(b);
    }
}
