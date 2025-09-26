using FluentAssertions;
using ProtoBuf;
using System.Text.Json;

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

    [Theory]
    [InlineData("#FF0000", 255, 0, 0, 255)]
    [InlineData("#00FF00", 0, 255, 0, 255)]
    [InlineData("#0000FF", 0, 0, 255, 255)]
    [InlineData("#80808080", 128, 128, 128, 128)]
    [InlineData("0xFF0000", 255, 0, 0, 255)]
    [InlineData("rgba(255,128,64,0.5)", 255, 128, 64, 127)]
    public void Parse_ValidInputs_ReturnsExpectedColor(string input, byte r, byte g, byte b, byte a)
    {
        var color = Color.Parse(input);

        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
        Assert.Equal(a, color.A);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("#GGGGGG")]
    public void Parse_InvalidInputs_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => Color.Parse(input));
    }

    [Theory]
    [InlineData("#FF0000", true)]
    [InlineData("invalid", false)]
    [InlineData("rgba(255,0,0,1)", true)]
    [InlineData("rgba(300,0,0,1)", false)]
    public void TryParse_ReturnsExpectedResult(string input, bool expectedResult)
    {
        bool result = Color.TryParse(input, out var color);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void JsonSerialization_RoundTrip_PreservesValues()
    {
        var colors = new[]
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(128, 0, 255, 0),
            Color.FromArgb(255, 0, 0, 255)
        };

        var options = new JsonSerializerOptions();
        string json = JsonSerializer.Serialize(colors, options);
        var deserialized = JsonSerializer.Deserialize<Color[]>(json, options);

        Assert.Equal(colors.Length, deserialized.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            Assert.Equal(colors[i].Value, deserialized[i].Value);
        }
    }

    [Fact]
    public void ProtobufSerialization_RoundTrip_PreservesValues()
    {
        var colors = new[]
        {
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(128, 0, 255, 0),
            Color.FromArgb(255, 0, 0, 255)
        };

        using var stream = new MemoryStream();
        Serializer.Serialize(stream, colors);

        stream.Position = 0;
        var deserialized = Serializer.Deserialize<Color[]>(stream);

        Assert.Equal(colors.Length, deserialized.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            Assert.Equal(colors[i].Value, deserialized[i].Value);
        }
    }

   

    [Fact]
    public void MakeTransparent_ReturnsExpectedColor()
    {
        var color = Color.FromArgb(255, 255, 0, 0);
        var transparent = color.MakeTransparent(0.5f);

        Assert.Equal(127, transparent.A);
        Assert.Equal(color.R, transparent.R);
        Assert.Equal(color.G, transparent.G);
        Assert.Equal(color.B, transparent.B);
    }
}