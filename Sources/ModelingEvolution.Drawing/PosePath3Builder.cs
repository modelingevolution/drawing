using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Mutable builder for constructing <see cref="PosePath3{T}"/> by anchoring at a start pose
/// and appending poses by relative cartesian + Euler-angle deltas.
/// </summary>
/// <remarks>
/// Translation deltas are applied in the world frame (additive).
/// Rotation deltas are applied component-wise in Euler degrees (additive). This is approximate
/// for non-trivial sequences of rotations — for straight-seam path generation with small per-segment
/// angle changes the approximation is adequate. For exact composition use quaternion multiplication
/// directly.
/// </remarks>
public sealed class PosePath3Builder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private Pose3<T>[] _buffer;
    private int _count;

    public PosePath3Builder(int initialCapacity = 16)
    {
        // Floor at 1 so doubling-grow always makes progress in EnsureCapacity.
        _buffer = new Pose3<T>[Math.Max(initialCapacity, 1)];
        _count = 0;
    }

    /// <summary>Number of poses added so far.</summary>
    public int Count => _count;

    /// <summary>Anchors the path at the given absolute pose. Must be called exactly once before any <see cref="Delta"/>.</summary>
    /// <exception cref="InvalidOperationException">When the path is already anchored.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PosePath3Builder<T> StartAt(Pose3<T> pose)
    {
        if (_count > 0)
            throw new InvalidOperationException("StartAt has already been called.");
        EnsureCapacity(1);
        _buffer[_count++] = pose;
        return this;
    }

    /// <summary>
    /// Appends a pose by relative cartesian translation and Euler-angle rotation delta from the previous pose.
    /// </summary>
    /// <param name="translation">World-frame cartesian offset from the previous pose's position.</param>
    /// <param name="rotation">Euler-angle delta (degrees) added component-wise to the previous pose's rotation.</param>
    /// <exception cref="InvalidOperationException">When <see cref="StartAt"/> has not been called.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PosePath3Builder<T> Delta(Vector3<T> translation, Rotation3<T> rotation)
    {
        if (_count == 0)
            throw new InvalidOperationException("StartAt must be called before Delta.");
        EnsureCapacity(1);
        var last = _buffer[_count - 1];
        var newPos = last.Position + translation;
        var newRot = new Rotation3<T>(
            last.Rotation.Rx + rotation.Rx,
            last.Rotation.Ry + rotation.Ry,
            last.Rotation.Rz + rotation.Rz);
        _buffer[_count++] = new Pose3<T>(newPos, newRot);
        return this;
    }

    /// <summary>Builds an immutable <see cref="PosePath3{T}"/> from the accumulated poses.</summary>
    public PosePath3<T> Build()
    {
        if (_count == 0) return new PosePath3<T>();
        var arr = new Pose3<T>[_count];
        Array.Copy(_buffer, arr, _count);
        return new PosePath3<T>(arr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int additional)
    {
        var required = _count + additional;
        if (required <= _buffer.Length) return;
        var newSize = _buffer.Length;
        while (newSize < required) newSize *= 2;
        Array.Resize(ref _buffer, newSize);
    }
}
