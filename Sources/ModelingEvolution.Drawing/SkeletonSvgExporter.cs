using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// SVG exporter for <see cref="Skeleton{T}"/> objects that renders edges as lines and nodes as circles.
/// Fill color controls node fill, stroke color controls edge color.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public class SkeletonSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var skeleton = (Skeleton<T>)obj;
        if (skeleton.EdgeCount == 0 && skeleton.NodeCount == 0)
            return string.Empty;

        var sb = new StringBuilder();

        // Render edges as lines
        foreach (var edge in skeleton.Edges().ToArray())
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" stroke-linecap=\"round\"/>",
                Convert.ToDouble(edge.Start.X),
                Convert.ToDouble(edge.Start.Y),
                Convert.ToDouble(edge.End.X),
                Convert.ToDouble(edge.End.Y),
                paint.Stroke,
                paint.StrokeWidth);
        }

        // Render nodes as circles
        foreach (var node in skeleton.Nodes().ToArray())
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\"/>",
                Convert.ToDouble(node.X),
                Convert.ToDouble(node.Y),
                paint.PointRadius,
                paint.Fill);
        }

        return sb.ToString();
    }
}
