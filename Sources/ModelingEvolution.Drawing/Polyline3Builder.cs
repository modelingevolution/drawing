using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Mutable builder for constructing <see cref="Polyline3{T}"/> by appending points or spans.
/// </summary>
public sealed class Polyline3Builder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private Point3<T>[] _buffer;
    private int _count;

    public Polyline3Builder(int initialCapacity = 64)
    {
        _buffer = new Point3<T>[initialCapacity];
        _count = 0;
    }

    /// <summary>Gets the number of points added so far.</summary>
    public int Count => _count;

    /// <summary>Appends a single point.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Polyline3Builder<T> Add(Point3<T> point)
    {
        EnsureCapacity(1);
        _buffer[_count++] = point;
        return this;
    }

    /// <summary>Appends a span of points.</summary>
    public Polyline3Builder<T> AddRange(ReadOnlySpan<Point3<T>> points)
    {
        EnsureCapacity(points.Length);
        points.CopyTo(_buffer.AsSpan(_count));
        _count += points.Length;
        return this;
    }

    /// <summary>Appends points from a ReadOnlyMemory.</summary>
    public Polyline3Builder<T> AddRange(ReadOnlyMemory<Point3<T>> points)
        => AddRange(points.Span);

    /// <summary>Appends all points of an existing polyline.</summary>
    public Polyline3Builder<T> AddPolyline(in Polyline3<T> polyline)
        => AddRange(polyline.AsSpan());

    /// <summary>
    /// Builds an immutable <see cref="Polyline3{T}"/> from the accumulated points.
    /// Uses <see cref="Alloc"/> to respect active <see cref="AllocationScope"/>.
    /// </summary>
    public Polyline3<T> Build()
    {
        if (_count == 0) return new Polyline3<T>();
        var mem = Alloc.Memory<Point3<T>>(_count);
        _buffer.AsSpan(0, _count).CopyTo(mem.Span);
        return new Polyline3<T>(mem);
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
