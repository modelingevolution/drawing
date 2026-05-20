using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Pose3CollectionBuilderTests
{
    [Fact]
    public void Create_FromCollectionLiteral_Builds()
    {
        Pose3<double> p = [10.0, 20.0, 30.0, 5.0, 10.0, 15.0];
        p.X.Should().Be(10.0);
        p.Y.Should().Be(20.0);
        p.Z.Should().Be(30.0);
        ((double)p.Rx).Should().Be(5.0);
        ((double)p.Ry).Should().Be(10.0);
        ((double)p.Rz).Should().Be(15.0);
    }

    [Fact]
    public void Create_FromAllZeros_EqualsIdentity()
    {
        Pose3<double> p = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0];
        p.Should().Be(Pose3<double>.Identity);
    }

    [Fact]
    public void Create_WithWrongLength_Throws()
    {
        Action act5 = () => { Span<double> s = stackalloc double[] { 1.0, 2.0, 3.0, 4.0, 5.0 }; Pose3CollectionBuilder.Create<double>(s); };
        Action act7 = () => { Span<double> s = stackalloc double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 }; Pose3CollectionBuilder.Create<double>(s); };
        act5.Should().Throw<ArgumentException>();
        act7.Should().Throw<ArgumentException>();
    }
}
