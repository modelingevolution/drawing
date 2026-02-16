using System.Buffers;
using System.Runtime.InteropServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Arena-style pooled allocation scope. When active on the current thread, all library
/// allocations use <see cref="MemoryPool{T}.Shared"/> instead of heap allocation.
/// Disposing the scope returns all tracked memory to the pool at once.
/// <para>
/// Usage:
/// <code>
/// using var scope = AllocationScope.Begin();
/// var polygon = somePolygon * new Size&lt;float&gt;(2, 2);  // pooled
/// var skeleton = polygon.Skeleton();                    // pooled
/// var lease = scope.Persist(ref skeleton);              // detach from scope
/// // scope.Dispose() returns everything except skeleton's memory
/// // lease.Dispose() returns skeleton's memory when you're done
/// </code>
/// </para>
/// </summary>
public class AllocationScope : IDisposable
{
    [ThreadStatic] private static AllocationScope? _current;

    private AllocationScope? _previous;
    private readonly List<IDisposable> _tracked = new();

    /// <summary>
    /// Gets the currently active scope on this thread, or null if none.
    /// </summary>
    public static AllocationScope? Current => _current;

    private AllocationScope() { }

    /// <summary>
    /// Begins a new allocation scope. Scopes nest — the previous scope is restored on Dispose.
    /// </summary>
    public static AllocationScope Begin()
    {
        var scope = new AllocationScope { _previous = _current };
        _current = scope;
        return scope;
    }

    /// <summary>
    /// Rents from <see cref="MemoryPool{T}.Shared"/>, tracks the owner, and returns
    /// a <see cref="Memory{T}"/> sliced to the exact requested length.
    /// </summary>
    internal Memory<T> Rent<T>(int length)
    {
        var owner = MemoryPool<T>.Shared.Rent(length);
        _tracked.Add(owner);
        return owner.Memory.Slice(0, length);
    }

    /// <summary>
    /// Removes a tracked owner so it won't be disposed when this scope ends.
    /// Used by <see cref="IPoolable{TSelf,TLease}.DetachFrom"/> during Persist.
    /// </summary>
    internal void Untrack(IDisposable owner)
    {
        _tracked.Remove(owner);
    }

    /// <summary>
    /// Finds and untracks the <see cref="IMemoryOwner{T}"/> backing the given memory,
    /// returning it for transfer into a <see cref="Lease{T}"/>.
    /// </summary>
    internal IMemoryOwner<T>? UntrackMemory<T>(Memory<T> memory)
    {
        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<T>)memory, out var segment) || segment.Array == null)
            return null;

        for (int i = 0; i < _tracked.Count; i++)
        {
            if (_tracked[i] is IMemoryOwner<T> owner)
            {
                if (MemoryMarshal.TryGetArray((ReadOnlyMemory<T>)owner.Memory, out var ownerSegment)
                    && ownerSegment.Array == segment.Array)
                {
                    _tracked.RemoveAt(i);
                    return owner;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Detaches a poolable value's memory from this scope and returns a Lease
    /// that the caller must dispose to return the memory to the pool.
    /// </summary>
    public TLease Persist<TValue, TLease>(ref TValue value)
        where TValue : struct, IPoolable<TValue, TLease>
        where TLease : struct, IDisposable
    {
        return value.DetachFrom(this);
    }

    /// <summary>
    /// Disposes all tracked memory owners and restores the previous scope.
    /// </summary>
    public void Dispose()
    {
        _current = _previous;
        foreach (var owner in _tracked)
            owner.Dispose();
        _tracked.Clear();
    }
}
