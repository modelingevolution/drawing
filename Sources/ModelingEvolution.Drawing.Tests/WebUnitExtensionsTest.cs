using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class WebUnitExtensionsTest
{
    [Fact]
    public void GetMaxVectorTest()
    {
        List<Vector<float>> data = new List<Vector<float>>();
        data.Add(new Vector<float>(0, 0));
        data.Add(new Vector<float>(0, 0));
        data.Add(new Vector<float>(0, 0));
        var sut = data.GetIndexOfMaxVector();
        sut.Should().Be(0);

        data.Add(new Vector<float>(5, 5));

         sut = data.GetIndexOfMaxVector();
        sut.Should().Be(3);

        data.Add(new Vector<float>(-6, -6));

         sut = data.GetIndexOfMaxVector();
        sut.Should().Be(4);
    
    }
    [Fact]
    public void SkippingMaxInAvg()
    {
        List<Vector<float>> data = new List<Vector<float>>();
        data.Add(new Vector<float>(0, 0));
        data.Add(new Vector<float>(5, 5));
        data.Avg().Should().Be(new Vector<double>(0, 0));

        data.Add(new Vector<float>(-5, -5));
        data.Avg().Should().Be(new Vector<double>(-2.5,-2.5));
        data.Add(new Vector<float>(6, 6));
        data.Avg().Should().Be(new Vector<double>(0, 0));


    }
}