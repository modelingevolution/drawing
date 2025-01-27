using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public class PolygonJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(Polygon<>);
    }

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