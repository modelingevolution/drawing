using System.Buffers;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A stack-allocated lifetime token holding a single <see cref="IMemoryOwner{T}"/>.
/// Disposing the lease returns the memory to the pool.
/// </summary>
public struct Lease<T> : IDisposable
{
    internal IMemoryOwner<T>? _owner;

    public void Dispose()
    {
        Interlocked.Exchange(ref _owner, null)?.Dispose();
    }
}

/// <summary>
/// A stack-allocated lifetime token holding two <see cref="IMemoryOwner{T}"/> instances.
/// Used by types with two backing arrays (e.g., Skeleton with nodes + edges).
/// </summary>
public struct Lease<T1, T2> : IDisposable
{
    internal IMemoryOwner<T1>? _o1;
    internal IMemoryOwner<T2>? _o2;

    public void Dispose()
    {
        Interlocked.Exchange(ref _o1, null)?.Dispose();
        Interlocked.Exchange(ref _o2, null)?.Dispose();
    }
}

/// <summary>
/// A stack-allocated lifetime token holding three <see cref="IMemoryOwner{T}"/> instances.
/// </summary>
public struct Lease<T1, T2, T3> : IDisposable
{
    internal IMemoryOwner<T1>? _o1;
    internal IMemoryOwner<T2>? _o2;
    internal IMemoryOwner<T3>? _o3;

    public void Dispose()
    {
        Interlocked.Exchange(ref _o1, null)?.Dispose();
        Interlocked.Exchange(ref _o2, null)?.Dispose();
        Interlocked.Exchange(ref _o3, null)?.Dispose();
    }
}
