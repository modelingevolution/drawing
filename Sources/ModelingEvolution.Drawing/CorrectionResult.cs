using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Result of computing corrections between a ComplexCurve template and a measured Polyline target.
/// </summary>
public readonly record struct CorrectionResult<T>(
    AlignmentResult<T> Alignment,
    ComplexCurve<T> CorrectedCurve,
    T TotalResidualError)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>;

/// <summary>
/// Projection result: the closest point on a polyline, which edge it's on, and the distance.
/// </summary>
internal readonly record struct PolylineProjection<T>(
    Point<T> ClosestPoint,
    int EdgeIndex,
    T EdgeParam,
    T Distance)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>;

/// <summary>
/// Static helpers for curve correction: projection onto polyline, sub-section extraction,
/// and least-squares Bezier fitting.
/// </summary>
internal static class CurveCorrection
{
    /// <summary>
    /// Projects a point onto the closest edge of a target polyline, using a KD-tree
    /// for fast nearest-vertex lookup followed by edge refinement.
    /// </summary>
    public static PolylineProjection<T> ProjectOntoPolyline<T>(
        Point<T> point,
        ReadOnlySpan<Point<T>> targetPoints,
        KdTree<T> targetTree)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
                  IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var (_, nearestIdx, _) = targetTree.NearestNeighbour(point);
        int n = targetPoints.Length;

        var bestDist = T.MaxValue;
        var bestPoint = targetPoints[nearestIdx];
        int bestEdge = 0;
        var bestParam = T.Zero;

        // Check edges around the nearest vertex
        int lo = int.Max(0, nearestIdx - 2);
        int hi = int.Min(n - 2, nearestIdx + 1);

        for (int e = lo; e <= hi; e++)
        {
            var seg = new Segment<T>(targetPoints[e], targetPoints[e + 1]);
            var proj = seg.ProjectPoint(point);
            var dist = point.DistanceTo(proj);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = proj;
                bestEdge = e;
                // Compute parameter along edge
                var d = seg.Direction;
                var lenSq = d.LengthSquared;
                bestParam = lenSq < T.CreateTruncating(1e-18)
                    ? T.Zero
                    : T.Max(T.Zero, T.Min(T.One, ((proj - seg.Start).X * d.X + (proj - seg.Start).Y * d.Y) / lenSq));
            }
        }

        return new PolylineProjection<T>(bestPoint, bestEdge, bestParam, bestDist);
    }

    /// <summary>
    /// Extracts the sub-section of target points between two projected positions.
    /// Returns points ordered from p0 to p3 (with intermediate target vertices).
    /// </summary>
    public static ReadOnlyMemory<Point<T>> ExtractTargetSubSection<T>(
            Point<T> p0, int edge0, T param0,
            Point<T> p3, int edge3, T param3,
            ReadOnlySpan<Point<T>> targetPoints)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
                  IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        // Handle direction: ensure we walk forward
        bool reversed = edge0 > edge3 || (edge0 == edge3 && param0 > param3);
        if (reversed)
        {
            (p0, p3) = (p3, p0);
            (edge0, edge3) = (edge3, edge0);
            (param0, param3) = (param3, param0);
        }

        // Collect points: p0, intermediate target vertices, p3
        var points = new List<Point<T>> { p0 };

        for (int e = edge0 + 1; e <= edge3; e++)
            points.Add(targetPoints[e]);

        points.Add(p3);

        // If reversed, flip order back so the Bezier direction matches the original
        if (reversed)
            points.Reverse();

        int count = points.Count;
        var ptsMem = Alloc.Memory<Point<T>>(count);
        var span = ptsMem.Span;
        for (int i = 0; i < count; i++)
            span[i] = points[i];

        return ptsMem;
    }

    /// <summary>
    /// Computes the sum of squared distances between a Bezier curve and data points
    /// using chord-length parameterization.
    /// </summary>
    public static T ComputeBezierResidual<T>(
        BezierCurve<T> curve,
        ReadOnlySpan<Point<T>> dataPoints)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
                  IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        int n = dataPoints.Length;
        if (n < 2)
            return T.Zero;

        // Compute chord-length parameters
        var cumLen = T.Zero;
        var arcLen = new T[n];
        arcLen[0] = T.Zero;
        for (int i = 1; i < n; i++)
        {
            cumLen += dataPoints[i - 1].DistanceTo(dataPoints[i]);
            arcLen[i] = cumLen;
        }

        var residual = T.Zero;
        var eps = T.CreateTruncating(1e-12);
        for (int i = 0; i < n; i++)
        {
            var t = cumLen > eps ? arcLen[i] / cumLen : T.CreateTruncating(i) / T.CreateTruncating(n - 1);
            var eval = curve.Evaluate(t);
            var dx = eval.X - dataPoints[i].X;
            var dy = eval.Y - dataPoints[i].Y;
            residual += dx * dx + dy * dy;
        }
        return residual;
    }
}
