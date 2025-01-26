namespace ModelingEvolution.Drawing;


public static class Colors
{
    // Basic colors
    public static Color Black = Color.FromRgb(0, 0, 0);
    public static Color White = Color.FromRgb(255, 255, 255);
    public static Color Transparent = Color.FromArgb(0, 255, 255, 255);
    public static Color Red = Color.FromRgb(255, 0, 0);
    public static Color Green = Color.FromRgb(0, 255, 0);
    public static Color Blue = Color.FromRgb(0, 0, 255);

    // Gray shades
    public static Color Gray = Color.FromRgb(128, 128, 128);
    public static Color DarkGray = Color.FromRgb(64, 64, 64);
    public static Color LightGray = Color.FromRgb(192, 192, 192);

    // Primary variations
    public static Color DarkRed = Color.FromRgb(139, 0, 0);
    public static Color DarkGreen = Color.FromRgb(0, 100, 0);
    public static Color DarkBlue = Color.FromRgb(0, 0, 139);

    // Secondary colors
    public static Color Yellow = Color.FromRgb(255, 255, 0);
    public static Color Magenta = Color.FromRgb(255, 0, 255);
    public static Color Cyan = Color.FromRgb(0, 255, 255);

    // Common UI colors
    public static Color Orange = Color.FromRgb(255, 165, 0);
    public static Color Purple = Color.FromRgb(128, 0, 128);
    public static Color Brown = Color.FromRgb(165, 42, 42);
    public static Color Pink = Color.FromRgb(255, 192, 203);

    // Common web colors
    public static Color AliceBlue = Color.FromRgb(240, 248, 255);
    public static Color Coral = Color.FromRgb(255, 127, 80);
    public static Color CornflowerBlue = Color.FromRgb(100, 149, 237);
    public static Color ForestGreen = Color.FromRgb(34, 139, 34);
    public static Color Gold = Color.FromRgb(255, 215, 0);
    public static Color IndianRed = Color.FromRgb(205, 92, 92);
    public static Color Lavender = Color.FromRgb(230, 230, 250);
    public static Color LimeGreen = Color.FromRgb(50, 205, 50);
    public static Color Navy = Color.FromRgb(0, 0, 128);
    public static Color Plum = Color.FromRgb(221, 160, 221);

    public static IEnumerable<Color> GrayShades(int steps = 256)
    {
        for (int i = 0; i < steps; i++)
        {
            byte value = (byte)(i * 255.0 / (steps - 1));
            yield return Color.FromRgb(value, value, value);
        }
    }

    public static IEnumerable<Color> Rainbow(int steps = 360)
    {
        for (int i = 0; i < steps; i++)
        {
            yield return (Color)new HsvColor(i * 360f / steps, 1, 1);
        }
    }

    public static IEnumerable<Color> Palette(Color baseColor, int steps = 5)
    {
        var hsv = (HsvColor)baseColor;

        // Original color
        yield return baseColor;

        // Darker shades
        for (int i = 1; i < steps; i++)
        {
            float value = hsv.V * (1 - (i / (float)steps));
            yield return (Color)new HsvColor(hsv.H, hsv.S, value);
        }

        // Reset value, vary saturation for pastel variants
        for (int i = 1; i < steps; i++)
        {
            float saturation = hsv.S * (1 - (i / (float)steps));
            yield return (Color)new HsvColor(hsv.H, saturation, hsv.V);
        }
    }

    public static IEnumerable<Color> Complementary(Color baseColor)
    {
        var hsv = (HsvColor)baseColor;
        yield return baseColor;
        yield return (Color)new HsvColor((hsv.H + 180) % 360, hsv.S, hsv.V);
    }

    public static IEnumerable<Color> Triadic(Color baseColor)
    {
        var hsv = (HsvColor)baseColor;
        yield return baseColor;
        yield return (Color)new HsvColor((hsv.H + 120) % 360, hsv.S, hsv.V);
        yield return (Color)new HsvColor((hsv.H + 240) % 360, hsv.S, hsv.V);
    }
}