using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using PolarPointF=ModelingEvolution.Drawing.PolarPoint<float>;
using VectorF = ModelingEvolution.Drawing.Vector<float>;
using PointF = ModelingEvolution.Drawing.Point<float>;
using CylindricalPointF = ModelingEvolution.Drawing.CylindricalPoint<float>;

namespace ModelingEvolution.Drawing.Tests
{
    public class PolarPointTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public PolarPointTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        class LidarDataSource
        {
            public IEnumerable<PolarPointF> Read()
            {
                yield return new PolarPointF(Degree<float>.Create(30), 10);
                yield return new PolarPointF(Degree<float>.Create(45), 12);
                yield return new PolarPointF(Degree<float>.Create(60), 18);
            }
        }

        [Fact]
        public void CalculatePointFromLidar()
        {
            LidarDataSource src = new LidarDataSource();

            IEnumerable<PolarPointF> points = src.Read();

            Vector3 v = new Vector3(0,0,5f);
            TimeSpanF dt = TimeSpan.FromSeconds(1f/10); // 10Hz
            Vector3 start = Vector3.Zero;

            start += v * dt;
            foreach (var i in points)
            {
                CylindricalPointF cp = i;
                var xyPoint = start + cp;
                testOutputHelper.WriteLine(xyPoint.ToString());
            }
        }
    }
}
