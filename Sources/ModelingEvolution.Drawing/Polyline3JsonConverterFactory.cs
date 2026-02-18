using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory that creates <see cref="Polyline3JsonConverter{T}"/> instances for the appropriate numeric type.
/// </summary>
public class Polyline3JsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Polyline3<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];

        if (elementType == typeof(float))
            return new Polyline3JsonConverter<float>(
                (writer, value) => writer.WriteNumberValue((float)value),
                reader => reader.GetSingle());

        if (elementType == typeof(double))
            return new Polyline3JsonConverter<double>(
                (writer, value) => writer.WriteNumberValue((double)value),
                reader => reader.GetDouble());

        throw new JsonException($"Unsupported type parameter: {elementType}");
    }
}
