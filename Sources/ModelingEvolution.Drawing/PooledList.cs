using System.Buffers;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A lightweight list backed by <see cref="ArrayPool{T}"/>.
/// Rents from the pool on creation, returns old buffer on growth,
/// returns final buffer on <see cref="Dispose"/>.
/// </summary>
internal struct PooledList<T> : IDisposable
{
    private T[] _buffer;
    private int _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledList(int capacity)
    {
        _buffer = capacity > 0 ? ArrayPool<T>.Shared.Rent(capacity) : [];
        _count = 0;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (_count == _buffer.Length)
            Grow();
        _buffer[_count++] = item;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow()
    {
        var newCap = _buffer.Length == 0 ? 4 : _buffer.Length * 2;
        var newBuf = ArrayPool<T>.Shared.Rent(newCap);
        if (_count > 0)
            _buffer.AsSpan(0, _count).CopyTo(newBuf);
        ArrayPool<T>.Shared.Return(_buffer);
        _buffer = newBuf;
    }

    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => _buffer.AsSpan(0, _count);

    /// <summary>
    /// Copies populated items into an <see cref="Alloc.Memory{T}"/> buffer
    /// (scope-tracked if AllocationScope is active) and returns the internal
    /// buffer to the pool. The list is empty after this call.
    /// </summary>
    public ReadOnlyMemory<T> ToReadOnlyMemory()
    {
        if (_count == 0)
        {
            Dispose();
            return ReadOnlyMemory<T>.Empty;
        }
        var result = Alloc.Memory<T>(_count);
        _buffer.AsSpan(0, _count).CopyTo(result.Span);
        Dispose();
        return result;
    }

    public void Clear() => _count = 0;

    public readonly bool Contains(T item)
    {
        var span = _buffer.AsSpan(0, _count);
        for (int i = 0; i < span.Length; i++)
            if (EqualityComparer<T>.Default.Equals(span[i], item))
                return true;
        return false;
    }

    public void RemoveAt(int index)
    {
        if (index < _count - 1)
            Array.Copy(_buffer, index + 1, _buffer, index, _count - index - 1);
        _count--;
    }

    public void Dispose()
    {
        if (_buffer is { Length: > 0 })
        {
            ArrayPool<T>.Shared.Return(_buffer);
            _buffer = [];
        }
        _count = 0;
    }
}
