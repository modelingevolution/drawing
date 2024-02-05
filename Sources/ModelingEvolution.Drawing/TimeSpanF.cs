namespace ModelingEvolution.Drawing;

public readonly record struct TimeSpanF
{
    private readonly float _seconds;
    public static readonly TimeSpanF Zero = new TimeSpanF(0f);
    public TimeSpanF(float seconds)
    {
        this._seconds = seconds;
    }

    public static TimeSpanF operator +(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds + sz._seconds);
    public static TimeSpanF operator -(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds - sz._seconds);
    public static implicit operator TimeSpanF(TimeSpan span) => new TimeSpanF((float)span.TotalSeconds);
    public static implicit operator float(TimeSpanF t) => t._seconds;

}