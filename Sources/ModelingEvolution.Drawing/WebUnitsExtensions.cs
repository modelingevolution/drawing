using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public static class CollectionExtensions
{
    public static Vector<double> Sum(this IEnumerable<Vector<double>> items)
    {

        Vector<double> sum = Vector<double>.Zero;

        foreach (var value in items)
            sum += value;

        return sum;
    }
    public static Vector<double> Sum(this IEnumerable<Vector<float>> items)
    {

        Vector<double> sum = Vector<double>.Zero;
        
        foreach (var value in items) 
            sum += value.Truncating<double>();

        return sum;
    }
    public static Vector<double> Avg(this IEnumerable<Vector<float>> items)
    {
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        foreach (var value in items)
        {
            sum += value.Truncating<double>();
            c += 1.0d;
        }

        return sum / c;
    }
    public static Vector<double> Avg(this IEnumerable<Vector<double>> items)
    {
        Vector<double> sum = Vector<double>.Zero;
        double c = 0;
        foreach (var value in items)
        {
            sum += value;
            c += 1.0d;
        }

        return sum / c;
    }
}
internal static class WebUnitsExtensions
{
    public static string ToPx(this int val) => $"{val}px";
    public static string ToPx(this int? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this long val) => $"{val}px";
    public static string ToPx(this long? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this double val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this double? val) => val != null ? val.Value.ToPx() : string.Empty;
    public static string ToPx(this float val) => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx<T>(this T val) where T:IFloatingPointIeee754<T> => $"{val.ToString("0.##", CultureInfo.InvariantCulture)}px";
    public static string ToPx(this float? val) => val != null ? val.Value.ToPx() : string.Empty;
}