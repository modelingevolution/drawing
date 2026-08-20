using System.Buffers;
using System.Runtime.InteropServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Arena-style pooled allocation scope. When active on the current asynchronous control flow, all
/// library allocations use <see cref="MemoryPool{T}.Shared"/> instead of heap allocation.
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
/// <para>
/// Threading contract: the active scope is ambient to the <b>execution context</b>, not to the thread.
/// It therefore survives an <c>await</c> — code resuming on another thread still allocates into the
/// same arena — and it cannot linger on a pooled thread after the flow that began it has ended.
/// Because the scope flows into work forked after <see cref="Begin"/>, several parallel children can
/// share one scope, so allocation and detachment are internally synchronised and safe to call
/// concurrently. Begin and dispose a scope on the same flow: <see cref="Dispose"/> restores the
/// previous scope for the flow that calls it, so disposing from an unrelated flow leaves the
/// beginning flow's ambient scope in place until that flow ends.
/// </para>
/// </summary>
public class AllocationScope : IDisposable
{
    // AsyncLocal, not [ThreadStatic]: the scope must follow the logical flow across awaits, and must
    // not survive on a recycled thread-pool thread once that flow is finished.
    private static readonly AsyncLocal<AllocationScope?> _current = new();

    // Guards _tracked. A lock rather than a concurrent/immutable collection because UntrackMemory is a
    // multi-step atomic (scan for the backing array, then remove at that index): two flows must never
    // both claim the same owner, which no lock-free collection can express here. Add/Remove sit behind
    // a pool rental that already synchronises internally, so the cost is not on a measurable path.
    private readonly object _gate = new();
    private readonly List<IDisposable> _tracked = new();

    private AllocationScope? _previous;
    private bool _disposed;

    /// <summary>
    /// Gets the scope active on the current execution flow, or null if none.
    /// </summary>
    public static AllocationScope? Current => _current.Value;

    private AllocationScope() { }

    /// <summary>
    /// Begins a new allocation scope. Scopes nest — the previous scope is restored on Dispose.
    /// </summary>
    public static AllocationScope Begin()
    {
        var scope = new AllocationScope { _previous = _current.Value };
        _current.Value = scope;
        return scope;
    }

    /// <summary>
    /// Rents from <see cref="MemoryPool{T}.Shared"/>, tracks the owner, and returns
    /// a <see cref="Memory{T}"/> sliced to the exact requested length.
    /// </summary>
    internal Memory<T> Rent<T>(int length)
    {
        var owner = MemoryPool<T>.Shared.Rent(length);
        lock (_gate)
            _tracked.Add(owner);
        return owner.Memory.Slice(0, length);
    }

    /// <summary>
    /// Removes a tracked owner so it won't be disposed when this scope ends.
    /// Used by <see cref="IPoolable{TSelf,TLease}.DetachFrom"/> during Persist.
    /// </summary>
    internal void Untrack(IDisposable owner)
    {
        lock (_gate)
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

        // The scan and the removal are one atomic step: a concurrent caller must not be handed the
        // same owner, which would return one buffer to the pool twice.
        lock (_gate)
        {
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
    /// Disposes all tracked memory owners and restores the previous scope. Idempotent.
    /// </summary>
    public void Dispose()
    {
        IDisposable[] owners;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;

            // Take the owners out under the gate rather than enumerating in place: a parallel child of
            // this flow may still be adding to the list as the scope is torn down.
            owners = _tracked.ToArray();
            _tracked.Clear();
        }

        _current.Value = _previous;

        // Outside the gate — owner.Dispose() is arbitrary code and must not run holding our lock.
        foreach (var owner in owners)
            owner.Dispose();
    }
}
