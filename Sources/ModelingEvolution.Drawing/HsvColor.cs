using ProtoBuf;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter for HsvColor that supports both string and array formats.
/// </summary>
public class JsonHsvColorConverter : JsonConverter<HsvColor>
{
    /// <summary>
    /// Reads a JSON representation of an HsvColor.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The deserialized HsvColor.</returns>
    public override HsvColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return HsvColor.Parse(reader.GetString(), CultureInfo.InvariantCulture);
        }

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected string or array");

        var values = JsonSerializer.Deserialize<float[]>(ref reader, options);

        switch (values.Length)
        {
            case 3:
                if (values[0] < 0 || values[0] >= 360 || values[1] < 0 || values[1] > 1 || values[2] < 0 || values[2] > 1)
                    throw new JsonException("Invalid HSV values. H must be [0-360), S and V must be [0-1]");
                return new HsvColor(values[0], values[1], values[2]);

            case 4:
                if (values[0] < 0 || values[0] >= 360 || values[1] < 0 || values[1] > 1 ||
                    values[2] < 0 || values[2] > 1 || values[3] < 0 || values[3] > 1)
                    throw new JsonException("Invalid HSVA values. H must be [0-360), S, V and A must be [0-1]");
                return new HsvColor(values[0], values[1], values[2], values[3]);

            default:
                throw new JsonException("Array must contain 3 or 4 values");
        }
    }

    /// <summary>
    /// Writes an HsvColor to JSON format.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The HsvColor value to write.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, HsvColor value, JsonSerializerOptions options)
    {
        if (value.IsTransparent)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.H);
            writer.WriteNumberValue(value.S);
            writer.WriteNumberValue(value.V);
            writer.WriteNumberValue(value.A);
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.H);
            writer.WriteNumberValue(value.S);
            writer.WriteNumberValue(value.V);
            writer.WriteEndArray();
        }
    }
}
/// <summary>
/// Represents a color in HSV (Hue, Saturation, Value) color space with optional alpha channel.
/// </summary>
[JsonConverter(typeof(JsonHsvColorConverter))]
[ProtoContract]
public readonly struct HsvColor : IEquatable<HsvColor>, IParsable<HsvColor>
{
    /// <summary>
    /// Gets the hue component of the color in degrees (0-360).
    /// </summary>
    [ProtoMember(1)]
    public float H { get; init; }  // Hue: 0-360 degrees
    /// <summary>
    /// Gets the saturation component of the color (0-1).
    /// </summary>
    [ProtoMember(2)]
    public float S { get; init; }  // Saturation: 0-1
    /// <summary>
    /// Gets the value (brightness) component of the color (0-1).
    /// </summary>
    [ProtoMember(3)]
    public float V { get; init; }  // Value: 0-1

    /// <summary>
    /// Gets the alpha (transparency) component of the color (0-1).
    /// </summary>
    [ProtoMember(4)]
    public float A { get; }  // Alpha: 0-1

    /// <summary>
    /// Initializes a new instance of the HsvColor struct.
    /// </summary>
    /// <param name="hue">The hue component in degrees (0-360).</param>
    /// <param name="saturation">The saturation component (0-1).</param>
    /// <param name="value">The value (brightness) component (0-1).</param>
    /// <param name="alpha">The alpha (transparency) component (0-1). Default is 1.0 (opaque).</param>
    public HsvColor(float hue, float saturation, float value, float alpha = 1f)
    {
        H = hue % 360f;
        S = Math.Clamp(saturation, 0f, 1f);
        V = Math.Clamp(value, 0f, 1f);
        A = Math.Clamp(alpha, 0f, 1f);
    }
    /// <summary>
    /// Gets a value indicating whether the color has transparency (alpha less than 1.0).
    /// </summary>
    public bool IsTransparent => A < 1f;

    /// <summary>
    /// Implicitly converts an HSV tuple to an HsvColor.
    /// </summary>
    /// <param name="tuple">The tuple containing H, S, and V values.</param>
    /// <returns>An HsvColor with the specified HSV values and full opacity.</returns>
    public static implicit operator HsvColor((float h, float s, float v) tuple)
    {
        return new HsvColor(tuple.h, tuple.s, tuple.v);
    }

    /// <summary>
    /// Implicitly converts an HsvColor to an HSV tuple.
    /// </summary>
    /// <param name="color">The HsvColor to convert.</param>
    /// <returns>A tuple containing the H, S, and V values.</returns>
    public static implicit operator (float h, float s, float v)(HsvColor color)
    {
        return (color.H, color.S, color.V);
    }

    /// <summary>
    /// Returns a string representation of the HsvColor in hsv() or hsva() format.
    /// </summary>
    /// <returns>A string representation of the color.</returns>
    public override string ToString()
    {
        return IsTransparent
            ? string.Format(CultureInfo.InvariantCulture, "hsva({0:0.##},{1:0.##}%,{2:0.##}%,{3:0.##})", H, S * 100f, V * 100f, A)
            : string.Format(CultureInfo.InvariantCulture, "hsv({0:0.##},{1:0.##}%,{2:0.##}%)", H, S * 100f, V * 100f);

    }

    /// <summary>
    /// Parses a string representation of an HsvColor.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed HsvColor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null.</exception>
    /// <exception cref="FormatException">Thrown when the input string is not in a valid format.</exception>
    public static HsvColor Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        s = s.Trim().ToLowerInvariant();

        if (s.StartsWith("#") || s.StartsWith("0x") || s.StartsWith("0X"))
            return (HsvColor)Color.FromString(s);

        if (s.StartsWith("["))
            return JsonSerializer.Deserialize<HsvColor>(s);

        bool hasAlpha = s.StartsWith("hsva(");
        if (hasAlpha || s.StartsWith("hsv("))
        {
            var values = s[(hasAlpha ? 5 : 4)..^1].Split(',');
            if (values.Length != (hasAlpha ? 4 : 3))
                throw new FormatException($"Invalid format. Expected {(hasAlpha ? "hsva" : "hsv")}(hue,saturation,value{(hasAlpha ? ",alpha" : "")})");

            float ParseValue(string value, bool isPercentage = false)
            {
                value = value.Trim();
                bool hasPercent = value.EndsWith('%');
                if (hasPercent) value = value[..^1];

                float parsed = float.Parse(value, CultureInfo.InvariantCulture);
                return hasPercent ? parsed / 100f : (isPercentage ? parsed / 100f : parsed);
            }

            return new HsvColor(
                ParseValue(values[0]),
                ParseValue(values[1], true),
                ParseValue(values[2], true),
                hasAlpha ? ParseValue(values[3]) : 1f);
        }

        throw new FormatException("Invalid format. Expected hsv(360,100%,100%), hsv(360,1,1), hsva(360,100%,100%,1.0), [360,1,1] or #RRGGBB");
    }
    /// <summary>
    /// Tries to parse a string representation of an HsvColor.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The parsed HsvColor if successful; otherwise, the default value.</param>
    /// <returns>true if the parsing was successful; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string s,
        [MaybeNullWhen(false)] out HsvColor result)
    {
        return TryParse(s, null, out result);
    }
    /// <summary>
    /// Tries to parse a string representation of an HsvColor with a specified format provider.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">The parsed HsvColor if successful; otherwise, the default value.</param>
    /// <returns>true if the parsing was successful; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider? provider, [MaybeNullWhen(false)] out HsvColor result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim().ToLowerInvariant();

        if (s.StartsWith("#") && Color.TryParse(s, provider, out var color))
        {
            result = (HsvColor)color;
            return true;
        }

        if (s.StartsWith("["))
        {
            try
            {
                result = JsonSerializer.Deserialize<HsvColor>(s);
                return true;
            }
            catch { return false; }
        }

        bool hasAlpha = s.StartsWith("hsva(");
        if ((hasAlpha || s.StartsWith("hsv(")) && s.EndsWith(")"))
        {
            var values = s[(hasAlpha ? 5 : 4)..^1].Split(',');
            if (values.Length != (hasAlpha ? 4 : 3))
                return false;

            bool TryParseValue(string value, bool isPercentage, out float f)
            {
                f = 0;
                value = value.Trim();
                bool hasPercent = value.EndsWith('%');
                if (hasPercent) value = value[..^1];

                if (!float.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
                    return false;
                f =  hasPercent ? parsed / 100f : (isPercentage ? parsed / 100f : parsed);
                return true;
            }

            float h, ss, v, a = 1f;
            if (!TryParseValue(values[0], false, out h)) return false;
            if (!TryParseValue(values[1], true, out ss)) return false;
            if (!TryParseValue(values[2], true, out v)) return false;
            if (hasAlpha && !TryParseValue(values[3], false, out a)) return false;

            if (h < 0 || h >= 360 || ss < 0 || ss > 1 || v < 0 || v > 1 || a < 0 || a > 1)
                return false;
            
            result = new HsvColor(h, ss, v, a);
            return true;
        }

        return false;
    }
    /// <summary>
    /// Implicitly converts an HsvColor to a Color (RGB).
    /// </summary>
    /// <param name="hsv">The HsvColor to convert.</param>
    /// <returns>The equivalent Color in RGB format.</returns>
    public static implicit operator Color(in HsvColor hsv)
    {
        float h = hsv.H / 60f;
        float c = hsv.V * hsv.S;
        float x = c * (1 - MathF.Abs(h % 2 - 1));
        float m = hsv.V - c;

        float r, g, b;
        if (h < 1) (r, g, b) = (c, x, 0);
        else if (h < 2) (r, g, b) = (x, c, 0);
        else if (h < 3) (r, g, b) = (0, c, x);
        else if (h < 4) (r, g, b) = (0, x, c);
        else if (h < 5) (r, g, b) = (x, 0, c);
        else (r, g, b) = (c, 0, x);

        byte red = (byte)((r + m) * 255);
        byte green = (byte)((g + m) * 255);
        byte blue = (byte)((b + m) * 255);
        byte alpha = (byte)(hsv.A * 255);

        return Color.FromArgb(alpha, red, green, blue);
    }

    /// <summary>
    /// Implicitly converts a Color (RGB) to an HsvColor.
    /// </summary>
    /// <param name="color">The Color to convert.</param>
    /// <returns>The equivalent HsvColor.</returns>
    public static implicit operator HsvColor(Color color)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;
        float a = color.A / 255f;

        float max = MathF.Max(MathF.Max(r, g), b);
        float min = MathF.Min(MathF.Min(r, g), b);
        float delta = max - min;

        float h = 0;
        float s = max == 0 ? 0 : delta / max;
        float v = max;

        if (delta != 0)
        {
            if (MathF.Abs(max - r) < float.Epsilon)
                h = (g - b) / delta + (g < b ? 6 : 0);
            else if (MathF.Abs(max - g) < float.Epsilon)
                h = (b - r) / delta + 2;
            else
                h = (r - g) / delta + 4;

            h *= 60;
        }

        return new HsvColor(h, s, v, a);
    }
    /// <summary>
    /// Determines whether two HsvColor instances are equal.
    /// </summary>
    /// <param name="left">The first HsvColor to compare.</param>
    /// <param name="right">The second HsvColor to compare.</param>
    /// <returns>true if the colors are equal; otherwise, false.</returns>
    public static bool operator ==(in HsvColor left, in HsvColor right) =>
        MathF.Abs(left.H - right.H) < float.Epsilon && 
        MathF.Abs(left.S - right.S) < float.Epsilon && 
        MathF.Abs(left.V - right.V) < float.Epsilon && 
        MathF.Abs(left.A - right.A) < float.Epsilon;

    /// <summary>
    /// Determines whether two HsvColor instances are not equal.
    /// </summary>
    /// <param name="left">The first HsvColor to compare.</param>
    /// <param name="right">The second HsvColor to compare.</param>
    /// <returns>true if the colors are not equal; otherwise, false.</returns>
    public static bool operator !=(in HsvColor left, in HsvColor right) => !(left == right);

    /// <summary>
    /// Determines whether the specified object is equal to the current HsvColor.
    /// </summary>
    /// <param name="obj">The object to compare with the current HsvColor.</param>
    /// <returns>true if the specified object is equal to the current HsvColor; otherwise, false.</returns>
    public override bool Equals(object obj) => obj is HsvColor other && Equals(other);

    /// <summary>
    /// Determines whether the specified HsvColor is equal to the current HsvColor.
    /// </summary>
    /// <param name="other">The HsvColor to compare with the current HsvColor.</param>
    /// <returns>true if the specified HsvColor is equal to the current HsvColor; otherwise, false.</returns>
    public bool Equals(HsvColor other) => this == other;

    /// <summary>
    /// Returns the hash code for this HsvColor.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(H, S, V, A);
}