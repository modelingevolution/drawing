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
}
