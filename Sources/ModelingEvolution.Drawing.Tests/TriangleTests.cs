using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class TriangleTests
{
    private const double Tolerance = 1e-10;

    #region Triangle<T> Tests

    [Fact]
    public void Triangle_Constructor_SetsVertices()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(3, 0);
        var c = new Point<double>(0, 4);

        var triangle = new Triangle<double>(a, b, c);

        triangle.A.Should().Be(a);
        triangle.B.Should().Be(b);
        triangle.C.Should().Be(c);
    }

    [Fact]
    public void Triangle_Centroid_ReturnsAverageOfVertices()
    {
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 3));

        var centroid = triangle.Centroid;

        centroid.X.Should().Be(1);
        centroid.Y.Should().Be(1);
    }

    [Fact]
    public void Triangle_Area_RightTriangle()
    {
        // 3-4-5 right triangle
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        triangle.Area.Should().Be(6); // (3 * 4) / 2
    }

    [Fact]
    public void Triangle_Perimeter_RightTriangle()
    {
        // 3-4-5 right triangle
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        triangle.Perimeter.Should().Be(12); // 3 + 4 + 5
    }

    [Fact]
    public void Triangle_Contains_PointInside_ReturnsTrue()
    {
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(4, 0),
            new Point<double>(0, 4));

        triangle.Contains(new Point<double>(1, 1)).Should().BeTrue();
        triangle.Contains(triangle.Centroid).Should().BeTrue();
    }

    [Fact]
    public void Triangle_Contains_PointOutside_ReturnsFalse()
    {
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(4, 0),
            new Point<double>(0, 4));

        triangle.Contains(new Point<double>(5, 5)).Should().BeFalse();
        triangle.Contains(new Point<double>(-1, 1)).Should().BeFalse();
    }

    [Fact]
    public void Triangle_PlusVector_TranslatesAllVertices()
    {
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var translated = triangle + new Vector<double>(10, 20);

        translated.A.Should().Be(new Point<double>(10, 20));
        translated.B.Should().Be(new Point<double>(13, 20));
        translated.C.Should().Be(new Point<double>(10, 24));
    }

    [Fact]
    public void Triangle_MinusVector_TranslatesAllVertices()
    {
        var triangle = new Triangle<double>(
            new Point<double>(10, 20),
            new Point<double>(13, 20),
            new Point<double>(10, 24));

        var translated = triangle - new Vector<double>(10, 20);

        translated.A.Should().Be(new Point<double>(0, 0));
        translated.B.Should().Be(new Point<double>(3, 0));
        translated.C.Should().Be(new Point<double>(0, 4));
    }

    [Fact]
    public void Triangle_Equality_SameVertices_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(1, 0),
            new Point<double>(0, 1));

        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(1, 0),
            new Point<double>(0, 1));

        t1.Should().Be(t2);
        (t1 == t2).Should().BeTrue();
    }

    #endregion
}
