using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for <see cref="Polyline{T}"/> that serializes as a flat coordinate array [x1, y1, x2, y2, ...].
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public class PolylineJsonConverter<T> : JsonConverter<Polyline<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Action<Utf8JsonWriter, T> _writeNumber;
    private readonly Func<Utf8JsonReader, T> _readNumber;

    /// <summary>
    /// Initializes a new converter with the specified number read/write delegates.
    /// </summary>
    /// <param name="writeNumber">Delegate that writes a numeric value to the JSON writer.</param>
    /// <param name="readNumber">Delegate that reads a numeric value from the JSON reader.</param>
    public PolylineJsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    /// <inheritdoc />
    public override Polyline<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var coords = new List<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            coords.Add(_readNumber(reader));
        }

        if (coords.Count % 2 != 0)
            throw new JsonException("Array length must be even");

        return new Polyline<T>(coords);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Polyline<T> value, JsonSerializerOptions options)
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
