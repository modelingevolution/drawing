using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating SVG exporters for <see cref="Point{T}"/> types.
/// </summary>
public class PointSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(PointSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Segment{T}"/> types.
/// </summary>
public class SegmentSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(SegmentSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Line{T}"/> types.
/// </summary>
public class LineSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(LineSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Circle{T}"/> types.
/// </summary>
public class CircleSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(CircleSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Triangle{T}"/> types.
/// </summary>
public class TriangleSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(TriangleSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Rectangle{T}"/> types.
/// </summary>
public class RectangleSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(RectangleSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Polyline{T}"/> types.
/// </summary>
public class PolylineSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(PolylineSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="Vector{T}"/> types.
/// </summary>
public class VectorSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(VectorSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="BezierCurve{T}"/> types.
/// </summary>
public class BezierCurveSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(BezierCurveSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}

/// <summary>
/// Factory for creating SVG exporters for <see cref="PolygonalCurve{T}"/> types.
/// </summary>
public class PolygonalCurveSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        var t = typeof(PolygonalCurveSvgExporter<>).MakeGenericType(obj.GetGenericArguments()[0]);
        return (ISvgExporter)Activator.CreateInstance(t)!;
    }
}
