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
}
