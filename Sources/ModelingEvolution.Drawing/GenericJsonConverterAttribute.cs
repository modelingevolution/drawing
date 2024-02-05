using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class PointJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();
    
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return typeFactory.GetOrAdd(typeToConvert, x =>
        {
            Func<JsonConverter> f = null;
            var genericArg = typeToConvert.GetGenericArguments()[0];
            if (genericArg == typeof(float))
                f = () => new PointConverterF();
            else if (genericArg == typeof(double))
                f = () => new PointConverterD();
            else throw new NotSupportedException();

            return f;
        })();
    }
}
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class VectorJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();

    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return typeFactory.GetOrAdd(typeToConvert, x =>
        {
            Func<JsonConverter> f = null;
            var genericArg = typeToConvert.GetGenericArguments()[0];
            if (genericArg == typeof(float))
                f = () => new VectorConverterF();
            else if (genericArg == typeof(double))
                f = () => new VectorConverterD();
            else throw new NotSupportedException();

            return f;
        })();
    }
}
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class RectangleJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();

    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return typeFactory.GetOrAdd(typeToConvert, x =>
        {
            Func<JsonConverter> f = null;
            var genericArg = typeToConvert.GetGenericArguments()[0];
            if (genericArg == typeof(float))
                f = () => new RectangleConverterF();
            else if (genericArg == typeof(double))
                f = () => new RectangleConverterD();
            else throw new NotSupportedException();

            return f;
        })();
    }
}