using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Polyline3Tests
{
    private const float Tol = 1e-4f;

    // ─────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultCtor_Empty()
    {
        var pl = new Polyline3<float>();
        pl.Count.Should().Be(0);
        pl.AsSpan().Length.Should().Be(0);
    }

    [Fact]
    public void ParamsCtor_StoresPoints()
    {
        var p1 = new Point3<float>(0, 0, 0);
        var p2 = new Point3<float>(1, 0, 0);
        var p3 = new Point3<float>(1, 1, 0);
        var pl = new Polyline3<float>(p1, p2, p3);
        pl.Count.Should().Be(3);
        pl[0].Should().Be(p1);
        pl[1].Should().Be(p2);
        pl[2].Should().Be(p3);
    }

    [Fact]
    public void ListCtor_CopiesPoints()
    {
        var list = new List<Point3<float>>
        {
            new(0, 0, 0), new(5, 5, 5), new(10, 0, 0)
        };
        var pl = new Polyline3<float>(list);
        pl.Count.Should().Be(3);
        pl[2].Should().Be(new Point3<float>(10, 0, 0));
    }

    [Fact]
    public void CoordsCtor_ParsesTriplets()
    {
        var coords = new float[] { 0, 0, 0, 10, 0, 0, 10, 10, 0 };
        var pl = new Polyline3<float>(coords);
        pl.Count.Should().Be(3);
        pl[1].Should().Be(new Point3<float>(10, 0, 0));
    }

    [Fact]
    public void MemoryCtor_ZeroCopy()
    {
        var arr = new Point3<float>[] { new(1, 2, 3), new(4, 5, 6), new(7, 8, 9) };
        var mem = new ReadOnlyMemory<Point3<float>>(arr);
        var pl = new Polyline3<float>(mem);
        pl.Count.Should().Be(3);
        pl[0].Should().Be(new Point3<float>(1, 2, 3));
    }

    // ─────────────────────────────────────────────
    // Start / End
    // ─────────────────────────────────────────────

    [Fact]
    public void Start_ReturnsFirstPoint()
    {
        var pl = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        pl.Start.Should().Be(new Point3<float>(1, 2, 3));
    }

    [Fact]
    public void End_ReturnsLastPoint()
    {
        var pl = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        pl.End.Should().Be(new Point3<float>(4, 5, 6));
    }

    // ─────────────────────────────────────────────
    // Length
    // ─────────────────────────────────────────────

    [Fact]
    public void Length_StraightLine()
    {
        var pl = new Polyline3<float>(new Point3<float>(0, 0, 0), new Point3<float>(10, 0, 0));
        pl.Length().Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Length_MultiSegment()
    {
        // L-shaped in 3D: (0,0,0)->(10,0,0)->(10,5,0)
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0),
            new Point3<float>(10, 5, 0));
        pl.Length().Should().BeApproximately(15f, Tol);
    }

    [Fact]
    public void Length_Diagonal3D()
    {
        // Diagonal from origin to (1,1,1), length = sqrt(3)
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(1, 1, 1));
        pl.Length().Should().BeApproximately(MathF.Sqrt(3f), Tol);
    }

    [Fact]
    public void Length_SinglePoint_ReturnsZero()
    {
        var pl = new Polyline3<float>(new Point3<float>(5, 5, 5));
        pl.Length().Should().Be(0f);
    }

    // ─────────────────────────────────────────────
    // Centroid
    // ─────────────────────────────────────────────

    [Fact]
    public void Centroid_SinglePoint_ReturnsThatPoint()
    {
        var pl = new Polyline3<float>(new Point3<float>(3, 4, 5));
        var c = pl.Centroid();
        c.X.Should().BeApproximately(3f, Tol);
        c.Y.Should().BeApproximately(4f, Tol);
        c.Z.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void Centroid_TwoPoints_IsMidpoint()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0));
        var c = pl.Centroid();
        c.X.Should().BeApproximately(5f, Tol);
        c.Y.Should().BeApproximately(0f, Tol);
        c.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Centroid_EqualSegments_IsGeometricCenter()
    {
        // Three equal-length segments along X: 0->10->20->30
        // Segment midpoints: 5, 15, 25 — all with equal length
        // Centroid X = (5+15+25)/3 = 15
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0),
            new Point3<float>(20, 0, 0),
            new Point3<float>(30, 0, 0));
        var c = pl.Centroid();
        c.X.Should().BeApproximately(15f, Tol);
        c.Y.Should().BeApproximately(0f, Tol);
        c.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Centroid_UnequalSegments_WeightedByLength()
    {
        // Segment 1: (0,0,0)->(10,0,0) length=10 midpoint=(5,0,0)
        // Segment 2: (10,0,0)->(11,0,0) length=1  midpoint=(10.5,0,0)
        // Centroid X = (5*10 + 10.5*1) / 11 = 60.5/11 ≈ 5.5
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0),
            new Point3<float>(11, 0, 0));
        var c = pl.Centroid();
        c.X.Should().BeApproximately(60.5f / 11f, Tol);
    }

    [Fact]
    public void Centroid_3D_AllAxes()
    {
        // Segment along Z axis: (0,0,0)->(0,0,10)
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(0, 0, 10));
        var c = pl.Centroid();
        c.X.Should().BeApproximately(0f, Tol);
        c.Y.Should().BeApproximately(0f, Tol);
        c.Z.Should().BeApproximately(5f, Tol);
    }

    // ─────────────────────────────────────────────
    // Reverse
    // ─────────────────────────────────────────────

    [Fact]
    public void Reverse_FlipsOrder()
    {
        var p1 = new Point3<float>(0, 0, 0);
        var p2 = new Point3<float>(1, 0, 0);
        var p3 = new Point3<float>(2, 0, 0);
        var pl = new Polyline3<float>(p1, p2, p3).Reverse();
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
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0),
            new Point3<float>(10, 10, 0));
        var edges = pl.Edges();
        edges.Length.Should().Be(2);
        var edgesSpan = edges.Span;
        edgesSpan[0].Start.Should().Be(new Point3<float>(0, 0, 0));
        edgesSpan[0].End.Should().Be(new Point3<float>(10, 0, 0));
        edgesSpan[1].Start.Should().Be(new Point3<float>(10, 0, 0));
        edgesSpan[1].End.Should().Be(new Point3<float>(10, 10, 0));
    }

    [Fact]
    public void Edges_SinglePoint_Empty()
    {
        var pl = new Polyline3<float>(new Point3<float>(5, 5, 5));
        pl.Edges().Span.ToArray().Should().BeEmpty();
    }

    // ─────────────────────────────────────────────
    // Operators
    // ─────────────────────────────────────────────

    [Fact]
    public void PlusVector_TranslatesAll()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0));
        var moved = pl + new Vector3<float>(5, 3, 1);
        moved[0].Should().Be(new Point3<float>(5, 3, 1));
        moved[1].Should().Be(new Point3<float>(15, 3, 1));
    }

    [Fact]
    public void MinusVector_TranslatesAll()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(5, 3, 1),
            new Point3<float>(15, 3, 1));
        var moved = pl - new Vector3<float>(5, 3, 1);
        moved[0].X.Should().BeApproximately(0f, Tol);
        moved[0].Y.Should().BeApproximately(0f, Tol);
        moved[0].Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void MultiplyScalar_ScalesAll()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(1, 2, 3),
            new Point3<float>(4, 5, 6));
        var scaled = pl * 2f;
        scaled[0].Should().Be(new Point3<float>(2, 4, 6));
        scaled[1].Should().Be(new Point3<float>(8, 10, 12));
    }

    [Fact]
    public void DivideScalar_DividesAll()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(2, 4, 6),
            new Point3<float>(8, 10, 12));
        var scaled = pl / 2f;
        scaled[0].Should().Be(new Point3<float>(1, 2, 3));
        scaled[1].Should().Be(new Point3<float>(4, 5, 6));
    }

    // ─────────────────────────────────────────────
    // Equality
    // ─────────────────────────────────────────────

    [Fact]
    public void Equality_SamePoints_AreEqual()
    {
        var a = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        var b = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentPoints_AreNotEqual()
    {
        var a = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        var b = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 7));
        a.Should().NotBe(b);
    }

    // ─────────────────────────────────────────────
    // Simplify (Ramer-Douglas-Peucker 3D)
    // ─────────────────────────────────────────────

    [Fact]
    public void Simplify_StraightLine_RemovesCollinearPoints()
    {
        // 5 collinear points along X → should reduce to 2 endpoints
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(2, 0, 0),
            new Point3<float>(5, 0, 0),
            new Point3<float>(7, 0, 0),
            new Point3<float>(10, 0, 0));
        var simplified = pl.Simplify(0.1f);
        simplified.Count.Should().Be(2);
        simplified[0].Should().Be(new Point3<float>(0, 0, 0));
        simplified[1].Should().Be(new Point3<float>(10, 0, 0));
    }

    [Fact]
    public void Simplify_CollinearAlongDiagonal3D()
    {
        // Points along (1,1,1) direction — all collinear
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(1, 1, 1),
            new Point3<float>(2, 2, 2),
            new Point3<float>(3, 3, 3));
        var simplified = pl.Simplify(0.1f);
        simplified.Count.Should().Be(2);
    }

    [Fact]
    public void Simplify_LShape_KeepsCorner()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0),
            new Point3<float>(10, 10, 0));
        var simplified = pl.Simplify(1f);
        simplified.Count.Should().Be(3);
    }

    [Fact]
    public void Simplify_3DDeviation()
    {
        // Point deviates in Z only
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(5, 0, 2f), // 2 units off the line in Z
            new Point3<float>(10, 0, 0));
        pl.Simplify(1f).Count.Should().Be(3);  // 2 > 1 → kept
        pl.Simplify(3f).Count.Should().Be(2);   // 2 < 3 → removed
    }

    [Fact]
    public void Simplify_TwoPoints_ReturnsSelf()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0));
        var simplified = pl.Simplify(1f);
        simplified.Count.Should().Be(2);
    }

    // ─────────────────────────────────────────────
    // Scale
    // ─────────────────────────────────────────────

    [Fact]
    public void Scale_AroundCentroid_PreservesCentroid()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0));
        var centroid = pl.Centroid();
        var scaled = pl.Scale(2f);
        var scaledCentroid = scaled.Centroid();
        scaledCentroid.X.Should().BeApproximately(centroid.X, Tol);
        scaledCentroid.Y.Should().BeApproximately(centroid.Y, Tol);
        scaledCentroid.Z.Should().BeApproximately(centroid.Z, Tol);
    }

    [Fact]
    public void Scale_DoublesLength()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(10, 0, 0));
        var scaled = pl.Scale(2f);
        scaled.Length().Should().BeApproximately(20f, Tol);
    }

    // ─────────────────────────────────────────────
    // Points list
    // ─────────────────────────────────────────────

    [Fact]
    public void Points_ReturnsReadOnlyList()
    {
        var pl = new Polyline3<float>(new Point3<float>(1, 2, 3), new Point3<float>(4, 5, 6));
        pl.Points.Count.Should().Be(2);
        pl.Points[0].Should().Be(new Point3<float>(1, 2, 3));
    }

    // ─────────────────────────────────────────────
    // ToString
    // ─────────────────────────────────────────────

    [Fact]
    public void ToString_ShowsCount()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(0, 0, 0),
            new Point3<float>(1, 1, 1));
        pl.ToString().Should().Be("Polyline3(2 points)");
    }

    // ─────────────────────────────────────────────
    // JSON round-trip
    // ─────────────────────────────────────────────

    [Fact]
    public void Json_RoundTrip_Float()
    {
        var pl = new Polyline3<float>(
            new Point3<float>(1, 2, 3),
            new Point3<float>(4, 5, 6));
        var json = System.Text.Json.JsonSerializer.Serialize(pl);
        json.Should().Be("[1,2,3,4,5,6]");
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Polyline3<float>>(json);
        deserialized.Should().Be(pl);
    }

    [Fact]
    public void Json_RoundTrip_Double()
    {
        var pl = new Polyline3<double>(
            new Point3<double>(1.5, 2.5, 3.5),
            new Point3<double>(4.5, 5.5, 6.5));
        var json = System.Text.Json.JsonSerializer.Serialize(pl);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Polyline3<double>>(json);
        deserialized.Should().Be(pl);
    }

    [Fact]
    public void Json_Empty()
    {
        var pl = new Polyline3<float>();
        var json = System.Text.Json.JsonSerializer.Serialize(pl);
        json.Should().Be("[]");
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Polyline3<float>>(json);
        deserialized.Count.Should().Be(0);
    }
}
