namespace ModelingEvolution.Drawing;

/// <summary>
/// Interface for types whose backing memory can be detached from an <see cref="AllocationScope"/>
/// and transferred to a <typeparamref name="TLease"/> for manual lifetime management.
/// </summary>
public interface IPoolable<TSelf, TLease>
    where TSelf : struct
    where TLease : struct, IDisposable
{
    /// <summary>
    /// Detaches this value's backing memory from the given scope and returns a lease.
    /// The caller becomes responsible for disposing the lease to return memory to the pool.
    /// </summary>
    TLease DetachFrom(AllocationScope scope);
}
