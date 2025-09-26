namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a time interval using single-precision floating-point seconds.
/// </summary>
public readonly record struct TimeSpanF
{
    private readonly float _seconds;
    
    /// <summary>
    /// Represents a zero time span.
    /// </summary>
    public static readonly TimeSpanF Zero = new TimeSpanF(0f);
    /// <summary>
    /// Initializes a new instance with the specified number of seconds.
    /// </summary>
    /// <param name="seconds">The duration in seconds.</param>
    public TimeSpanF(float seconds)
    {
        this._seconds = seconds;
    }

    /// <summary>
    /// Adds two TimeSpanF values.
    /// </summary>
    /// <param name="pt">The first time span.</param>
    /// <param name="sz">The second time span.</param>
    /// <returns>A new TimeSpanF representing the sum of the two time spans.</returns>
    public static TimeSpanF operator +(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds + sz._seconds);
    /// <summary>
    /// Subtracts one TimeSpanF from another.
    /// </summary>
    /// <param name="pt">The time span to subtract from.</param>
    /// <param name="sz">The time span to subtract.</param>
    /// <returns>A new TimeSpanF representing the difference between the two time spans.</returns>
    public static TimeSpanF operator -(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds - sz._seconds);
    /// <summary>
    /// Implicitly converts a TimeSpan to a TimeSpanF.
    /// </summary>
    /// <param name="span">The TimeSpan to convert.</param>
    /// <returns>A TimeSpanF representing the same duration.</returns>
    public static implicit operator TimeSpanF(TimeSpan span) => new TimeSpanF((float)span.TotalSeconds);
    /// <summary>
    /// Implicitly converts a TimeSpanF to its floating-point seconds value.
    /// </summary>
    /// <param name="t">The TimeSpanF to convert.</param>
    /// <returns>The duration in seconds as a float.</returns>
    public static implicit operator float(TimeSpanF t) => t._seconds;

}