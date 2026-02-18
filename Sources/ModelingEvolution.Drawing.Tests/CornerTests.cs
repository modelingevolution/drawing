using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class CornerTests
{
    [Fact]
    public void Sharp_IsSharp()
    {
        Corner<float>.Sharp.IsSharp.Should().BeTrue();
        Corner<float>.Sharp.Radius.Should().Be(0f);
    }

    [Fact]
    public void Round_HasRadius()
    {
        var c = Corner<float>.Round(5f);
        c.IsSharp.Should().BeFalse();
        c.Radius.Should().Be(5f);
    }

    [Fact]
    public void Round_NegativeRadius_Throws()
    {
        var act = () => Corner<float>.Round(-1f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Round_Zero_IsSharp()
    {
        Corner<float>.Round(0f).IsSharp.Should().BeTrue();
    }

    [Fact]
    public void ToString_Sharp()
    {
        Corner<float>.Sharp.ToString().Should().Be("Sharp");
    }

    [Fact]
    public void ToString_Round()
    {
        Corner<float>.Round(5f).ToString().Should().Contain("5");
    }
}
