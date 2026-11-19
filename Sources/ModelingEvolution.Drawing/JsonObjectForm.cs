using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// The OBJECT wire form of the 3-D value objects: <c>{"x":1,"y":2,"z":3}</c> (Point3, Vector3),
/// <c>{"x":…,"y":…,"z":…,"rx":…,"ry":…,"rz":…}</c> (Pose3), <c>{"j1":…,…,"j6":…}</c> (Joints6). Parse both, write one: every array converter of these types also READS the object form (member names in
/// any case, unknown members skipped); the array stays the written form. The <c>*ObjectJsonConverter</c>s below read both
/// and WRITE the object form (names through the options' naming policy), for a wire whose readers still read objects:
/// register them on that wire's options; delete the registration once its readers take the array. (Quaternion stays
/// array-only: its old object shape is refused on purpose, #7.)
/// </summary>
internal static class JsonObjectForm
{
    internal static readonly string[] Xyz = ["X", "Y", "Z"];
    internal static readonly string[] Pose = ["X", "Y", "Z", "Rx", "Ry", "Rz"];
    internal static readonly string[] Joints = ["J1", "J2", "J3", "J4", "J5", "J6"];

    /// <summary>Reads the object at the reader (on its StartObject) into <paramref name="values"/>, by
    /// <paramref name="names"/>; every name is required.</summary>
    internal static void Read(ref Utf8JsonReader reader, string[] names, scoped Span<double> values, string type)
    {
        var seen = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();
            reader.Read();
            var i = Array.FindIndex(names, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            if (i < 0) { reader.Skip(); continue; }
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException($"{type}.{names[i]} must be a number.");
            values[i] = reader.GetDouble();
            seen |= 1 << i;
        }
        if (seen != (1 << names.Length) - 1)
            throw new JsonException($"{type} as an object needs {string.Join(", ", names)}.");
    }

    internal static void Write(Utf8JsonWriter writer, string[] names, ReadOnlySpan<double> values, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        for (var i = 0; i < names.Length; i++)
            writer.WriteNumber(options.PropertyNamingPolicy?.ConvertName(names[i]) ?? names[i], values[i]);
        writer.WriteEndObject();
    }
}

/// <summary>Reads a Pose3{double} as <c>[x, y, z, rx, ry, rz]</c> or the object; writes the object (<see cref="JsonObjectForm"/>).</summary>
public sealed class Pose3ObjectJsonConverter : JsonConverter<Pose3<double>>
{
    private static readonly Pose3ConverterD Both = new();
    public override Pose3<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Both.Read(ref reader, typeToConvert, options);
    public override void Write(Utf8JsonWriter writer, Pose3<double> v, JsonSerializerOptions options) =>
        JsonObjectForm.Write(writer, JsonObjectForm.Pose, [v.X, v.Y, v.Z, (double)v.Rx, (double)v.Ry, (double)v.Rz], options);
}

/// <summary>Reads a Joints6{double} as <c>[j1, …, j6]</c> or the object; writes the object (<see cref="JsonObjectForm"/>).</summary>
public sealed class Joints6ObjectJsonConverter : JsonConverter<Joints6<double>>
{
    private static readonly Joints6ConverterD Both = new();
    public override Joints6<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Both.Read(ref reader, typeToConvert, options);
    public override void Write(Utf8JsonWriter writer, Joints6<double> v, JsonSerializerOptions options) =>
        JsonObjectForm.Write(writer, JsonObjectForm.Joints,
            [(double)v.J1, (double)v.J2, (double)v.J3, (double)v.J4, (double)v.J5, (double)v.J6], options);
}

/// <summary>Reads a Point3{double} as <c>[x, y, z]</c> or the object; writes the object (<see cref="JsonObjectForm"/>).</summary>
public sealed class Point3ObjectJsonConverter : JsonConverter<Point3<double>>
{
    private static readonly Point3ConverterD Both = new();
    public override Point3<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Both.Read(ref reader, typeToConvert, options);
    public override void Write(Utf8JsonWriter writer, Point3<double> v, JsonSerializerOptions options) =>
        JsonObjectForm.Write(writer, JsonObjectForm.Xyz, [v.X, v.Y, v.Z], options);
}

/// <summary>Reads a Vector3{double} as <c>[x, y, z]</c> or the object; writes the object (<see cref="JsonObjectForm"/>).</summary>
public sealed class Vector3ObjectJsonConverter : JsonConverter<Vector3<double>>
{
    private static readonly Vector3ConverterD Both = new();
    public override Vector3<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Both.Read(ref reader, typeToConvert, options);
    public override void Write(Utf8JsonWriter writer, Vector3<double> v, JsonSerializerOptions options) =>
        JsonObjectForm.Write(writer, JsonObjectForm.Xyz, [v.X, v.Y, v.Z], options);
}
