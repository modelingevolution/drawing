using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// SVG exporter for ComplexCurve{T} that generates SVG path elements.
/// </summary>
public class ComplexCurveSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    public string Export(object obj, in SvgPaint paint)
    {
        var curve = (ComplexCurve<T>)obj;

        if (curve.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder("<path ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "fill=\"{0}\" stroke=\"{1}\" stroke-width=\"{2}\" d=\"",
            paint.Fill, paint.Stroke, paint.StrokeWidth);

        sb.Append(curve.ToString());

        sb.Append("\"/>");
        return sb.ToString();
    }
}
