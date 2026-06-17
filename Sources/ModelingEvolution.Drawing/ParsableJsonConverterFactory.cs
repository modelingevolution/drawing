using System.Text.Json;
using System.Text.Json.Serialization;
using ModelingEvolution.JsonParsableConverter;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A non-generic <see cref="JsonConverterFactory"/> that serializes any type implementing
/// <see cref="IParsable{TSelf}"/> through <see cref="ModelingEvolution.JsonParsableConverter.JsonParsableConverter{T}"/>
/// (value → <see cref="object.ToString"/>, JSON string → <c>Parse</c>).
///
/// It is applied as an attribute to the OPEN generic quantity types (e.g.
/// <c>[JsonConverter(typeof(ParsableJsonConverterFactory))]</c> on <see cref="Amps{T}"/>).
/// System.Text.Json instantiates the factory once and asks it to produce a converter for each
/// CLOSED form it encounters at runtime (<c>Amps&lt;float&gt;</c>, <c>Amps&lt;double&gt;</c>, …) — so
/// one factory serves every closed <c>T</c> of every unit type without per-T attributes.
/// </summary>
public sealed class ParsableJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// True when <paramref name="typeToConvert"/> implements <c>IParsable&lt;itself&gt;</c> — i.e. it has
    /// an <see cref="IParsable{TSelf}"/> interface whose type argument is the type itself. This is
    /// the constraint <see cref="ModelingEvolution.JsonParsableConverter.JsonParsableConverter{T}"/>
    /// requires (<c>where T : IParsable&lt;T&gt;</c>).
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        foreach (var i in typeToConvert.GetInterfaces())
        {
            if (i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IParsable<>)
                && i.GetGenericArguments()[0] == typeToConvert)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates a closed <see cref="ModelingEvolution.JsonParsableConverter.JsonParsableConverter{T}"/>
    /// for the requested closed type.
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(JsonParsableConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
