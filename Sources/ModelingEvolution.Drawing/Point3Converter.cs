using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Point3{T} objects as arrays of three numeric values.
/// </summary>
public abstract class Point3Converter<T> : JsonConverter<Point3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    public override Point3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Point3.");

        if (!reader.Read()) throw new JsonException("Expected x value.");
        var x = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value.");
        var y = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected z value.");
        var z = ReadValue(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Point3.");

        return new Point3<T>(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Point3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, value.X);
        WriteValue(writer, value.Y);
        WriteValue(writer, value.Z);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Point3{float}.
/// </summary>
public class Point3ConverterF : Point3Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Point3{double}.
/// </summary>
public class Point3ConverterD : Point3Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// Abstract base class for JSON converters that serialize Vector3{T} objects as arrays of three numeric values.
/// </summary>
public abstract class Vector3Converter<T> : JsonConverter<Vector3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    public override Vector3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Vector3.");

        if (!reader.Read()) throw new JsonException("Expected x value.");
        var x = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value.");
        var y = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected z value.");
        var z = ReadValue(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Vector3.");

        return new Vector3<T>(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, value.X);
        WriteValue(writer, value.Y);
        WriteValue(writer, value.Z);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Vector3{float}.
/// </summary>
public class Vector3ConverterF : Vector3Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Vector3{double}.
/// </summary>
public class Vector3ConverterD : Vector3Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// Abstract base class for JSON converters that serialize Rotation3{T} objects as arrays of three numeric values [rx, ry, rz].
/// </summary>
public abstract class Rotation3Converter<T> : JsonConverter<Rotation3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    public override Rotation3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Rotation3.");

        if (!reader.Read()) throw new JsonException("Expected rx value.");
        var rx = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected ry value.");
        var ry = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected rz value.");
        var rz = ReadValue(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Rotation3.");

        return new Rotation3<T>(rx, ry, rz);
    }

    public override void Write(Utf8JsonWriter writer, Rotation3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, (T)value.Rx);
        WriteValue(writer, (T)value.Ry);
        WriteValue(writer, (T)value.Rz);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Rotation3{float}.
/// </summary>
public class Rotation3ConverterF : Rotation3Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Rotation3{double}.
/// </summary>
public class Rotation3ConverterD : Rotation3Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// Abstract base class for JSON converters that serialize Pose3{T} objects as arrays of six numeric values [x, y, z, rx, ry, rz].
/// </summary>
public abstract class Pose3Converter<T> : JsonConverter<Pose3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T ReadValue(ref Utf8JsonReader reader);
    protected abstract void WriteValue(Utf8JsonWriter writer, T value);

    public override Pose3<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Pose3.");

        if (!reader.Read()) throw new JsonException("Expected x value.");
        var x = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value.");
        var y = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected z value.");
        var z = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected rx value.");
        var rx = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected ry value.");
        var ry = ReadValue(ref reader);
        if (!reader.Read()) throw new JsonException("Expected rz value.");
        var rz = ReadValue(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array for Pose3.");

        return new Pose3<T>(x, y, z, rx, ry, rz);
    }

    public override void Write(Utf8JsonWriter writer, Pose3<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        WriteValue(writer, value.X);
        WriteValue(writer, value.Y);
        WriteValue(writer, value.Z);
        WriteValue(writer, (T)value.Rx);
        WriteValue(writer, (T)value.Ry);
        WriteValue(writer, (T)value.Rz);
        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter for Pose3{float}.
/// </summary>
public class Pose3ConverterF : Pose3Converter<float>
{
    protected override float ReadValue(ref Utf8JsonReader reader) => reader.GetSingle();
    protected override void WriteValue(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Pose3{double}.
/// </summary>
public class Pose3ConverterD : Pose3Converter<double>
{
    protected override double ReadValue(ref Utf8JsonReader reader) => reader.GetDouble();
    protected override void WriteValue(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}
