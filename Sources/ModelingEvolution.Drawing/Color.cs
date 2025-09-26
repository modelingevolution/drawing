using ProtoBuf;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a color with ARGB (alpha, red, green, blue) components.
/// </summary>
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


    /// <summary>
    /// Parses a string representation of a color.
    /// </summary>
    /// <param name="s">The string to parse (supports hex, rgba, and hsv formats).</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed color.</returns>
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

    /// <summary>
    /// Tries to parse a string representation of a color.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed color if successful.</param>
    /// <returns>true if the parsing was successful; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string s, 
        [MaybeNullWhen(false)] out Color result)
    {
        return TryParse(s, null, out result);
    }
    /// <summary>
    /// Tries to parse a string representation of a color with a format provider.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">When this method returns, contains the parsed color if successful.</param>
    /// <returns>true if the parsing was successful; otherwise, false.</returns>
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

    /// <summary>
    /// Implicitly converts a string to a Color.
    /// </summary>
    /// <param name="hex">The string representation of the color.</param>
    public static implicit operator Color(string hex) => FromString(hex);
    /// <summary>
    /// Gets the ARGB value of the color as a 32-bit unsigned integer.
    /// </summary>
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

    /// <summary>
    /// Gets the red component value of the color.
    /// </summary>
    public byte R => unchecked((byte)(Value >> ARGBRedShift));

    /// <summary>
    /// Gets the green component value of the color.
    /// </summary>
    public byte G => unchecked((byte)(Value >> ARGBGreenShift));

    /// <summary>
    /// Gets the blue component value of the color.
    /// </summary>
    public byte B => unchecked((byte)(Value >> ARGBBlueShift));

    /// <summary>
    /// Gets the alpha component value of the color.
    /// </summary>
    public byte A => unchecked((byte)(Value >> ARGBAlphaShift));

    /// <summary>
    /// Initializes a new instance of the Color struct from a 32-bit ARGB value.
    /// </summary>
    /// <param name="value">The 32-bit ARGB value.</param>
    public Color(uint value)
    {
        this.Value = value;
    }
    /// <summary>
    /// Gets the lightness component (L in HSL) of the color.
    /// </summary>
    /// <returns>The lightness value between 0 and 1.</returns>
    public float GetLightness()
    {
        GetRgbValues(out int r, out int g, out int b);
        int min = Math.Min(Math.Min(r, g), b);
        int max = Math.Max(Math.Max(r, g), b);

        return (max + min) / (byte.MaxValue * 2f);
    }
    /// <summary>
    /// Gets the brightness of the color.
    /// </summary>
    /// <returns>The brightness value between 0 and 1.</returns>
    public float GetBrightness()
    {
        return (0.299f * R + 0.587f * G + 0.114f * B) / 255f;
    }

    /// <summary>
    /// Gets the hue component of the color.
    /// </summary>
    /// <returns>The hue value in degrees (0-360).</returns>
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
   
    /// <summary>
    /// Gets the saturation component of the color.
    /// </summary>
    /// <returns>The saturation value between 0 and 1.</returns>
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

    /// <summary>
    /// Creates a Color from a string representation.
    /// </summary>
    /// <param name="hex">The string representation of the color.</param>
    /// <returns>The created color.</returns>
    public static Color FromString(string hex) => Parse(hex, CultureInfo.InvariantCulture);

    /// <summary>
    /// Creates a Color from ARGB components.
    /// </summary>
    /// <param name="alpha">The alpha component (0-255).</param>
    /// <param name="red">The red component (0-255).</param>
    /// <param name="green">The green component (0-255).</param>
    /// <param name="blue">The blue component (0-255).</param>
    /// <returns>The created color.</returns>
    public static Color FromArgb(int alpha, int red, int green, int blue)
    {
        return new Color((uint)alpha << ARGBAlphaShift |
                         (uint)red << ARGBRedShift |
                         (uint)green << ARGBGreenShift |
                         (uint)blue << ARGBBlueShift);
    }
    /// <summary>
    /// Creates a Color from RGB components with full opacity.
    /// </summary>
    /// <param name="red">The red component (0-255).</param>
    /// <param name="green">The green component (0-255).</param>
    /// <param name="blue">The blue component (0-255).</param>
    /// <returns>The created color.</returns>
    public static Color FromRgb(int red, int green, int blue)
    {
        return new Color((uint)255 << ARGBAlphaShift |
                         (uint)red << ARGBRedShift |
                         (uint)green << ARGBGreenShift |
                         (uint)blue << ARGBBlueShift);
    }
    
    /// <summary>
    /// Converts the color to a JSON string representation.
    /// </summary>
    /// <returns>A JSON-compatible string representation of the color.</returns>
    public string ToJson()
    {
        return IsTransparent ? $"#{Value:x8}" : $"#{Value:x6}";
    }
    /// <summary>
    /// Returns a string representation of the color.
    /// </summary>
    /// <returns>A string representation in rgba or hex format.</returns>
    public override string ToString()
    {
        // rgba(,,,1) - color is not transparent
        // rgba(,,,0) - color is transparent
        //return A > 0 ? $"#{Value:x8}" : $"#{Value:x6}";
        return IsTransparent ? $"rgba({R},{G},{B},{(A / 255f).ToString(EN_US)})" : $"#{(Value & ~ARGBAlphaMask):x6}";
    }
    private static readonly CultureInfo EN_US = new CultureInfo("en-US");
    /// <summary>
    /// Creates a new color with adjusted transparency.
    /// </summary>
    /// <param name="d">The transparency factor (0 = fully transparent, 1 = original alpha).</param>
    /// <returns>A new color with adjusted transparency.</returns>
    public Color MakeTransparent(float d)
    {
        float value = MathF.Min(MathF.Max(0, d), 1);
        float alpha = (float)(this.A) * value;
        return Color.FromArgb((byte)alpha, this.R, this.G, this.B);
    }

        
}