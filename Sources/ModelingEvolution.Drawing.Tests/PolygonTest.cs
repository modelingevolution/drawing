using System;

using System.Drawing;
using System.Linq;
using ModelingEvolution.Drawing;
using Xunit;

namespace ModelingEvolution.Yolo.Tests;
using Polygon = Polygon<float>;
    

public class PolygonTest
{
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
        var renderedPoints = (polygon * new Size<float>(100, 100)).Select(x => (Point)x).ToList();
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
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Empty(intersectedPolygon);
    }
    [Fact]
    public void Intersect_RectangleFullyInsidePolygon_ReturnsRectangleAsPolygon()
    {
        var points = new float[] { 0, 0, 6, 0, 6, 6, 0, 6 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(2, 2, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon);
    }
    [Fact]
    public void Intersect_PolygonFullyInsideRectangle_ReturnsOriginalPolygon()
    {
        var points = new float[] { 2, 2, 4, 2, 4, 4, 2, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(0, 0, 6, 6);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(2, 2), intersectedPolygon);
        Assert.Contains(new Point<float>(4, 2), intersectedPolygon);
        Assert.Contains(new Point<float>(4, 4), intersectedPolygon);
        Assert.Contains(new Point<float>(2, 4), intersectedPolygon);
    }
    [Fact]
    public void Intersect_RectangleTouchesPolygonEdge_ReturnsCorrectIntersection()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(3, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
        Assert.Equal(4, intersectedPolygon.Count);
        Assert.Contains(new Point<float>(4, 1), intersectedPolygon);
        Assert.Contains(new Point<float>(4, 3), intersectedPolygon);
        Assert.Contains(new Point<float>(3, 1), intersectedPolygon);
        Assert.Contains(new Point<float>(3, 3), intersectedPolygon);
    }

    [Fact]
    public void Intersect_ValidRectangle_ReturnsIntersectedPolygon()
    {
        var points = new float[] { 0, 0, 4, 0, 4, 4, 0, 4 };
        var polygon = new Polygon(points);
        var rect = new Rectangle<float>(1, 1, 2, 2);
        var intersectedPolygon = polygon.Intersect(rect);
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
        var enumerator = polygon.GetEnumerator();
        int count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }

        Assert.Equal(3, count);
    }
}