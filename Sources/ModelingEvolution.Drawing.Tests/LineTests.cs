using FluentAssertions;
using ModelingEvolution.Drawing;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing.Tests;

public class LineTests
{
    [Fact]
    public void FromTwoPoints_HorizontalLine()
    {
        var line = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5));
        line.IsVertical.Should().BeFalse();
        line.Compute(0f).Should().BeApproximately(5f, 1e-5f);
        line.Compute(10f).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void FromTwoPoints_DiagonalLine()
    {
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        line.IsVertical.Should().BeFalse();
        line.Compute(5f).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void FromTwoPoints_VerticalLine()
    {
        var line = Line<float>.From(new Point<float>(5, 0), new Point<float>(5, 10));
        line.IsVertical.Should().BeTrue();
    }

    [Fact]
    public void Vertical_Factory()
    {
        var line = Line<float>.Vertical(3f);
        line.IsVertical.Should().BeTrue();
        line.VerticalX.Should().Be(3f);
    }

    [Fact]
    public void FromEquation_WrapsLinearEquation()
    {
        var eq = new LinearEquation<float>(2f, 3f); // y = 2x + 3
        var line = Line<float>.FromEquation(eq);
        line.IsVertical.Should().BeFalse();
        line.Equation.A.Should().Be(2f);
        line.Equation.B.Should().Be(3f);
        line.Compute(1f).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void FromPointAndDirection_NonVertical()
    {
        var line = Line<float>.From(new Point<float>(0, 0), new Vector<float>(1, 2));
        line.IsVertical.Should().BeFalse();
        line.Compute(5f).Should().BeApproximately(10f, 1e-5f);
    }

    [Fact]
    public void FromPointAndDirection_Vertical()
    {
        var line = Line<float>.From(new Point<float>(7, 0), new Vector<float>(0, 1));
        line.IsVertical.Should().BeTrue();
        line.VerticalX.Should().Be(7f);
    }

    [Fact]
    public void Equation_ThrowsForVerticalLine()
    {
        var line = Line<float>.Vertical(5f);
        var act = () => line.Equation;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Compute_ThrowsForVerticalLine()
    {
        var line = Line<float>.Vertical(5f);
        var act = () => line.Compute(1f);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void VerticalX_ThrowsForNonVerticalLine()
    {
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        var act = () => line.VerticalX;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Intersect_TwoNonVerticalLines()
    {
        var line1 = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10)); // y = x
        var line2 = Line<float>.From(new Point<float>(0, 10), new Point<float>(10, 0)); // y = -x + 10
        var result = line1.Intersect(line2);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(5f, 1e-4f);
        result.Value.Y.Should().BeApproximately(5f, 1e-4f);
    }

    [Fact]
    public void Intersect_ParallelLines_ReturnsNull()
    {
        var line1 = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 0)); // y = 0
        var line2 = Line<float>.From(new Point<float>(0, 5), new Point<float>(10, 5)); // y = 5
        line1.Intersect(line2).Should().BeNull();
    }

    [Fact]
    public void Intersect_BothVertical_ReturnsNull()
    {
        var line1 = Line<float>.Vertical(3f);
        var line2 = Line<float>.Vertical(5f);
        line1.Intersect(line2).Should().BeNull();
    }

    [Fact]
    public void Intersect_OneVerticalOneNormal()
    {
        var vertical = Line<float>.Vertical(5f);
        var diagonal = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10)); // y = x
        var result = vertical.Intersect(diagonal);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(5f, 1e-5f);
        result.Value.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void Intersect_NormalAndVertical_CommutativeResult()
    {
        var vertical = Line<float>.Vertical(5f);
        var diagonal = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        var r1 = vertical.Intersect(diagonal);
        var r2 = diagonal.Intersect(vertical);
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();
        r1!.Value.X.Should().BeApproximately(r2!.Value.X, 1e-5f);
        r1.Value.Y.Should().BeApproximately(r2.Value.Y, 1e-5f);
    }

    [Fact]
    public void AddVector_TranslatesNonVerticalLine()
    {
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10)); // y = x
        var translated = line + new Vector<float>(0, 5);
        // y = x translated up by 5 → y = x + 5
        translated.Compute(0f).Should().BeApproximately(5f, 1e-5f);
        translated.Compute(10f).Should().BeApproximately(15f, 1e-5f);
    }

    [Fact]
    public void AddVector_TranslatesVerticalLine()
    {
        var line = Line<float>.Vertical(3f);
        var translated = line + new Vector<float>(5, 0);
        translated.IsVertical.Should().BeTrue();
        translated.VerticalX.Should().Be(8f);
    }

    [Fact]
    public void SubtractVector_TranslatesLine()
    {
        var line = Line<float>.Vertical(8f);
        var translated = line - new Vector<float>(5, 0);
        translated.IsVertical.Should().BeTrue();
        translated.VerticalX.Should().Be(3f);
    }

    [Fact]
    public void Intersect_VerticalAndHorizontal()
    {
        var vertical = Line<float>.Vertical(3f);
        var horizontal = Line<float>.From(new Point<float>(0, 7), new Point<float>(10, 7));
        var result = vertical.Intersect(horizontal);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(3f, 1e-5f);
        result.Value.Y.Should().BeApproximately(7f, 1e-5f);
    }

    [Fact]
    public void Horizontal_Factory()
    {
        var line = Line<float>.Horizontal(7f);
        line.IsVertical.Should().BeFalse();
        line.Equation.A.Should().Be(0f);
        line.Equation.B.Should().Be(7f);
        line.Compute(0f).Should().BeApproximately(7f, 1e-5f);
        line.Compute(100f).Should().BeApproximately(7f, 1e-5f);
    }

    [Fact]
    public void Horizontal_IntersectsVertical()
    {
        var horizontal = Line<float>.Horizontal(7f);
        var vertical = Line<float>.Vertical(3f);
        var result = horizontal.Intersect(vertical);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(3f, 1e-5f);
        result.Value.Y.Should().BeApproximately(7f, 1e-5f);
    }

    [Fact]
    public void Rotate_HorizontalBy90_BecomesVertical()
    {
        // Use double — float cos(90°) ≈ 4.37e-8, above 1e-9 vertical threshold
        var line = Line<double>.Horizontal(0);
        var rotated = line.Rotate(Degree<double>.Create(90));
        rotated.IsVertical.Should().BeTrue();
        rotated.VerticalX.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void Rotate_VerticalByMinus90_BecomesHorizontal()
    {
        var line = Line<double>.Vertical(0);
        var rotated = line.Rotate(Degree<double>.Create(-90));
        rotated.IsVertical.Should().BeFalse();
        rotated.Compute(5).Should().BeApproximately(0, 1e-8);
    }

    [Fact]
    public void Rotate_DiagonalBy45_AroundOrigin()
    {
        // y = x rotated 45 degrees CCW should become vertical (x = 0)
        var line = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var rotated = line.Rotate(Degree<double>.Create(45));
        rotated.IsVertical.Should().BeTrue();
        rotated.VerticalX.Should().BeApproximately(0, 1e-8);
    }

    [Fact]
    public void Rotate_AroundCustomOrigin()
    {
        // Horizontal line y=0, rotate 90 around (5, 0) → vertical line x=5
        var line = Line<double>.Horizontal(0);
        var rotated = line.Rotate(Degree<double>.Create(90), new Point<double>(5, 0));
        rotated.IsVertical.Should().BeTrue();
        rotated.VerticalX.Should().BeApproximately(5, 1e-8);
    }

    [Fact]
    public void Rotate_30Degrees_SlopeChanges()
    {
        // y = 0 rotated 30 degrees → slope = tan(30°) ≈ 0.5774
        var line = Line<float>.Horizontal(0f);
        var rotated = line.Rotate(Degree<float>.Create(30f));
        rotated.IsVertical.Should().BeFalse();
        rotated.Equation.A.Should().BeApproximately(0.5774f, 1e-3f);
    }

    [Fact]
    public void PlusDegree_Operator()
    {
        var line = Line<double>.Horizontal(0);
        var rotated = line + Degree<double>.Create(90);
        rotated.IsVertical.Should().BeTrue();
    }

    [Fact]
    public void MinusDegree_Operator()
    {
        var line = Line<double>.Horizontal(0);
        var rotated = line - Degree<double>.Create(-90); // subtracting -90 = +90
        rotated.IsVertical.Should().BeTrue();
    }

    [Fact]
    public void DistanceTo_PointOnLine_ReturnsZero()
    {
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        line.DistanceTo(new Point<float>(5, 5)).Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_HorizontalLine()
    {
        var line = Line<float>.Horizontal(3f);
        line.DistanceTo(new Point<float>(0, 8)).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_VerticalLine()
    {
        var line = Line<float>.Vertical(3f);
        line.DistanceTo(new Point<float>(8, 0)).Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void DistanceTo_DiagonalLine()
    {
        // y = x, distance from (0, 1) is 1/sqrt(2) ≈ 0.7071
        var line = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        line.DistanceTo(new Point<float>(0, 1)).Should().BeApproximately(0.7071f, 1e-3f);
    }

    [Fact]
    public void CalculateTranslation_ParallelHorizontals_ReturnsVector()
    {
        var line1 = Line<float>.Horizontal(0f);
        var line2 = Line<float>.Horizontal(5f);
        var result = line1.CalculateTranslation(line2);
        result.Should().NotBeNull();
        result!.Value.Length.Should().BeApproximately(5f, 1e-5f);
        // Vector should point from y=0 to y=5, i.e. (0, 5)
        result.Value.X.Should().BeApproximately(0f, 1e-5f);
        result.Value.Y.Should().BeApproximately(5f, 1e-5f);
    }

    [Fact]
    public void CalculateTranslation_ParallelVerticals_ReturnsVector()
    {
        var line1 = Line<float>.Vertical(2f);
        var line2 = Line<float>.Vertical(7f);
        var result = line1.CalculateTranslation(line2);
        result.Should().NotBeNull();
        result!.Value.X.Should().BeApproximately(5f, 1e-5f);
        result.Value.Y.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void CalculateTranslation_NotParallel_ReturnsNull()
    {
        var line1 = Line<float>.Horizontal(0f);
        var line2 = Line<float>.From(new Point<float>(0, 0), new Point<float>(10, 10));
        line1.CalculateTranslation(line2).Should().BeNull();
    }

    [Fact]
    public void CalculateTranslation_VerticalAndNonVertical_ReturnsNull()
    {
        var line1 = Line<float>.Vertical(3f);
        var line2 = Line<float>.Horizontal(5f);
        line1.CalculateTranslation(line2).Should().BeNull();
    }

    [Fact]
    public void CalculateTranslation_SameLine_ReturnsZeroVector()
    {
        var line1 = Line<float>.Horizontal(5f);
        var line2 = Line<float>.Horizontal(5f);
        var result = line1.CalculateTranslation(line2);
        result.Should().NotBeNull();
        result!.Value.Length.Should().BeApproximately(0f, 1e-5f);
    }

    [Fact]
    public void AngleBetween_PerpendicularLines_ReturnsHalfPi()
    {
        var horizontal = Line<double>.Horizontal(0);
        var vertical = Line<double>.Vertical(0);
        var angle = (double)horizontal.AngleBetween(vertical);
        Math.Abs(angle).Should().BeApproximately(Math.PI / 2, 1e-9);
    }

    [Fact]
    public void AngleBetween_ParallelLines_ReturnsZero()
    {
        var line1 = Line<double>.Horizontal(0);
        var line2 = Line<double>.Horizontal(5);
        var angle = (double)line1.AngleBetween(line2);
        angle.Should().BeApproximately(0, 1e-9);
    }

    [Fact]
    public void AngleBetween_45DegreeLines()
    {
        // y = 0 and y = x (slope 1, angle = 45° = π/4)
        var horizontal = Line<double>.Horizontal(0);
        var diagonal = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var angle = (double)horizontal.AngleBetween(diagonal);
        angle.Should().BeApproximately(Math.PI / 4, 1e-9);
    }

    [Fact]
    public void AngleBetween_NegativeSlope()
    {
        // y = 0 and y = -x (slope -1, angle = -π/4)
        var horizontal = Line<double>.Horizontal(0);
        var negDiagonal = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, -1));
        var angle = (double)horizontal.AngleBetween(negDiagonal);
        angle.Should().BeApproximately(-Math.PI / 4, 1e-9);
    }

    [Fact]
    public void AngleBetween_IsAnticommutative()
    {
        var line1 = Line<double>.Horizontal(0);
        var line2 = Line<double>.From(new Point<double>(0, 0), new Point<double>(1, 1));
        var a1 = (double)line1.AngleBetween(line2);
        var a2 = (double)line2.AngleBetween(line1);
        a1.Should().BeApproximately(-a2, 1e-9);
    }

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

    private static readonly Rectangle<double> Roi = new(0, 0, 10, 10);

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
}
