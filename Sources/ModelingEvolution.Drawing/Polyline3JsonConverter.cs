using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for <see cref="Polyline3{T}"/> that serializes as a flat coordinate array [x1, y1, z1, x2, y2, z2, ...].
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public class Polyline3JsonConverter<T> : JsonConverter<Polyline3<T>>
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
    public Polyline3JsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    /// <inheritdoc />
    public override Polyline3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        if (coords.Count % 3 != 0)
            throw new JsonException("Array length must be a multiple of 3");

        return new Polyline3<T>(coords);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Polyline3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var point in value.Points)
        {
            _writeNumber(writer, point.X);
            _writeNumber(writer, point.Y);
            _writeNumber(writer, point.Z);
        }
        writer.WriteEndArray();
    }
}
