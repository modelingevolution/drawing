using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Vector3CollectionBuilderTests
{
    [Fact]
    public void Create_FromValidSpan_BuildsVector3()
    {
        Span<double> values = stackalloc double[] { 1.0, 2.0, 3.0 };
        var v = Vector3CollectionBuilder.Create<double>(values);
        v.X.Should().Be(1.0);
        v.Y.Should().Be(2.0);
        v.Z.Should().Be(3.0);
    }

    [Fact]
    public void Create_FromCollectionLiteral_ProducesSameValuesAsConstructor()
    {
        Vector3<double> fromLiteral = [10.0, 20.0, 30.0];
        var fromCtor = new Vector3<double>(10.0, 20.0, 30.0);
        fromLiteral.Should().Be(fromCtor);
    }

    [Fact]
    public void Create_FromCollectionLiteralWithFloat_Works()
    {
        Vector3<float> v = [1.5f, 2.5f, 3.5f];
        v.X.Should().Be(1.5f);
        v.Y.Should().Be(2.5f);
        v.Z.Should().Be(3.5f);
    }

    [Fact]
    public void Create_WithWrongLength_Throws()
    {
        Span<double> tooShort = stackalloc double[] { 1.0, 2.0 };
        Span<double> tooLong = stackalloc double[] { 1.0, 2.0, 3.0, 4.0 };

        // Local helpers avoid the "Span<T> in lambda" capture problem.
        Action act2 = () => { Span<double> s = stackalloc double[] { 1.0, 2.0 }; Vector3CollectionBuilder.Create<double>(s); };
        Action act4 = () => { Span<double> s = stackalloc double[] { 1.0, 2.0, 3.0, 4.0 }; Vector3CollectionBuilder.Create<double>(s); };
        Action actEmpty = () => Vector3CollectionBuilder.Create<double>(ReadOnlySpan<double>.Empty);

        act2.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
    }
}
