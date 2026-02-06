using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class MovingMedianTests
{
    [Fact]
    public void Constructor_WithZeroWindowSize_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MovingMedian<float>(0));
    }

    [Fact]
    public void NewInstance_HasZeroCountAndMedian()
    {
        var m = new MovingMedian<float>(5);

        m.Count.Should().Be(0);
        m.Capacity.Should().Be(5);
        m.Median.Should().Be(Point<float>.Zero);
    }

    [Fact]
    public void Add_SinglePoint_MedianIsThatPoint()
    {
        var m = new MovingMedian<float>(5);
        m.Add(new Point<float>(10f, 20f));

        m.Count.Should().Be(1);
        m.Median.X.Should().Be(10f);
        m.Median.Y.Should().Be(20f);
    }

    [Fact]
    public void OddCount_ReturnsMiddleComponentWise()
    {
        var m = new MovingMedian<float>(5);
        m.Add(new Point<float>(10f, 300f));
        m.Add(new Point<float>(30f, 100f));
        m.Add(new Point<float>(20f, 200f));

        // X sorted: [10, 20, 30] → 20
        // Y sorted: [100, 200, 300] → 200
        m.Median.X.Should().Be(20f);
        m.Median.Y.Should().Be(200f);
    }

    [Fact]
    public void EvenCount_ReturnsAverageOfTwoMiddle()
    {
        var m = new MovingMedian<float>(5);
        m.Add(new Point<float>(10f, 100f));
        m.Add(new Point<float>(30f, 300f));
        m.Add(new Point<float>(20f, 200f));
        m.Add(new Point<float>(40f, 400f));

        // X sorted: [10, 20, 30, 40] → (20+30)/2 = 25
        // Y sorted: [100, 200, 300, 400] → (200+300)/2 = 250
        m.Median.X.Should().Be(25f);
        m.Median.Y.Should().Be(250f);
    }

    [Fact]
    public void WindowSlides_OldestDropsOut()
    {
        var m = new MovingMedian<float>(3);
        m.Add(new Point<float>(1000f, 1000f));
        m.Add(new Point<float>(1f, 1f));
        m.Add(new Point<float>(2f, 2f));
        m.Add(new Point<float>(3f, 3f)); // 1000 drops out

        // X sorted: [1, 2, 3] → 2
        m.Median.X.Should().Be(2f);
        m.Median.Y.Should().Be(2f);
    }

    [Fact]
    public void MedianResistsOutliers()
    {
        var m = new MovingMedian<float>(5);
        m.Add(new Point<float>(10f, 10f));
        m.Add(new Point<float>(11f, 11f));
        m.Add(new Point<float>(12f, 12f));
        m.Add(new Point<float>(11f, 11f));
        m.Add(new Point<float>(9999f, 9999f)); // outlier

        // X sorted: [10, 11, 11, 12, 9999] → 11
        m.Median.X.Should().Be(11f);
        m.Median.Y.Should().Be(11f);
    }

    [Fact]
    public void Clear_ResetsToInitialState()
    {
        var m = new MovingMedian<float>(5);
        m.Add(new Point<float>(10f, 20f));
        m.Add(new Point<float>(30f, 40f));

        m.Clear();

        m.Count.Should().Be(0);
        m.Median.Should().Be(Point<float>.Zero);
    }

    [Fact]
    public void OperatorPlus_AddsPoint()
    {
        var m = new MovingMedian<float>(3);
        m += new Point<float>(10f, 10f);
        m += new Point<float>(20f, 20f);
        m += new Point<float>(30f, 30f);

        m.Median.X.Should().Be(20f);
        m.Median.Y.Should().Be(20f);
    }

    [Fact]
    public void ImplicitConversion_ReturnsMedian()
    {
        var m = new MovingMedian<float>(3);
        m.Add(new Point<float>(10f, 100f));
        m.Add(new Point<float>(20f, 200f));
        m.Add(new Point<float>(30f, 300f));

        Point<float> pt = m;
        pt.X.Should().Be(20f);
        pt.Y.Should().Be(200f);
    }

    [Fact]
    public void WorksWithDouble()
    {
        var m = new MovingMedian<double>(3);
        m.Add(new Point<double>(1.5, 2.5));
        m.Add(new Point<double>(3.5, 4.5));
        m.Add(new Point<double>(5.5, 6.5));

        m.Median.X.Should().BeApproximately(3.5, 1e-10);
        m.Median.Y.Should().BeApproximately(4.5, 1e-10);
    }
}
