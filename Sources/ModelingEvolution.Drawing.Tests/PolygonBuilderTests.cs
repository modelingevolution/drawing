using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class PolygonBuilderTests
{
    [Fact]
    public void EmptyBuilder_HasZeroCount()
    {
        var builder = new PolygonBuilder<float>();
        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(1, 2));
        builder.Add(new Point<float>(3, 4));

        builder.Count.Should().Be(2);
        builder[0].Should().Be(new Point<float>(1, 2));
        builder[1].Should().Be(new Point<float>(3, 4));
    }

    [Fact]
    public void InsertAt_InsertsAtCorrectPosition()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(2, 0));

        builder.InsertAt(1, new Point<float>(1, 0));

        builder.Count.Should().Be(3);
        builder[0].Should().Be(new Point<float>(0, 0));
        builder[1].Should().Be(new Point<float>(1, 0));
        builder[2].Should().Be(new Point<float>(2, 0));
    }

    [Fact]
    public void RemoveAt_RemovesCorrectPoint()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 0));
        builder.Add(new Point<float>(2, 0));

        builder.RemoveAt(1);

        builder.Count.Should().Be(2);
        builder[0].Should().Be(new Point<float>(0, 0));
        builder[1].Should().Be(new Point<float>(2, 0));
    }

    [Fact]
    public void IndexerSet_UpdatesPoint()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 0));

        builder[0] = new Point<float>(5, 5);

        builder[0].Should().Be(new Point<float>(5, 5));
    }

    [Fact]
    public void Clear_RemovesAllPoints()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 0));

        builder.Clear();

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Build_ReturnsImmutablePolygon()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(4, 0));
        builder.Add(new Point<float>(4, 3));

        var polygon = builder.Build();

        polygon.Count.Should().Be(3);
        polygon[0].Should().Be(new Point<float>(0, 0));
        polygon[1].Should().Be(new Point<float>(4, 0));
        polygon[2].Should().Be(new Point<float>(4, 3));
        polygon.Area().Should().Be(6f);
    }

    [Fact]
    public void Build_DoesNotShareStateWithBuilder()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 0));

        var polygon = builder.Build();
        builder.Add(new Point<float>(2, 0));

        polygon.Count.Should().Be(2, "polygon should not be affected by builder mutation");
        builder.Count.Should().Be(3);
    }

    [Fact]
    public void ImplicitConversion_ToPolygon()
    {
        var builder = new PolygonBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 0));
        builder.Add(new Point<float>(1, 1));

        Polygon<float> polygon = builder;

        polygon.Count.Should().Be(3);
        polygon[2].Should().Be(new Point<float>(1, 1));
    }

    [Fact]
    public void ConstructFromPolygon_CopiesPoints()
    {
        var polygon = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(1, 0), new Point<float>(1, 1));

        var builder = new PolygonBuilder<float>(polygon);

        builder.Count.Should().Be(3);
        builder[0].Should().Be(new Point<float>(0, 0));
        builder[1].Should().Be(new Point<float>(1, 0));
        builder[2].Should().Be(new Point<float>(1, 1));
    }

    [Fact]
    public void ConstructWithCapacity_IsEmpty()
    {
        var builder = new PolygonBuilder<float>(10);
        builder.Count.Should().Be(0);
    }

    [Fact]
    public void ConstructFromEnumerable_CopiesPoints()
    {
        var points = new[] { new Point<float>(0, 0), new Point<float>(1, 1) };

        var builder = new PolygonBuilder<float>(points);

        builder.Count.Should().Be(2);
        builder[0].Should().Be(new Point<float>(0, 0));
        builder[1].Should().Be(new Point<float>(1, 1));
    }
}
