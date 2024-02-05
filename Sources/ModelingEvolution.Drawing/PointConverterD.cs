using System.Text.Json;

namespace ModelingEvolution.Drawing;

public class PointConverterD : PointConverter<double>
{
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}
public class VectorConverterD : VectorConverter<double>
{
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}
public class RectangleConverterD : RectangleConverter<double>
{
    protected override double _readMth(ref Utf8JsonReader reader) => reader.GetDouble();

    protected override void _writeMth(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);
}