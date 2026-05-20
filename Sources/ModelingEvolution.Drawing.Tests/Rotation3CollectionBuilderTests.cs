using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Rotation3CollectionBuilderTests
{
    [Fact]
    public void Create_FromCollectionLiteral_Builds()
    {
        Rotation3<double> r = [10.0, 20.0, 30.0];
        ((double)r.Rx).Should().Be(10.0);
        ((double)r.Ry).Should().Be(20.0);
        ((double)r.Rz).Should().Be(30.0);
    }

    [Fact]
    public void Create_FromZeros_EqualsIdentity()
    {
        Rotation3<double> r = [0.0, 0.0, 0.0];
        r.Should().Be(Rotation3<double>.Identity);
    }

    [Fact]
    public void Create_WithWrongLength_Throws()
    {
        Action actShort = () => { Span<double> s = stackalloc double[] { 1.0, 2.0 }; Rotation3CollectionBuilder.Create<double>(s); };
        Action actLong = () => { Span<double> s = stackalloc double[] { 1.0, 2.0, 3.0, 4.0 }; Rotation3CollectionBuilder.Create<double>(s); };
        actShort.Should().Throw<ArgumentException>();
        actLong.Should().Throw<ArgumentException>();
    }
}
