using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

public class PolygonSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
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