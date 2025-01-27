using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

public class PolygonSvgExporterFactory : ISvgExporterFactory
{
    public ISvgExporter Create(Type obj)
    {
        Type elementType = obj.GetGenericArguments()[0];
        var exporterType = typeof(PolygonSvgExporter<>).MakeGenericType(elementType);
        return (ISvgExporter)Activator.CreateInstance(exporterType);
    }
}