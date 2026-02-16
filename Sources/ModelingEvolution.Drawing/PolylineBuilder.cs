using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Mutable builder for constructing <see cref="Polyline{T}"/> by appending points or spans.
/// </summary>
public sealed class PolylineBuilder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private Point<T>[] _buffer;
    private int _count;

    public PolylineBuilder(int initialCapacity = 64)
    {
        _buffer = new Point<T>[initialCapacity];
        _count = 0;
    }

    /// <summary>Gets the number of points added so far.</summary>
    public int Count => _count;

    /// <summary>Appends a single point.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PolylineBuilder<T> Add(Point<T> point)
    {
        EnsureCapacity(1);
        _buffer[_count++] = point;
        return this;
    }

    /// <summary>Appends a span of points.</summary>
    public PolylineBuilder<T> AddRange(ReadOnlySpan<Point<T>> points)
    {
        EnsureCapacity(points.Length);
        points.CopyTo(_buffer.AsSpan(_count));
        _count += points.Length;
        return this;
    }

    /// <summary>Appends points from a ReadOnlyMemory.</summary>
    public PolylineBuilder<T> AddRange(ReadOnlyMemory<Point<T>> points)
        => AddRange(points.Span);

    /// <summary>Appends all points of an existing polyline.</summary>
    public PolylineBuilder<T> AddPolyline(in Polyline<T> polyline)
        => AddRange(polyline.AsSpan());

    /// <summary>
    /// Builds an immutable <see cref="Polyline{T}"/> from the accumulated points.
    /// Uses <see cref="Alloc"/> to respect active <see cref="AllocationScope"/>.
    /// </summary>
    public Polyline<T> Build()
    {
        if (_count == 0) return new Polyline<T>();
        var mem = Alloc.Memory<Point<T>>(_count);
        _buffer.AsSpan(0, _count).CopyTo(mem.Span);
        return new Polyline<T>(mem);
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
