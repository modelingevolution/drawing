using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory that creates <see cref="SkeletonJsonConverter{T}"/> instances for the appropriate numeric type.
/// </summary>
public class SkeletonJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Skeleton<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];

        if (elementType == typeof(float))
            return new SkeletonJsonConverter<float>(
                (writer, value) => writer.WriteNumberValue((float)value),
                reader => reader.GetSingle());

        if (elementType == typeof(double))
            return new SkeletonJsonConverter<double>(
                (writer, value) => writer.WriteNumberValue((double)value),
                reader => reader.GetDouble());

        throw new JsonException($"Unsupported type parameter: {elementType}");
    }
}
