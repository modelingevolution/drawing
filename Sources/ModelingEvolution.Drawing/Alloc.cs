using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Allocation helper that routes through <see cref="AllocationScope"/> when one is active,
/// falling back to regular heap allocation otherwise.
/// <para>
/// PERF RULE: Never access <c>.Span</c> inside a loop — hoist it before the loop.
/// <c>Memory&lt;T&gt;.Span</c> involves a method call on each access.
/// <code>
/// // BAD: var mem = Alloc.Memory&lt;int&gt;(n); for (int i = 0; i &lt; n; i++) mem.Span[i] = i;
/// // GOOD: var mem = Alloc.Memory&lt;int&gt;(n); var span = mem.Span; for (int i = 0; i &lt; n; i++) span[i] = i;
/// </code>
/// </para>
/// </summary>
internal static class Alloc
{
    /// <summary>
    /// Allocates a <see cref="Memory{T}"/> of the specified length.
    /// When an <see cref="AllocationScope"/> is active, memory comes from <see cref="System.Buffers.MemoryPool{T}.Shared"/>
    /// and is sliced to the exact length. Otherwise, allocates a new array on the heap.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Memory<T> Memory<T>(int length)
    {
        var scope = AllocationScope.Current;
        if (scope == null) return new T[length]; // implicit cast T[] → Memory<T>
        return scope.Rent<T>(length);
    }
}
