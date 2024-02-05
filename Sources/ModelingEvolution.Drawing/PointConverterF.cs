using System.Text.Json;

namespace ModelingEvolution.Drawing;

public class PointConverterF : PointConverter<float>
{
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}
public class VectorConverterF : VectorConverter<float>
{
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}
public class RectangleConverterF : RectangleConverter<float>
{
    protected override float _readMth(ref Utf8JsonReader reader) => reader.GetSingle();

    protected override void _writeMth(Utf8JsonWriter writer, float value) => writer.WriteNumberValue(value);
}