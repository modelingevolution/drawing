using System.ComponentModel;
using System.Globalization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Provides type conversion capabilities for the Color struct.
/// </summary>
public class ColorConverter : TypeConverter
{
    /// <summary>
    /// Determines whether this converter can convert from the specified type.
    /// </summary>
    /// <param name="context">The type descriptor context.</param>
    /// <param name="sourceType">The type to convert from.</param>
    /// <returns>true if conversion is possible; otherwise, false.</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(uint) || sourceType == typeof(int))
            return true;
        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    /// Converts the given value to a Color.
    /// </summary>
    /// <param name="context">The type descriptor context.</param>
    /// <param name="culture">The culture info.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted Color object.</returns>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        switch (value)
        {
            case string s:
                return Color.FromString(s);
            case uint ui:
                return new Color(ui);
            case int i:
                return new Color((uint)i);
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>
    /// Converts a Color to the specified type.
    /// </summary>
    /// <param name="context">The type descriptor context.</param>
    /// <param name="culture">The culture info.</param>
    /// <param name="value">The Color value to convert.</param>
    /// <param name="destinationType">The destination type.</param>
    /// <returns>The converted value.</returns>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        var c = (Color)value;
        if (destinationType == typeof(string))
            return c.ToString();

        else if (destinationType == typeof(int))
            return (int)c.Value;

        else if (destinationType == typeof(uint))
            return c.Value;

        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>
    /// Determines whether this converter can convert to the specified type.
    /// </summary>
    /// <param name="context">The type descriptor context.</param>
    /// <param name="destinationType">The type to convert to.</param>
    /// <returns>true if conversion is possible; otherwise, false.</returns>
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string) || destinationType == typeof(uint) || destinationType == typeof(int))
            return true;
        return base.CanConvertTo(context, destinationType);
    }
}