using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Factory for creating SVG exporters for ComplexCurve{T} types.
/// </summary>
public class ComplexCurveSvgExporterFactory : ISvgExporterFactory
{
    public ISvgExporter Create(Type type)
    {
        var genericArg = type.GetGenericArguments()[0];
        var exporterType = typeof(ComplexCurveSvgExporter<>).MakeGenericType(genericArg);
        return (ISvgExporter)Activator.CreateInstance(exporterType)!;
    }
}
