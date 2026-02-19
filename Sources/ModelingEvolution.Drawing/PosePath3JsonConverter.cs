using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for <see cref="PosePath3{T}"/> that serializes as a flat coordinate array
/// [x1, y1, z1, rx1, ry1, rz1, x2, y2, z2, rx2, ry2, rz2, ...].
/// </summary>
public class PosePath3JsonConverter<T> : JsonConverter<PosePath3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Action<Utf8JsonWriter, T> _writeNumber;
    private readonly Func<Utf8JsonReader, T> _readNumber;

    public PosePath3JsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    public override PosePath3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var coords = new List<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            coords.Add(_readNumber(reader));
        }

        if (coords.Count % 6 != 0)
            throw new JsonException("Array length must be a multiple of 6");

        var poses = new Pose3<T>[coords.Count / 6];
        for (int i = 0; i < poses.Length; i++)
        {
            int j = i * 6;
            poses[i] = new Pose3<T>(coords[j], coords[j + 1], coords[j + 2],
                                     coords[j + 3], coords[j + 4], coords[j + 5]);
        }
        return new PosePath3<T>(poses);
    }

    public override void Write(Utf8JsonWriter writer, PosePath3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var pose in value.Poses)
        {
            _writeNumber(writer, pose.X);
            _writeNumber(writer, pose.Y);
            _writeNumber(writer, pose.Z);
            _writeNumber(writer, (T)pose.Rx);
            _writeNumber(writer, (T)pose.Ry);
            _writeNumber(writer, (T)pose.Rz);
        }
        writer.WriteEndArray();
    }
}

/// <summary>
/// Factory that creates <see cref="PosePath3JsonConverter{T}"/> instances for the appropriate numeric type.
/// </summary>
public class PosePath3JsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(PosePath3<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];

        if (elementType == typeof(float))
            return new PosePath3JsonConverter<float>(
                (writer, value) => writer.WriteNumberValue((float)value),
                reader => reader.GetSingle());

        if (elementType == typeof(double))
            return new PosePath3JsonConverter<double>(
                (writer, value) => writer.WriteNumberValue((double)value),
                reader => reader.GetDouble());

        throw new JsonException($"Unsupported type parameter: {elementType}");
    }
}
