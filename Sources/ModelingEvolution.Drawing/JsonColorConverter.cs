using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for Color objects that serializes colors as string values.
/// </summary>
public class JsonColorConverter : JsonConverter<Color>
{
    /// <summary>
    /// Reads a Color from JSON string representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Color).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Color object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON token is not a string.</exception>
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString();
            return Color.FromString(value);
        }
        else throw new JsonException("Expecting color to have different schema.");
    }

    /// <summary>
    /// Writes a Color to JSON as a string representation.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Color value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string str = value.ToJson();
        writer.WriteStringValue(str);
    }
}