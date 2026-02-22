using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Matrix3x3{T} as an array of arrays:
/// [[M11, M12, M13], [M21, M22, M23], [M31, M32, M33]].
/// </summary>
public abstract class Matrix3x3Converter<T> : JsonConverter<Matrix3x3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>Reads a single numeric value from the JSON reader.</summary>
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    /// <summary>Writes a single numeric value to the JSON writer.</summary>
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    /// <inheritdoc />
    public override Matrix3x3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of outer array for Matrix3x3.");

        // Row 0
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of row 0 array.");
        if (!reader.Read()) throw new JsonException("Expected M11.");
        var m11 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M12.");
        var m12 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M13.");
        var m13 = ReadValue(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of row 0 array.");

        // Row 1
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of row 1 array.");
        if (!reader.Read()) throw new JsonException("Expected M21.");
        var m21 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M22.");
        var m22 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M23.");
        var m23 = ReadValue(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of row 1 array.");

        // Row 2
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of row 2 array.");
        if (!reader.Read()) throw new JsonException("Expected M31.");
        var m31 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M32.");
        var m32 = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected M33.");
        var m33 = ReadValue(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of row 2 array.");

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of outer array for Matrix3x3.");

        return new Matrix3x3<T>(m11, m12, m13, m21, m22, m23, m31, m32, m33);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Matrix3x3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteStartArray();
        WriteValue(writer, value.M11);
        WriteValue(writer, value.M12);
        WriteValue(writer, value.M13);
        writer.WriteEndArray();

        writer.WriteStartArray();
        WriteValue(writer, value.M21);
        WriteValue(writer, value.M22);
        WriteValue(writer, value.M23);
        writer.WriteEndArray();

        writer.WriteStartArray();
        WriteValue(writer, value.M31);
        WriteValue(writer, value.M32);
        WriteValue(writer, value.M33);
        writer.WriteEndArray();

        writer.WriteEndArray();
    }
}

/// <summary>JSON converter for <see cref="Matrix3x3{T}"/> with <c>float</c> elements.</summary>
public sealed class Matrix3x3ConverterF : Matrix3x3Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>JSON converter for <see cref="Matrix3x3{T}"/> with <c>double</c> elements.</summary>
public sealed class Matrix3x3ConverterD : Matrix3x3Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// Attribute that applies the appropriate <see cref="Matrix3x3Converter{T}"/> for JSON serialization.
/// Supports <c>float</c> and <c>double</c> element types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class Matrix3x3JsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> _typeFactory = new();

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return _typeFactory.GetOrAdd(typeToConvert, x =>
        {
            var genericArg = typeToConvert.GetGenericArguments()[0];
            if (genericArg == typeof(float))
                return () => new Matrix3x3ConverterF();
            if (genericArg == typeof(double))
                return () => new Matrix3x3ConverterD();
            throw new NotSupportedException($"Matrix3x3<{genericArg.Name}> is not supported for JSON serialization.");
        })();
    }
}
