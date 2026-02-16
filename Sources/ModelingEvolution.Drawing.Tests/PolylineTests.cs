using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class PolylineTests
{
    private const float Tol = 1e-4f;

    // ─────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultCtor_Empty()
    {
        var pl = new Polyline<float>();
        pl.Count.Should().Be(0);
        pl.AsSpan().Length.Should().Be(0);
    }

    [Fact]
    public void ParamsCtor_StoresPoints()
    {
        var p1 = new Point<float>(0, 0);
        var p2 = new Point<float>(1, 0);
        var p3 = new Point<float>(1, 1);
        var pl = new Polyline<float>(p1, p2, p3);
        pl.Count.Should().Be(3);
        pl[0].Should().Be(p1);
        pl[1].Should().Be(p2);
        pl[2].Should().Be(p3);
    }

    [Fact]
    public void ListCtor_CopiesPoints()
    {
        var list = new List<Point<float>>
        {
            new(0, 0), new(5, 5), new(10, 0)
        };
        var pl = new Polyline<float>(list);
        pl.Count.Should().Be(3);
        pl[2].Should().Be(new Point<float>(10, 0));
    }

    [Fact]
    public void CoordsCtor_ParsesPairs()
    {
        var coords = new float[] { 0, 0, 10, 0, 10, 10 };
        var pl = new Polyline<float>(coords);
        pl.Count.Should().Be(3);
        pl[1].Should().Be(new Point<float>(10, 0));
    }

    [Fact]
    public void MemoryCtor_ZeroCopy()
    {
        var arr = new Point<float>[] { new(1, 2), new(3, 4), new(5, 6) };
        var mem = new ReadOnlyMemory<Point<float>>(arr);
        var pl = new Polyline<float>(mem);
        pl.Count.Should().Be(3);
        pl[0].Should().Be(new Point<float>(1, 2));
    }

    // ─────────────────────────────────────────────
    // Start / End
    // ─────────────────────────────────────────────

    [Fact]
    public void Start_ReturnsFirstPoint()
    {
        var pl = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        pl.Start.Should().Be(new Point<float>(1, 2));
    }

    [Fact]
    public void End_ReturnsLastPoint()
    {
        var pl = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        pl.End.Should().Be(new Point<float>(3, 4));
    }

    // ─────────────────────────────────────────────
    // Length
    // ─────────────────────────────────────────────

    [Fact]
    public void Length_StraightLine()
    {
        var pl = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        pl.Length().Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Length_MultiSegment()
    {
        // L-shaped: (0,0)->(10,0)->(10,5)
        var pl = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 0), new Point<float>(10, 5));
        pl.Length().Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Length_SinglePoint_ReturnsZero()
    {
        var pl = new Polyline<float>(new Point<float>(5, 5));
        pl.Length().Should().Be(0f);
    }

    // ─────────────────────────────────────────────
    // BoundingBox
    // ─────────────────────────────────────────────

    [Fact]
    public void BoundingBox_Correct()
    {
        var pl = new Polyline<float>(
            new Point<float>(1, 2),
            new Point<float>(5, 8),
            new Point<float>(3, 0));
        var bb = pl.BoundingBox();
        bb.X.Should().BeApproximately(1f, Tol);
        bb.Y.Should().BeApproximately(0f, Tol);
        bb.Width.Should().BeApproximately(4f, Tol);
        bb.Height.Should().BeApproximately(8f, Tol);
    }

    [Fact]
    public void BoundingBox_Empty_ReturnsZero()
    {
        var pl = new Polyline<float>();
        var bb = pl.BoundingBox();
        bb.Width.Should().Be(0f);
        bb.Height.Should().Be(0f);
    }

    // ─────────────────────────────────────────────
    // Reverse
    // ─────────────────────────────────────────────

    [Fact]
    public void Reverse_FlipsOrder()
    {
        var p1 = new Point<float>(0, 0);
        var p2 = new Point<float>(1, 0);
        var p3 = new Point<float>(2, 0);
        var pl = new Polyline<float>(p1, p2, p3).Reverse();
        pl[0].Should().Be(p3);
        pl[1].Should().Be(p2);
        pl[2].Should().Be(p1);
    }

    // ─────────────────────────────────────────────
    // Edges (open — n-1 segments, no wrap-around)
    // ─────────────────────────────────────────────

    [Fact]
    public void Edges_ReturnsNMinus1Segments()
    {
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(10, 10));
        var edges = pl.Edges();
        edges.Length.Should().Be(2);
        var edgesSpan = edges.Span;
        edgesSpan[0].Start.Should().Be(new Point<float>(0, 0));
        edgesSpan[0].End.Should().Be(new Point<float>(10, 0));
        edgesSpan[1].Start.Should().Be(new Point<float>(10, 0));
        edgesSpan[1].End.Should().Be(new Point<float>(10, 10));
    }

    [Fact]
    public void Edges_NoWrapAround()
    {
        // A triangle polygon would have 3 edges; as polyline it should have 2
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(5, 10));
        var edges = pl.Edges();
        edges.Length.Should().Be(2);
        // No edge from (5,10) back to (0,0)
    }

    [Fact]
    public void Edges_SinglePoint_Empty()
    {
        var pl = new Polyline<float>(new Point<float>(5, 5));
        pl.Edges().Span.ToArray().Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // Operators
    // ─────────────────────────────────────────────

    [Fact]
    public void PlusVector_TranslatesAll()
    {
        var pl = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var moved = pl + new Vector<float>(5, 3);
        moved[0].Should().Be(new Point<float>(5, 3));
        moved[1].Should().Be(new Point<float>(15, 3));
    }

    [Fact]
    public void MinusVector_TranslatesAll()
    {
        var pl = new Polyline<float>(new Point<float>(5, 3), new Point<float>(15, 3));
        var moved = pl - new Vector<float>(5, 3);
        moved[0].X.Should().BeApproximately(0f, Tol);
        moved[0].Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void MultiplySize_ScalesPoints()
    {
        var pl = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var scaled = pl * new Size<float>(2, 3);
        scaled[0].X.Should().BeApproximately(2f, Tol);
        scaled[0].Y.Should().BeApproximately(6f, Tol);
        scaled[1].X.Should().BeApproximately(6f, Tol);
        scaled[1].Y.Should().BeApproximately(12f, Tol);
    }

    [Fact]
    public void PlusDegree_Rotates()
    {
        var pl = new Polyline<float>(new Point<float>(1, 0), new Point<float>(2, 0));
        var rotated = pl + Degree<float>.Create(90);
        rotated[0].X.Should().BeApproximately(0f, Tol);
        rotated[0].Y.Should().BeApproximately(1f, Tol);
    }

    // ─────────────────────────────────────────────
    // Equality
    // ─────────────────────────────────────────────

    [Fact]
    public void Equality_SamePoints_AreEqual()
    {
        var a = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var b = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentPoints_AreNotEqual()
    {
        var a = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var b = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 5));
        a.Should().NotBe(b);
    }

    // ─────────────────────────────────────────────
    // Simplify (Ramer-Douglas-Peucker)
    // ─────────────────────────────────────────────

    [Fact]
    public void Simplify_StraightLine_RemovesCollinearPoints()
    {
        // 5 collinear points → should reduce to 2 endpoints
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(2, 0),
            new Point<float>(5, 0),
            new Point<float>(7, 0),
            new Point<float>(10, 0));
        var simplified = pl.Simplify(0.1f);
        simplified.Count.Should().Be(2);
        simplified[0].Should().Be(new Point<float>(0, 0));
        simplified[1].Should().Be(new Point<float>(10, 0));
    }

    [Fact]
    public void Simplify_LShape_KeepsCorner()
    {
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(10, 0),
            new Point<float>(10, 10));
        var simplified = pl.Simplify(1f);
        simplified.Count.Should().Be(3);
    }

    [Fact]
    public void Simplify_SmallDeviation_RemovedByLargeEpsilon()
    {
        // Point deviates by 0.5 from the straight line
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(5, 0.5f),
            new Point<float>(10, 0));
        pl.Simplify(0.3f).Count.Should().Be(3);  // 0.5 > 0.3 → kept
        pl.Simplify(1f).Count.Should().Be(2);     // 0.5 < 1.0 → removed
    }

    [Fact]
    public void Simplify_TwoPoints_ReturnsSelf()
    {
        var pl = new Polyline<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var simplified = pl.Simplify(1f);
        simplified.Count.Should().Be(2);
    }

    [Fact]
    public void Simplify_PreservesEndpoints()
    {
        var pl = new Polyline<float>(
            new Point<float>(0, 0),
            new Point<float>(3, 0.1f),
            new Point<float>(7, -0.1f),
            new Point<float>(10, 0));
        var simplified = pl.Simplify(5f);
        simplified.Count.Should().Be(2);
        simplified[0].Should().Be(new Point<float>(0, 0));
        simplified[1].Should().Be(new Point<float>(10, 0));
    }

    // ─────────────────────────────────────────────
    // Points list
    // ─────────────────────────────────────────────

    [Fact]
    public void Points_ReturnsReadOnlyList()
    {
        var pl = new Polyline<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        pl.Points.Count.Should().Be(2);
        pl.Points[0].Should().Be(new Point<float>(1, 2));
    }
}
