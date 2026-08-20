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

    // Replaces ThreadIsolation_ScopeOnOneThreadDoesNotAffectAnother. That test asserted a scope was
    // invisible to work forked inside it, which was a restatement of [ThreadStatic] rather than a
    // desired guarantee — it also meant an arena silently stopped applying across an await. The scope
    // is now ambient to the execution flow, so the meaningful pair is: forked work inherits it, and an
    // unrelated flow does not.
    [Fact]
    public async Task FlowIsolation_ScopeInheritedByForkedWork_NotByUnrelatedFlows()
    {
        AllocationScope? unrelatedFlowScope = null;
        var unrelated = Task.Run(() => { unrelatedFlowScope = AllocationScope.Current; });

        using var scope = AllocationScope.Begin();
        AllocationScope.Current.Should().BeSameAs(scope);

        AllocationScope? forkedScope = null;
        await Task.Run(() => { forkedScope = AllocationScope.Current; });

        // Forked after Begin: the arena applies, which is the point of the scope.
        forkedScope.Should().BeSameAs(scope);

        // Forked from a flow that had no scope: unaffected.
        await unrelated;
        unrelatedFlowScope.Should().BeNull();
    }

    [Fact]
    public async Task ScopeSurvivesAnAwait_SoAllocationsAfterItStillUseTheArena()
    {
        using var scope = AllocationScope.Begin();

        await Task.Delay(1);

        AllocationScope.Current.Should().BeSameAs(scope);

        var scaled = UnitSquare() * new Size<float>(2, 2);
        scaled.Count.Should().Be(4);
    }

    // Regression: a scope must never survive on a pooled thread after the flow that began it ended.
    // Under [ThreadStatic] the scope was written to the thread that called Begin, but Dispose ran on
    // whichever thread resumed the continuation, so the beginning thread kept a disposed scope and the
    // next unrelated work item on that thread allocated into it.
    [Fact]
    public async Task ScopeNeverLeaksOntoAPooledThreadRunningUnrelatedWork()
    {
        const int rounds = 200;
        var leaked = 0;

        var owners = new Task[rounds];
        var probes = new Task[rounds];

        for (int i = 0; i < rounds; i++)
        {
            owners[i] = Task.Run(async () =>
            {
                using var scope = AllocationScope.Begin();
                var scaled = UnitSquare() * new Size<float>(2, 2);
                scaled.Count.Should().Be(4);
                // Resumes on a different pool thread than the one that ran Begin.
                await Task.Delay(1);
            });

            // Forked from the test flow, which has no scope, so it must never observe one.
            probes[i] = Task.Run(() =>
            {
                if (AllocationScope.Current is not null)
                    Interlocked.Increment(ref leaked);
            });
        }

        await Task.WhenAll(owners);
        await Task.WhenAll(probes);

        leaked.Should().Be(0);
    }

    // Regression: because the scope now flows into forked work, one scope can be allocated from by
    // several threads at once. The tracking list must tolerate that.
    [Fact]
    public async Task ParallelChildrenOfOneScope_AllocateConcurrently()
    {
        for (int round = 0; round < 20; round++)
        {
            using var scope = AllocationScope.Begin();

            var workers = Enumerable.Range(0, Environment.ProcessorCount * 4).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    var scaled = UnitSquare() * new Size<float>(2, 2);
                    scaled.Count.Should().Be(4);
                }
            }));

            await Task.WhenAll(workers);
        }
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var outer = AllocationScope.Begin();
        var inner = AllocationScope.Begin();

        inner.Dispose();
        inner.Dispose();

        // A second Dispose must not re-install the previous scope over the current one.
        AllocationScope.Current.Should().BeSameAs(outer);
        outer.Dispose();
        AllocationScope.Current.Should().BeNull();
    }

    private static Polygon<float> UnitSquare() => new(
        new Point<float>(0, 0), new Point<float>(1, 0),
        new Point<float>(1, 1), new Point<float>(0, 1));

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
