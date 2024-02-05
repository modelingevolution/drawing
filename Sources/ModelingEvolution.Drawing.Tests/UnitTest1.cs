using FluentAssertions;
using ModelingEvolution.Drawing.Equations;
using PointF = ModelingEvolution.Drawing.Point<float>;
using VectorF = ModelingEvolution.Drawing.Vector<float>;
using PolygonalCurveF = ModelingEvolution.Drawing.Equations.PolygonalCurve<float>;
using LinearEquationF = ModelingEvolution.Drawing.Equations.LinearEquation<float>;
using SizeF = ModelingEvolution.Drawing.Size<float>;
using RectangleF = ModelingEvolution.Drawing.Rectangle<float>;
using RectangleAreaF = ModelingEvolution.Drawing.RectangleArea<float>;

namespace ModelingEvolution.Drawing.Tests
{
    public class SmoothCurveFTests
    {
        [Fact]
        public void Single()
        {
            PolygonalCurveF c =
                PolygonalCurveF.From(new PointF(0, 1), new PointF(1, 2), new PointF(2, 2), new PointF(3, 1));
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

            PolygonalCurveF c =
                PolygonalCurveF.From(new PointF(0, 1), new PointF(1, 2), new PointF(2, 2), new PointF(3, 1));
            var s1 = c.GetSmoothSegment(0);
            var s2 = c.GetSmoothSegment(1);
            var s3 = c.GetSmoothSegment(2);

            var ex = s2.CalculateExtremumPoints().Single();
            ex.X.Should().BeInRange(1, 2);
            ex.Y.Should().BeInRange(2, 3);

            var e0 = s1.CalculateExtremumPoints().Single();
            var e1 = s3.CalculateExtremumPoints().Single();
            e0.Should().Be(new PointF(1, 2));
            e1.Should().Be(new PointF(2, 2));
        }

        [Fact]
        public void IntersectionWithALine()
        {
            PolygonalCurveF c =
                PolygonalCurveF.From(new PointF(0, 1), new PointF(1, 2), new PointF(2, 2), new PointF(3, 1));
            var s1 = c.GetSmoothSegment(0);
            var s2 = c.GetSmoothSegment(1);
            var s3 = c.GetSmoothSegment(2);

            LinearEquationF l2 = new LinearEquationF(1,0.5f);
            var point = s2.Intersection(l2);
            point.X.Should().BeInRange(1.5f, 2);
            point.Y.Should().BeInRange(2, 2.5f);
        }
    }
    public class RectangleAreaFTests
    {
        [Fact]
        public void Empty()
        {
            RectangleAreaF area = new RectangleAreaF();
            area.Value.Should().Be(0f);
        }
        [Fact]
        public void Single()
        {
            RectangleAreaF area = new RectangleAreaF();

            var actual = new RectangleF(10, 10, 20, 20);
            area += actual;
            
            area.Value.Should().Be(400f);
            
            RectangleF r = (RectangleF)area;
            r.Should().Be(actual);
        }

        [Fact]
        public void SameUnion()
        {
            RectangleAreaF area = new RectangleAreaF();

            var r1 = new RectangleF(10, 10, 20, 20);
            var r2 = new RectangleF(10, 10, 20, 20);
            
            area += r1;
            area += r2;

            area.Value.Should().Be(400f);

            RectangleF r = (RectangleF)area;
            r.Should().Be(r1);
        }
        [Fact]
        public void MergeUnion()
        {
            RectangleAreaF area = new RectangleAreaF();

            var r1 = new RectangleF(10, 10, 20, 20);
            var r2 = new RectangleF(20, 20, 20, 20);

            area += r1;
            area += r2;

            area.Value.Should().Be(900f);

            RectangleF r = (RectangleF)area;
            r.Should().Be(RectangleF.Union(r1,r2));
        }
    }
}