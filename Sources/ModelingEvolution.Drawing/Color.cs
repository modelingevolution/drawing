using ProtoBuf;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

[Serializable]
[JsonConverter(typeof(JsonColorConverter))]
[TypeConverter(typeof(ColorConverter))]
[ProtoContract]
public readonly record struct Color : IEquatable<Color>, IParsable<Color>
{
    internal const int ARGBAlphaShift = 24;
    internal const int ARGBRedShift = 16;
    internal const int ARGBGreenShift = 8;
    internal const int ARGBBlueShift = 0;
    internal const uint ARGBAlphaMask = 0xFFu << ARGBAlphaShift;
    internal const uint ARGBRedMask = 0xFFu << ARGBRedShift;
    internal const uint ARGBGreenMask = 0xFFu << ARGBGreenShift;
    internal const uint ARGBBlueMask = 0xFFu << ARGBBlueShift;


    public static Color Parse(string s, IFormatProvider provider=null)
    {
        ArgumentNullException.ThrowIfNull(s);

        if (s.StartsWith("hsv"))
            return (Color)HsvColor.Parse(s, provider);

        if (s.StartsWith("rgba("))
        {
            var values = s[5..^1].Split(',');
            if (values.Length != 4)
                throw new FormatException("Invalid rgba format. Expected rgba(r,g,b,a)");

            byte r = byte.Parse(values[0], CultureInfo.InvariantCulture);
            byte g = byte.Parse(values[1], CultureInfo.InvariantCulture);
            byte b = byte.Parse(values[2], CultureInfo.InvariantCulture);
            float a = float.Parse(values[3], CultureInfo.InvariantCulture);

            return FromArgb((byte)(a * 255), r, g, b);
        }

        uint alpha = 255;
        if (s.StartsWith("0x") || s.StartsWith("0X")) s = s[2..];

        if (s.StartsWith("#"))
            s = s[1..];
        if (s.Length == 8) 
            alpha = 0;
        var value = Convert.ToUInt32(s , 16);
        return new Color(value | alpha << ARGBAlphaShift);
    }

    public static bool TryParse([NotNullWhen(true)] string s, 
        [MaybeNullWhen(false)] out Color result)
    {
        return TryParse(s, null, out result);
    }
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider? provider, [MaybeNullWhen(false)] out Color result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        if (s.StartsWith("hsv"))
        {
            var r = HsvColor.TryParse(s, provider, out var c);
            result = c;
            return r;
        }
        if (s.StartsWith("rgba(") && s.EndsWith(")"))
        {
            var values = s[5..^1].Split(',');
            if (values.Length != 4) return false;

            if (!byte.TryParse(values[0], provider, out var r)) return false;
            if (!byte.TryParse(values[1], provider, out var g)) return false;
            if (!byte.TryParse(values[2], provider, out var b)) return false;
            if (!float.TryParse(values[3], provider, out var a)) return false;

            result = FromArgb((byte)(a * 255), r, g, b);
            return true;
        }
        s = s.StartsWith("0x") || s.StartsWith("0X") ? s[2..] : s;
        s = s.StartsWith("#") ? s[1..] : s;

        if (!uint.TryParse(s, NumberStyles.HexNumber, provider, out var value)) return false;
        
        result = new Color(value);
        return true;

    }

    public static implicit operator Color(string hex) => FromString(hex);
    [ProtoMember(1)]
    public uint Value { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Color FromKnownColor(KnownColor color)
    {
        return new Color((uint)color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetRgbValues(out int r, out int g, out int b)
    {
        uint value = (uint)Value;
        r = (int)(value & ARGBRedMask) >> ARGBRedShift;
        g = (int)(value & ARGBGreenMask) >> ARGBGreenShift;
        b = (int)(value & ARGBBlueMask) >> ARGBBlueShift;
    }

    public byte R => unchecked((byte)(Value >> ARGBRedShift));

    public byte G => unchecked((byte)(Value >> ARGBGreenShift));

    public byte B => unchecked((byte)(Value >> ARGBBlueShift));

    public byte A => unchecked((byte)(Value >> ARGBAlphaShift));

    public Color(uint value)
    {
        this.Value = value;
    }
    // The L in HSL
    public float GetLightness()
    {
        GetRgbValues(out int r, out int g, out int b);
        int min = Math.Min(Math.Min(r, g), b);
        int max = Math.Max(Math.Max(r, g), b);

        return (max + min) / (byte.MaxValue * 2f);
    }
    public float GetBrightness()
    {
        return (0.299f * R + 0.587f * G + 0.114f * B) / 255f;
    }

    public float GetHue()
    {
        GetRgbValues(out int r, out int g, out int b);

        if (r == g && g == b)
            return 0f;

        int min = Math.Min(Math.Min(r, g), b);
        int max = Math.Max(Math.Max(r, g), b);

        float delta = max - min;
        float hue;

        if (r == max)
            hue = (g - b) / delta;
        else if (g == max)
            hue = (b - r) / delta + 2f;
        else
            hue = (r - g) / delta + 4f;

        hue *= 60f;
        if (hue < 0f)
            hue += 360f;

        return hue;
    }
    /// <summary>
    /// Gets a value indicating whether this instance is fully or partly transparent.
    /// </summary>
    public bool IsTransparent
    {
        get => A < 255;
    }
   
    public float GetSaturation()
    {
        GetRgbValues(out int r, out int g, out int b);

        if (r == g && g == b)
            return 0f;

        int min = Math.Min(Math.Min(r, g), b);
        int max = Math.Max(Math.Max(r, g), b);

        int div = max + min;
        if (div > byte.MaxValue)
            div = byte.MaxValue * 2 - max - min;

        return (max - min) / (float)div;
    }

    public static Color FromString(string hex) => Parse(hex, CultureInfo.InvariantCulture);

    public static Color FromArgb(int alpha, int red, int green, int blue)
    {
        return new Color((uint)alpha << ARGBAlphaShift |
                         (uint)red << ARGBRedShift |
                         (uint)green << ARGBGreenShift |
                         (uint)blue << ARGBBlueShift);
    }
    public static Color FromRgb(int red, int green, int blue)
    {
        return new Color((uint)255 << ARGBAlphaShift |
                         (uint)red << ARGBRedShift |
                         (uint)green << ARGBGreenShift |
                         (uint)blue << ARGBBlueShift);
    }
    
    // A == 0 - the color is fully transparent, A< 255 is partially transparent.
    public string ToJson()
    {
        return IsTransparent ? $"#{Value:x8}" : $"#{Value:x6}";
    }
    public override string ToString()
    {
        // rgba(,,,1) - color is not transparent
        // rgba(,,,0) - color is transparent
        //return A > 0 ? $"#{Value:x8}" : $"#{Value:x6}";
        return IsTransparent ? $"rgba({R},{G},{B},{(A / 255f).ToString(EN_US)})" : $"#{(Value & ~ARGBAlphaMask):x6}";
    }
    private static readonly CultureInfo EN_US = new CultureInfo("en-US");
    public Color MakeTransparent(float d)
    {
        float value = MathF.Min(MathF.Max(0, d), 1);
        float alpha = (float)(this.A) * value;
        return Color.FromArgb((byte)alpha, this.R, this.G, this.B);
    }

        
}