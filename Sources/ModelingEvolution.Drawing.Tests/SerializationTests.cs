using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using ModelingEvolution.Drawing.Equations;
using Xunit.Abstractions;
using PointF = ModelingEvolution.Drawing.Point<float>;
using VectorF = ModelingEvolution.Drawing.Vector<float>;
using SizeF = ModelingEvolution.Drawing.Size<float>;

namespace ModelingEvolution.Drawing.Tests
{
    public class SerializationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SerializationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        record FooPoint(Point<float> Value);
        record FooVector(Vector<float> Value);
        record FooRectangle(Rectangle<float> Value);

        record FooPolygon(Polygon<float> Value);
        [Fact]
        public void PointSerialization()
        {
            FooPoint tmp = new FooPoint(PointF.Random());
            var json = JsonSerializer.Serialize(tmp);
            _testOutputHelper.WriteLine(json);
            var actual = JsonSerializer.Deserialize<FooPoint>(json);

            actual.Should().Be(tmp);
        }
        
        [Fact]
        public void VectorSerialization()
        {
            FooVector tmp = new FooVector(Vector<float>.Random());
            var json = JsonSerializer.Serialize(tmp);
            _testOutputHelper.WriteLine(json);
            var actual = JsonSerializer.Deserialize<FooVector>(json);

            actual.Should().Be(tmp);
        }
        [Fact]
        public void PolygonSerialization()
        {
            FooPolygon tmp = new FooPolygon(new Polygon<float>(new PointF(10,20), new PointF(10, 20)));
            var json = JsonSerializer.Serialize(tmp);
            _testOutputHelper.WriteLine(json);
            var actual = JsonSerializer.Deserialize<FooPolygon>(json);

            actual.Should().Be(tmp);
        }

        [Fact]
        public void RectangleSerialization()
        {
            FooRectangle tmp = new FooRectangle(Rectangle<float>.Random());
            var json = JsonSerializer.Serialize(tmp);
            _testOutputHelper.WriteLine(json);
            var actual = JsonSerializer.Deserialize<FooRectangle>(json);

            actual.Should().Be(tmp);
        }
    }
}