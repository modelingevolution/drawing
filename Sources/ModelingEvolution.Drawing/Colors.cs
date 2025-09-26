namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides predefined colors and color generation utilities.
/// </summary>
public static class Colors
{
    // Basic colors
    /// <summary>
    /// Gets the color black.
    /// </summary>
    public static Color Black = Color.FromRgb(0, 0, 0);
    /// <summary>
    /// Gets the color white.
    /// </summary>
    public static Color White = Color.FromRgb(255, 255, 255);
    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static Color Transparent = Color.FromArgb(0, 255, 255, 255);
    /// <summary>
    /// Gets the color red.
    /// </summary>
    public static Color Red = Color.FromRgb(255, 0, 0);
    /// <summary>
    /// Gets the color green.
    /// </summary>
    public static Color Green = Color.FromRgb(0, 255, 0);
    /// <summary>
    /// Gets the color blue.
    /// </summary>
    public static Color Blue = Color.FromRgb(0, 0, 255);

    // Gray shades
    /// <summary>
    /// Gets the color gray.
    /// </summary>
    public static Color Gray = Color.FromRgb(128, 128, 128);
    /// <summary>
    /// Gets the color dark gray.
    /// </summary>
    public static Color DarkGray = Color.FromRgb(64, 64, 64);
    /// <summary>
    /// Gets the color light gray.
    /// </summary>
    public static Color LightGray = Color.FromRgb(192, 192, 192);

    // Primary variations
    /// <summary>
    /// Gets the color dark red.
    /// </summary>
    public static Color DarkRed = Color.FromRgb(139, 0, 0);
    /// <summary>
    /// Gets the color dark green.
    /// </summary>
    public static Color DarkGreen = Color.FromRgb(0, 100, 0);
    /// <summary>
    /// Gets the color dark blue.
    /// </summary>
    public static Color DarkBlue = Color.FromRgb(0, 0, 139);

    // Secondary colors
    /// <summary>
    /// Gets the color yellow.
    /// </summary>
    public static Color Yellow = Color.FromRgb(255, 255, 0);
    /// <summary>
    /// Gets the color magenta.
    /// </summary>
    public static Color Magenta = Color.FromRgb(255, 0, 255);
    /// <summary>
    /// Gets the color cyan.
    /// </summary>
    public static Color Cyan = Color.FromRgb(0, 255, 255);

    // Common UI colors
    /// <summary>
    /// Gets the color orange.
    /// </summary>
    public static Color Orange = Color.FromRgb(255, 165, 0);
    /// <summary>
    /// Gets the color purple.
    /// </summary>
    public static Color Purple = Color.FromRgb(128, 0, 128);
    /// <summary>
    /// Gets the color brown.
    /// </summary>
    public static Color Brown = Color.FromRgb(165, 42, 42);
    /// <summary>
    /// Gets the color pink.
    /// </summary>
    public static Color Pink = Color.FromRgb(255, 192, 203);

    // Common web colors
    /// <summary>
    /// Gets the color alice blue.
    /// </summary>
    public static Color AliceBlue = Color.FromRgb(240, 248, 255);
    /// <summary>
    /// Gets the color coral.
    /// </summary>
    public static Color Coral = Color.FromRgb(255, 127, 80);
    /// <summary>
    /// Gets the color cornflower blue.
    /// </summary>
    public static Color CornflowerBlue = Color.FromRgb(100, 149, 237);
    /// <summary>
    /// Gets the color forest green.
    /// </summary>
    public static Color ForestGreen = Color.FromRgb(34, 139, 34);
    /// <summary>
    /// Gets the color gold.
    /// </summary>
    public static Color Gold = Color.FromRgb(255, 215, 0);
    /// <summary>
    /// Gets the color indian red.
    /// </summary>
    public static Color IndianRed = Color.FromRgb(205, 92, 92);
    /// <summary>
    /// Gets the color lavender.
    /// </summary>
    public static Color Lavender = Color.FromRgb(230, 230, 250);
    /// <summary>
    /// Gets the color lime green.
    /// </summary>
    public static Color LimeGreen = Color.FromRgb(50, 205, 50);
    /// <summary>
    /// Gets the color navy.
    /// </summary>
    public static Color Navy = Color.FromRgb(0, 0, 128);
    /// <summary>
    /// Gets the color plum.
    /// </summary>
    public static Color Plum = Color.FromRgb(221, 160, 221);

    /// <summary>
    /// Generates a sequence of gray shades.
    /// </summary>
    /// <param name="steps">The number of shades to generate (default: 256).</param>
    /// <returns>An enumerable of gray colors from black to white.</returns>
    public static IEnumerable<Color> GrayShades(int steps = 256)
    {
        for (int i = 0; i < steps; i++)
        {
            byte value = (byte)(i * 255.0 / (steps - 1));
            yield return Color.FromRgb(value, value, value);
        }
    }

    /// <summary>
    /// Generates a rainbow spectrum of colors.
    /// </summary>
    /// <param name="steps">The number of colors to generate (default: 360).</param>
    /// <returns>An enumerable of colors representing the rainbow spectrum.</returns>
    public static IEnumerable<Color> Rainbow(int steps = 360)
    {
        for (int i = 0; i < steps; i++)
        {
            yield return (Color)new HsvColor(i * 360f / steps, 1, 1);
        }
    }

    /// <summary>
    /// Generates a palette based on a base color.
    /// </summary>
    /// <param name="baseColor">The base color for palette generation.</param>
    /// <param name="steps">The number of variations to generate (default: 5).</param>
    /// <returns>An enumerable of color variations.</returns>
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

    /// <summary>
    /// Generates complementary colors for the given base color.
    /// </summary>
    /// <param name="baseColor">The base color.</param>
    /// <returns>The base color and its complement.</returns>
    public static IEnumerable<Color> Complementary(Color baseColor)
    {
        var hsv = (HsvColor)baseColor;
        yield return baseColor;
        yield return (Color)new HsvColor((hsv.H + 180) % 360, hsv.S, hsv.V);
    }

    /// <summary>
    /// Generates triadic colors for the given base color.
    /// </summary>
    /// <param name="baseColor">The base color.</param>
    /// <returns>Three colors forming a triadic color scheme.</returns>
    public static IEnumerable<Color> Triadic(Color baseColor)
    {
        var hsv = (HsvColor)baseColor;
        yield return baseColor;
        yield return (Color)new HsvColor((hsv.H + 120) % 360, hsv.S, hsv.V);
        yield return (Color)new HsvColor((hsv.H + 240) % 360, hsv.S, hsv.V);
    }
}