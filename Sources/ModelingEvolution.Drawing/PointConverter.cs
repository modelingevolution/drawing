using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Abstract base class for JSON converters that serialize Point{T} objects as arrays of two numeric values.
/// </summary>
/// <typeparam name="T">The numeric type used for point coordinates.</typeparam>
public abstract class PointConverter<T> : JsonConverter<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Reads a numeric value of type T from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The numeric value read from the JSON.</returns>
    protected abstract T _readMth(ref Utf8JsonReader reader);
    
    /// <summary>
    /// Writes a numeric value of type T to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The numeric value to write.</param>
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);

    /// <summary>
    /// Reads a Point{T} from JSON array representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Point{T}).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Point{T} object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
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

    /// <summary>
    /// Writes a Point{T} to JSON as an array of two numeric values.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Point{T} value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Point<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        _writeMth(writer, value.X);
        _writeMth(writer, value.Y);
        writer.WriteEndArray();
    }
}

/// <summary>
/// Abstract base class for JSON converters that serialize Vector{T} objects as arrays of two numeric values.
/// </summary>
/// <typeparam name="T">The numeric type used for vector components.</typeparam>
public abstract class VectorConverter<T> : JsonConverter<Vector<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Reads a numeric value of type T from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The numeric value read from the JSON.</returns>
    protected abstract T _readMth(ref Utf8JsonReader reader);
    
    /// <summary>
    /// Writes a numeric value of type T to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The numeric value to write.</param>
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);

    /// <summary>
    /// Reads a Vector{T} from JSON array representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Vector{T}).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Vector{T} object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
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

    /// <summary>
    /// Writes a Vector{T} to JSON as an array of two numeric values.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Vector{T} value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Vector<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        _writeMth(writer, value.X);
        _writeMth(writer, value.Y);
        writer.WriteEndArray();
    }
}

/// <summary>
/// Abstract base class for JSON converters that serialize Rectangle{T} objects as arrays of four numeric values (X, Y, Width, Height).
/// </summary>
/// <typeparam name="T">The numeric type used for rectangle coordinates and dimensions.</typeparam>
public abstract class RectangleConverter<T> : JsonConverter<Rectangle<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Reads a numeric value of type T from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The numeric value read from the JSON.</returns>
    protected abstract T _readMth(ref Utf8JsonReader reader);
    
    /// <summary>
    /// Writes a numeric value of type T to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The numeric value to write.</param>
    protected abstract void _writeMth(Utf8JsonWriter writer, T value);

    /// <summary>
    /// Reads a Rectangle{T} from JSON array representation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (Rectangle{T}).</param>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>The deserialized Rectangle{T} object.</returns>
    /// <exception cref="JsonException">Thrown when the JSON format is invalid.</exception>
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

    /// <summary>
    /// Writes a Rectangle{T} to JSON as an array of four numeric values (X, Y, Width, Height).
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The Rectangle{T} value to serialize.</param>
    /// <param name="options">JSON serialization options.</param>
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