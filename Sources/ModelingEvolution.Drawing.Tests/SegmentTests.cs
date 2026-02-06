using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class SegmentTests
{
    [Fact]
    public void Constructor_SetsStartAndEnd()
    {
        var s = new Segment<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        s.Start.Should().Be(new Point<float>(1, 2));
        s.End.Should().Be(new Point<float>(3, 4));
    }

    [Fact]
    public void From_CreatesSegment()
    {
        var s = Segment<float>.From(new Point<float>(0, 0), new Point<float>(3, 4));
        s.Start.Should().Be(new Point<float>(0, 0));
        s.End.Should().Be(new Point<float>(3, 4));
    }

    [Fact]
    public void Direction_ReturnsEndMinusStart()
    {
        var s = new Segment<float>(new Point<float>(1, 1), new Point<float>(4, 5));
        s.Direction.X.Should().Be(3f);
        s.Direction.Y.Should().Be(4f);
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(3, 4));
        s.Length.Should().BeApproximately(5f, 1e-6f);
    }

    [Fact]
    public void Middle_ReturnsCorrectMidpoint()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(4, 6));
        s.Middle.Should().Be(new Point<float>(2, 3));
    }

    [Fact]
    public void AddVector_TranslatesSegment()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(1, 1));
        var result = s + new Vector<float>(10, 20);
        result.Start.Should().Be(new Point<float>(10, 20));
        result.End.Should().Be(new Point<float>(11, 21));
    }

    [Fact]
    public void SubtractVector_TranslatesSegment()
    {
        var s = new Segment<float>(new Point<float>(10, 20), new Point<float>(11, 21));
        var result = s - new Vector<float>(10, 20);
        result.Start.Should().Be(new Point<float>(0, 0));
        result.End.Should().Be(new Point<float>(1, 1));
    }

    [Fact]
    public void MultiplyBySize_ScalesBothEndpoints()
    {
        var s = new Segment<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var result = s * new Size<float>(2, 3);
        result.Start.Should().Be(new Point<float>(2, 6));
        result.End.Should().Be(new Point<float>(6, 12));
    }

    [Fact]
    public void DivideBySize_ScalesBothEndpoints()
    {
        var s = new Segment<float>(new Point<float>(4, 6), new Point<float>(8, 12));
        var result = s / new Size<float>(2, 3);
        result.Start.Should().Be(new Point<float>(2, 2));
        result.End.Should().Be(new Point<float>(4, 4));
    }

    [Fact]
    public void Contains_PointOnSegment_ReturnsTrue()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(5, 0)).Should().BeTrue();
    }

    [Fact]
    public void Contains_StartPoint_ReturnsTrue()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(0, 0)).Should().BeTrue();
    }

    [Fact]
    public void Contains_EndPoint_ReturnsTrue()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(10, 0)).Should().BeTrue();
    }

    [Fact]
    public void Contains_PointOffSegment_ReturnsFalse()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(5, 1)).Should().BeFalse();
    }

    [Fact]
    public void Contains_PointBeyondEnd_ReturnsFalse()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(15, 0)).Should().BeFalse();
    }

    [Fact]
    public void Contains_PointBeforeStart_ReturnsFalse()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.Contains(new Point<float>(-5, 0)).Should().BeFalse();
    }

    [Fact]
    public void Intersect_CrossingSegments_ReturnsIntersectionPoint()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        var s2 = new Segment<float>(new Point<float>(0, 10), new Point<float>(10, 0));
        var result = s1.Intersect(s2);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(5f, 1e-5f);
        result.Value.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void Intersect_ParallelSegments_ReturnsNull()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var s2 = new Segment<float>(new Point<float>(0, 1), new Point<float>(10, 1));
        s1.Intersect(s2).Should().BeNull();
    }

    [Fact]
    public void Intersect_NonIntersectingSegments_ReturnsNull()
    {
        var s1 = new Segment<float>(new Point<float>(0, 0), new Point<float>(1, 0));
        var s2 = new Segment<float>(new Point<float>(5, 5), new Point<float>(6, 5));
        s1.Intersect(s2).Should().BeNull();
    }

    [Fact]
    public void Intersect_TShapeSegments_ReturnsEndpoint()
    {
        var s1 = new Segment<float>(new Point<float>(0, 5), new Point<float>(10, 5));
        var s2 = new Segment<float>(new Point<float>(5, 0), new Point<float>(5, 5));
        var result = s1.Intersect(s2);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(5f, 1e-5f);
        result.Value.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void ToLine_ReturnsLineContainingSegment()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        var line = s.ToLine();
        line.IsVertical.Should().BeFalse();
        line.Compute(5f).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void ToLine_VerticalSegment_ReturnsVerticalLine()
    {
        var s = new Segment<float>(new Point<float>(5, 0), new Point<float>(5, 10));
        var line = s.ToLine();
        line.IsVertical.Should().BeTrue();
    }

    [Fact]
    public void Contains_DiagonalSegment_PointOnDiagonal_ReturnsTrue()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 10));
        s.Contains(new Point<float>(5, 5)).Should().BeTrue();
    }

    [Fact]
    public void Rotate_90Degrees_AroundOrigin()
    {
        // (10, 0) rotated 90 CCW → (0, 10)
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var rotated = s.Rotate(Degree<float>.Create(90f));
        rotated.Start.X.Should().BeApproximately(0f, 1e-4f);
        rotated.Start.Y.Should().BeApproximately(0f, 1e-4f);
        rotated.End.X.Should().BeApproximately(0f, 1e-4f);
        rotated.End.Y.Should().BeApproximately(10f, 1e-4f);
    }

    [Fact]
    public void Rotate_AroundCustomOrigin()
    {
        // Segment (10, 0)→(20, 0) rotated 90 around (10, 0)
        // Start stays at (10, 0), End goes to (10, 10)
        var s = new Segment<float>(new Point<float>(10, 0), new Point<float>(20, 0));
        var rotated = s.Rotate(Degree<float>.Create(90f), new Point<float>(10, 0));
        rotated.Start.X.Should().BeApproximately(10f, 1e-4f);
        rotated.Start.Y.Should().BeApproximately(0f, 1e-4f);
        rotated.End.X.Should().BeApproximately(10f, 1e-4f);
        rotated.End.Y.Should().BeApproximately(10f, 1e-4f);
    }

    [Fact]
    public void Rotate_PreservesLength()
    {
        var s = new Segment<float>(new Point<float>(1, 2), new Point<float>(4, 6));
        var rotated = s.Rotate(Degree<float>.Create(37f));
        rotated.Length.Should().BeApproximately(s.Length, 1e-4f);
    }

    [Fact]
    public void PlusDegree_Operator()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var rotated = s + Degree<float>.Create(90f);
        rotated.End.X.Should().BeApproximately(0f, 1e-4f);
        rotated.End.Y.Should().BeApproximately(10f, 1e-4f);
    }

    [Fact]
    public void DistanceTo_PointOnSegment_ReturnsZero()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.DistanceTo(new Point<float>(5, 0)).Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_PointPerpendicularToMid()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        s.DistanceTo(new Point<float>(5, 3)).Should().BeApproximately(3f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_PointBeyondEnd_ReturnsDistanceToEndpoint()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        // Point (15, 0) is beyond end — closest point is (10, 0), distance = 5
        s.DistanceTo(new Point<float>(15, 0)).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_PointBeforeStart_ReturnsDistanceToStartpoint()
    {
        var s = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        // Point (-3, 4) is before start — closest point is (0, 0), distance = 5
        s.DistanceTo(new Point<float>(-3, 4)).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void AngleBetween_Perpendicular_ReturnsHalfPi()
    {
        var s1 = new Segment<double>(new Point<double>(0, 0), new Point<double>(1, 0)); // →
        var s2 = new Segment<double>(new Point<double>(0, 0), new Point<double>(0, 1)); // ↑
        var angle = (double)s1.AngleBetween(s2);
        angle.Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void AngleBetween_Opposite_ReturnsPi()
    {
        var s1 = new Segment<double>(new Point<double>(0, 0), new Point<double>(1, 0)); // →
        var s2 = new Segment<double>(new Point<double>(0, 0), new Point<double>(-1, 0)); // ←
        var angle = (double)s1.AngleBetween(s2);
        Math.Abs(angle).Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void AngleBetween_ClockwiseRotation_ReturnsNegative()
    {
        var s1 = new Segment<double>(new Point<double>(0, 0), new Point<double>(1, 0)); // →
        var s2 = new Segment<double>(new Point<double>(0, 0), new Point<double>(0, -1)); // ↓
        var angle = (double)s1.AngleBetween(s2);
        angle.Should().BeApproximately(-Math.PI / 2, 1e-9);
    }

    [Fact]
    public void AngleBetween_SameDirection_ReturnsZero()
    {
        var s1 = new Segment<double>(new Point<double>(0, 0), new Point<double>(3, 4));
        var s2 = new Segment<double>(new Point<double>(1, 1), new Point<double>(4, 5));
        var angle = (double)s1.AngleBetween(s2);
        angle.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void AngleBetween_ResultInRange_NegPiToPi()
    {
        // 135° CCW — should be +3π/4
        var s1 = new Segment<double>(new Point<double>(0, 0), new Point<double>(1, 0));
        var s2 = new Segment<double>(new Point<double>(0, 0), new Point<double>(-1, 1));
        var angle = (double)s1.AngleBetween(s2);
        angle.Should().BeApproximately(3 * Math.PI / 4, 1e-9);
    }

    [Fact]
    public void ProjectPoint_OnSegment()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(5, 5));
        proj.X.Should().BeApproximately(5, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void ProjectPoint_BeyondEnd_ClampsToEnd()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(20, 5));
        proj.X.Should().BeApproximately(10, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void ProjectPoint_BeforeStart_ClampsToStart()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(-5, 5));
        proj.X.Should().BeApproximately(0, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(0);
        p.X.Should().BeApproximately(0, 1e-9);
        p.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(1);
        p.X.Should().BeApproximately(10, 1e-9);
        p.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(0.5);
        p.X.Should().BeApproximately(5, 1e-9);
        p.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Split_AtHalf()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var (left, right) = s.Split(0.5);
        left.Start.X.Should().BeApproximately(0, 1e-9);
        left.End.X.Should().BeApproximately(5, 1e-9);
        right.Start.X.Should().BeApproximately(5, 1e-9);
        right.End.X.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void IsParallelTo_ParallelSegments()
    {
        var s1 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var s2 = Segment<double>.From(new Point<double>(0, 5), new Point<double>(10, 5));
        s1.IsParallelTo(s2).Should().BeTrue();
    }

    [Fact]
    public void IsParallelTo_NonParallelSegments()
    {
        var s1 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var s2 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 10));
        s1.IsParallelTo(s2).Should().BeFalse();
    }

    [Fact]
    public void Reverse_SwapsStartEnd()
    {
        var s = Segment<double>.From(new Point<double>(1, 2), new Point<double>(3, 4));
        var r = s.Reverse();
        r.Start.X.Should().BeApproximately(3, 1e-9);
        r.Start.Y.Should().BeApproximately(4, 1e-9);
        r.End.X.Should().BeApproximately(1, 1e-9);
        r.End.Y.Should().BeApproximately(2, 1e-9);
    }

    private static readonly Rectangle<double> Roi = new(0, 0, 10, 10);

    [Fact]
    public void Segment_FullyInside_ReturnsSame()
    {
        var s = Segment<double>.From(new Point<double>(2, 2), new Point<double>(8, 8));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(2, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(8, 1e-9);
    }

    [Fact]
    public void Segment_FullyOutside_ReturnsNull()
    {
        var s = Segment<double>.From(new Point<double>(20, 20), new Point<double>(30, 30));
        s.Intersect(Roi).Should().BeNull();
    }

    [Fact]
    public void Segment_PartiallyInside_ClipsCorrectly()
    {
        var s = Segment<double>.From(new Point<double>(-5, 5), new Point<double>(15, 5));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(10, 1e-9);
        clipped.Value.Start.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void Segment_Diagonal_ClipsBothEnds()
    {
        var s = Segment<double>.From(new Point<double>(-5, -5), new Point<double>(15, 15));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(0, 1e-9);
        clipped.Value.Start.Y.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(10, 1e-9);
        clipped.Value.End.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Segment_ProjectOntoLine()
    {
        var line = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var seg = Segment<double>.From(new Point<double>(2, 0), new Point<double>(0, 2));
        var proj = seg.ProjectOnto(line);
        proj.Start.X.Should().BeApproximately(1, 1e-9);
        proj.Start.Y.Should().BeApproximately(1, 1e-9);
        proj.End.X.Should().BeApproximately(1, 1e-9);
        proj.End.Y.Should().BeApproximately(1, 1e-9);
    }
}
