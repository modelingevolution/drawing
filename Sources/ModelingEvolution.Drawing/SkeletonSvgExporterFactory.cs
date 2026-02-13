using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating SVG exporters for <see cref="Skeleton{T}"/> types.
/// </summary>
public class SkeletonSvgExporterFactory : ISvgExporterFactory
{
    /// <inheritdoc />
    public ISvgExporter Create(Type obj)
    {
        Type elementType = obj.GetGenericArguments()[0];
        var exporterType = typeof(SkeletonSvgExporter<>).MakeGenericType(elementType);
        return (ISvgExporter)Activator.CreateInstance(exporterType)!;
    }
}
