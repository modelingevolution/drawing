using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Mutable builder for constructing <see cref="PosePath3{T}"/> by anchoring at a start pose
/// and appending poses by relative steps.
/// </summary>
/// <remarks>
/// A step is a <see cref="Frame3{T}"/> expressed in the previous pose: the next pose is <c>previous · step</c>,
/// composed exactly (epic-129 design, rule 2). The stored poses are wire <see cref="Pose3{T}"/> converted with
/// lengthScale 1, so a step's lengths are in the path's unit.
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

    /// <summary>Anchors the path at the given absolute pose. Must be called exactly once before any <see cref="Step"/>.</summary>
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
    /// Appends the pose <c>previous · step</c>: <paramref name="step"/> is expressed in the previous pose's frame.
    /// </summary>
    /// <exception cref="InvalidOperationException">When <see cref="StartAt"/> has not been called.</exception>
    public PosePath3Builder<T> Step(in Frame3<T> step)
    {
        if (_count == 0)
            throw new InvalidOperationException("StartAt must be called before Step.");
        EnsureCapacity(1);
        var last = Frame3<T>.FromPose3(_buffer[_count - 1], T.One);
        _buffer[_count++] = (last * step).ToPose3(T.One);
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
