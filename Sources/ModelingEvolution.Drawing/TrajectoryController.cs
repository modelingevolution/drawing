using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Event arguments for pose changes on a <see cref="TrajectoryController{T}"/>.
/// </summary>
public class PoseEventArgs<T> : EventArgs
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>The current interpolated pose.</summary>
    public Pose3<T> Pose { get; }

    /// <summary>The current trajectory time.</summary>
    public T Time { get; }

    /// <summary>The normalized progress (0..1).</summary>
    public T Progress { get; }

    public PoseEventArgs(Pose3<T> pose, T time, T progress)
    {
        Pose = pose;
        Time = time;
        Progress = progress;
    }
}

/// <summary>
/// Runtime controller that executes a <see cref="Trajectory3{T}"/> in real time.
/// Uses a <see cref="Stopwatch"/> as its clock source. All control methods are fluent (return <c>this</c>).
/// </summary>
public sealed class TrajectoryController<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private enum State { Stopped, Running, Paused }

    private readonly Trajectory3<T> _trajectory;
    private readonly Stopwatch _stopwatch = new();
    private readonly T _startTime;
    private readonly T _endTime;
    private readonly T _duration;

    private State _state = State.Stopped;
    private T _baseTime;

    /// <summary>
    /// Fired on state changes (Start, Stop, Pause, Resume, Seek) with the current pose.
    /// </summary>
    public event EventHandler<PoseEventArgs<T>>? PoseChanged;

    /// <summary>
    /// Creates a new controller for the given trajectory.
    /// </summary>
    public TrajectoryController(Trajectory3<T> trajectory)
    {
        _trajectory = trajectory;
        if (trajectory.Count > 0)
        {
            _startTime = trajectory.Start.Time;
            _endTime = trajectory.End.Time;
            _duration = _endTime - _startTime;
        }
        else
        {
            _startTime = T.Zero;
            _endTime = T.Zero;
            _duration = T.Zero;
        }
        _baseTime = _startTime;
    }

    /// <summary>
    /// Gets the trajectory being controlled.
    /// </summary>
    public Trajectory3<T> Trajectory => _trajectory;

    /// <summary>
    /// Gets whether the controller is currently running (playing).
    /// </summary>
    public bool IsRunning => _state == State.Running;

    /// <summary>
    /// Gets whether the controller has reached the end of the trajectory.
    /// </summary>
    public bool IsCompleted => _trajectory.Count > 0 && CurrentTime >= _endTime;

    /// <summary>
    /// Gets the wall-clock time elapsed since Start was called (accounting for pauses).
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the current time within the trajectory.
    /// </summary>
    public T CurrentTime
    {
        get
        {
            var t = _baseTime + T.CreateTruncating(_stopwatch.Elapsed.TotalSeconds);
            return t > _endTime ? _endTime : t;
        }
    }

    /// <summary>
    /// Gets the normalized progress (0..1) through the trajectory.
    /// </summary>
    public T Progress
    {
        get
        {
            if (_duration == T.Zero) return T.Zero;
            var elapsed = CurrentTime - _startTime;
            var p = elapsed / _duration;
            return p > T.One ? T.One : p < T.Zero ? T.Zero : p;
        }
    }

    /// <summary>
    /// Gets the interpolated pose at the current time.
    /// </summary>
    public Pose3<T> CurrentPose => _trajectory.AtTime(CurrentTime);

    /// <summary>
    /// Starts playback from the beginning of the trajectory.
    /// </summary>
    public TrajectoryController<T> Start()
    {
        _baseTime = _startTime;
        _stopwatch.Restart();
        _state = State.Running;
        FirePoseChanged();
        return this;
    }

    /// <summary>
    /// Pauses playback, freezing the current time.
    /// </summary>
    public TrajectoryController<T> Pause()
    {
        if (_state == State.Running)
        {
            _stopwatch.Stop();
            _state = State.Paused;
            FirePoseChanged();
        }
        return this;
    }

    /// <summary>
    /// Resumes playback from the paused position.
    /// </summary>
    public TrajectoryController<T> Resume()
    {
        if (_state == State.Paused)
        {
            _stopwatch.Start();
            _state = State.Running;
            FirePoseChanged();
        }
        return this;
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    public TrajectoryController<T> Stop()
    {
        _stopwatch.Reset();
        _baseTime = _startTime;
        _state = State.Stopped;
        FirePoseChanged();
        return this;
    }

    /// <summary>
    /// Seeks to the specified trajectory time. Maintains the current play/pause state.
    /// </summary>
    public TrajectoryController<T> Seek(T time)
    {
        _baseTime = time;
        if (_state == State.Running)
            _stopwatch.Restart();
        else
            _stopwatch.Reset();
        FirePoseChanged();
        return this;
    }

    private void FirePoseChanged()
    {
        PoseChanged?.Invoke(this, new PoseEventArgs<T>(CurrentPose, CurrentTime, Progress));
    }
}
