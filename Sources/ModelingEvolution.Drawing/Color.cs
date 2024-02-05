using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

[Serializable]
[JsonConverter(typeof(JsonColorConverter))]
[TypeConverter(typeof(ColorConverter))]
public readonly struct Color : IEquatable<Color>
{
    internal const int ARGBAlphaShift = 24;
    internal const int ARGBRedShift = 16;
    internal const int ARGBGreenShift = 8;
    internal const int ARGBBlueShift = 0;
    internal const uint ARGBAlphaMask = 0xFFu << ARGBAlphaShift;
    internal const uint ARGBRedMask = 0xFFu << ARGBRedShift;
    internal const uint ARGBGreenMask = 0xFFu << ARGBGreenShift;
    internal const uint ARGBBlueMask = 0xFFu << ARGBBlueShift;
        
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
    public float GetBrightness()
    {
        GetRgbValues(out int r, out int g, out int b);

        int min = Math.Min(Math.Min(r, g), b);
        int max = Math.Max(Math.Max(r, g), b);

        return (max + min) / (byte.MaxValue * 2f);
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
    public bool IsTransparent
    {
        get => A < 255;
    }
    public static bool operator ==(Color left, Color right) =>
        left.Value == right.Value;

    public static bool operator !=(Color left, Color right) => !(left == right);

    public override bool Equals(object obj) => obj is Color other && Equals(other);

    public bool Equals(Color other) => this == other;

    public override int GetHashCode()
    {
        return this.Value.GetHashCode();
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

    public static Color FromString(string hex)
    {
        if (hex.StartsWith("0x") || hex.StartsWith("0X"))
            hex = hex.Substring(2);

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        var value = Convert.ToUInt32(hex, 16);
        return new Color(value);
    }
    public static Color FromArgb(int alpha, int red, int green, int blue)
    {
        return new Color((uint)alpha << ARGBAlphaShift |
                         (uint)red << ARGBRedShift |
                         (uint)green << ARGBGreenShift |
                         (uint)blue << ARGBBlueShift);
    }
    // A == 255 - the color is fully transparent
    public string ToJson()
    {
        return IsTransparent ? $"#{Value:x8}" : $"#{Value:x6}";
    }
    public override string ToString()
    {
        // rgba(,,,1) - color is not transparent
        // rgba(,,,0) - color is transparent
        //return A > 0 ? $"#{Value:x8}" : $"#{Value:x6}";
        return IsTransparent ? $"rgba({R},{G},{B},{(A / 255f).ToString(EN_US)})" : $"#{Value:x6}";
    }
    private static readonly CultureInfo EN_US = new CultureInfo("en-US");
    public Color MakeTransparent(float d)
    {
        float value = Math.Min(Math.Max(0, d), 1);
        float alpha = (float)(this.A) * value;
        return Color.FromArgb((byte)alpha, this.R, this.G, this.B);
    }

        
}