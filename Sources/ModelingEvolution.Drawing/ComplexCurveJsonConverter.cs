using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for ComplexCurve{T} that serializes as SVG path data strings.
/// </summary>
public class ComplexCurveJsonConverter<T> : JsonConverter<ComplexCurve<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    public override ComplexCurve<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string value for ComplexCurve");

        var pathData = reader.GetString();
        if (pathData == null)
            return default;

        return ComplexCurve<T>.Parse(pathData, null);
    }

    public override void Write(Utf8JsonWriter writer, ComplexCurve<T> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
