using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating JSON converters for Polygon{T} types.
/// </summary>
public class PolygonJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the converter factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to check for conversion support.</param>
    /// <returns>True if the type is a generic Polygon{T}; otherwise, false.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Polygon<>);
    }

    /// <summary>
    /// Creates a JSON converter for the specified Polygon{T} type.
    /// </summary>
    /// <param name="typeToConvert">The Polygon{T} type to create a converter for.</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>A JSON converter for the specified Polygon{T} type.</returns>
    /// <exception cref="JsonException">Thrown when the generic type parameter is not supported.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];

        if (elementType == typeof(float))
            return new PolygonJsonConverter<float>(
                (writer, value) => writer.WriteNumberValue((float)value),
                reader => reader.GetSingle());

        if (elementType == typeof(double))
            return new PolygonJsonConverter<double>(
                (writer, value) => writer.WriteNumberValue((double)value),
                reader => reader.GetDouble());

        
        throw new JsonException($"Unsupported type parameter: {elementType}");
    }
}