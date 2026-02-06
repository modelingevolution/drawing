using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class DegreeEnhancementsTests
{
    [Fact]
    public void Multiply_ByScalar()
    {
        var d = Degree<double>.Create(45);
        var result = d * 2.0;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Multiply_ScalarByDegree()
    {
        var d = Degree<double>.Create(30);
        var result = 3.0 * d;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Divide_ByScalar()
    {
        var d = Degree<double>.Create(180);
        var result = d / 2.0;
        ((double)result).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Abs_NegativeDegree()
    {
        var d = Degree<double>.Create(-45);
        ((double)d.Abs()).Should().BeApproximately(45, 1e-9);
    }

    [Fact]
    public void Normalize_LargePositive()
    {
        var d = Degree<double>.Create(270);
        ((double)d.Normalize()).Should().BeApproximately(-90, 1e-9);
    }

    [Fact]
    public void Normalize_LargeNegative()
    {
        var d = Degree<double>.Create(-270);
        ((double)d.Normalize()).Should().BeApproximately(90, 1e-9);
    }

    [Fact]
    public void Normalize_180_StaysAt180()
    {
        var d = Degree<double>.Create(180);
        ((double)d.Normalize()).Should().BeApproximately(180, 1e-9);
    }

    [Fact]
    public void Normalize_AlreadyInRange()
    {
        var d = Degree<double>.Create(45);
        ((double)d.Normalize()).Should().BeApproximately(45, 1e-9);
    }
}

public class RadianEnhancementsTests
{
    [Fact]
    public void Negate()
    {
        var r = Radian<double>.FromRadian(Math.PI / 4);
        var neg = -r;
        ((double)neg).Should().BeApproximately(-Math.PI / 4, 1e-9);
    }

    [Fact]
    public void Multiply_ByScalar()
    {
        var r = Radian<double>.FromRadian(Math.PI / 4);
        var result = r * 2.0;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Multiply_ScalarByRadian()
    {
        var r = Radian<double>.FromRadian(Math.PI / 6);
        var result = 3.0 * r;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Divide_ByScalar()
    {
        var r = Radian<double>.FromRadian(Math.PI);
        var result = r / 2.0;
        ((double)result).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void Abs_NegativeRadian()
    {
        var r = Radian<double>.FromRadian(-Math.PI / 3);
        ((double)r.Abs()).Should().BeApproximately(Math.PI / 3, 1e-9);
    }

    [Fact]
    public void Normalize_LargePositive()
    {
        var r = Radian<double>.FromRadian(3 * Math.PI);
        ((double)r.Normalize()).Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void Normalize_LargeNegative()
    {
        var r = Radian<double>.FromRadian(-3 * Math.PI);
        ((double)r.Normalize()).Should().BeApproximately(Math.PI, 1e-9);
    }
}

public class PointEnhancementsTests
{
    [Fact]
    public void DistanceTo_Origin_To_3_4()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(3, 4);
        a.DistanceTo(b).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_SamePoint_IsZero()
    {
        var p = new Point<double>(5, 7);
        p.DistanceTo(p).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 0);
        result.X.Should().BeApproximately(0, 1e-9);
        result.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 1);
        result.X.Should().BeApproximately(10, 1e-9);
        result.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var a = new Point<double>(0, 0);
        var b = new Point<double>(10, 20);
        var result = a.Lerp(b, 0.5);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossOrigin()
    {
        var p = new Point<double>(3, 4);
        var center = new Point<double>(0, 0);
        var result = p.Reflect(center);
        result.X.Should().BeApproximately(-3, 1e-9);
        result.Y.Should().BeApproximately(-4, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossCenter()
    {
        var p = new Point<double>(1, 1);
        var center = new Point<double>(3, 3);
        var result = p.Reflect(center);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(5, 1e-9);
    }
}

public class VectorEnhancementsTests
{
    [Fact]
    public void Rotate_90Degrees()
    {
        var v = new Vector<double>(1, 0);
        var rotated = v.Rotate(Degree<double>.Create(90));
        rotated.X.Should().BeApproximately(0, 1e-9);
        rotated.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void Rotate_180Degrees()
    {
        var v = new Vector<double>(1, 0);
        var rotated = v.Rotate(Degree<double>.Create(180));
        rotated.X.Should().BeApproximately(-1, 1e-9);
        rotated.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void PerpendicularCW_FromRight()
    {
        var v = new Vector<double>(1, 0);
        var perp = v.PerpendicularCW;
        perp.X.Should().BeApproximately(0, 1e-9);
        perp.Y.Should().BeApproximately(-1, 1e-9);
    }

    [Fact]
    public void PerpendicularCCW_FromRight()
    {
        var v = new Vector<double>(1, 0);
        var perp = v.PerpendicularCCW;
        perp.X.Should().BeApproximately(0, 1e-9);
        perp.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void PerpendicularCW_DotProduct_IsZero()
    {
        var v = new Vector<double>(3, 4);
        var perp = v.PerpendicularCW;
        v.Dot(perp).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossXAxis()
    {
        var v = new Vector<double>(1, 1);
        var normal = new Vector<double>(0, 1);
        var reflected = v.Reflect(normal);
        reflected.X.Should().BeApproximately(1, 1e-9);
        reflected.Y.Should().BeApproximately(-1, 1e-9);
    }

    [Fact]
    public void Dot_Perpendicular_IsZero()
    {
        var v1 = new Vector<double>(1, 0);
        var v2 = new Vector<double>(0, 1);
        v1.Dot(v2).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Dot_SameDirection()
    {
        var v = new Vector<double>(3, 4);
        v.Dot(v).Should().BeApproximately(25, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf()
    {
        var a = new Vector<double>(0, 0);
        var b = new Vector<double>(10, 20);
        var result = a.Lerp(b, 0.5);
        result.X.Should().BeApproximately(5, 1e-9);
        result.Y.Should().BeApproximately(10, 1e-9);
    }
}

public class LineEnhancementsTests
{
    [Fact]
    public void IsParallelTo_ParallelLines_ReturnsTrue()
    {
        var l1 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var l2 = Line<double>.From(new Point<double>(0, 1), new Point<double>(1, 2));
        l1.IsParallelTo(l2).Should().BeTrue();
    }

    [Fact]
    public void IsParallelTo_NonParallel_ReturnsFalse()
    {
        var l1 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var l2 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 0));
        l1.IsParallelTo(l2).Should().BeFalse();
    }

    [Fact]
    public void IsParallelTo_BothVertical_ReturnsTrue()
    {
        var l1 = Line<double>.Vertical(1);
        var l2 = Line<double>.Vertical(5);
        l1.IsParallelTo(l2).Should().BeTrue();
    }

    [Fact]
    public void IsPerpendicularTo_HorizontalAndVertical()
    {
        var h = Line<double>.Horizontal(0);
        var v = Line<double>.Vertical(0);
        h.IsPerpendicularTo(v).Should().BeTrue();
    }

    [Fact]
    public void IsPerpendicularTo_45_And_Neg45()
    {
        var l1 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var l2 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, -1));
        l1.IsPerpendicularTo(l2).Should().BeTrue();
    }

    [Fact]
    public void IsPerpendicularTo_Parallel_ReturnsFalse()
    {
        var l1 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var l2 = Line<double>.From(new Point<double>(0, 1), new Point<double>(1, 2));
        l1.IsPerpendicularTo(l2).Should().BeFalse();
    }

    [Fact]
    public void PerpendicularAt_Horizontal()
    {
        var line = Line<double>.Horizontal(5);
        var perp = line.PerpendicularAt(new Point<double>(3, 5));
        perp.IsVertical.Should().BeTrue();
        perp.VerticalX.Should().BeApproximately(3, 1e-9);
    }

    [Fact]
    public void PerpendicularAt_Vertical()
    {
        var line = Line<double>.Vertical(5);
        var perp = line.PerpendicularAt(new Point<double>(5, 3));
        perp.IsVertical.Should().BeFalse();
        perp.Compute(0).Should().BeApproximately(3, 1e-9);
        perp.Compute(10).Should().BeApproximately(3, 1e-9);
    }

    [Fact]
    public void ProjectPoint_OnHorizontal()
    {
        var line = Line<double>.Horizontal(0);
        var proj = line.ProjectPoint(new Point<double>(5, 10));
        proj.X.Should().BeApproximately(5, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void ProjectPoint_OnVertical()
    {
        var line = Line<double>.Vertical(3);
        var proj = line.ProjectPoint(new Point<double>(10, 7));
        proj.X.Should().BeApproximately(3, 1e-9);
        proj.Y.Should().BeApproximately(7, 1e-9);
    }

    [Fact]
    public void ProjectPoint_On45DegreeLine()
    {
        var line = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var proj = line.ProjectPoint(new Point<double>(2, 0));
        proj.X.Should().BeApproximately(1, 1e-9);
        proj.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossHorizontal()
    {
        var line = Line<double>.Horizontal(0);
        var result = line.Reflect(new Point<double>(3, 5));
        result.X.Should().BeApproximately(3, 1e-9);
        result.Y.Should().BeApproximately(-5, 1e-9);
    }

    [Fact]
    public void Reflect_AcrossVertical()
    {
        var line = Line<double>.Vertical(0);
        var result = line.Reflect(new Point<double>(5, 3));
        result.X.Should().BeApproximately(-5, 1e-9);
        result.Y.Should().BeApproximately(3, 1e-9);
    }
}

public class SegmentEnhancementsTests
{
    [Fact]
    public void ProjectPoint_OnSegment()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(5, 5));
        proj.X.Should().BeApproximately(5, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void ProjectPoint_BeyondEnd_ClampsToEnd()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(20, 5));
        proj.X.Should().BeApproximately(10, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void ProjectPoint_BeforeStart_ClampsToStart()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var proj = s.ProjectPoint(new Point<double>(-5, 5));
        proj.X.Should().BeApproximately(0, 1e-9);
        proj.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(0);
        p.X.Should().BeApproximately(0, 1e-9);
        p.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsEnd()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(1);
        p.X.Should().BeApproximately(10, 1e-9);
        p.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 20));
        var p = s.Lerp(0.5);
        p.X.Should().BeApproximately(5, 1e-9);
        p.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Split_AtHalf()
    {
        var s = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var (left, right) = s.Split(0.5);
        left.Start.X.Should().BeApproximately(0, 1e-9);
        left.End.X.Should().BeApproximately(5, 1e-9);
        right.Start.X.Should().BeApproximately(5, 1e-9);
        right.End.X.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void IsParallelTo_ParallelSegments()
    {
        var s1 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var s2 = Segment<double>.From(new Point<double>(0, 5), new Point<double>(10, 5));
        s1.IsParallelTo(s2).Should().BeTrue();
    }

    [Fact]
    public void IsParallelTo_NonParallelSegments()
    {
        var s1 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 0));
        var s2 = Segment<double>.From(new Point<double>(0, 0), new Point<double>(10, 10));
        s1.IsParallelTo(s2).Should().BeFalse();
    }

    [Fact]
    public void Reverse_SwapsStartEnd()
    {
        var s = Segment<double>.From(new Point<double>(1, 2), new Point<double>(3, 4));
        var r = s.Reverse();
        r.Start.X.Should().BeApproximately(3, 1e-9);
        r.Start.Y.Should().BeApproximately(4, 1e-9);
        r.End.X.Should().BeApproximately(1, 1e-9);
        r.End.Y.Should().BeApproximately(2, 1e-9);
    }
}

public class CircleEnhancementsTests
{
    [Fact]
    public void DistanceTo_PointOnCircle_IsZero()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(5, 0)).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointOutside()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(10, 0)).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointInside()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        c.DistanceTo(new Point<double>(2, 0)).Should().BeApproximately(3, 1e-9);
    }

    [Fact]
    public void PointAt_ZeroAngle()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var p = c.PointAt(Radian<double>.FromRadian(0));
        p.X.Should().BeApproximately(5, 1e-9);
        p.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void PointAt_90Degrees()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var p = c.PointAt(Radian<double>.FromRadian(Math.PI / 2));
        p.X.Should().BeApproximately(0, 1e-9);
        p.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void PointAt_WithOffset()
    {
        var c = new Circle<double>(new Point<double>(10, 20), 3);
        var p = c.PointAt(Radian<double>.FromRadian(0));
        p.X.Should().BeApproximately(13, 1e-9);
        p.Y.Should().BeApproximately(20, 1e-9);
    }

    [Fact]
    public void Rotate_MovesCenter()
    {
        var c = new Circle<double>(new Point<double>(5, 0), 2);
        var rotated = c.Rotate(Degree<double>.Create(90));
        rotated.Center.X.Should().BeApproximately(0, 1e-9);
        rotated.Center.Y.Should().BeApproximately(5, 1e-9);
        rotated.Radius.Should().BeApproximately(2, 1e-9);
    }

    [Fact]
    public void Scale_ByFactor()
    {
        var c = new Circle<double>(new Point<double>(0, 0), 5);
        var scaled = c.Scale(2.0);
        scaled.Radius.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Scale_BySize()
    {
        var c = new Circle<double>(new Point<double>(1, 2), 5);
        var scaled = c.Scale(new Size<double>(2, 2));
        scaled.Radius.Should().BeApproximately(10, 1e-9);
        scaled.Center.X.Should().BeApproximately(2, 1e-9);
        scaled.Center.Y.Should().BeApproximately(4, 1e-9);
    }
}

public class TriangleEnhancementsTests
{
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
}

public class RectangleEnhancementsTests
{
    [Fact]
    public void Area_ReturnsCorrectValue()
    {
        var r = new Rectangle<double>(0, 0, 10, 5);
        r.Area().Should().BeApproximately(50, 1e-9);
    }

    [Fact]
    public void Perimeter_ReturnsCorrectValue()
    {
        var r = new Rectangle<double>(0, 0, 10, 5);
        r.Perimeter().Should().BeApproximately(30, 1e-9);
    }

    [Fact]
    public void Diagonal_ReturnsCorrectValue()
    {
        var r = new Rectangle<double>(0, 0, 3, 4);
        r.Diagonal.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointInside_ReturnsZero()
    {
        var r = new Rectangle<double>(0, 0, 10, 10);
        r.DistanceTo(new Point<double>(5, 5)).Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointOutside()
    {
        var r = new Rectangle<double>(0, 0, 10, 10);
        r.DistanceTo(new Point<double>(13, 14)).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void DistanceTo_PointAlongEdge()
    {
        var r = new Rectangle<double>(0, 0, 10, 10);
        r.DistanceTo(new Point<double>(5, 15)).Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void ImplicitToPolygon_HasFourVertices()
    {
        var r = new Rectangle<double>(0, 0, 10, 5);
        Polygon<double> poly = r;
        poly.Count.Should().Be(4);
    }

    [Fact]
    public void ImplicitToPolygon_AreaMatchesRectangle()
    {
        var r = new Rectangle<double>(0, 0, 10, 5);
        Polygon<double> poly = r;
        poly.Area().Should().BeApproximately(50, 1e-9);
    }
}

public class PolygonEnhancementsTests
{
    private static Polygon<double> Square => new(
        new Point<double>(0, 0),
        new Point<double>(10, 0),
        new Point<double>(10, 10),
        new Point<double>(0, 10));

    [Fact]
    public void ContainsPoint_Inside_ReturnsTrue()
    {
        Square.Contains(new Point<double>(5, 5)).Should().BeTrue();
    }

    [Fact]
    public void ContainsPoint_Outside_ReturnsFalse()
    {
        Square.Contains(new Point<double>(15, 5)).Should().BeFalse();
    }

    [Fact]
    public void ContainsPoint_ConcavePolygon()
    {
        // L-shaped polygon
        var lShape = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 5),
            new Point<double>(5, 5),
            new Point<double>(5, 10),
            new Point<double>(0, 10));
        lShape.Contains(new Point<double>(2, 2)).Should().BeTrue();
        lShape.Contains(new Point<double>(7, 7)).Should().BeFalse();
    }

    [Fact]
    public void Perimeter_Square()
    {
        Square.Perimeter().Should().BeApproximately(40, 1e-9);
    }

    [Fact]
    public void Centroid_Square()
    {
        var c = Square.Centroid();
        c.X.Should().BeApproximately(5, 1e-9);
        c.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void IsConvex_Square_ReturnsTrue()
    {
        Square.IsConvex().Should().BeTrue();
    }

    [Fact]
    public void IsConvex_LShape_ReturnsFalse()
    {
        var lShape = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 5),
            new Point<double>(5, 5),
            new Point<double>(5, 10),
            new Point<double>(0, 10));
        lShape.IsConvex().Should().BeFalse();
    }

    [Fact]
    public void Edges_Square_ReturnsFourEdges()
    {
        var edges = Square.Edges();
        edges.Length.Should().Be(4);
        edges[0].Length.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void ConvexHull_AlreadyConvex_ReturnsSame()
    {
        var hull = Square.ConvexHull();
        hull.Count.Should().Be(4);
        hull.Area().Should().BeApproximately(100, 1e-9);
    }

    [Fact]
    public void ConvexHull_WithInternalPoint_RemovesIt()
    {
        var poly = new Polygon<double>(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(5, 5),
            new Point<double>(10, 10),
            new Point<double>(0, 10));
        var hull = poly.ConvexHull();
        hull.Count.Should().Be(4);
        hull.Area().Should().BeApproximately(100, 1e-9);
    }
}

public class PathEnhancementsTests2
{
    [Fact]
    public void Rotate_NonEmpty_TransformsPoints()
    {
        var path = Path<double>.FromPoints(
            new Point<double>(0, 0),
            new Point<double>(10, 0),
            new Point<double>(10, 10));
        var rotated = path.Rotate(Degree<double>.Create(90));
        rotated.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Length_StraightLine()
    {
        // A straight line from (0,0) to (10,0) as a path
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(3.33, 0),
            new Point<double>(6.67, 0),
            new Point<double>(10, 0));
        var path = Path<double>.FromSegments(curve);
        path.Length().Should().BeApproximately(10, 0.1);
    }

    [Fact]
    public void PointAt_Zero_ReturnsStart()
    {
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(3.33, 0),
            new Point<double>(6.67, 0),
            new Point<double>(10, 0));
        var path = Path<double>.FromSegments(curve);
        var start = path.PointAt(0.0);
        start.X.Should().BeApproximately(0, 0.5);
        start.Y.Should().BeApproximately(0, 0.5);
    }

    [Fact]
    public void PointAt_One_ReturnsEnd()
    {
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0),
            new Point<double>(3.33, 0),
            new Point<double>(6.67, 0),
            new Point<double>(10, 0));
        var path = Path<double>.FromSegments(curve);
        var end = path.PointAt(1.0);
        end.X.Should().BeApproximately(10, 0.5);
        end.Y.Should().BeApproximately(0, 0.5);
    }

    [Fact]
    public void Length_EmptyPath_ReturnsZero()
    {
        var path = new Path<double>();
        path.Length().Should().BeApproximately(0, 1e-9);
    }
}

public class IntersectRectangleTests
{
    private static readonly Rectangle<double> Roi = new(0, 0, 10, 10);

    [Fact]
    public void Segment_FullyInside_ReturnsSame()
    {
        var s = Segment<double>.From(new Point<double>(2, 2), new Point<double>(8, 8));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(2, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(8, 1e-9);
    }

    [Fact]
    public void Segment_FullyOutside_ReturnsNull()
    {
        var s = Segment<double>.From(new Point<double>(20, 20), new Point<double>(30, 30));
        s.Intersect(Roi).Should().BeNull();
    }

    [Fact]
    public void Segment_PartiallyInside_ClipsCorrectly()
    {
        var s = Segment<double>.From(new Point<double>(-5, 5), new Point<double>(15, 5));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(10, 1e-9);
        clipped.Value.Start.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void Segment_Diagonal_ClipsBothEnds()
    {
        var s = Segment<double>.From(new Point<double>(-5, -5), new Point<double>(15, 15));
        var clipped = s.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(0, 1e-9);
        clipped.Value.Start.Y.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(10, 1e-9);
        clipped.Value.End.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Line_Horizontal_ClipsToRect()
    {
        var line = Line<double>.Horizontal(5);
        var clipped = line.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.X.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.X.Should().BeApproximately(10, 1e-9);
        clipped.Value.Start.Y.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void Line_Vertical_ClipsToRect()
    {
        var line = Line<double>.Vertical(5);
        var clipped = line.Intersect(Roi);
        clipped.Should().NotBeNull();
        clipped!.Value.Start.Y.Should().BeApproximately(0, 1e-9);
        clipped.Value.End.Y.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void Line_Outside_ReturnsNull()
    {
        var line = Line<double>.Horizontal(20);
        line.Intersect(Roi).Should().BeNull();
    }

    [Fact]
    public void Line_Vertical_Outside_ReturnsNull()
    {
        var line = Line<double>.Vertical(20);
        line.Intersect(Roi).Should().BeNull();
    }

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

    [Fact]
    public void Circle_FullyInside_ReturnsApproximation()
    {
        var c = new Circle<double>(new Point<double>(5, 5), 2);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().BeGreaterThan(0);
        clipped.Area().Should().BeApproximately(c.Area(), 0.5);
    }

    [Fact]
    public void Circle_PartiallyInside_ClipsCorrectly()
    {
        var c = new Circle<double>(new Point<double>(0, 5), 5);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().BeGreaterThan(0);
        clipped.Area().Should().BeLessThan(c.Area());
    }

    [Fact]
    public void Circle_FullyOutside_ReturnsEmpty()
    {
        var c = new Circle<double>(new Point<double>(50, 50), 2);
        var clipped = c.Intersect(Roi);
        clipped.Count.Should().Be(0);
    }

    [Fact]
    public void Line_ProjectSegmentOntoLine()
    {
        var line = Line<double>.Horizontal(0);
        var seg = Segment<double>.From(new Point<double>(3, 5), new Point<double>(7, 5));
        var proj = line.Project(seg);
        proj.Start.X.Should().BeApproximately(3, 1e-9);
        proj.Start.Y.Should().BeApproximately(0, 1e-9);
        proj.End.X.Should().BeApproximately(7, 1e-9);
        proj.End.Y.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Segment_ProjectOntoLine()
    {
        var line = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var seg = Segment<double>.From(new Point<double>(2, 0), new Point<double>(0, 2));
        var proj = seg.ProjectOnto(line);
        proj.Start.X.Should().BeApproximately(1, 1e-9);
        proj.Start.Y.Should().BeApproximately(1, 1e-9);
        proj.End.X.Should().BeApproximately(1, 1e-9);
        proj.End.Y.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void BezierCurve_Split_PreservesEndpoints()
    {
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0), new Point<double>(1, 3),
            new Point<double>(3, 3), new Point<double>(4, 0));
        var (left, right) = curve.Split(0.5);

        left.Start.X.Should().BeApproximately(0, 1e-9);
        left.Start.Y.Should().BeApproximately(0, 1e-9);
        right.End.X.Should().BeApproximately(4, 1e-9);
        right.End.Y.Should().BeApproximately(0, 1e-9);

        // Split point should be the same
        left.End.X.Should().BeApproximately(right.Start.X, 1e-9);
        left.End.Y.Should().BeApproximately(right.Start.Y, 1e-9);

        // Split point should match Evaluate
        var expected = curve.Evaluate(0.5);
        left.End.X.Should().BeApproximately(expected.X, 1e-9);
        left.End.Y.Should().BeApproximately(expected.Y, 1e-9);
    }

    [Fact]
    public void BezierCurve_SubCurve_MatchesEvaluatedEndpoints()
    {
        var curve = new BezierCurve<double>(
            new Point<double>(0, 0), new Point<double>(1, 4),
            new Point<double>(3, 4), new Point<double>(4, 0));
        var sub = curve.SubCurve(0.25, 0.75);

        var p1 = curve.Evaluate(0.25);
        var p2 = curve.Evaluate(0.75);

        sub.Start.X.Should().BeApproximately(p1.X, 1e-9);
        sub.Start.Y.Should().BeApproximately(p1.Y, 1e-9);
        sub.End.X.Should().BeApproximately(p2.X, 1e-9);
        sub.End.Y.Should().BeApproximately(p2.Y, 1e-9);
    }

    [Fact]
    public void Path_Intersect_FullyInside_ReturnsSinglePath()
    {
        // Path fully inside a large rectangle
        var path = Path<double>.FromPoints(
            new Point<double>(2, 2),
            new Point<double>(4, 8),
            new Point<double>(8, 5));
        var roi = new Rectangle<double>(0, 0, 10, 10);

        var results = path.Intersect(roi).ToList();
        results.Should().HaveCount(1);
        results[0].Count.Should().Be(path.Count);
    }

    [Fact]
    public void Path_Intersect_FullyOutside_ReturnsEmpty()
    {
        var path = Path<double>.FromPoints(
            new Point<double>(20, 20),
            new Point<double>(25, 28),
            new Point<double>(30, 22));
        var roi = new Rectangle<double>(0, 0, 10, 10);

        var results = path.Intersect(roi).ToList();
        results.Should().BeEmpty();
    }

    [Fact]
    public void Path_Intersect_CrossingRect_ProducesClippedSubPaths()
    {
        // Straight-line path that crosses through the rectangle
        var path = Path<double>.FromSegments(
            new BezierCurve<double>(
                new Point<double>(-5, 5), new Point<double>(-2, 5),
                new Point<double>(12, 5), new Point<double>(15, 5)));
        var roi = new Rectangle<double>(0, 0, 10, 10);

        var results = path.Intersect(roi).ToList();
        results.Should().HaveCount(1);

        // The clipped path's start should be near x=0, end near x=10
        var clipped = results[0];
        clipped.Segments[0].Start.X.Should().BeApproximately(0, 0.5);
        clipped.Segments[^1].End.X.Should().BeApproximately(10, 0.5);
    }

    [Fact]
    public void Path_Intersect_EmptyPath_ReturnsEmpty()
    {
        var path = new Path<double>();
        var roi = new Rectangle<double>(0, 0, 10, 10);

        path.Intersect(roi).Should().BeEmpty();
    }
}
