using System.Text.Json;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for Point{double} objects.
/// </summary>
public class PointConverterD : PointConverter<double>
{
    /// <summary>
    /// Reads a double value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The double value read from the JSON.</returns>
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    /// <summary>
    /// Writes a double value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The double value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Vector{double} objects.
/// </summary>
public class VectorConverterD : VectorConverter<double>
{
    /// <summary>
    /// Reads a double value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The double value read from the JSON.</returns>
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    /// <summary>
    /// Writes a double value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The double value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}

/// <summary>
/// JSON converter for Rectangle{double} objects.
/// </summary>
public class RectangleConverterD : RectangleConverter<double>
{
    /// <summary>
    /// Reads a double value from the JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The double value read from the JSON.</returns>
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    /// <summary>
    /// Writes a double value to the JSON writer.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The double value to write.</param>
    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}