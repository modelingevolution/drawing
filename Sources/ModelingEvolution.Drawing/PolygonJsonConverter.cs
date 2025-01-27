using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public class PolygonJsonConverter<T> : JsonConverter<Polygon<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Action<Utf8JsonWriter, T> _writeNumber;
    private readonly Func<Utf8JsonReader, T> _readNumber;

    public PolygonJsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    public override Polygon<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var points = new List<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            points.Add(_readNumber(reader));
        }

        if (points.Count % 2 != 0)
            throw new JsonException("Array length must be even");

        return new Polygon<T>(points);
    }

    public override void Write(Utf8JsonWriter writer, Polygon<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var point in value.Points)
        {
            _writeNumber(writer, point.X);
            _writeNumber(writer, point.Y);
        }

        writer.WriteEndArray();
    }
}