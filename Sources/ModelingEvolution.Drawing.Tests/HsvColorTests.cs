using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProtoBuf;

namespace ModelingEvolution.Drawing.Tests
{

    [ProtoContract]
    public class ColorContainer
    {
        [ProtoMember(1)]
        public Color Color { get; set; }
        [ProtoMember(2)]
        public HsvColor HsvColor { get; set; }
    }
    public class ProtobufSerializationTests
    {
        [Fact]
        public void ColorContainer_Serialization_RoundTrip()
        {
            var original = new ColorContainer
            {
                Color = Color.FromArgb(128, 255, 0, 0),      // Semi-transparent red
                HsvColor = new HsvColor(120, 1, 0.5f, 0.75f) // Semi-transparent green
            };

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, original);

            stream.Position = 0;
            var deserialized = Serializer.Deserialize<ColorContainer>(stream);

            Assert.Equal(original.Color.Value, deserialized.Color.Value);
            Assert.Equal(original.HsvColor.H, deserialized.HsvColor.H);
            Assert.Equal(original.HsvColor.S, deserialized.HsvColor.S);
            Assert.Equal(original.HsvColor.V, deserialized.HsvColor.V);
            Assert.Equal(original.HsvColor.A, deserialized.HsvColor.A);
        }

        [Theory]
        [InlineData(0xFF0000FF)]   // Red
        [InlineData(0x00FF00FF)]   // Green
        [InlineData(0x0000FFFF)]   // Blue
        [InlineData(0x80808080)]   // Semi-transparent gray
        public void Color_Serialization_RoundTrip(uint value)
        {
            var original = new Color(value);

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, original);

            stream.Position = 0;
            var deserialized = Serializer.Deserialize<Color>(stream);

            Assert.Equal(original.Value, deserialized.Value);
        }

        [Theory]
        [InlineData(0, 1, 1, 1)]       // Red HSV
        [InlineData(120, 1, 1, 0.5f)]  // Semi-transparent green
        [InlineData(240, 0.5f, 0.5f, 1)] // Desaturated blue
        public void HsvColor_Serialization_RoundTrip(float h, float s, float v, float a)
        {
            var original = new HsvColor(h, s, v, a);

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, original);

            stream.Position = 0;
            var deserialized = Serializer.Deserialize<HsvColor>(stream);

            Assert.Equal(original.H, deserialized.H);
            Assert.Equal(original.S, deserialized.S);
            Assert.Equal(original.V, deserialized.V);
            Assert.Equal(original.A, deserialized.A);
        }
    }
    public class HsvColorTests
    {
        [Theory]
        [InlineData("hsv(0,100%,100%)", 0, 1, 1, 1)]
        [InlineData("hsv(120,50%,75%)", 120, 0.5f, 0.75f, 1)]
        [InlineData("hsva(240,25%,50%,0.5)", 240, 0.25f, 0.5f, 0.5f)]
        [InlineData("[0,1,1]", 0, 1, 1, 1)]
        [InlineData("[120,0.5,0.75,0.5]", 120, 0.5f, 0.75f, 0.5f)]
        public void Parse_ValidInputs_ReturnsExpectedColor(string input, float h, float s, float v, float a)
        {
            var color = HsvColor.Parse(input);

            Assert.Equal(h, color.H);
            Assert.Equal(s, color.S, 0.001f);
            Assert.Equal(v, color.V, 0.001f);
            Assert.Equal(a, color.A, 0.001f);
        }

        [Theory]
        [InlineData("invalid")]
        public void Parse_InvalidInputs_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => HsvColor.Parse(input));
        }

        [Theory]
        [InlineData("hsv(0,100%,100%)", true)]
        [InlineData("invalid", false)]
        [InlineData("[0,1,1]", true)]
        [InlineData("[0,2,1]", false)]
        public void TryParse_ReturnsExpectedResult(string input, bool expectedResult)
        {
            bool result = HsvColor.TryParse(input, out var color);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(255, 0, 0, 255, 0, 1, 1)]      // Red
        [InlineData(0, 255, 0, 255, 120, 1, 1)]    // Green
        [InlineData(0, 0, 255, 255, 240, 1, 1)]    // Blue
        [InlineData(128, 128, 128, 255, 0, 0, 0.502f)] // Gray
        public void RgbToHsv_Conversion_IsAccurate(byte r, byte g, byte b, byte a,
            float expectedH, float expectedS, float expectedV)
        {
            var rgb = Color.FromArgb(a, r, g, b);
            var hsv = (HsvColor)rgb;

            Assert.Equal(expectedH, hsv.H, 1f);
            Assert.Equal(expectedS, hsv.S, 0.001f);
            Assert.Equal(expectedV, hsv.V, 0.001f);
            Assert.Equal(a / 255f, hsv.A, 0.001f);
        }

        [Theory]
        [InlineData(0, 1, 1, 1, 255, 0, 0, 255)]      // Red
        [InlineData(120, 1, 1, 1, 0, 255, 0, 255)]    // Green
        [InlineData(240, 1, 1, 1, 0, 0, 255, 255)]    // Blue
        [InlineData(0, 0, 0.5f, 0.5f, 127, 127, 127, 127)] // Gray 50% + 50% alpha
        public void HsvToRgb_Conversion_IsAccurate(float h, float s, float v, float a,
            byte expectedR, byte expectedG, byte expectedB, byte expectedA)
        {
            var hsv = new HsvColor(h, s, v, a);
            var rgb = (Color)hsv;

            Assert.Equal(expectedR, rgb.R);
            Assert.Equal(expectedG, rgb.G);
            Assert.Equal(expectedB, rgb.B);
            Assert.Equal(expectedA, rgb.A);
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesValues()
        {
            var colors = new[]
            {
            new HsvColor(0, 1, 1),
            new HsvColor(120, 0.5f, 0.75f, 0.5f),
            new HsvColor(240, 0.25f, 0.5f)
        };

            var options = new JsonSerializerOptions();
            string json = JsonSerializer.Serialize(colors, options);
            var deserialized = JsonSerializer.Deserialize<HsvColor[]>(json, options);

            Assert.Equal(colors.Length, deserialized.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                Assert.Equal(colors[i].H, deserialized[i].H);
                Assert.Equal(colors[i].S, deserialized[i].S);
                Assert.Equal(colors[i].V, deserialized[i].V);
                Assert.Equal(colors[i].A, deserialized[i].A);
            }
        }

        [Fact]
        public void ProtobufSerialization_RoundTrip_PreservesValues()
        {
            var colors = new[]
            {
            new HsvColor(0, 1, 1),
            new HsvColor(120, 0.5f, 0.75f, 0.5f),
            new HsvColor(240, 0.25f, 0.5f)
        };

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, colors);

            stream.Position = 0;
            var deserialized = Serializer.Deserialize<HsvColor[]>(stream);

            Assert.Equal(colors.Length, deserialized.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                Assert.Equal(colors[i].H, deserialized[i].H);
                Assert.Equal(colors[i].S, deserialized[i].S);
                Assert.Equal(colors[i].V, deserialized[i].V);
                Assert.Equal(colors[i].A, deserialized[i].A);
            }
        }

        [Theory]
        [InlineData(0, 1, 1, 1, "hsv(0,100%,100%)")]
        [InlineData(120, 0.5f, 0.75f, 0.5f, "hsva(120,50%,75%,0.5)")]
        public void ToString_ReturnsExpectedFormat(float h, float s, float v, float a, string expected)
        {
            var color = new HsvColor(h, s, v, a);
            Assert.Equal(expected, color.ToString());
        }
    }
}
