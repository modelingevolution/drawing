using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Point3CollectionBuilderTests
{
    [Fact]
    public void Create_FromCollectionLiteral_Builds()
    {
        Point3<double> p = [10.0, 20.0, 30.0];
        p.X.Should().Be(10.0);
        p.Y.Should().Be(20.0);
        p.Z.Should().Be(30.0);
    }

    [Fact]
    public void Create_FromZeros_EqualsZeroPoint()
    {
        Point3<double> p = [0.0, 0.0, 0.0];
        p.Should().Be(Point3<double>.Zero);
    }

    [Fact]
    public void Create_WithWrongLength_Throws()
    {
        Action actShort = () => { Span<double> s = stackalloc double[] { 1.0, 2.0 }; Point3CollectionBuilder.Create<double>(s); };
        Action actLong = () => { Span<double> s = stackalloc double[] { 1.0, 2.0, 3.0, 4.0 }; Point3CollectionBuilder.Create<double>(s); };
        actShort.Should().Throw<ArgumentException>();
        actLong.Should().Throw<ArgumentException>();
    }
}
