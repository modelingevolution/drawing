using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class AllocationScopeTests
{
    [Fact]
    public void BasicScope_AllocAndDispose()
    {
        using var scope = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(scope);

        // Polygon operator * allocates via Alloc.Memory when scope is active
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(1, 0),
            new Point<float>(1, 1), new Point<float>(0, 1));

        var scaled = poly * new Size<float>(2, 2);
        scaled.Count.Should().Be(4);
        scaled[0].Should().Be(new Point<float>(0, 0));
        scaled[1].Should().Be(new Point<float>(2, 0));
    }

    [Fact]
    public void AfterDispose_ScopeIsCleared()
    {
        var scope = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(scope);
        scope.Dispose();
        AllocationScope.Current.Should().BeNull();
    }

    [Fact]
    public void NestedScopes_InnerDisposeRestoresOuter()
    {
        using var outer = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(outer);

        var inner = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(inner);

        inner.Dispose();
        AllocationScope.Current.Should().BeSameAs(outer);
    }

    [Fact]
    public void NoScope_FallsBackToHeapAllocation()
    {
        AllocationScope.Current.Should().BeNull();

        // Without scope, operations still work (heap allocation)
        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(1, 0),
            new Point<float>(1, 1), new Point<float>(0, 1));

        var scaled = poly * new Size<float>(3, 3);
        scaled.Count.Should().Be(4);
        scaled[0].Should().Be(new Point<float>(0, 0));
        scaled[1].Should().Be(new Point<float>(3, 0));
    }

    [Fact]
    public void Persist_DetachedResultSurvivesScopeDisposal()
    {
        Lease<Point<float>> lease;
        Polygon<float> persisted;

        using (var scope = AllocationScope.Begin())
        {
            var poly = new Polygon<float>(
                new Point<float>(0, 0), new Point<float>(1, 0),
                new Point<float>(1, 1), new Point<float>(0, 1));

            persisted = poly * new Size<float>(2, 2);
            lease = scope.Persist<Polygon<float>, Lease<Point<float>>>(ref persisted);
        }

        // After scope disposal, persisted polygon should still be accessible
        persisted.Count.Should().Be(4);
        persisted[0].Should().Be(new Point<float>(0, 0));
        persisted[1].Should().Be(new Point<float>(2, 0));

        // Clean up the lease
        lease.Dispose();
    }

    [Fact]
    public void PolygonEdges_WorkThroughScope()
    {
        using var scope = AllocationScope.Begin();

        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(1, 0),
            new Point<float>(1, 1), new Point<float>(0, 1));

        var edges = poly.Edges();
        edges.Length.Should().Be(4);

        var edgesSpan = edges.Span;
        edgesSpan[0].Start.Should().Be(new Point<float>(0, 0));
        edgesSpan[0].End.Should().Be(new Point<float>(1, 0));
    }

    [Fact]
    public void PolylineEdges_WorkThroughScope()
    {
        using var scope = AllocationScope.Begin();

        var polyline = new Polyline<float>(
            new Point<float>(0, 0), new Point<float>(1, 0),
            new Point<float>(1, 1));

        var edges = polyline.Edges();
        edges.Length.Should().Be(2);

        var edgesSpan = edges.Span;
        edgesSpan[0].Start.Should().Be(new Point<float>(0, 0));
        edgesSpan[0].End.Should().Be(new Point<float>(1, 0));
    }

    [Fact]
    public void QuadraticZeroPoints_WorkThroughScope()
    {
        using var scope = AllocationScope.Begin();

        var eq = new ModelingEvolution.Drawing.Equations.QuadraticEquation<float>(1, -3, 2);
        var roots = eq.ZeroPoints();
        roots.Length.Should().Be(2);

        var rootsSpan = roots.Span;
        var sorted = roots.Span.ToArray().OrderBy(x => x).ToArray();
        sorted[0].Should().BeApproximately(1f, 1e-5f);
        sorted[1].Should().BeApproximately(2f, 1e-5f);
    }

    [Fact]
    public void ComputeTiles_WorksThroughScope()
    {
        using var scope = AllocationScope.Begin();

        var rect = new Rectangle<float>(0, 0, 100, 100);
        var tiles = rect.ComputeTiles(new Size<float>(50, 50));
        tiles.Length.Should().Be(4);
    }

    [Fact]
    public async Task ThreadIsolation_ScopeOnOneThreadDoesNotAffectAnother()
    {
        using var scope = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(scope);

        AllocationScope? otherThreadScope = null;
        await Task.Run(() => { otherThreadScope = AllocationScope.Current; });

        otherThreadScope.Should().BeNull();
    }

    [Fact]
    public void PolygonOperatorChain_ThroughScope()
    {
        using var scope = AllocationScope.Begin();

        var poly = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(10, 0),
            new Point<float>(10, 10), new Point<float>(0, 10));

        // Chain: scale then translate
        var scaled = poly * new Size<float>(2, 2);
        var translated = scaled - new Vector<float>(5, 5);

        translated.Count.Should().Be(4);
        translated[0].Should().Be(new Point<float>(-5, -5));
        translated[1].Should().Be(new Point<float>(15, -5));
    }
}
