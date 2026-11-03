using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Quaternion{T} as an array of four numeric values [w, x, y, z].
/// Only the four components are recorded; derived members (Length, LengthSquared, IsNormalized) never reach the wire.
/// Any other shape (an object, a wrong arity, a non-number) is refused with a <see cref="JsonException"/>.
/// </summary>
public abstract class QuaternionConverter<T> : JsonConverter<Quaternion<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>Reads or writes one component.</summary>
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    /// <summary>Reads or writes one component.</summary>
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    /// <inheritdoc/>
    public override Quaternion<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected start of array [w, x, y, z] for Quaternion, got {reader.TokenType}.");

        var w = ReadComponent(ref reader, "w");
        var x = ReadComponent(ref reader, "x");
        var y = ReadComponent(ref reader, "y");
        var z = ReadComponent(ref reader, "z");

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Quaternion: exactly four values [w, x, y, z].");

        return new Quaternion<T>(w, x, y, z);
    }

    private T ReadComponent(ref Utf8JsonReader reader, string name)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException($"Expected numeric {name} value for Quaternion.");
        return ReadValue(ref reader);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Quaternion<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, value.W);
        WriteValue(writer, value.X);
        WriteValue(writer, value.Y);
        WriteValue(writer, value.Z);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Quaternion{float}.
/// </summary>
public class QuaternionConverterF : QuaternionConverter<float>
{
    /// <inheritdoc/>
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    /// <inheritdoc/>
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Quaternion{double}.
/// </summary>
public class QuaternionConverterD : QuaternionConverter<double>
{
    /// <inheritdoc/>
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    /// <inheritdoc/>
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}
