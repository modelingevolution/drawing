using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Component-wise moving median over a sliding window of points.
/// X and Y medians are computed independently via maintained sorted arrays.
/// O(1) median read, O(n) add.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public struct MovingMedian<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Point<T>[] _buffer;
    private readonly T[] _sortedX;
    private readonly T[] _sortedY;
    private readonly int _capacity;
    private int _index;
    private int _count;
    private static readonly T _two = T.CreateChecked(2);

    /// <summary>
    /// Creates a moving median calculator with specified window size.
    /// </summary>
    /// <param name="windowSize">Number of points in the window (must be > 0)</param>
    public MovingMedian(int windowSize)
    {
        if (windowSize <= 0)
            throw new ArgumentException("Window size must be greater than zero", nameof(windowSize));

        _capacity = windowSize;
        _buffer = new Point<T>[windowSize];
        _sortedX = new T[windowSize];
        _sortedY = new T[windowSize];
        _index = 0;
        _count = 0;
    }

    /// <summary>
    /// Current number of points in the buffer.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// Maximum number of points that can be stored.
    /// </summary>
    public readonly int Capacity => _capacity;

    /// <summary>
    /// Current component-wise median of all points in the buffer. O(1).
    /// X and Y medians are computed independently.
    /// For even count, returns the average of the two middle values per component.
    /// Returns Point.Zero if no points have been added.
    /// </summary>
    public readonly Point<T> Median
    {
        get
        {
            if (_count == 0)
                return Point<T>.Zero;

            return new Point<T>(MedianOf(_sortedX), MedianOf(_sortedY));
        }
    }

    private readonly T MedianOf(T[] sorted)
    {
        int mid = _count / 2;
        if (_count % 2 == 1)
            return sorted[mid];
        return (sorted[mid - 1] + sorted[mid]) / _two;
    }

    /// <summary>
    /// Adds a new point to the moving median.
    /// If buffer is full, replaces the oldest point.
    /// O(n) due to sorted array maintenance, O(1) median read.
    /// </summary>
    public void Add(Point<T> value)
    {
        if (_count == _capacity)
        {
            var old = _buffer[_index];
            RemoveFromSorted(_sortedX, old.X, _count);
            RemoveFromSorted(_sortedY, old.Y, _count);
        }
        else
        {
            _count++;
        }

        InsertIntoSorted(_sortedX, value.X, _count - 1);
        InsertIntoSorted(_sortedY, value.Y, _count - 1);

        _buffer[_index] = value;
        _index = (_index + 1) % _capacity;
    }

    private static void InsertIntoSorted(T[] sorted, T value, int length)
    {
        int pos = LowerBound(sorted, value, length);
        Array.Copy(sorted, pos, sorted, pos + 1, length - pos);
        sorted[pos] = value;
    }

    private static void RemoveFromSorted(T[] sorted, T value, int length)
    {
        int pos = LowerBound(sorted, value, length);
        Array.Copy(sorted, pos + 1, sorted, pos, length - pos - 1);
    }

    private static int LowerBound(T[] sorted, T value, int length)
    {
        int lo = 0, hi = length;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (sorted[mid] < value)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    /// <summary>
    /// Resets the moving median to empty state.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_buffer);
        Array.Clear(_sortedX);
        Array.Clear(_sortedY);
        _index = 0;
        _count = 0;
    }

    /// <summary>
    /// Adds a point using += operator.
    /// </summary>
    public static MovingMedian<T> operator +(MovingMedian<T> m, Point<T> value)
    {
        m.Add(value);
        return m;
    }

    /// <summary>
    /// Implicit conversion to Point returns the current median.
    /// </summary>
    public static implicit operator Point<T>(MovingMedian<T> m) => m.Median;

    /// <summary>
    /// Returns the current median as a string.
    /// </summary>
    public override readonly string ToString() => Median.ToString();
}
