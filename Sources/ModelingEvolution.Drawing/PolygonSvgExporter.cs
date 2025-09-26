using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// SVG exporter for Polygon{T} objects that generates SVG path elements.
/// </summary>
/// <typeparam name="T">The numeric type used for polygon coordinates.</typeparam>
public class PolygonSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Exports a Polygon{T} to SVG format as a closed path element.
    /// </summary>
    /// <param name="obj">The polygon object to export (must be of type Polygon{T}).</param>
    /// <param name="paint">The paint settings for fill, stroke, and stroke width.</param>
    /// <returns>An SVG path element string representing the polygon.</returns>
    public string Export(object obj, in SvgPaint paint)
    {
        var polygon = (Polygon<T>)obj;
        var points = polygon.Points;

        if (points.Count < 2) return string.Empty;

        var sb = new StringBuilder("<path ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "fill=\"{0}\" stroke=\"{1}\" stroke-width=\"{2}\" d=\"",
            paint.Fill, paint.Stroke, paint.StrokeWidth);

        sb.AppendFormat(CultureInfo.InvariantCulture, "M{0},{1}", Convert.ToDouble(points[0].X), Convert.ToDouble(points[0].Y));

        for (int i = 1; i < points.Count; i++)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, " L{0},{1}", Convert.ToDouble(points[i].X), Convert.ToDouble(points[i].Y));
        }

        sb.Append(" Z\"/>");
        return sb.ToString();
    }
}