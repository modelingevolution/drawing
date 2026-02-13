using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// SVG exporter for <see cref="Point{T}"/> — renders as a filled circle.
/// </summary>
public class PointSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var p = (Point<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\"/>",
            Convert.ToDouble(p.X), Convert.ToDouble(p.Y),
            paint.PointRadius, paint.Fill, paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Segment{T}"/> — renders as a line element.
/// </summary>
public class SegmentSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var s = (Segment<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\" stroke-linecap=\"round\"/>",
            Convert.ToDouble(s.Start.X), Convert.ToDouble(s.Start.Y),
            Convert.ToDouble(s.End.X), Convert.ToDouble(s.End.Y),
            paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Line{T}"/> — renders as a long line segment spanning ±10000 units.
/// </summary>
public class LineSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var line = (Line<T>)obj;
        double x1, y1, x2, y2;

        if (line.IsVertical)
        {
            var vx = Convert.ToDouble(line.VerticalX);
            x1 = vx; y1 = -10000;
            x2 = vx; y2 = 10000;
        }
        else
        {
            x1 = -10000;
            y1 = Convert.ToDouble(line.Compute(T.CreateTruncating(-10000)));
            x2 = 10000;
            y2 = Convert.ToDouble(line.Compute(T.CreateTruncating(10000)));
        }

        return string.Format(CultureInfo.InvariantCulture,
            "<line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\"/>",
            x1, y1, x2, y2, paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Circle{T}"/> — renders as an SVG circle element.
/// </summary>
public class CircleSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var c = (Circle<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" fill=\"{3}\" stroke=\"{4}\" stroke-width=\"{5}\"/>",
            Convert.ToDouble(c.Center.X), Convert.ToDouble(c.Center.Y),
            Convert.ToDouble(c.Radius), paint.Fill, paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Triangle{T}"/> — renders as a closed polygon with 3 vertices.
/// </summary>
public class TriangleSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var t = (Triangle<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<polygon points=\"{0},{1} {2},{3} {4},{5}\" fill=\"{6}\" stroke=\"{7}\" stroke-width=\"{8}\"/>",
            Convert.ToDouble(t.A.X), Convert.ToDouble(t.A.Y),
            Convert.ToDouble(t.B.X), Convert.ToDouble(t.B.Y),
            Convert.ToDouble(t.C.X), Convert.ToDouble(t.C.Y),
            paint.Fill, paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Rectangle{T}"/> — renders as an SVG rect element.
/// </summary>
public class RectangleSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var r = (Rectangle<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\" stroke=\"{5}\" stroke-width=\"{6}\"/>",
            Convert.ToDouble(r.X), Convert.ToDouble(r.Y),
            Convert.ToDouble(r.Width), Convert.ToDouble(r.Height),
            paint.Fill, paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="Polyline{T}"/> — renders as an SVG polyline element (open path).
/// </summary>
public class PolylineSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var pl = (Polyline<T>)obj;
        var pts = pl.Points;
        if (pts.Count == 0) return string.Empty;

        var sb = new StringBuilder("<polyline points=\"");
        for (int i = 0; i < pts.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}",
                Convert.ToDouble(pts[i].X), Convert.ToDouble(pts[i].Y));
        }
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "\" fill=\"none\" stroke=\"{0}\" stroke-width=\"{1}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            paint.Stroke, paint.StrokeWidth);
        return sb.ToString();
    }
}

/// <summary>
/// SVG exporter for <see cref="Vector{T}"/> — renders as a line from origin with an arrowhead.
/// </summary>
public class VectorSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var v = (Vector<T>)obj;
        var vx = Convert.ToDouble(v.X);
        var vy = Convert.ToDouble(v.Y);
        var len = Math.Sqrt(vx * vx + vy * vy);
        if (len < 1e-10) return string.Empty;

        // Arrowhead parameters
        var headLen = Math.Min(len * 0.2, (double)paint.StrokeWidth * 4);
        var headW = headLen * 0.5;
        var nx = vx / len;
        var ny = vy / len;
        var baseX = vx - nx * headLen;
        var baseY = vy - ny * headLen;

        var sb = new StringBuilder();
        // Shaft
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "<line x1=\"0\" y1=\"0\" x2=\"{0}\" y2=\"{1}\" stroke=\"{2}\" stroke-width=\"{3}\"/>",
            baseX, baseY, paint.Stroke, paint.StrokeWidth);
        // Arrowhead
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "<polygon points=\"{0},{1} {2},{3} {4},{5}\" fill=\"{6}\"/>",
            vx, vy,
            baseX - ny * headW, baseY + nx * headW,
            baseX + ny * headW, baseY - nx * headW,
            paint.Stroke);
        return sb.ToString();
    }
}

/// <summary>
/// SVG exporter for <see cref="BezierCurve{T}"/> — renders as an SVG cubic bezier path.
/// </summary>
public class BezierCurveSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var b = (BezierCurve<T>)obj;
        return string.Format(CultureInfo.InvariantCulture,
            "<path d=\"M{0},{1} C{2},{3} {4},{5} {6},{7}\" fill=\"none\" stroke=\"{8}\" stroke-width=\"{9}\"/>",
            Convert.ToDouble(b.Start.X), Convert.ToDouble(b.Start.Y),
            Convert.ToDouble(b.C0.X), Convert.ToDouble(b.C0.Y),
            Convert.ToDouble(b.C1.X), Convert.ToDouble(b.C1.Y),
            Convert.ToDouble(b.End.X), Convert.ToDouble(b.End.Y),
            paint.Stroke, paint.StrokeWidth);
    }
}

/// <summary>
/// SVG exporter for <see cref="PolygonalCurve{T}"/> — renders as an SVG polyline (open path).
/// </summary>
public class PolygonalCurveSvgExporter<T> : ISvgExporter
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <inheritdoc />
    public string Export(object obj, in SvgPaint paint)
    {
        var curve = (PolygonalCurve<T>)obj;
        if (curve.Count == 0) return string.Empty;

        var sb = new StringBuilder("<polyline points=\"");
        for (int i = 0; i < curve.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}",
                Convert.ToDouble(curve[i].X), Convert.ToDouble(curve[i].Y));
        }
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "\" fill=\"none\" stroke=\"{0}\" stroke-width=\"{1}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
            paint.Stroke, paint.StrokeWidth);
        return sb.ToString();
    }
}
