using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class ColorTests
{
    [Fact]
    public void TransparentTest()
    {
        System.Drawing.Color c = System.Drawing.Color.Transparent;
        Color actual = Color.FromArgb(0, 255, 255, 255);
        actual.A.Should().Be(c.A);
        actual.IsTransparent.Should().BeTrue();
    }
    [Fact]
    public void BlackWithAlpha()
    {
        Color c = Colors.Black.MakeTransparent(0.5f);
        c.ToString().Should().Be("rgba(0,0,0,0.49803922)");
    }
    [Fact]
    public void Black()
    {
        Color c = Colors.Black;
        c.ToString().Should().Be("#000000");
    }
    [Fact]
    public void White()
    {
        Color c = Colors.White;
        c.ToString().Should().Be("#ffffff");
    }
}