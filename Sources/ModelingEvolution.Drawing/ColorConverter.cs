using System.ComponentModel;
using System.Globalization;

namespace ModelingEvolution.Drawing;

public class ColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(uint) || sourceType == typeof(int))
            return true;
        return base.CanConvertFrom(context, sourceType);
    }

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

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string) || destinationType == typeof(uint) || destinationType == typeof(int))
            return true;
        return base.CanConvertTo(context, destinationType);
    }
}