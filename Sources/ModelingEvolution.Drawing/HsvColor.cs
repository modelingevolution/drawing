using ProtoBuf;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public class JsonHsvColorConverter : JsonConverter<HsvColor>
{
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
[JsonConverter(typeof(JsonHsvColorConverter))]
[ProtoContract]
public readonly struct HsvColor : IEquatable<HsvColor>, IParsable<HsvColor>
{
    [ProtoMember(1)]
    public float H { get; init; }  // Hue: 0-360 degrees
    [ProtoMember(2)]
    public float S { get; init; }  // Saturation: 0-1
    [ProtoMember(3)]
    public float V { get; init; }  // Value: 0-1

    [ProtoMember(4)]
    public float A { get; }  // Alpha: 0-1

    public HsvColor(float hue, float saturation, float value, float alpha = 1f)
    {
        H = hue % 360f;
        S = Math.Clamp(saturation, 0f, 1f);
        V = Math.Clamp(value, 0f, 1f);
        A = Math.Clamp(alpha, 0f, 1f);
    }
    public bool IsTransparent => A < 1f;

    public override string ToString()
    {
        return IsTransparent
            ? string.Format(CultureInfo.InvariantCulture, "hsva({0:0.##},{1:0.##}%,{2:0.##}%,{3:0.##})", H, S * 100f, V * 100f, A)
            : string.Format(CultureInfo.InvariantCulture, "hsv({0:0.##},{1:0.##}%,{2:0.##}%)", H, S * 100f, V * 100f);

    }

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
    public static bool TryParse([NotNullWhen(true)] string s,
        [MaybeNullWhen(false)] out HsvColor result)
    {
        return TryParse(s, null, out result);
    }
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
    public static bool operator ==(in HsvColor left, in HsvColor right) =>
        MathF.Abs(left.H - right.H) < float.Epsilon && 
        MathF.Abs(left.S - right.S) < float.Epsilon && 
        MathF.Abs(left.V - right.V) < float.Epsilon && 
        MathF.Abs(left.A - right.A) < float.Epsilon;

    public static bool operator !=(in HsvColor left, in HsvColor right) => !(left == right);

    public override bool Equals(object obj) => obj is HsvColor other && Equals(other);

    public bool Equals(HsvColor other) => this == other;

    public override int GetHashCode() => HashCode.Combine(H, S, V, A);
}