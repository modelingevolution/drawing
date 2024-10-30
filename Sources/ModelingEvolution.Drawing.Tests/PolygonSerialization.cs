using ProtoBuf;
using ProtoBuf.Meta;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonSerialization
{

    [Fact]
    public void Polygon_Serialization_Deserialization_Test()
    {
        // Arrange
        var points = new Point<double>[]
        {
            new Point<double>(0, 0),
            new Point<double>(1, 0),
            new Point<double>(0, 1)
        };
        var polygon = new Polygon<double>(points);
        // Act
        Polygon<double> deserializedPolygon;
        using (var stream = new MemoryStream())
        {
            Serializer.Serialize(stream, polygon);
            stream.Position = 0;
            deserializedPolygon = Serializer.Deserialize<Polygon<double>>(stream);
        }

        // Assert
        Assert.Equal(polygon.Count, deserializedPolygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            Assert.Equal(polygon[i].X, deserializedPolygon[i].X);
            Assert.Equal(polygon[i].Y, deserializedPolygon[i].Y);
        }
    }
}