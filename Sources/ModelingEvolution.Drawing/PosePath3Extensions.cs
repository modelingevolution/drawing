using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Extension methods for <see cref="PosePath3{T}"/>.
/// </summary>
public static class PosePath3Extensions
{
    /// <summary>
    /// Adapts a teach-time path to a new pair of endpoints by applying the similarity transform
    /// that maps (teach start → teach end) onto (adapted start → adapted end), then walking
    /// the teach-time mid-pose offsets through the adapted-start anchor.
    /// </summary>
    /// <param name="teachPath">The path captured at teach time. Must have at least 2 poses.</param>
    /// <param name="adaptedStart">The new pose of the first endpoint (replaces <c>teachPath[0]</c>).</param>
    /// <param name="adaptedEnd">The new pose of the last endpoint (replaces <c>teachPath[^1]</c>).</param>
    /// <returns>A new <see cref="PosePath3{T}"/> of the same count, with endpoints exactly equal to the adapted ones.</returns>
    /// <remarks>
    /// Mid-pose positions are transformed by the similarity matrix. Mid-pose rotations are
    /// preserved from the teach-time values (the operator's torch orientation is kept as taught).
    /// This is correct when per-part shifts are small; for parts that rotate bodily significantly,
    /// the torch orientation will not follow — a future overload may apply the rotation part of M
    /// to teach-time rotations as well.
    /// </remarks>
    /// <exception cref="ArgumentException">When <paramref name="teachPath"/> has fewer than 2 poses or the teach-time endpoints coincide.</exception>
    public static PosePath3<T> AdaptToEndpoints<T>(
        this PosePath3<T> teachPath,
        Pose3<T> adaptedStart,
        Pose3<T> adaptedEnd)
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
                  ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
    {
        if (teachPath.Count < 2)
            throw new ArgumentException("Teach path must have at least 2 poses.", nameof(teachPath));

        var teachSpan = teachPath.AsSpan();
        var teachStart = teachSpan[0];
        var teachEnd = teachSpan[^1];

        var teachSegment = teachEnd.Position - teachStart.Position;
        var runSegment = adaptedEnd.Position - adaptedStart.Position;

        if (teachSegment.LengthSquared == T.Zero)
            throw new ArgumentException(
                "Teach-time path's endpoints are at the same position; cannot derive a similarity transform.",
                nameof(teachPath));

        var M = Matrix3x3<T>.SimilarityFromVectors(teachSegment, runSegment);

        var adapted = new Pose3<T>[teachPath.Count];
        adapted[0] = adaptedStart;
        adapted[^1] = adaptedEnd;

        for (int i = 1; i < teachPath.Count - 1; i++)
        {
            var teachPose = teachSpan[i];
            var localOffset = teachPose.Position - teachStart.Position;
            var transformedOffset = M.Transform(localOffset);
            var newPos = adaptedStart.Position + transformedOffset;
            adapted[i] = new Pose3<T>(newPos, teachPose.Rotation);
        }

        return new PosePath3<T>(adapted);
    }
}
