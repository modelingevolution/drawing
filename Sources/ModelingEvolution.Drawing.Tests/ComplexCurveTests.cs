using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class ComplexCurveTests
{
    private static readonly float Tol = 1e-5f;

    private static BezierCurve<float> MakeBezier() =>
        new(new Point<float>(0, 0), new Point<float>(1, 2),
            new Point<float>(3, 2), new Point<float>(4, 0));

    private static Point<float>[] MakePolyPoints() =>
    [
        new(0, 0), new(1, 1), new(2, 0), new(3, 1), new(4, 0)
    ];

    [Fact]
    public void Empty_Default_IsEmpty()
    {
        var curve = default(ComplexCurve<float>);

        curve.IsEmpty.Should().BeTrue();
        curve.SegmentCount.Should().Be(0);
        curve.ByteLength.Should().Be(0);
    }

    [Fact]
    public void Empty_Enumerator_YieldsNothing()
    {
        var curve = default(ComplexCurve<float>);
        int count = 0;
        foreach (var _ in curve)
            count++;
        count.Should().Be(0);
    }

    [Fact]
    public void SingleBezier_RoundTrip()
    {
        var bezier = MakeBezier();
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(bezier)
            .Build();

        curve.IsEmpty.Should().BeFalse();
        curve.SegmentCount.Should().Be(1);

        foreach (var seg in curve)
        {
            seg.IsBezier.Should().BeTrue();
            seg.IsPolyline.Should().BeFalse();
            seg.PointCount.Should().Be(4);

            var decoded = seg.AsBezier();
            decoded.Start.X.Should().BeApproximately(0f, Tol);
            decoded.Start.Y.Should().BeApproximately(0f, Tol);
            decoded.C0.X.Should().BeApproximately(1f, Tol);
            decoded.C0.Y.Should().BeApproximately(2f, Tol);
            decoded.C1.X.Should().BeApproximately(3f, Tol);
            decoded.C1.Y.Should().BeApproximately(2f, Tol);
            decoded.End.X.Should().BeApproximately(4f, Tol);
            decoded.End.Y.Should().BeApproximately(0f, Tol);
        }
    }

    [Fact]
    public void SinglePolyline_RoundTrip()
    {
        var pts = MakePolyPoints();
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(pts)
            .Build();

        curve.SegmentCount.Should().Be(1);

        foreach (var seg in curve)
        {
            seg.IsPolyline.Should().BeTrue();
            seg.IsBezier.Should().BeFalse();
            seg.PointCount.Should().Be(5);

            var decoded = seg.AsPoints();
            decoded.Length.Should().Be(5);
            for (int i = 0; i < pts.Length; i++)
            {
                decoded[i].X.Should().BeApproximately(pts[i].X, Tol);
                decoded[i].Y.Should().BeApproximately(pts[i].Y, Tol);
            }
        }
    }

    [Fact]
    public void Mixed_BezierPolylineBezier_Order()
    {
        var b1 = MakeBezier();
        var pts = MakePolyPoints();
        var b2 = new BezierCurve<float>(
            new Point<float>(10, 10), new Point<float>(11, 12),
            new Point<float>(13, 12), new Point<float>(14, 10));

        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(b1)
            .AddPoints(pts)
            .AddBezier(b2)
            .Build();

        curve.SegmentCount.Should().Be(3);

        int index = 0;
        foreach (var seg in curve)
        {
            switch (index)
            {
                case 0:
                    seg.IsBezier.Should().BeTrue();
                    seg.AsBezier().Start.X.Should().BeApproximately(0f, Tol);
                    break;
                case 1:
                    seg.IsPolyline.Should().BeTrue();
                    seg.AsPoints().Length.Should().Be(5);
                    break;
                case 2:
                    seg.IsBezier.Should().BeTrue();
                    seg.AsBezier().Start.X.Should().BeApproximately(10f, Tol);
                    break;
            }
            index++;
        }
        index.Should().Be(3);
    }

    [Fact]
    public void AddSegment_StoredAs2PointPolyline()
    {
        var seg = new Segment<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var curve = new ComplexCurveBuilder<float>()
            .AddSegment(seg)
            .Build();

        curve.SegmentCount.Should().Be(1);

        foreach (var s in curve)
        {
            s.IsPolyline.Should().BeTrue();
            s.PointCount.Should().Be(2);
            var pts = s.AsPoints();
            pts[0].X.Should().BeApproximately(1f, Tol);
            pts[0].Y.Should().BeApproximately(2f, Tol);
            pts[1].X.Should().BeApproximately(3f, Tol);
            pts[1].Y.Should().BeApproximately(4f, Tol);
        }
    }

    [Fact]
    public void ByteLength_Bezier_Correct()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        // 1 tag byte + 4 Points × 2 floats × 4 bytes = 1 + 32 = 33
        var expectedBezierSize = 1 + Unsafe.SizeOf<BezierCurve<float>>();
        curve.ByteLength.Should().Be(expectedBezierSize);
    }

    [Fact]
    public void ByteLength_Polyline_Correct()
    {
        var pts = MakePolyPoints(); // 5 points
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(pts)
            .Build();

        // 1 tag + 2 count + 5 × 8 = 43
        var expectedSize = 1 + sizeof(ushort) + 5 * Unsafe.SizeOf<Point<float>>();
        curve.ByteLength.Should().Be(expectedSize);
    }

    [Fact]
    public void ByteLength_Mixed_Correct()
    {
        var pts = MakePolyPoints(); // 5 points
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .AddPoints(pts)
            .Build();

        var bezierPart = 1 + Unsafe.SizeOf<BezierCurve<float>>();
        var polylinePart = 1 + sizeof(ushort) + 5 * Unsafe.SizeOf<Point<float>>();
        curve.ByteLength.Should().Be(bezierPart + polylinePart);
    }

    [Fact]
    public void Densify_BezierSegment_ArcLength()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        var densified = curve.Densify();
        densified.Count.Should().BeGreaterThan(2);

        // First point should be near Start
        var span = densified.AsSpan();
        span[0].X.Should().BeApproximately(0f, Tol);
        span[0].Y.Should().BeApproximately(0f, Tol);
        // Last point should be near End
        span[^1].X.Should().BeApproximately(4f, Tol);
        span[^1].Y.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Densify_Mixed_CombinesPoints()
    {
        var pts = MakePolyPoints(); // 5 points
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .AddPoints(pts)
            .Build();

        var densified = curve.Densify();
        // Should have points from Bezier densification + polyline densification
        densified.Count.Should().BeGreaterThan(5);
    }

    [Fact]
    public void Densify_Empty_ReturnsEmptyPolyline()
    {
        var curve = default(ComplexCurve<float>);
        var densified = curve.Densify();
        densified.Count.Should().Be(0);
    }

    [Fact]
    public void BoundingBox_SingleBezier()
    {
        // Bezier: (0,0) (1,2) (3,2) (4,0) — control hull bounds
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        var bb = curve.BoundingBox();
        bb.X.Should().BeApproximately(0f, Tol);
        bb.Y.Should().BeApproximately(0f, Tol);
        bb.Width.Should().BeApproximately(4f, Tol);
        bb.Height.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void BoundingBox_Polyline()
    {
        var pts = new Point<float>[] { new(1, 2), new(5, 8), new(3, 0) };
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(pts)
            .Build();

        var bb = curve.BoundingBox();
        bb.X.Should().BeApproximately(1f, Tol);
        bb.Y.Should().BeApproximately(0f, Tol);
        bb.Width.Should().BeApproximately(4f, Tol);
        bb.Height.Should().BeApproximately(8f, Tol);
    }

    [Fact]
    public void BoundingBox_Empty_ReturnsDefault()
    {
        var curve = default(ComplexCurve<float>);
        var bb = curve.BoundingBox();
        bb.Should().Be(default(Rectangle<float>));
    }

    [Fact]
    public void TransformBy_Translation()
    {
        var bezier = MakeBezier();
        var pts = MakePolyPoints();
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(bezier)
            .AddPoints(pts)
            .Build();

        // Translate by (10, 20)
        var m = Matrix<float>.Identity;
        m.Translate(10f, 20f);
        var transformed = curve.TransformBy(m);

        transformed.SegmentCount.Should().Be(2);

        int idx = 0;
        foreach (var seg in transformed)
        {
            if (idx == 0)
            {
                seg.IsBezier.Should().BeTrue();
                var b = seg.AsBezier();
                b.Start.X.Should().BeApproximately(10f, Tol);
                b.Start.Y.Should().BeApproximately(20f, Tol);
                b.End.X.Should().BeApproximately(14f, Tol);
                b.End.Y.Should().BeApproximately(20f, Tol);
            }
            else
            {
                seg.IsPolyline.Should().BeTrue();
                var p = seg.AsPoints();
                p[0].X.Should().BeApproximately(10f, Tol);
                p[0].Y.Should().BeApproximately(20f, Tol);
                p[4].X.Should().BeApproximately(14f, Tol);
                p[4].Y.Should().BeApproximately(20f, Tol);
            }
            idx++;
        }
    }

    [Fact]
    public void TransformBy_Empty_ReturnsEmpty()
    {
        var curve = default(ComplexCurve<float>);
        var m = Matrix<float>.Identity;
        m.Translate(10f, 20f);
        var transformed = curve.TransformBy(m);
        transformed.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void LargeCurve_100Segments()
    {
        var builder = new ComplexCurveBuilder<float>(4096);
        for (int i = 0; i < 50; i++)
        {
            float x = i * 10f;
            builder.AddBezier(new BezierCurve<float>(
                new Point<float>(x, 0), new Point<float>(x + 2, 3),
                new Point<float>(x + 5, 3), new Point<float>(x + 7, 0)));
        }
        for (int i = 0; i < 50; i++)
        {
            float x = i * 5f;
            builder.AddPoints([
                new Point<float>(x, 0), new Point<float>(x + 1, 1),
                new Point<float>(x + 2, 0)
            ]);
        }
        var curve = builder.Build();

        curve.SegmentCount.Should().Be(100);

        int count = 0;
        int bezierCount = 0;
        int polylineCount = 0;
        foreach (var seg in curve)
        {
            if (seg.IsBezier) bezierCount++;
            else polylineCount++;
            count++;
        }
        count.Should().Be(100);
        bezierCount.Should().Be(50);
        polylineCount.Should().Be(50);
    }

    [Fact]
    public void PointCount_Bezier_Is4()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        foreach (var seg in curve)
            seg.PointCount.Should().Be(4);
    }

    [Fact]
    public void PointCount_Polyline_MatchesInput()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(0, 0), new(1, 1), new(2, 2) })
            .Build();

        foreach (var seg in curve)
            seg.PointCount.Should().Be(3);
    }

    [Fact]
    public void DoublePrecision_RoundTrip()
    {
        var bezier = new BezierCurve<double>(
            new Point<double>(0.123456789, 0.987654321),
            new Point<double>(1.111111111, 2.222222222),
            new Point<double>(3.333333333, 2.444444444),
            new Point<double>(4.555555555, 0.666666666));

        var curve = new ComplexCurveBuilder<double>()
            .AddBezier(bezier)
            .Build();

        foreach (var seg in curve)
        {
            var decoded = seg.AsBezier();
            decoded.Start.X.Should().BeApproximately(0.123456789, 1e-12);
            decoded.Start.Y.Should().BeApproximately(0.987654321, 1e-12);
            decoded.End.X.Should().BeApproximately(4.555555555, 1e-12);
            decoded.End.Y.Should().BeApproximately(0.666666666, 1e-12);
        }
    }

    [Fact]
    public void DoublePrecision_ByteLength()
    {
        var curve = new ComplexCurveBuilder<double>()
            .AddBezier(new BezierCurve<double>(
                new Point<double>(0, 0), new Point<double>(1, 1),
                new Point<double>(2, 2), new Point<double>(3, 3)))
            .Build();

        // 1 tag + 4 points × 2 doubles × 8 bytes = 1 + 64 = 65
        var expected = 1 + Unsafe.SizeOf<BezierCurve<double>>();
        curve.ByteLength.Should().Be(expected);
    }

    // ════════════════════════════════════════════════
    //  Operator tests
    // ════════════════════════════════════════════════

    [Fact]
    public void Operator_PlusVector_TranslatesAll()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .AddPoints(MakePolyPoints())
            .Build();

        var shifted = curve + new Vector<float>(10, 20);

        int idx = 0;
        foreach (var seg in shifted)
        {
            if (idx == 0)
            {
                var b = seg.AsBezier();
                b.Start.X.Should().BeApproximately(10f, Tol);
                b.Start.Y.Should().BeApproximately(20f, Tol);
            }
            else
            {
                var pts = seg.AsPoints();
                pts[0].X.Should().BeApproximately(10f, Tol);
                pts[0].Y.Should().BeApproximately(20f, Tol);
            }
            idx++;
        }
    }

    [Fact]
    public void Operator_MinusVector_TranslatesAll()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(10, 20), new(14, 20) })
            .Build();

        var shifted = curve - new Vector<float>(10, 20);

        foreach (var seg in shifted)
        {
            var pts = seg.AsPoints();
            pts[0].X.Should().BeApproximately(0f, Tol);
            pts[0].Y.Should().BeApproximately(0f, Tol);
        }
    }

    [Fact]
    public void Operator_MultiplySize_Scales()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(1, 2), new(3, 4) })
            .Build();

        var scaled = curve * new Size<float>(2, 3);

        foreach (var seg in scaled)
        {
            var pts = seg.AsPoints();
            pts[0].X.Should().BeApproximately(2f, Tol);
            pts[0].Y.Should().BeApproximately(6f, Tol);
            pts[1].X.Should().BeApproximately(6f, Tol);
            pts[1].Y.Should().BeApproximately(12f, Tol);
        }
    }

    [Fact]
    public void Operator_DivideSize_Scales()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(4, 6), new(8, 12) })
            .Build();

        var scaled = curve / new Size<float>(2, 3);

        foreach (var seg in scaled)
        {
            var pts = seg.AsPoints();
            pts[0].X.Should().BeApproximately(2f, Tol);
            pts[0].Y.Should().BeApproximately(2f, Tol);
        }
    }

    [Fact]
    public void Operator_PlusDegree_Rotates()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(1, 0), new(2, 0) })
            .Build();

        var rotated = curve + Degree<float>.Create(90);

        foreach (var seg in rotated)
        {
            var pts = seg.AsPoints();
            pts[0].X.Should().BeApproximately(0f, 0.01f);
            pts[0].Y.Should().BeApproximately(1f, 0.01f);
        }
    }

    [Fact]
    public void Operator_Empty_ReturnsEmpty()
    {
        var curve = default(ComplexCurve<float>);
        var shifted = curve + new Vector<float>(1, 1);
        shifted.IsEmpty.Should().BeTrue();
    }

    // ════════════════════════════════════════════════
    //  Length
    // ════════════════════════════════════════════════

    [Fact]
    public void Length_StraightPolyline()
    {
        // Simple horizontal line from (0,0) to (10,0) via (5,0)
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(0, 0), new(5, 0), new(10, 0) })
            .Build();

        curve.Length().Should().BeApproximately(10f, 0.01f);
    }

    [Fact]
    public void Length_Empty_IsZero()
    {
        var curve = default(ComplexCurve<float>);
        curve.Length().Should().Be(0f);
    }

    [Fact]
    public void Length_Bezier_Positive()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        var len = curve.Length();
        len.Should().BeGreaterThan(4f); // Must be longer than the chord (0,0)-(4,0)
    }

    // ════════════════════════════════════════════════
    //  IParsable / ToString
    // ════════════════════════════════════════════════

    [Fact]
    public void ToString_Empty_ReturnsEmpty()
    {
        var curve = default(ComplexCurve<float>);
        curve.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ToString_Bezier_ContainsMAndC()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        var str = curve.ToString();
        str.Should().Contain("M");
        str.Should().Contain("C");
    }

    [Fact]
    public void ToString_Polyline_ContainsMAndL()
    {
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(MakePolyPoints())
            .Build();

        var str = curve.ToString();
        str.Should().Contain("M");
        str.Should().Contain("L");
    }

    [Fact]
    public void Parse_RoundTrip_Bezier()
    {
        var original = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .Build();

        var str = original.ToString();
        var parsed = ComplexCurve<float>.Parse(str, null);

        parsed.SegmentCount.Should().Be(1);
        foreach (var seg in parsed)
        {
            seg.IsBezier.Should().BeTrue();
            var b = seg.AsBezier();
            b.Start.X.Should().BeApproximately(0f, 0.01f);
            b.End.X.Should().BeApproximately(4f, 0.01f);
        }
    }

    [Fact]
    public void Parse_RoundTrip_Polyline()
    {
        var pts = MakePolyPoints();
        var original = new ComplexCurveBuilder<float>()
            .AddPoints(pts)
            .Build();

        var str = original.ToString();
        var parsed = ComplexCurve<float>.Parse(str, null);

        parsed.SegmentCount.Should().Be(1);
        foreach (var seg in parsed)
        {
            seg.IsPolyline.Should().BeTrue();
            var decoded = seg.AsPoints();
            decoded.Length.Should().Be(5);
            decoded[0].X.Should().BeApproximately(0f, 0.01f);
            decoded[4].X.Should().BeApproximately(4f, 0.01f);
        }
    }

    [Fact]
    public void Parse_RoundTrip_Mixed()
    {
        var original = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .AddPoints(new Point<float>[] { new(10, 0), new(11, 1), new(12, 0) })
            .Build();

        var str = original.ToString();
        var parsed = ComplexCurve<float>.Parse(str, null);

        parsed.SegmentCount.Should().Be(2);
        int idx = 0;
        foreach (var seg in parsed)
        {
            if (idx == 0) seg.IsBezier.Should().BeTrue();
            else seg.IsPolyline.Should().BeTrue();
            idx++;
        }
    }

    [Fact]
    public void TryParse_Empty_ReturnsTrue()
    {
        ComplexCurve<float>.TryParse("", null, out var result).Should().BeTrue();
        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TryParse_Null_ReturnsTrue()
    {
        ComplexCurve<float>.TryParse(null, null, out var result).Should().BeTrue();
        result.IsEmpty.Should().BeTrue();
    }

    // ════════════════════════════════════════════════
    //  JSON converter
    // ════════════════════════════════════════════════

    [Fact]
    public void Json_RoundTrip()
    {
        var original = new ComplexCurveBuilder<float>()
            .AddBezier(MakeBezier())
            .AddPoints(MakePolyPoints())
            .Build();

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ComplexCurve<float>>(json);

        deserialized.SegmentCount.Should().Be(2);
    }

    [Fact]
    public void Json_Empty()
    {
        var original = default(ComplexCurve<float>);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ComplexCurve<float>>(json);
        deserialized.IsEmpty.Should().BeTrue();
    }

    // ════════════════════════════════════════════════
    //  Intersections
    // ════════════════════════════════════════════════

    [Fact]
    public void Intersect_Line_Polyline()
    {
        // Polyline from (0,0) to (10,0) — horizontal
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(0, -5), new(0, 5) })
            .Build();

        // Horizontal line y = 0
        var line = Line<float>.From(new Point<float>(0, 0), new Vector<float>(1, 0));
        var hits = curve.Intersect(line);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Intersect_Segment_Polyline()
    {
        // Vertical polyline crossing a horizontal segment
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(5, -5), new(5, 5) })
            .Build();

        var segment = new Segment<float>(new Point<float>(0, 0), new Point<float>(10, 0));
        var hits = curve.Intersect(segment);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
        hits.Span[0].X.Should().BeApproximately(5f, 0.1f);
        hits.Span[0].Y.Should().BeApproximately(0f, 0.1f);
    }

    [Fact]
    public void Intersect_Rectangle_Polyline()
    {
        // Diagonal polyline crossing a rectangle
        var curve = new ComplexCurveBuilder<float>()
            .AddPoints(new Point<float>[] { new(-5, -5), new(5, 5) })
            .Build();

        var rect = new Rectangle<float>(0, 0, 3, 3);
        var hits = curve.Intersect(rect);
        hits.Length.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Intersect_Empty_ReturnsEmpty()
    {
        var curve = default(ComplexCurve<float>);
        var line = Line<float>.From(new Point<float>(0, 0), new Vector<float>(1, 0));
        curve.Intersect(line).Length.Should().Be(0);
    }

    // ════════════════════════════════════════════════
    //  PolylineBuilder
    // ════════════════════════════════════════════════

    [Fact]
    public void PolylineBuilder_AddAndBuild()
    {
        var builder = new PolylineBuilder<float>();
        builder.Add(new Point<float>(0, 0));
        builder.Add(new Point<float>(1, 1));
        builder.Add(new Point<float>(2, 0));

        var poly = builder.Build();
        poly.Count.Should().Be(3);
        poly[0].X.Should().BeApproximately(0f, Tol);
        poly[2].X.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void PolylineBuilder_AddRange()
    {
        var builder = new PolylineBuilder<float>();
        var pts = new Point<float>[] { new(0, 0), new(1, 1), new(2, 0) };
        builder.AddRange(pts);

        var more = new Point<float>[] { new(3, 1), new(4, 0) };
        builder.AddRange(more);

        var poly = builder.Build();
        poly.Count.Should().Be(5);
    }

    [Fact]
    public void PolylineBuilder_AddPolyline()
    {
        var p1 = new Polyline<float>(new Point<float>(0, 0), new Point<float>(1, 1));
        var p2 = new Polyline<float>(new Point<float>(2, 2), new Point<float>(3, 3));

        var builder = new PolylineBuilder<float>();
        builder.AddPolyline(p1);
        builder.AddPolyline(p2);

        var result = builder.Build();
        result.Count.Should().Be(4);
    }

    [Fact]
    public void PolylineBuilder_Empty_Build()
    {
        var builder = new PolylineBuilder<float>();
        var poly = builder.Build();
        poly.Count.Should().Be(0);
    }

    // ════════════════════════════════════════════════
    //  BezierCurve.Densify
    // ════════════════════════════════════════════════

    [Fact]
    public void BezierCurve_Densify_StartsAndEnds()
    {
        var bezier = MakeBezier();
        var densified = bezier.Densify();
        var span = densified.Span;

        span.Length.Should().BeGreaterThan(2);
        span[0].X.Should().BeApproximately(0f, Tol);
        span[0].Y.Should().BeApproximately(0f, Tol);
        span[^1].X.Should().BeApproximately(4f, Tol);
        span[^1].Y.Should().BeApproximately(0f, Tol);
    }
}
