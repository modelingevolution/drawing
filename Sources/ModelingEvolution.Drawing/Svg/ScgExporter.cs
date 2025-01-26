using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelingEvolution.Drawing.Svg
{
    public class SvgExporterAttribute(Type exporterType) : Attribute
    {
        public Type Exporter => exporterType;
    }


    public readonly record struct SvgPaint(Color Fill, Color Stroke, float StrokeWidth=1f, float PointRadius = 2f)
    {
        public static SvgPaint WithFill(Color fill) => new SvgPaint(fill, Colors.Transparent, 0);
        public static SvgPaint WithStroke(Color stroke, float strokeWidth=1f) => new SvgPaint(Colors.Transparent, stroke, strokeWidth);

    }

    public interface ISvgExporterFactory
    {
        ISvgExporter Create(Type obj);
    }
    public interface ISvgExporter
    {
        public string Export(object obj, in SvgPaint paint);
    }
    
    public static class SvgExporter
    {
        private static readonly ConcurrentDictionary<Type, ISvgExporter> _exporter = new();


        public static string Export(object obj, int width, int height, Color fill) =>
            Export(obj, width, height, SvgPaint.WithFill(fill));

        public static string Export(object obj, int width, int height, Color stroke, float strokeWidth) =>
            Export(obj, width, height, SvgPaint.WithStroke(stroke, strokeWidth));

        public static string Export(object obj, int width, int height, Color fill, Color stroke, float strokeWidth) =>
            Export(obj, width, height, new SvgPaint(fill, stroke, strokeWidth));

        public static string Export(object obj, int width, int height, in SvgPaint paint)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var exporter = GetOrCreate(obj.GetType());
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{exporter.Export(obj, paint)}</svg>";
        }

        private static ISvgExporter GetOrCreate(Type obj) => _exporter.GetOrAdd(obj, GetExporter);

        private static ISvgExporter GetExporter(Type obj)
        {
            var exporterType = obj.GetCustomAttributes(typeof(SvgExporterAttribute), false)
                .Cast<SvgExporterAttribute>()
                .Select(a => a.Exporter)
                .FirstOrDefault();

            if (exporterType == null)
            {
                throw new InvalidOperationException($"No exporter found for type {obj.Name}");
            }

            // can be ISvgExporterFactory or ISvgExporter
            var instance = Activator.CreateInstance(exporterType);

            switch (instance)
            {
                case ISvgExporterFactory factory:
                    return factory.Create(obj);
                case ISvgExporter exporter:
                    return exporter;
                default:
                    throw new ArgumentException("Instance doesn't have any relevant interface implemented.");
            }
        }
    }
}
