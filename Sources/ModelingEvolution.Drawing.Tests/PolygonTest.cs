using System;
using System.Buffers;
using System.Drawing;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ModelingEvolution.Drawing;
using Xunit;

using Polygon = ModelingEvolution.Drawing.Polygon<float>;
using PointF = ModelingEvolution.Drawing.Point<float>;

public class PolygonTest
{
    [Fact]
    public void Union_WithHoleFilling_ShouldProduceSinglePolygon()
    {
        var cShape = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(100, 0), new(100, 100), new(80, 100),
            new(80, 20), new(20, 20), new(20, 100), new(0, 100),
        });

        var bridge = new Polygon<float>(new List<Point<float>>
        {
            new(10, 40), new(90, 40), new(90, 60), new(10, 60),
        });

        var resultWithHoles = Polygon<float>.Union([cShape, bridge]);
        var isOberlapping = cShape.IsOverlapping(bridge);
        var withoutHoles = Polygon<float>.Union([cShape, bridge], true);

        Assert.True(resultWithHoles.Count > 1);
        Assert.True(isOberlapping);
        Assert.True(withoutHoles.Count == 1);

        float areaWithoutHoles = resultWithHoles[0].Area() - resultWithHoles[1].Area();
        float areaWithHoles = withoutHoles[0].Area();
        Assert.True(areaWithHoles > areaWithoutHoles);
    }

    [Fact]
    public void Indexer_ReturnsCorrectPoint()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        polygon[0].Should().Be(new PointF(0, 0));
        polygon[1].Should().Be(new PointF(4, 0));
        polygon[2].Should().Be(new PointF(4, 3));
    }

    [Fact]
    public void Polygon_IsImmutable_CopyDoesNotShareState()
    {
        var polygon = new Polygon(new PointF(0, 0), new PointF(4, 0), new PointF(4, 3));
        var polygon2 = polygon.Add(new PointF(1, 1));

        polygon.Count.Should().Be(3);
        polygon2.Count.Should().Be(4);
    }

    [Fact]
    public void Area_ValidPolygon_ReturnsCorrectArea()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var area = polygon.Area();
        Assert.Equal(6, area);
    }

    [Fact]
    public void Area_LessThanThreePoints_ThrowsArgumentException()
    {
        var points = new float[] { 0, 0, 4, 0 };
        var polygon = new Polygon(points);
        Assert.Throws<ArgumentException>(() => polygon.Area());
    }

    [Fact]
    public void Render_ValidDimensions_ReturnsCorrectPoints()
    {
        var points = new float[] { 0, 0, 1, 0, 1, 1 };
        var polygon = new Polygon(points);
        var renderedPoints = (polygon * new Size<float>(100, 100)).Points.Select(x => (Point)x).ToList();
        Assert.Equal(new Point(0, 0), renderedPoints[0]);
        Assert.Equal(new Point(100, 0), renderedPoints[1]);
        Assert.Equal(new Point(100, 100), renderedPoints[2]);
    }

    [Fact]
    public void OperatorDivide_ValidSize_ReturnsScaledPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var size = new Size<float>(2, 2);
        var scaledPolygon = polygon / size;
        Assert.Equal(new Point<float>(0, 0), scaledPolygon[0]);
        Assert.Equal(new Point<float>(2, 0), scaledPolygon[1]);
        Assert.Equal(new Point<float>(2, 1.5f), scaledPolygon[2]);
    }

    [Fact]
    public void Intersect_NoIntersection_ReturnsEmptyPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(5, 5, 2, 2);
        polygon.Intersect(rect).Should().BeEmpty();
    }

    [Fact]
    public void Intersect_RectangleFullyInsidePolygon_ReturnsRectangleAsPolygon()
    {
        var points = new float[] { 0, 0, 6, 0, 6, 6, 0, 6 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(2, 2, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect).Single();
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon.Points);
    }

    [Fact]
    public void Intersect_PolygonFullyInsideRectangle_ReturnsOriginalPolygon()
    {
        var points = new float[] { 2, 2, 4, 2, 4, 4, 2, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(0, 0, 6, 6);
        var intersectedPolygon = polygon.Intersect(rect).Single();
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon.Points);
    }

    [Fact]
    public void Intersect_RectangleTouchesPolygonEdge_ReturnsCorrectIntersection()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(3, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect).Single();
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(4, 1), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(4, 3), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(3, 1), intersectedPolygon.Points);
        Assert.Contains(new Point<float>(3, 3), intersectedPolygon.Points);
    }

    [Fact]
    public void Intersect_ValidRectangle_ReturnsIntersectedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(1, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect).Single();
        Assert.Equal(4, intersectedPolygon.Count);
    }

    [Fact]
    public void OperatorSubtract_ValidVector_ReturnsTranslatedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var vector = new ModelingEvolution.Drawing.Vector<float>(1, 1);
        var translatedPolygon = polygon - vector;
        Assert.Equal(new Point<float>(-1, -1), translatedPolygon[0]);
        Assert.Equal(new Point<float>(3, -1), translatedPolygon[1]);
        Assert.Equal(new Point<float>(3, 2), translatedPolygon[2]);
    }

    [Fact]
    public void OperatorAdd_ValidVector_ReturnsTranslatedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var vector = new ModelingEvolution.Drawing.Vector<float>(1, 1);
        var translatedPolygon = polygon + vector;
        Assert.Equal(new Point<float>(1, 1), translatedPolygon[0]);
        Assert.Equal(new Point<float>(5, 1), translatedPolygon[1]);
        Assert.Equal(new Point<float>(5, 4), translatedPolygon[2]);
    }

    [Fact]
    public void Indexer_ValidIndex_ReturnsCorrectPoint()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        Assert.Equal(new Point<float>(4, 0), polygon[1]);
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfPoints()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        Assert.Equal(3, polygon.Count);
    }

    [Fact]
    public void GetEnumerator_ReturnsAllPoints()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var enumerator = polygon.Points.GetEnumerator();
        int count = 0;
        while (enumerator.MoveNext())
            count++;
        Assert.Equal(3, count);
    }

    [Fact]
    public void Add_Vector()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var d = new Vector<float>(1, 1);
        polygon += d;

        Assert.Equal(3, polygon.Count);
        Assert.Equal(new Point<float>(1, 1), polygon.Points[0]);
        Assert.Equal(new Point<float>(5, 1), polygon.Points[1]);
        Assert.Equal(new Point<float>(5, 4), polygon.Points[2]);
    }

    [Fact]
    public void Add_Point()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 3 };
        var polygon = new Polygon(points);
        var d = new Point<float>(1, 1);
        polygon += d;

        Assert.Equal(4, polygon.Count);
    }

    [Fact]
    public void Union_TouchingPolygons_ReturnsSinglePolygon()
    {
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)
        });
        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new(1, 0), new(2, 0), new(2, 1), new(1, 1)
        });

        var result = polygon1 | polygon2;

        Assert.Equal(4, result.Points.Count);
        Assert.Equal(2.0f, result.Area(), 0.0001f);
    }

    [Fact]
    public void Union_DisconnectedPolygons_ThrowsInvalidOperationException()
    {
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)
        });
        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new(3, 3), new(4, 3), new(4, 4), new(3, 4)
        });

        Assert.Throws<InvalidOperationException>(() => polygon1 | polygon2);
    }

    [Fact]
    public void Intersection_TwoOverlappingSquares_ReturnsSinglePolygon()
    {
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(2, 0), new(2, 2), new(0, 2)
        });
        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new(1, 1), new(3, 1), new(3, 3), new(1, 3)
        });

        var result = polygon1 & polygon2;

        Assert.Equal(4, result.Points.Count);
        var exPoints = new PointF[]
        {
            new(2, 2), new(1, 2), new(1, 1), new(2, 1)
        };
        for (int i = 0; i < result.Points.Count; i++)
            Assert.True(result.Points[i].Equals(exPoints[i]));
        Assert.True(polygon1.IsOverlapping(polygon2));
    }

    [Fact]
    public void Union_TwoOverlappingSquares_ReturnsSinglePolygon()
    {
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(2, 0), new(2, 2), new(0, 2)
        });
        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new(1, 1), new(3, 1), new(3, 3), new(1, 3)
        });

        var result = polygon1 | polygon2;

        Assert.Equal(8, result.Points.Count);
        Assert.True(result.Area() > polygon1.Area());
        Assert.True(result.Area() > polygon2.Area());
    }

    [Fact]
    public void Intersection_PartiallyOverlappingTriangles_ReturnsSinglePolygon()
    {
        var polygon1 = new Polygon<float>(new List<Point<float>>
        {
            new(0, 0), new(2, 0), new(1, 2)
        });
        var polygon2 = new Polygon<float>(new List<Point<float>>
        {
            new(1, 0), new(3, 0), new(2, 2)
        });

        var result = polygon1 & polygon2;

        Assert.True(result.Points.Count > 2);
        Assert.True(result.Area() > 0);
    }

    #region ReadOnlyMemory construction

    [Fact]
    public void ConstructFromReadOnlyMemory_PreservesPoints()
    {
        var array = new Point<float>[] { new(1, 2), new(3, 4), new(5, 6) };
        ReadOnlyMemory<Point<float>> memory = array;

        var polygon = new Polygon<float>(memory);

        polygon.Count.Should().Be(3);
        polygon[0].Should().Be(new Point<float>(1, 2));
        polygon[1].Should().Be(new Point<float>(3, 4));
        polygon[2].Should().Be(new Point<float>(5, 6));
    }

    [Fact]
    public void ConstructFromReadOnlyMemory_ArrayBacked_PreservesData()
    {
        var array = new Point<float>[] { new(1, 2), new(3, 4) };
        ReadOnlyMemory<Point<float>> memory = array;

        var polygon = new Polygon<float>(memory);

        polygon.Count.Should().Be(2);
        polygon[0].Should().Be(new Point<float>(1, 2));
        polygon[1].Should().Be(new Point<float>(3, 4));
    }

    [Fact]
    public void ConstructFromEmptyMemory_CreatesEmptyPolygon()
    {
        var polygon = new Polygon<float>(ReadOnlyMemory<Point<float>>.Empty);
        polygon.Count.Should().Be(0);
        polygon.AsSpan().Length.Should().Be(0);
    }

    [Fact]
    public void Span_ReturnsCorrectData()
    {
        var polygon = new Polygon<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var span = polygon.AsSpan();

        span.Length.Should().Be(2);
        span[0].Should().Be(new Point<float>(1, 2));
        span[1].Should().Be(new Point<float>(3, 4));
    }

    [Fact]
    public void AsSpan_ReturnsCorrectData()
    {
        var polygon = new Polygon<float>(new Point<float>(1, 2), new Point<float>(3, 4));
        var span = polygon.AsSpan();

        span.Length.Should().Be(2);
        span[0].Should().Be(new Point<float>(1, 2));
    }

    [Fact]
    public void ConstructFromReadOnlyMemory_NonZeroOffset_ZeroCopy()
    {
        // Simulate pooled buffer with multiple polygons sharing one array
        var buffer = new Point<float>[]
        {
            new(1, 2), new(3, 4), new(5, 6),  // polygon A: offset 0, count 3
            new(10, 20), new(30, 40),           // polygon B: offset 3, count 2
            new(100, 200)                        // polygon C: offset 5, count 1
        };

        ReadOnlyMemory<Point<float>> sliceA = buffer.AsMemory(0, 3);
        ReadOnlyMemory<Point<float>> sliceB = buffer.AsMemory(3, 2);
        ReadOnlyMemory<Point<float>> sliceC = buffer.AsMemory(5, 1);

        var polyA = new Polygon<float>(sliceA);
        var polyB = new Polygon<float>(sliceB);
        var polyC = new Polygon<float>(sliceC);

        polyA.Count.Should().Be(3);
        polyA[0].Should().Be(new Point<float>(1, 2));
        polyA[2].Should().Be(new Point<float>(5, 6));

        polyB.Count.Should().Be(2);
        polyB[0].Should().Be(new Point<float>(10, 20));
        polyB[1].Should().Be(new Point<float>(30, 40));

        polyC.Count.Should().Be(1);
        polyC[0].Should().Be(new Point<float>(100, 200));
    }

    [Fact]
    public void ConstructFromReadOnlyMemory_NonZeroOffset_SpanAndCountConsistent()
    {
        var buffer = new Point<float>[]
        {
            new(0, 0), new(1, 1), new(2, 2), new(3, 3), new(4, 4)
        };
        var polygon = new Polygon<float>(buffer.AsMemory(2, 3));

        polygon.AsSpan().Length.Should().Be(3);
        polygon.AsSpan()[0].Should().Be(new Point<float>(2, 2));
        polygon.AsSpan()[2].Should().Be(new Point<float>(4, 4));

        polygon.Count.Should().Be(3);
        polygon.AsSpan()[0].Should().Be(new Point<float>(2, 2));
    }

    [Fact]
    public void ConstructFromReadOnlyMemory_PooledBuffer_MultipleSlices()
    {
        // Simulate the exact pattern used by SegmentationFrameHandle:
        // one ArrayPool buffer, multiple instances slice into it
        var pool = System.Buffers.ArrayPool<Point<float>>.Shared;
        var buffer = pool.Rent(20);

        try
        {
            // Fill like a decoder would
            buffer[0] = new Point<float>(0, 0);
            buffer[1] = new Point<float>(10, 0);
            buffer[2] = new Point<float>(10, 10);
            buffer[3] = new Point<float>(0, 10);
            // instance 1: offset 0, count 4

            buffer[4] = new Point<float>(20, 20);
            buffer[5] = new Point<float>(30, 20);
            buffer[6] = new Point<float>(25, 30);
            // instance 2: offset 4, count 3

            var poly1 = new Polygon<float>(buffer.AsMemory(0, 4));
            var poly2 = new Polygon<float>(buffer.AsMemory(4, 3));

            poly1.Count.Should().Be(4);
            poly1[0].Should().Be(new Point<float>(0, 0));
            poly1[3].Should().Be(new Point<float>(0, 10));

            poly2.Count.Should().Be(3);
            poly2[0].Should().Be(new Point<float>(20, 20));
            poly2[2].Should().Be(new Point<float>(25, 30));

            // Verify geometric operations work on offset-backed polygons
            poly1.Area().Should().BeApproximately(100f, 0.01f);
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    #endregion

    #region Immutable Add / InsertAt / RemoveAt

    [Fact]
    public void Add_ReturnsNewPolygonWithAppendedPoint()
    {
        var polygon = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 0));
        var result = polygon.Add(new Point<float>(1, 1));

        result.Count.Should().Be(3);
        result[2].Should().Be(new Point<float>(1, 1));
        polygon.Count.Should().Be(2, "original polygon should be unchanged");
    }

    [Fact]
    public void InsertAt_ReturnsNewPolygonWithInsertedPoint()
    {
        var polygon = new Polygon<float>(new Point<float>(0, 0), new Point<float>(2, 0), new Point<float>(2, 2));
        var result = polygon.InsertAt(1, new Point<float>(1, 0));

        result.Count.Should().Be(4);
        result[0].Should().Be(new Point<float>(0, 0));
        result[1].Should().Be(new Point<float>(1, 0));
        result[2].Should().Be(new Point<float>(2, 0));
        result[3].Should().Be(new Point<float>(2, 2));
        polygon.Count.Should().Be(3, "original polygon should be unchanged");
    }

    [Fact]
    public void RemoveAt_ReturnsNewPolygonWithPointRemoved()
    {
        var polygon = new Polygon<float>(
            new Point<float>(0, 0), new Point<float>(1, 0),
            new Point<float>(2, 0), new Point<float>(2, 2));

        var result = polygon.RemoveAt(1);

        result.Count.Should().Be(3);
        result[0].Should().Be(new Point<float>(0, 0));
        result[1].Should().Be(new Point<float>(2, 0));
        result[2].Should().Be(new Point<float>(2, 2));
        polygon.Count.Should().Be(4, "original polygon should be unchanged");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SamePoints_ReturnsTrue()
    {
        var a = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 1));
        var b = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 1));

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentPoints_ReturnsFalse()
    {
        var a = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 1));
        var b = new Polygon<float>(new Point<float>(0, 0), new Point<float>(2, 2));

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SamePoints_ReturnsSameHash()
    {
        var a = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 1));
        var b = new Polygon<float>(new Point<float>(0, 0), new Point<float>(1, 1));

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region Default / empty

    [Fact]
    public void DefaultStruct_HasZeroCount()
    {
        Polygon<float> polygon = default;
        polygon.Count.Should().Be(0);
        polygon.AsSpan().Length.Should().Be(0);
    }

    [Fact]
    public void EmptyConstructor_HasZeroCount()
    {
        var polygon = new Polygon<float>();
        polygon.Count.Should().Be(0);
    }

    #endregion
}
