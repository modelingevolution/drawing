using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for Path{T} objects that serializes paths as SVG path data strings.
/// </summary>
/// <typeparam name="T">The numeric type used for path coordinates.</typeparam>
public class PathJsonConverter<T> : JsonConverter<Path<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Reads a Path{T} from JSON string representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Path{T}).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Path{T} object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
    public override Path<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string value for Path");

        var pathData = reader.GetString();
        if (pathData == null)
            return new Path<T>();

        return Path<T>.Parse(pathData, null);
    }

    /// <summary>
    /// Writes a Path{T} to JSON as an SVG path data string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Path{T} value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Path<T> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}