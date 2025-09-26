using System.Text.Json;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for Point{float} objects.
/// </summary>
public class PointConverterF : PointConverter<float>
{
    /// <summary>
    /// Reads a float value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The float value read from the JSON.</returns>
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    /// <summary>
    /// Writes a float value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The float value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Vector{float} objects.
/// </summary>
public class VectorConverterF : VectorConverter<float>
{
    /// <summary>
    /// Reads a float value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The float value read from the JSON.</returns>
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    /// <summary>
    /// Writes a float value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The float value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Rectangle{float} objects.
/// </summary>
public class RectangleConverterF : RectangleConverter<float>
{
    /// <summary>
    /// Reads a float value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The float value read from the JSON.</returns>
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    /// <summary>
    /// Writes a float value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The float value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}