using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating JSON converters for Path{T} types.
/// </summary>
public class PathJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the converter factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to check for conversion support.</param>
    /// <returns>True if the type is a generic Path{T}; otherwise, false.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Path<>);
    }

    /// <summary>
    /// Creates a JSON converter for the specified Path{T} type.
    /// </summary>
    /// <param name="typeToConvert">The Path{T} type to create a converter for.</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>A JSON converter for the specified Path{T} type.</returns>
    /// <exception cref="JsonException">Thrown when the generic type parameter is not supported.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];

        Type converterType = typeof(PathJsonConverter<>).MakeGenericType(elementType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}