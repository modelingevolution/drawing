using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public class JsonColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString();
            return Color.FromString(value);
        }
        else throw new JsonException("Expecting color to have different schema.");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string str = value.ToJson();
        writer.WriteStringValue(str);
    }
}