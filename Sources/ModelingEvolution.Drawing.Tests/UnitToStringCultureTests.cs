using System.Globalization;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

[CollectionDefinition(nameof(CurrentCultureCollection), DisableParallelization = true)]
public class CurrentCultureCollection { }

// Every SiPrefix-based unit writes with the invariant culture, the culture its Parse defaults to,
// so its own ToString round-trips whatever the host culture (pl-PL wrote "1,5rad", ICU en-US "∞rad").
[Collection(nameof(CurrentCultureCollection))]
public class UnitToStringCultureTests
{
    public static TheoryData<string, double> Cases()
    {
        var data = new TheoryData<string, double>();
        foreach (var culture in new[] { "pl-PL", "en-US" })
            foreach (var value in new[] { 1.5, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                data.Add(culture, value);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ToString_UsesInvariantCulture_AndParseRoundTrips(string culture, double value)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var number = value.ToString(CultureInfo.InvariantCulture);

            Check(Radian<double>.FromRadian(value), number + "rad", r => (double)r, value);
            Check(Degree<double>.Create(value), number + "°", d => (double)d, value);
            Check(Speed<double>.From(value), number + " mm/min", s => (double)s, value);
            Check(Amps<double>.From(value), number + " A", a => (double)a, value);
            Check(Volts<double>.From(value), number + " V", v => (double)v, value);
            Check(Frequency<double>.FromHertz(value), number + " Hz", f => (double)f, value);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    private static void Check<TUnit>(TUnit unit, string expected, Func<TUnit, double> get, double value)
        where TUnit : IParsable<TUnit>
    {
        var text = unit.ToString();
        text.Should().Be(expected);

        var back = get(TUnit.Parse(text!, null));
        if (double.IsNaN(value)) double.IsNaN(back).Should().BeTrue($"'{text}' should read back as NaN");
        else back.Should().Be(value);
    }
}
