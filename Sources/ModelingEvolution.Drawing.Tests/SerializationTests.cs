using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing.Equations;
using PointF = ModelingEvolution.Drawing.Point<float>;
using VectorF = ModelingEvolution.Drawing.Vector<float>;
using SizeF = ModelingEvolution.Drawing.Size<float>;

namespace ModelingEvolution.Drawing.Tests
{
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
    }
    public class SerializationTests
    {
        record FooPoint(Point<float> Value);
        record FooVector(Vector<float> Value);
        record FooRectangle(Rectangle<float> Value);
        [Fact]
        public void PointSerialization()
        {
            FooPoint tmp = new FooPoint(PointF.Random());
            var json = JsonSerializer.Serialize(tmp);
            var actual = JsonSerializer.Deserialize<FooPoint>(json);

            actual.Should().Be(tmp);
        }
        
        [Fact]
        public void VectorSerialization()
        {
            FooVector tmp = new FooVector(Vector<float>.Random());
            var json = JsonSerializer.Serialize(tmp);
            var actual = JsonSerializer.Deserialize<FooVector>(json);

            actual.Should().Be(tmp);
        }
        [Fact]
        public void RectangleSerialization()
        {
            FooRectangle tmp = new FooRectangle(Rectangle<float>.Random());
            var json = JsonSerializer.Serialize(tmp);
            var actual = JsonSerializer.Deserialize<FooRectangle>(json);

            actual.Should().Be(tmp);
        }
    }
}