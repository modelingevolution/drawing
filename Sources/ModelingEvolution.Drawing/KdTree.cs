// Adapted from https://github.com/codeandcats/KdTree (MIT License)
// Original by codeandcats — adapted for 2D Point<T> with INumber<T> generic math.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A 2D KD-tree for fast nearest-neighbor queries on <see cref="Point{T}"/>.
/// </summary>
public class KdTree<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private KdTreeNode? _root;

    /// <summary>Number of points in the tree.</summary>
    public int Count { get; private set; }

    /// <summary>
    /// Builds a balanced KD-tree from the given points.
    /// </summary>
    public static KdTree<T> Build(ReadOnlySpan<Point<T>> points)
    {
        var tree = new KdTree<T>();
        if (points.Length == 0) return tree;

        var nodes = new (Point<T> pt, int idx)[points.Length];
        for (int i = 0; i < points.Length; i++)
            nodes[i] = (points[i], i);

        tree._root = BuildBalanced(nodes, 0, points.Length - 1, 0);
        tree.Count = points.Length;
        return tree;
    }

    /// <summary>
    /// Adds a single point to the tree. For bulk insertion prefer <see cref="Build"/>.
    /// </summary>
    public void Add(Point<T> point, int index = 0)
    {
        var node = new KdTreeNode(point, index);
        if (_root == null)
        {
            _root = node;
            Count++;
            return;
        }

        var current = _root;
        int depth = 0;
        while (true)
        {
            int dim = depth & 1; // 0 = X, 1 = Y
            var coord = dim == 0 ? point.X : point.Y;
            var nodeCoord = dim == 0 ? current.Point.X : current.Point.Y;
            bool goLeft = coord < nodeCoord;

            if (goLeft)
            {
                if (current.Left == null) { current.Left = node; Count++; return; }
                current = current.Left;
            }
            else
            {
                if (current.Right == null) { current.Right = node; Count++; return; }
                current = current.Right;
            }
            depth++;
        }
    }

    /// <summary>
    /// Finds the nearest point in the tree to the query point.
    /// Returns the point and its original index from the build array.
    /// </summary>
    public (Point<T> Point, int Index, T DistanceSquared) NearestNeighbour(Point<T> query)
    {
        if (_root == null) throw new InvalidOperationException("Tree is empty.");

        var best = new BestNode(_root, DistSq(query, _root.Point));
        SearchNearest(_root, query, 0, ref best);
        return (best.Node.Point, best.Node.Index, best.DistSq);
    }

    /// <summary>
    /// Finds the k nearest neighbours to the query point, ordered by distance (closest first).
    /// </summary>
    public ReadOnlyMemory<(Point<T> Point, int Index, T DistanceSquared)> GetNearestNeighbours(Point<T> query, int k)
    {
        if (_root == null || k <= 0) return ReadOnlyMemory<(Point<T>, int, T)>.Empty;
        if (k > Count) k = Count;

        var heap = new BoundedMaxHeap(k);
        SearchKNearest(_root, query, 0, heap);

        var results = Alloc.Memory<(Point<T>, int, T)>(heap.Count);
        var span = results.Span;
        for (int i = heap.Count - 1; i >= 0; i--)
        {
            var (node, distSq) = heap.RemoveMax();
            span[i] = (node.Point, node.Index, distSq);
        }
        return results;
    }

    // ── Internal types ──

    private sealed class KdTreeNode
    {
        public readonly Point<T> Point;
        public readonly int Index;
        public KdTreeNode? Left;
        public KdTreeNode? Right;

        public KdTreeNode(Point<T> point, int index)
        {
            Point = point;
            Index = index;
        }
    }

    private struct BestNode
    {
        public KdTreeNode Node;
        public T DistSq;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BestNode(KdTreeNode node, T distSq) { Node = node; DistSq = distSq; }
    }

    // ── Build ──

    private static KdTreeNode BuildBalanced((Point<T> pt, int idx)[] nodes, int from, int to, int depth)
    {
        if (from == to)
            return new KdTreeNode(nodes[from].pt, nodes[from].idx);

        int dim = depth & 1;
        SortRange(nodes, from, to, dim);

        int mid = from + (to - from) / 2;
        var node = new KdTreeNode(nodes[mid].pt, nodes[mid].idx);

        if (from < mid)
            node.Left = BuildBalanced(nodes, from, mid - 1, depth + 1);
        if (mid < to)
            node.Right = BuildBalanced(nodes, mid + 1, to, depth + 1);

        return node;
    }

    private static void SortRange((Point<T> pt, int idx)[] arr, int from, int to, int dim)
    {
        var count = to - from + 1;
        if (dim == 0)
            Array.Sort(arr, from, count, Comparer<(Point<T> pt, int idx)>.Create(
                static (a, b) =>
                {
                    if (a.pt.X < b.pt.X) return -1;
                    if (a.pt.X > b.pt.X) return 1;
                    return a.idx.CompareTo(b.idx);
                }));
        else
            Array.Sort(arr, from, count, Comparer<(Point<T> pt, int idx)>.Create(
                static (a, b) =>
                {
                    if (a.pt.Y < b.pt.Y) return -1;
                    if (a.pt.Y > b.pt.Y) return 1;
                    return a.idx.CompareTo(b.idx);
                }));
    }

    // ── Nearest-1 search ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SearchNearest(KdTreeNode? node, Point<T> query, int depth, ref BestNode best)
    {
        if (node == null) return;

        var d = DistSq(query, node.Point);
        if (d < best.DistSq)
            best = new BestNode(node, d);

        int dim = depth & 1;
        var diff = dim == 0 ? query.X - node.Point.X : query.Y - node.Point.Y;
        var diffSq = diff * diff;

        bool goLeft = T.IsNegative(diff);
        var near = goLeft ? node.Left : node.Right;
        var far = goLeft ? node.Right : node.Left;

        SearchNearest(near, query, depth + 1, ref best);

        if (diffSq < best.DistSq)
            SearchNearest(far, query, depth + 1, ref best);
    }

    // ── K-nearest search ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SearchKNearest(KdTreeNode? node, Point<T> query, int depth, BoundedMaxHeap heap)
    {
        if (node == null) return;

        var d = DistSq(query, node.Point);
        heap.TryAdd(node, d);

        int dim = depth & 1;
        var diff = dim == 0 ? query.X - node.Point.X : query.Y - node.Point.Y;
        var diffSq = diff * diff;

        bool goLeft = T.IsNegative(diff);
        var near = goLeft ? node.Left : node.Right;
        var far = goLeft ? node.Right : node.Left;

        SearchKNearest(near, query, depth + 1, heap);

        if (!heap.IsFull || diffSq < heap.MaxDist)
            SearchKNearest(far, query, depth + 1, heap);
    }

    // ── Bounded max-heap for K-nearest ──

    private sealed class BoundedMaxHeap
    {
        private readonly (KdTreeNode node, T distSq)[] _items;
        private readonly int _capacity;
        public int Count { get; private set; }

        public T MaxDist
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count > 0 ? _items[0].distSq : T.MaxValue;
        }

        public bool IsFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count >= _capacity;
        }

        public BoundedMaxHeap(int capacity)
        {
            _capacity = capacity;
            _items = new (KdTreeNode, T)[capacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryAdd(KdTreeNode node, T distSq)
        {
            if (Count < _capacity)
            {
                _items[Count] = (node, distSq);
                Count++;
                SiftUp(Count - 1);
            }
            else if (distSq < _items[0].distSq)
            {
                _items[0] = (node, distSq);
                SiftDown(0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (KdTreeNode node, T distSq) RemoveMax()
        {
            var max = _items[0];
            Count--;
            _items[0] = _items[Count];
            SiftDown(0);
            return max;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_items[i].distSq > _items[parent].distSq)
                {
                    (_items[i], _items[parent]) = (_items[parent], _items[i]);
                    i = parent;
                }
                else break;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int largest = i;
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                if (left < Count && _items[left].distSq > _items[largest].distSq)
                    largest = left;
                if (right < Count && _items[right].distSq > _items[largest].distSq)
                    largest = right;
                if (largest == i) break;
                (_items[i], _items[largest]) = (_items[largest], _items[i]);
                i = largest;
            }
        }
    }

    // ── Distance ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T DistSq(Point<T> a, Point<T> b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
