using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// SVG exporter for Path{T} objects that generates SVG path elements.
/// </summary>
/// <typeparam name="T">The numeric type used for path coordinates.</typeparam>
public class PathSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Exports a Path{T} to SVG format as a path element with cubic Bezier curves.
    /// </summary>
    /// <param name="obj">The path object to export (must be of type Path{T}).</param>
    /// <param name="paint">The paint settings for fill, stroke, and stroke width.</param>
    /// <returns>An SVG path element string representing the path.</returns>
    public string Export(object obj, in SvgPaint paint)
    {
        var path = (Path<T>)obj;

        if (path.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder("<path ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "fill=\"{0}\" stroke=\"{1}\" stroke-width=\"{2}\" d=\"",
            paint.Fill, paint.Stroke, paint.StrokeWidth);

        // Use the existing ToString() method which already generates SVG path data
        var pathData = path.ToString();
        sb.Append(pathData);

        sb.Append("\"/>");
        return sb.ToString();
    }
}