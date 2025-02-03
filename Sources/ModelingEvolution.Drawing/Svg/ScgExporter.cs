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

        /// <summary>
        /// Generates an SVG representation of a collection of items.
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="items">The collection of items to be exported.</param>
        /// <param name="objectSelector">A function to select the object to be exported from each item.</param>
        /// <param name="paintSelector">A function to select the <see cref="SvgPaint"/> for each item.</param>
        /// <param name="width">The width of the SVG canvas.</param>
        /// <param name="height">The height of the SVG canvas.</param>
        /// <returns>A string containing the SVG representation of the collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        /// <example>
        /// The following example demonstrates how to use the Export method:
        /// <code>
        /// // Define a class with a Polygon property
        /// public class MyObject
        /// {
        ///     public Polygon<float> Shape { get; set; }
        /// }
        ///
        /// // Create a list of MyObject instances with Polygon data
        /// var items = new List<MyObject>
        /// {
        ///     new MyObject { Shape = new Polygon<float>() },
        ///     new MyObject { Shape = new Polygon<float>() }
        /// };
        ///
        /// // Generate the SVG
        /// string svg = SvgExporter.Export(
        ///     items,
        ///     obj => obj.Shape, // Object selector to select the Polygon
        ///     obj => new SvgPaint(Colors.Red, Colors.Black, 2f), // Paint selector
        ///     500, // Width
        ///     300  // Height
        /// );
        ///
        /// Console.WriteLine(svg);
        /// </code>
        /// </example>
        public static string Export<T>(IEnumerable<T> items, Func<T, object> objectSelector, Func<T, SvgPaint> paintSelector, int width, int height)
        {
            
            StringBuilder sb = new StringBuilder();
            foreach (var i in items)
            {
                var o = objectSelector(i);
                var p = paintSelector(i);
                var exporter = GetOrCreate(o.GetType());
                sb.Append(exporter.Export(o, p));
            }

            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{sb}</svg>";

        }
        public static string Export(object obj, int width, int height, in SvgPaint paint)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var exporter = GetOrCreate(obj.GetType());
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{exporter.Export(obj, paint)}</svg>";
        }

        public static string Export<T>(IEnumerable<T> objs, int width, int height, in SvgPaint paint)
        {
            return Export(objs.Cast<object>(), width, height, in paint);
        }
        public static string Export(IEnumerable<object> objs, int width, int height, in SvgPaint paint)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));

            StringBuilder sb = new StringBuilder();
            foreach (var o in objs)
            {
                var exporter = GetOrCreate(o.GetType());
                sb.Append(exporter.Export(o, paint));
            }
            
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{sb}</svg>";
        }

        public static string Export(IEnumerable<object> objs, int width, int height, Func<object,SvgPaint> paint)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));

            StringBuilder sb = new StringBuilder();
            foreach (var o in objs)
            {
                var exporter = GetOrCreate(o.GetType());
                sb.Append(exporter.Export(o, paint(o)));
            }

            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">{sb}</svg>";
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
