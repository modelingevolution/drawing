using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Matrix2x2{T} as an array of arrays: [[M11, M12], [M21, M22]].
/// </summary>
public abstract class Matrix2x2Converter<T> : JsonConverter<Matrix2x2<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    public override Matrix2x2<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of outer array for Matrix2x2.");

        // Row 0
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of row 0 array.");
        if (!reader.Read()) throw new JsonException("Expected M11.");
        var m11 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M12.");
        var m12 = ReadValue(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of row 0 array.");

        // Row 1
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of row 1 array.");
        if (!reader.Read()) throw new JsonException("Expected M21.");
        var m21 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M22.");
        var m22 = ReadValue(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of row 1 array.");

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of outer array for Matrix2x2.");

        return new Matrix2x2<T>(m11, m12, m21, m22);
    }

    public override void Write(Utf8JsonWriter writer, Matrix2x2<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteStartArray();
        WriteValue(writer, value.M11);
        WriteValue(writer, value.M12);
        writer.WriteEndArray();

        writer.WriteStartArray();
        WriteValue(writer, value.M21);
        WriteValue(writer, value.M22);
        writer.WriteEndArray();

        writer.WriteEndArray();
    }
}

public sealed class Matrix2x2ConverterF : Matrix2x2Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

public sealed class Matrix2x2ConverterD : Matrix2x2Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class Matrix2x2JsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> _typeFactory = new();

    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return _typeFactory.GetOrAdd(typeToConvert, x =>
        {
            var genericArg = typeToConvert.GetGenericArguments()[0];
            if (genericArg == typeof(float))
                return () => new Matrix2x2ConverterF();
            if (genericArg == typeof(double))
                return () => new Matrix2x2ConverterD();
            throw new NotSupportedException($"Matrix2x2<{genericArg.Name}> is not supported for JSON serialization.");
        })();
    }
}
