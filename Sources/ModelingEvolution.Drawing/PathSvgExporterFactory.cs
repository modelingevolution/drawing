using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating SVG exporters for Path{T} types with different numeric type parameters.
/// </summary>
public class PathSvgExporterFactory : ISvgExporterFactory
{
    /// <summary>
    /// Creates an appropriate SVG exporter for a Path{T} type.
    /// </summary>
    /// <param name="type">The type of Path{T} to create an exporter for.</param>
    /// <returns>An SVG exporter instance for the specified Path{T} type.</returns>
    public ISvgExporter Create(Type type)
    {
        var genericArg = type.GetGenericArguments()[0];
        var exporterType = typeof(PathSvgExporter<>).MakeGenericType(genericArg);
        return (ISvgExporter)Activator.CreateInstance(exporterType)!;
    }
}