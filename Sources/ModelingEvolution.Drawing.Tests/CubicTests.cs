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