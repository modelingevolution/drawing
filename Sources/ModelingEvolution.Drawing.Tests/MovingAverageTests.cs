using FluentAssertions;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class MovingAverageTests
{
    [Fact]
    public void Constructor_WithZeroWindowSize_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MovingAverage<float>(0));
    }

    [Fact]
    public void NewInstance_HasZeroCountAndAverage()
    {
        var avg = new MovingAverage<float>(5);

        avg.Count.Should().Be(0);
        avg.Capacity.Should().Be(5);
        avg.Average.Should().Be(Point<float>.Zero);
    }

    [Fact]
    public void Add_SinglePoint_AverageIsThatPoint()
    {
        var avg = new MovingAverage<float>(5);
        avg.Add(new Point<float>(10f, 20f));

        avg.Count.Should().Be(1);
        avg.Average.X.Should().Be(10f);
        avg.Average.Y.Should().Be(20f);
    }

    [Fact]
    public void Add_MultiplePoints_AveragesComponentWise()
    {
        var avg = new MovingAverage<float>(5);
        avg.Add(new Point<float>(10f, 100f));
        avg.Add(new Point<float>(20f, 200f));
        avg.Add(new Point<float>(30f, 300f));

        avg.Count.Should().Be(3);
        avg.Average.X.Should().Be(20f);
        avg.Average.Y.Should().Be(200f);
    }

    [Fact]
    public void WindowSlides_OldestDropsOut()
    {
        var avg = new MovingAverage<float>(3);
        avg.Add(new Point<float>(100f, 100f));
        avg.Add(new Point<float>(1f, 1f));
        avg.Add(new Point<float>(2f, 2f));
        avg.Add(new Point<float>(3f, 3f)); // 100 drops out

        avg.Count.Should().Be(3);
        avg.Average.X.Should().Be(2f);
        avg.Average.Y.Should().Be(2f);
    }

    [Fact]
    public void Clear_ResetsToInitialState()
    {
        var avg = new MovingAverage<float>(5);
        avg.Add(new Point<float>(10f, 20f));
        avg.Add(new Point<float>(30f, 40f));

        avg.Clear();

        avg.Count.Should().Be(0);
        avg.Average.Should().Be(Point<float>.Zero);
    }

    [Fact]
    public void OperatorPlus_AddsPoint()
    {
        var avg = new MovingAverage<float>(3);
        avg += new Point<float>(10f, 10f);
        avg += new Point<float>(20f, 20f);
        avg += new Point<float>(30f, 30f);

        avg.Average.X.Should().Be(20f);
        avg.Average.Y.Should().Be(20f);
    }

    [Fact]
    public void ImplicitConversion_ReturnsAverage()
    {
        var avg = new MovingAverage<float>(3);
        avg.Add(new Point<float>(10f, 100f));
        avg.Add(new Point<float>(20f, 200f));
        avg.Add(new Point<float>(30f, 300f));

        Point<float> pt = avg;
        pt.X.Should().Be(20f);
        pt.Y.Should().Be(200f);
    }

    [Fact]
    public void WorksWithDouble()
    {
        var avg = new MovingAverage<double>(3);
        avg.Add(new Point<double>(1.5, 2.5));
        avg.Add(new Point<double>(3.5, 4.5));
        avg.Add(new Point<double>(5.5, 6.5));

        avg.Average.X.Should().BeApproximately(3.5, 1e-10);
        avg.Average.Y.Should().BeApproximately(4.5, 1e-10);
    }
}
