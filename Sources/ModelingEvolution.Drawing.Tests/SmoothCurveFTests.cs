using FluentAssertions;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing.Tests;

public class SmoothCurveFTests
{
    [Fact]
    public void Single()
    {
        PolygonalCurve<float> c =
            PolygonalCurve<float>.From(new Point<float>(0, 1), new Point<float>(1, 2), new Point<float>(2, 2), new Point<float>(3, 1));
        var s1 = c.GetSmoothSegment(0);
        var s2 = c.GetSmoothSegment(1);
        var s3 = c.GetSmoothSegment(2);

        s1.End.Should().Be(s2.Start);
        s2.End.Should().Be(s3.Start);

        s1.C0.X.Should().BeInRange(s1.Start.X, s1.End.X);
        s1.C1.X.Should().BeInRange(s1.Start.X, s1.End.X);

        s1.C0.Y.Should().BeInRange(s1.Start.Y, s1.End.Y);
        s1.C1.Y.Should().BeInRange(s1.Start.Y, s1.End.Y);

        s2.C0.X.Should().BeInRange(s2.Start.X, s2.End.X);
        s2.C1.X.Should().BeInRange(s2.Start.X, s2.End.X);

        s2.C0.Y.Should().BeInRange(2, 3); // mamy brzuszek...
        s2.C1.Y.Should().BeInRange(2, 3);

        s3.C0.X.Should().BeInRange(2, 3);
        s3.C1.X.Should().BeInRange(2, 3);

        s3.C0.Y.Should().BeInRange(s1.Start.Y, s1.End.Y);
        s3.C1.Y.Should().BeInRange(s1.Start.Y, s1.End.Y);

    }

    [Fact]
    public void CanFindExtremum()
    {

        PolygonalCurve<float> c =
            PolygonalCurve<float>.From(new Point<float>(0, 1), new Point<float>(1, 2), new Point<float>(2, 2), new Point<float>(3, 1));
        var s1 = c.GetSmoothSegment(0);
        var s2 = c.GetSmoothSegment(1);
        var s3 = c.GetSmoothSegment(2);

        var ex = s2.CalculateExtremumPoints().Single();
        ex.X.Should().BeInRange(1, 2);
        ex.Y.Should().BeInRange(2, 3);

        var e0 = s1.CalculateExtremumPoints().Single();
        var e1 = s3.CalculateExtremumPoints().Single();
        e0.Should().Be(new Point<float>(1, 2));
        e1.Should().Be(new Point<float>(2, 2));
    }

    [Fact]
    public void IntersectionWithALine()
    {
        PolygonalCurve<float> c =
            PolygonalCurve<float>.From(new Point<float>(0, 1), new Point<float>(1, 2), new Point<float>(2, 2), new Point<float>(3, 1));
        var s1 = c.GetSmoothSegment(0);
        var s2 = c.GetSmoothSegment(1);
        var s3 = c.GetSmoothSegment(2);

        LinearEquation<float> l2 = new LinearEquation<float>(1,0.5f);
        var point = s2.Intersection(l2);
        point.X.Should().BeInRange(1.5f, 2);
        point.Y.Should().BeInRange(2, 2.5f);
    }
}