using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

public class BezierCurveTests
{
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
}
