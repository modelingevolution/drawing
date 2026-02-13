using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for <see cref="Skeleton{T}"/> that serializes as an object with
/// "nodes" (flat coordinate array) and "edges" (flat coordinate array of segment endpoints).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public class SkeletonJsonConverter<T> : JsonConverter<Skeleton<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Action<Utf8JsonWriter, T> _writeNumber;
    private readonly Func<Utf8JsonReader, T> _readNumber;

    /// <summary>
    /// Initializes a new converter with the specified number read/write delegates.
    /// </summary>
    /// <param name="writeNumber">Delegate that writes a numeric value to the JSON writer.</param>
    /// <param name="readNumber">Delegate that reads a numeric value from the JSON reader.</param>
    public SkeletonJsonConverter(Action<Utf8JsonWriter, T> writeNumber, Func<Utf8JsonReader, T> readNumber)
    {
        _writeNumber = writeNumber;
        _readNumber = readNumber;
    }

    /// <inheritdoc />
    public override Skeleton<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object start");

        Point<T>[]? nodes = null;
        Segment<T>[]? edges = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "nodes":
                    nodes = ReadNodes(ref reader);
                    break;
                case "edges":
                    edges = ReadEdges(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new Skeleton<T>(
            nodes ?? Array.Empty<Point<T>>(),
            edges ?? Array.Empty<Segment<T>>());
    }

    private Point<T>[] ReadNodes(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for nodes");

        var coords = new List<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            coords.Add(_readNumber(reader));
        }

        if (coords.Count % 2 != 0)
            throw new JsonException("Nodes array length must be even");

        var nodes = new Point<T>[coords.Count / 2];
        for (int i = 0; i < coords.Count; i += 2)
            nodes[i / 2] = new Point<T>(coords[i], coords[i + 1]);
        return nodes;
    }

    private Segment<T>[] ReadEdges(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for edges");

        var coords = new List<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            coords.Add(_readNumber(reader));
        }

        if (coords.Count % 4 != 0)
            throw new JsonException("Edges array length must be divisible by 4");

        var edges = new Segment<T>[coords.Count / 4];
        for (int i = 0; i < coords.Count; i += 4)
            edges[i / 4] = new Segment<T>(
                new Point<T>(coords[i], coords[i + 1]),
                new Point<T>(coords[i + 2], coords[i + 3]));
        return edges;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Skeleton<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();
        if (value._nodes != null)
        {
            foreach (var node in value._nodes)
            {
                _writeNumber(writer, node.X);
                _writeNumber(writer, node.Y);
            }
        }
        writer.WriteEndArray();

        writer.WritePropertyName("edges");
        writer.WriteStartArray();
        if (value._edges != null)
        {
            foreach (var edge in value._edges)
            {
                _writeNumber(writer, edge.Start.X);
                _writeNumber(writer, edge.Start.Y);
                _writeNumber(writer, edge.End.X);
                _writeNumber(writer, edge.End.Y);
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
