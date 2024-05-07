using FluentAssertions;
using ModelingEvolution.Drawing;
using PointF = ModelingEvolution.Drawing.Point<float>;
using BezierCurveF = ModelingEvolution.Drawing.BezierCurve<float>;
using ModelingEvolution.Drawing.Equations;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class CubicTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public CubicTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(8.334f)]
    [InlineData(15.387f)]
    [InlineData(19.236f)]
    public void Velocity(float v)
    {
        QuadraticEquation<float> eq = new QuadraticEquation<float>(-0.178f, 3.953f, -1.923f-v);
        var r = eq.ZeroPoints();
        testOutputHelper.WriteLine($"{r[0]}, {r[1]}");
    }
}

public class BezierTests
{
    [Fact]
    public void CreateFrom2Points()
    {
        PointF a = new PointF(0, 0);
        PointF b = new PointF(1, 1);

        var curves = BezierCurveF.Create(a, b).ToArray();

        curves.Should().HaveCount(1);

        var c = curves[0];
        c.Start.Should().Be(a);
        c.End.Should().Be(b);
        var expected = new PointF(0.5f,0.5f);
        c.C0.Should().Be(expected);
        c.C1.Should().Be(expected);
    }
    [Fact]
    public void CreateFrom3Points()
    {
        PointF a = new PointF(0, 0);
        PointF b = new PointF(1, 1);
        PointF c = new PointF(2, 0);

        var c0Expected = new PointF(0.5f, 0.5f);
        var c1Expected = new PointF(1.5f, 0.5f);


        var curves = BezierCurveF.Create(a, b,c).ToArray();

        curves.Should().HaveCount(2);

        var c1 = curves[0];
        c1.Start.Should().Be(a);
        c1.C0.Should().Be(c0Expected);
        c1.C0.Should().Be(new PointF(0.5f, 1));
        c1.End.Should().Be(b);
        
        
    }
    [Fact]
    public void CreateFrom4Points()
    {
        PointF a = new PointF(0, 0);
        PointF b = new PointF(1, 1);
        PointF c = new PointF(2, 1);
        PointF d = new PointF(3, 0);

       

        var curves = BezierCurveF.Create(a, b, c,d).ToArray();

        curves.Should().HaveCount(3);

    }
}