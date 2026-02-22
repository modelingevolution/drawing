using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Joints6{T} as arrays of six numeric values.
/// </summary>
/// <typeparam name="T">The numeric type used for angle values.</typeparam>
public abstract class Joints6Converter<T> : JsonConverter<Joints6<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>Reads a single numeric value from the JSON reader.</summary>
    protected abstract T ReadValue(ref Utf8JsonReader reader);

    /// <summary>Writes a single numeric value to the JSON writer.</summary>
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    /// <inheritdoc />
    public override Joints6<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Joints6.");

        if (!reader.Read()) throw new JsonException("Expected j1 value.");
        var j1 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected j2 value.");
        var j2 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected j3 value.");
        var j3 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected j4 value.");
        var j4 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected j5 value.");
        var j5 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected j6 value.");
        var j6 = ReadValue(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Joints6.");

        return new Joints6<T>(j1, j2, j3, j4, j5, j6);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Joints6<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, (T)value.J1);
        WriteValue(writer, (T)value.J2);
        WriteValue(writer, (T)value.J3);
        WriteValue(writer, (T)value.J4);
        WriteValue(writer, (T)value.J5);
        WriteValue(writer, (T)value.J6);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Joints6{float}.
/// </summary>
public class Joints6ConverterF : Joints6Converter<float>
{
    /// <inheritdoc />
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();

    /// <inheritdoc />
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Joints6{double}.
/// </summary>
public class Joints6ConverterD : Joints6Converter<double>
{
    /// <inheritdoc />
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();

    /// <inheritdoc />
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}
