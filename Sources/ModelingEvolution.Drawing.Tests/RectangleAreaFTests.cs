using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class RectangleAreaFTests
{
    [Fact]
    public void Empty()
    {
        RectangleArea<float> area = new RectangleArea<float>();
        area.Value.Should().Be(0f);
    }
    [Fact]
    public void Single()
    {
        RectangleArea<float> area = new RectangleArea<float>();

        var actual = new Rectangle<float>(10, 10, 20, 20);
        area += actual;
            
        area.Value.Should().Be(400f);
            
        Rectangle<float> r = (Rectangle<float>)area;
        r.Should().Be(actual);
    }

    [Fact]
    public void SameUnion()
    {
        RectangleArea<float> area = new RectangleArea<float>();

        var r1 = new Rectangle<float>(10, 10, 20, 20);
        var r2 = new Rectangle<float>(10, 10, 20, 20);
            
        area += r1;
        area += r2;

        area.Value.Should().Be(400f);

        Rectangle<float> r = (Rectangle<float>)area;
        r.Should().Be(r1);
    }
    [Fact]
    public void MergeUnion()
    {
        RectangleArea<float> area = new RectangleArea<float>();

        var r1 = new Rectangle<float>(10, 10, 20, 20);
        var r2 = new Rectangle<float>(20, 20, 20, 20);

        area += r1;
        area += r2;

        area.Value.Should().Be(900f);

        Rectangle<float> r = (Rectangle<float>)area;
        r.Should().Be(Rectangle<float>.Union(r1,r2));
    }
}