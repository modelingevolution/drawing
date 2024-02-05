using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public abstract class PointConverter<T> : JsonConverter<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    protected abstract T _readMth(ref Utf8JsonReader reader);
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);

    

    public override Point<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        if (!reader.Read()) throw new JsonException("Expected x value for point.");
        var x = _readMth(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value for point.");
        var y = _readMth(ref reader);

        if (!reader.Read()||  reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array.");

        return new Point<T>(x,y);
    }

    public override void Write(Utf8JsonWriter writer, Point<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        _writeMth(writer, value.X);
        _writeMth(writer, value.Y);
        writer.WriteEndArray();
    }
}
public abstract class VectorConverter<T> : JsonConverter<Vector<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    protected abstract T _readMth(ref Utf8JsonReader reader);
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);



    public override Vector<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        if (!reader.Read()) throw new JsonException("Expected x value for point.");
        var x = _readMth(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value for point.");
        var y = _readMth(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array.");

        return new Vector<T>(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        _writeMth(writer, value.X);
        _writeMth(writer, value.Y);
        writer.WriteEndArray();
    }
}
public abstract class RectangleConverter<T> : JsonConverter<Rectangle<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    protected abstract T _readMth(ref Utf8JsonReader reader);
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);



    public override Rectangle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        if (!reader.Read()) throw new JsonException("Expected x value for rect.");
        var x = _readMth(ref reader);
        if (!reader.Read()) throw new JsonException("Expected y value for rect.");
        var y = _readMth(ref reader);
        if (!reader.Read()) throw new JsonException("Expected w value for rect.");
        var w = _readMth(ref reader);
        if (!reader.Read()) throw new JsonException("Expected h value for rect.");
        var h = _readMth(ref reader);

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected end of array.");

        return new Rectangle<T>(x, y,w,h);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        _writeMth(writer, value.X);
        _writeMth(writer, value.Y);
        _writeMth(writer, value.Width);
        _writeMth(writer, value.Height);
        writer.WriteEndArray();
    }
}