using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

/// <summary>
/// JSON converter attribute for Point{T} types that automatically selects the appropriate converter based on the generic type parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class PointJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();
    
    /// <summary>
    /// Creates a JSON converter for the specified Point{T} type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert (must be Point{float} or Point{double}).</param>
    /// <returns>A JSON converter for the specified type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the generic type parameter is not supported.</exception>
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
/// <summary>
/// JSON converter attribute for Vector{T} types that automatically selects the appropriate converter based on the generic type parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class VectorJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();

    /// <summary>
    /// Creates a JSON converter for the specified Vector{T} type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert (must be Vector{float} or Vector{double}).</param>
    /// <returns>A JSON converter for the specified type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the generic type parameter is not supported.</exception>
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
/// <summary>
/// JSON converter attribute for Rectangle{T} types that automatically selects the appropriate converter based on the generic type parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class RectangleJsonConverterAttribute : JsonConverterAttribute
{
    private readonly ConcurrentDictionary<Type, Func<JsonConverter>> typeFactory = new();

    /// <summary>
    /// Creates a JSON converter for the specified Rectangle{T} type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert (must be Rectangle{float} or Rectangle{double}).</param>
    /// <returns>A JSON converter for the specified type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the generic type parameter is not supported.</exception>
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