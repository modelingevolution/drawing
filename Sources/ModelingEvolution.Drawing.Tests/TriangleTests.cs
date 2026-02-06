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

        var centroid = triangle.Centroid();

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

        triangle.Area().Should().Be(6); // (3 * 4) / 2
    }

    [Fact]
    public void Triangle_Perimeter_RightTriangle()
    {
        // 3-4-5 right triangle
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        triangle.Perimeter().Should().Be(12); // 3 + 4 + 5
    }

    [Fact]
    public void Triangle_Contains_PointInside_ReturnsTrue()
    {
        var triangle = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(4, 0),
            new Point<double>(0, 4));

        triangle.Contains(new Point<double>(1, 1)).Should().BeTrue();
        triangle.Contains(triangle.Centroid()).Should().BeTrue();
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

    [Fact]
    public void Triangle_Rotate_90Degrees_AroundOrigin()
    {
        // Right triangle along axes rotated 90 CCW
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var rotated = t.Rotate(Degree<double>.Create(90));

        rotated.A.X.Should().BeApproximately(0, Tolerance);
        rotated.A.Y.Should().BeApproximately(0, Tolerance);
        rotated.B.X.Should().BeApproximately(0, Tolerance);
        rotated.B.Y.Should().BeApproximately(3, Tolerance);
        rotated.C.X.Should().BeApproximately(-4, Tolerance);
        rotated.C.Y.Should().BeApproximately(0, Tolerance);
    }

    [Fact]
    public void Triangle_Rotate_PreservesArea()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var rotated = t.Rotate(Degree<double>.Create(37));

        rotated.Area().Should().BeApproximately(t.Area(), 1e-8);
    }

    [Fact]
    public void Triangle_Rotate_AroundCentroid()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 3));

        var centroid = t.Centroid();
        var rotated = t.Rotate(Degree<double>.Create(120), centroid);

        // Centroid should stay the same after rotation
        rotated.Centroid().X.Should().BeApproximately(centroid.X, 1e-8);
        rotated.Centroid().Y.Should().BeApproximately(centroid.Y, 1e-8);
    }

    [Fact]
    public void Triangle_PlusDegree_Operator()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var rotated = t + Degree<double>.Create(90);

        rotated.B.X.Should().BeApproximately(0, Tolerance);
        rotated.B.Y.Should().BeApproximately(3, Tolerance);
    }

    #endregion

    #region IsSimilarTo Tests

    [Fact]
    public void IsSimilarTo_IdenticalTriangles_ReturnsTrue()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        t.IsSimilarTo(t).Should().BeTrue();
    }

    [Fact]
    public void IsSimilarTo_ScaledTriangle_ReturnsTrue()
    {
        // 3-4-5 right triangle
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        // 6-8-10 right triangle (scaled 2x)
        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(6, 0),
            new Point<double>(0, 8));

        t1.IsSimilarTo(t2).Should().BeTrue();
    }

    [Fact]
    public void IsSimilarTo_RotatedAndScaled_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        // Rotate 45° and scale 3x
        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(9, 0),
            new Point<double>(0, 12));

        t1.IsSimilarTo(t2).Should().BeTrue();
    }

    [Fact]
    public void IsSimilarTo_DifferentShapes_ReturnsFalse()
    {
        // 3-4-5 right triangle
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        // Equilateral triangle
        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(5, 0),
            new Point<double>(2.5, 5 * Math.Sqrt(3) / 2));

        t1.IsSimilarTo(t2).Should().BeFalse();
    }

    [Fact]
    public void IsSimilarTo_DifferentVertexOrder_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        // Same triangle, vertices in different order
        var t2 = new Triangle<double>(
            new Point<double>(0, 4),
            new Point<double>(0, 0),
            new Point<double>(3, 0));

        t1.IsSimilarTo(t2).Should().BeTrue();
    }

    [Fact]
    public void IsSimilarTo_EquilateralTriangles_AlwaysSimilar()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(1, 0),
            new Point<double>(0.5, Math.Sqrt(3) / 2));

        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(5, 10 * Math.Sqrt(3) / 2));

        t1.IsSimilarTo(t2).Should().BeTrue();
    }

    #endregion

    #region IsCongruentTo Tests

    [Fact]
    public void IsCongruentTo_IdenticalTriangles_ReturnsTrue()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        t.IsCongruentTo(t).Should().BeTrue();
    }

    [Fact]
    public void IsCongruentTo_TranslatedTriangle_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var t2 = t1 + new Vector<double>(10, 20);

        t1.IsCongruentTo(t2).Should().BeTrue();
    }

    [Fact]
    public void IsCongruentTo_RotatedTriangle_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var t2 = t1.Rotate(Degree<double>.Create(73));

        t1.IsCongruentTo(t2).Should().BeTrue();
    }

    [Fact]
    public void IsCongruentTo_ScaledTriangle_ReturnsFalse()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var t2 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(6, 0),
            new Point<double>(0, 8));

        t1.IsCongruentTo(t2).Should().BeFalse();
    }

    [Fact]
    public void IsCongruentTo_DifferentVertexOrder_ReturnsTrue()
    {
        var t1 = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var t2 = new Triangle<double>(
            new Point<double>(0, 4),
            new Point<double>(0, 0),
            new Point<double>(3, 0));

        t1.IsCongruentTo(t2).Should().BeTrue();
    }

    #endregion

    #region Incircle Tests

    [Fact]
    public void Incircle_RightTriangle345_CorrectRadius()
    {
        // 3-4-5 right triangle: inradius = (3 + 4 - 5) / 2 = 1
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var incircle = t.Incircle();

        incircle.Radius.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Incircle_EquilateralTriangle_CorrectRadius()
    {
        // Equilateral with side s: inradius = s / (2 * sqrt(3))
        var s = 6.0;
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(s, 0),
            new Point<double>(s / 2, s * Math.Sqrt(3) / 2));

        var incircle = t.Incircle();

        var expectedRadius = s / (2 * Math.Sqrt(3));
        incircle.Radius.Should().BeApproximately(expectedRadius, 1e-9);
    }

    [Fact]
    public void Incircle_CenterInsideTriangle()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var incircle = t.Incircle();

        t.Contains(incircle.Center).Should().BeTrue();
    }

    [Fact]
    public void Incircle_TouchesAllThreeSides()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var incircle = t.Incircle();

        // Distance from incircle center to each side should equal the radius
        var ab = Segment<double>.From(t.A, t.B);
        var bc = Segment<double>.From(t.B, t.C);
        var ca = Segment<double>.From(t.C, t.A);

        ab.DistanceTo(incircle.Center).Should().BeApproximately(incircle.Radius, 1e-9);
        bc.DistanceTo(incircle.Center).Should().BeApproximately(incircle.Radius, 1e-9);
        ca.DistanceTo(incircle.Center).Should().BeApproximately(incircle.Radius, 1e-9);
    }

    #endregion

    #region Circumcircle Tests

    [Fact]
    public void Circumcircle_RightTriangle345_CorrectRadius()
    {
        // 3-4-5 right triangle: circumradius = hypotenuse / 2 = 2.5
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var circumcircle = t.Circumcircle();

        circumcircle.Radius.Should().BeApproximately(2.5, 1e-9);
    }

    [Fact]
    public void Circumcircle_EquilateralTriangle_CorrectRadius()
    {
        // Equilateral with side s: circumradius = s / sqrt(3)
        var s = 6.0;
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(s, 0),
            new Point<double>(s / 2, s * Math.Sqrt(3) / 2));

        var circumcircle = t.Circumcircle();

        var expectedRadius = s / Math.Sqrt(3);
        circumcircle.Radius.Should().BeApproximately(expectedRadius, 1e-9);
    }

    [Fact]
    public void Circumcircle_PassesThroughAllVertices()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var cc = t.Circumcircle();

        var distA = Math.Sqrt(Math.Pow(t.A.X - cc.Center.X, 2) + Math.Pow(t.A.Y - cc.Center.Y, 2));
        var distB = Math.Sqrt(Math.Pow(t.B.X - cc.Center.X, 2) + Math.Pow(t.B.Y - cc.Center.Y, 2));
        var distC = Math.Sqrt(Math.Pow(t.C.X - cc.Center.X, 2) + Math.Pow(t.C.Y - cc.Center.Y, 2));

        distA.Should().BeApproximately(cc.Radius, 1e-9);
        distB.Should().BeApproximately(cc.Radius, 1e-9);
        distC.Should().BeApproximately(cc.Radius, 1e-9);
    }

    [Fact]
    public void Circumcircle_RightTriangle_CenterAtHypotenuseMidpoint()
    {
        // For a right triangle, circumcenter is at the midpoint of the hypotenuse
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(3, 0),
            new Point<double>(0, 4));

        var cc = t.Circumcircle();

        // Hypotenuse is from (3,0) to (0,4), midpoint = (1.5, 2)
        cc.Center.X.Should().BeApproximately(1.5, 1e-9);
        cc.Center.Y.Should().BeApproximately(2.0, 1e-9);
    }

    [Fact]
    public void Incircle_InsideCircumcircle()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(7, 0),
            new Point<double>(2, 5));

        var incircle = t.Incircle();
        var circumcircle = t.Circumcircle();

        incircle.Radius.Should().BeLessThan(circumcircle.Radius);
        circumcircle.Contains(incircle.Center).Should().BeTrue();
    }

    #endregion

    #region Enhancement Tests

    private static Triangle<double> Right345 => new(
        new Point<double>(0, 0),
        new Point<double>(3, 0),
        new Point<double>(0, 4));

    private static Triangle<double> Equilateral => new(
        new Point<double>(0, 0),
        new Point<double>(1, 0),
        new Point<double>(0.5, Math.Sqrt(3) / 2));

    [Fact]
    public void Orthocenter_RightTriangle_AtRightAngleVertex()
    {
        var t = Right345;
        var h = t.Orthocenter;
        h.X.Should().BeApproximately(0, 1e-6);
        h.Y.Should().BeApproximately(0, 1e-6);
    }

    [Fact]
    public void Orthocenter_Equilateral_EqualsCentroid()
    {
        var t = Equilateral;
        var h = t.Orthocenter;
        var c = t.Centroid();
        h.X.Should().BeApproximately(c.X, 1e-6);
        h.Y.Should().BeApproximately(c.Y, 1e-6);
    }

    [Fact]
    public void Edges_Returns_ThreeSegments()
    {
        var t = Right345;
        var (ab, bc, ca) = t.Edges;
        ab.Length.Should().BeApproximately(3, 1e-9);
        bc.Length.Should().BeApproximately(5, 1e-9);
        ca.Length.Should().BeApproximately(4, 1e-9);
    }

    [Fact]
    public void Angles_RightTriangle_HasHalfPi()
    {
        var t = Right345;
        var (atA, atB, atC) = t.Angles;
        ((double)atA).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Angles_Equilateral_AllSixtyDegrees()
    {
        var t = Equilateral;
        var (atA, atB, atC) = t.Angles;
        ((double)atA).Should().BeApproximately(Math.PI / 3, 1e-9);
        ((double)atB).Should().BeApproximately(Math.PI / 3, 1e-9);
        ((double)atC).Should().BeApproximately(Math.PI / 3, 1e-9);
    }

    [Fact]
    public void Angles_SumToPi()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(5, 0),
            new Point<double>(2, 3));
        var (a, b, c) = t.Angles;
        ((double)a + (double)b + (double)c).Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void IsRight_345_ReturnsTrue()
    {
        Right345.IsRight().Should().BeTrue();
    }

    [Fact]
    public void IsRight_Equilateral_ReturnsFalse()
    {
        Equilateral.IsRight().Should().BeFalse();
    }

    [Fact]
    public void IsAcute_Equilateral_ReturnsTrue()
    {
        Equilateral.IsAcute().Should().BeTrue();
    }

    [Fact]
    public void IsAcute_RightTriangle_ReturnsFalse()
    {
        Right345.IsAcute().Should().BeFalse();
    }

    [Fact]
    public void IsObtuse_ObtuseTriangle_ReturnsTrue()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(1, 0.1));
        t.IsObtuse().Should().BeTrue();
    }

    [Fact]
    public void IsEquilateral_EquilateralTriangle_ReturnsTrue()
    {
        Equilateral.IsEquilateral().Should().BeTrue();
    }

    [Fact]
    public void IsEquilateral_RightTriangle_ReturnsFalse()
    {
        Right345.IsEquilateral().Should().BeFalse();
    }

    [Fact]
    public void IsIsosceles_EquilateralIsAlsoIsosceles()
    {
        Equilateral.IsIsosceles().Should().BeTrue();
    }

    [Fact]
    public void IsIsosceles_TwoEqualSides()
    {
        var t = new Triangle<double>(
            new Point<double>(0, 0),
            new Point<double>(2, 0),
            new Point<double>(1, 2));
        t.IsIsosceles().Should().BeTrue();
    }

    [Fact]
    public void IsScalene_345_ReturnsTrue()
    {
        Right345.IsScalene().Should().BeTrue();
    }

    [Fact]
    public void IsScalene_Equilateral_ReturnsFalse()
    {
        Equilateral.IsScalene().Should().BeFalse();
    }

    #endregion

    #region Intersect Rectangle Tests

    private static readonly Rectangle<double> Roi = new(0, 0, 10, 10);

    [Fact]
    public void Triangle_FullyInside_ReturnsTriangleAsPolygon()
    {
        var t = new Triangle<double>(
            new Point<double>(2, 2),
            new Point<double>(8, 2),
            new Point<double>(5, 8));
        var clipped = t.Intersect(Roi);
        clipped.Count.Should().Be(3);
        clipped.Area().Should().BeApproximately(t.Area(), 1e-6);
    }

    [Fact]
    public void Triangle_PartiallyInside_ClipsCorrectly()
    {
        var t = new Triangle<double>(
            new Point<double>(-5, 5),
            new Point<double>(15, 5),
            new Point<double>(5, -5));
        var clipped = t.Intersect(Roi);
        clipped.Count.Should().BeGreaterThan(0);
        clipped.Area().Should().BeLessThan(t.Area());
    }

    [Fact]
    public void Triangle_FullyOutside_ReturnsEmpty()
    {
        var t = new Triangle<double>(
            new Point<double>(20, 20),
            new Point<double>(30, 20),
            new Point<double>(25, 30));
        var clipped = t.Intersect(Roi);
        clipped.Count.Should().Be(0);
    }

    #endregion
}
