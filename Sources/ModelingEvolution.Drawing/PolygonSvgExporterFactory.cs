using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating SVG exporters for Polygon{T} types.
/// </summary>
public class PolygonSvgExporterFactory : ISvgExporterFactory
{
    /// <summary>
    /// Creates an SVG exporter for the specified Polygon{T} type.
    /// </summary>
    /// <param name="obj">The Polygon{T} type to create an exporter for.</param>
    /// <returns>An SVG exporter instance for the specified type.</returns>
    public ISvgExporter Create(Type obj)
    {
        Type elementType = obj.GetGenericArguments()[0];
        var exporterType = typeof(PolygonSvgExporter<>).MakeGenericType(elementType);
        return (ISvgExporter)Activator.CreateInstance(exporterType);
    }
}