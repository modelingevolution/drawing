using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for Polygon{T} objects that serializes polygons as flat arrays of coordinate values.
/// </summary>
/// <typeparam name="T">The numeric type used for polygon coordinates.</typeparam>
public class PolygonJsonConverter<T> : JsonConverter<Polygon<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Action<Utf8JsonWriter, T> _writeNumber;
    private readonly Func<Utf8JsonReader, T> _readNumber;

    /// <summary>
    /// Initializes a new instance of the PolygonJsonConverter class.
    /// </summary>
    /// <param name="writeNumber">Function to write a numeric value to JSON.</param>
    /// <param name="readNumber">Function to read a numeric value from JSON.</param>
    public PolygonJsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    /// <summary>
    /// Reads a Polygon{T} from JSON array representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Polygon{T}).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Polygon{T} object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid or array length is odd.</exception>
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

    /// <summary>
    /// Writes a Polygon{T} to JSON as a flat array of coordinate values.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Polygon{T} value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
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