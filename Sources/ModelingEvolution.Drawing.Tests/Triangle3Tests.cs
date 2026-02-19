using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class Triangle3Tests
{
    private const double Tolerance = 1e-6;

    #region Constructor and Properties

    [Fact]
    public void Triangle3_Constructor_SetsVertices()
    {
        var a = new Point3<double>(0, 0, 0);
        var b = new Point3<double>(3, 0, 0);
        var c = new Point3<double>(0, 4, 0);

        var triangle = new Triangle3<double>(a, b, c);

        triangle.A.Should().Be(a);
        triangle.B.Should().Be(b);
        triangle.C.Should().Be(c);
    }

    [Fact]
    public void Triangle3_Centroid_ReturnsAverageOfVertices()
    {
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 3, 0));

        var centroid = triangle.Centroid;

        centroid.X.Should().Be(1);
        centroid.Y.Should().Be(1);
        centroid.Z.Should().Be(0);
    }

    [Fact]
    public void Triangle3_Centroid_WithZOffset()
    {
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 3),
            new Point3<double>(3, 0, 3),
            new Point3<double>(0, 3, 3));

        var centroid = triangle.Centroid;

        centroid.X.Should().Be(1);
        centroid.Y.Should().Be(1);
        centroid.Z.Should().Be(3);
    }

    #endregion

    #region Area and Perimeter

    [Fact]
    public void Triangle3_Area_HorizontalRightTriangle()
    {
        // 3-4-5 right triangle in XY plane
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 4, 0));

        triangle.Area.Should().Be(6); // (3 * 4) / 2
    }

    [Fact]
    public void Triangle3_Area_VerticalTriangle()
    {
        // Triangle in XZ plane
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 0, 4));

        triangle.Area.Should().Be(6);
    }

    [Fact]
    public void Triangle3_Perimeter_RightTriangle()
    {
        // 3-4-5 right triangle
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 4, 0));

        triangle.Perimeter.Should().Be(12); // 3 + 4 + 5
    }

    #endregion

    #region Normal

    [Fact]
    public void Triangle3_Normal_HorizontalTriangle_PointsUp()
    {
        // Triangle in XY plane, counter-clockwise from above
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(1, 0, 0),
            new Point3<double>(0, 1, 0));

        var normal = triangle.Normal;

        normal.X.Should().BeApproximately(0, Tolerance);
        normal.Y.Should().BeApproximately(0, Tolerance);
        normal.Z.Should().BeApproximately(1, Tolerance);
    }

    [Fact]
    public void Triangle3_Normal_VerticalTriangleXZ_PointsAlongY()
    {
        // Triangle in XZ plane
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(1, 0, 0),
            new Point3<double>(0, 0, 1));

        var normal = triangle.Normal;

        // Cross product of (1,0,0) x (0,0,1) = (0,-1,0)
        normal.X.Should().BeApproximately(0, Tolerance);
        normal.Y.Should().BeApproximately(-1, Tolerance);
        normal.Z.Should().BeApproximately(0, Tolerance);
    }

    #endregion

    #region ToPose

    [Fact]
    public void Triangle3_ToPose_PositionAtCentroid()
    {
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 3, 0));

        var pose = triangle.ToPose();

        pose.X.Should().Be(1);
        pose.Y.Should().Be(1);
        pose.Z.Should().Be(0);
    }

    [Fact]
    public void Triangle3_ToPose_HorizontalTriangle_ZAxisPointsUp()
    {
        // Horizontal triangle in XY plane
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 5),
            new Point3<double>(1, 0, 5),
            new Point3<double>(0, 1, 5));

        var pose = triangle.ToPose();

        // Check that Z-axis of pose points along (0,0,1)
        var rotation = pose.Rotation;
        // For a horizontal surface, Rx and Ry should be near 0
        ((double)rotation.Rx).Should().BeApproximately(0, 1);
        ((double)rotation.Ry).Should().BeApproximately(0, 1);
    }

    #endregion

    #region Operators

    [Fact]
    public void Triangle3_PlusVector_TranslatesAllVertices()
    {
        var triangle = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(3, 0, 0),
            new Point3<double>(0, 4, 0));

        var translated = triangle + new Vector3<double>(10, 20, 30);

        translated.A.Should().Be(new Point3<double>(10, 20, 30));
        translated.B.Should().Be(new Point3<double>(13, 20, 30));
        translated.C.Should().Be(new Point3<double>(10, 24, 30));
    }

    [Fact]
    public void Triangle3_MinusVector_TranslatesAllVertices()
    {
        var triangle = new Triangle3<double>(
            new Point3<double>(10, 20, 30),
            new Point3<double>(13, 20, 30),
            new Point3<double>(10, 24, 30));

        var translated = triangle - new Vector3<double>(10, 20, 30);

        translated.A.Should().Be(new Point3<double>(0, 0, 0));
        translated.B.Should().Be(new Point3<double>(3, 0, 0));
        translated.C.Should().Be(new Point3<double>(0, 4, 0));
    }

    [Fact]
    public void Triangle3_Equality_SameVertices_ReturnsTrue()
    {
        var t1 = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(1, 0, 0),
            new Point3<double>(0, 1, 0));

        var t2 = new Triangle3<double>(
            new Point3<double>(0, 0, 0),
            new Point3<double>(1, 0, 0),
            new Point3<double>(0, 1, 0));

        t1.Should().Be(t2);
        (t1 == t2).Should().BeTrue();
    }

    #endregion
}
