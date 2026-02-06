using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Component-wise moving average over a sliding window of points.
/// Uses circular buffer and running sums for O(1) add and average operations.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public struct MovingAverage<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Point<T>[] _buffer;
    private readonly int _capacity;
    private int _index;
    private int _count;
    private T _sumX;
    private T _sumY;

    /// <summary>
    /// Creates a moving average calculator with specified window size.
    /// </summary>
    /// <param name="windowSize">Number of points to average (must be > 0)</param>
    public MovingAverage(int windowSize)
    {
        if (windowSize <= 0)
            throw new ArgumentException("Window size must be greater than zero", nameof(windowSize));

        _capacity = windowSize;
        _buffer = new Point<T>[windowSize];
        _index = 0;
        _count = 0;
        _sumX = T.Zero;
        _sumY = T.Zero;
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
    /// Current component-wise average of all points in the buffer.
    /// Returns Point.Zero if no points have been added.
    /// </summary>
    public readonly Point<T> Average
    {
        get
        {
            if (_count == 0)
                return Point<T>.Zero;

            var n = T.CreateChecked(_count);
            return new Point<T>(_sumX / n, _sumY / n);
        }
    }

    /// <summary>
    /// Adds a new point to the moving average.
    /// If buffer is full, replaces the oldest point.
    /// </summary>
    public void Add(Point<T> value)
    {
        if (_count == _capacity)
        {
            _sumX -= _buffer[_index].X;
            _sumY -= _buffer[_index].Y;
        }
        else
        {
            _count++;
        }

        _buffer[_index] = value;
        _sumX += value.X;
        _sumY += value.Y;

        _index = (_index + 1) % _capacity;
    }

    /// <summary>
    /// Resets the moving average to empty state.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_buffer);
        _index = 0;
        _count = 0;
        _sumX = T.Zero;
        _sumY = T.Zero;
    }

    /// <summary>
    /// Adds a point using += operator.
    /// </summary>
    public static MovingAverage<T> operator +(MovingAverage<T> avg, Point<T> value)
    {
        avg.Add(value);
        return avg;
    }

    /// <summary>
    /// Implicit conversion to Point returns the current average.
    /// </summary>
    public static implicit operator Point<T>(MovingAverage<T> avg) => avg.Average;

    /// <summary>
    /// Returns the current average as a string.
    /// </summary>
    public override readonly string ToString() => Average.ToString();
}
