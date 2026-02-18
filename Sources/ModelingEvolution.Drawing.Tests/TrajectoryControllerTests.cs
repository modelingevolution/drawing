using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class TrajectoryControllerTests
{
    private const float Tol = 1e-2f;

    private static Pose3<float> P(float x, float y, float z) => new(x, y, z, 0, 0, 0);
    private static Waypoint3<float> W(float x, float y, float z, float t) => new(P(x, y, z), t);

    private static Trajectory3<float> SimpleTraj() =>
        new(W(0, 0, 0, 0), W(10, 0, 0, 2));

    // ─────────────────────────────────────────────
    // Initial state
    // ─────────────────────────────────────────────

    [Fact]
    public void InitialState_IsStopped()
    {
        var ctrl = SimpleTraj().Control();
        ctrl.IsRunning.Should().BeFalse();
        ctrl.IsCompleted.Should().BeFalse();
        ctrl.Progress.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void CurrentPose_BeforeStart_IsFirstWaypoint()
    {
        var ctrl = SimpleTraj().Control();
        ctrl.CurrentPose.X.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // Start / Stop
    // ─────────────────────────────────────────────

    [Fact]
    public void Start_SetsIsRunning()
    {
        var ctrl = SimpleTraj().Control().Start();
        ctrl.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Stop_ResetsToBeginning()
    {
        var ctrl = SimpleTraj().Control().Start();
        Thread.Sleep(50);
        ctrl.Stop();
        ctrl.IsRunning.Should().BeFalse();
        ctrl.CurrentPose.X.Should().BeApproximately(0f, Tol);
        ctrl.Progress.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // Pause / Resume
    // ─────────────────────────────────────────────

    [Fact]
    public void Pause_FreezesTime()
    {
        var ctrl = SimpleTraj().Control().Start();
        Thread.Sleep(100);
        ctrl.Pause();
        var poseAtPause = ctrl.CurrentPose;
        Thread.Sleep(100);
        var poseAfterWait = ctrl.CurrentPose;
        // Poses should be identical since time is frozen
        poseAtPause.X.Should().BeApproximately(poseAfterWait.X, Tol);
        ctrl.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Resume_ContinuesFromPaused()
    {
        var ctrl = SimpleTraj().Control().Start();
        Thread.Sleep(50);
        ctrl.Pause();
        var progressAtPause = ctrl.Progress;
        ctrl.Resume();
        ctrl.IsRunning.Should().BeTrue();
        Thread.Sleep(50);
        ctrl.Progress.Should().BeGreaterThan(progressAtPause);
    }

    // ─────────────────────────────────────────────
    // Seek
    // ─────────────────────────────────────────────

    [Fact]
    public void Seek_JumpsToTime()
    {
        var ctrl = SimpleTraj().Control();
        ctrl.Seek(1f); // midpoint: t=1 of 0..2
        ctrl.CurrentPose.X.Should().BeApproximately(5f, Tol);
        ctrl.Progress.Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void Seek_WhileRunning_MaintainsRunningState()
    {
        var ctrl = SimpleTraj().Control().Start();
        ctrl.Seek(1f);
        ctrl.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void Seek_WhilePaused_MaintainsPausedState()
    {
        var ctrl = SimpleTraj().Control().Start().Pause();
        ctrl.Seek(1f);
        ctrl.IsRunning.Should().BeFalse();
        ctrl.CurrentPose.X.Should().BeApproximately(5f, Tol);
    }

    // ─────────────────────────────────────────────
    // Completion
    // ─────────────────────────────────────────────

    [Fact]
    public void Seek_ToEnd_IsCompleted()
    {
        var ctrl = SimpleTraj().Control();
        ctrl.Seek(2f);
        ctrl.IsCompleted.Should().BeTrue();
        ctrl.CurrentPose.X.Should().BeApproximately(10f, Tol);
        ctrl.Progress.Should().BeApproximately(1f, Tol);
    }

    // ─────────────────────────────────────────────
    // Event
    // ─────────────────────────────────────────────

    [Fact]
    public void PoseChanged_FiresOnStart()
    {
        var ctrl = SimpleTraj().Control();
        PoseEventArgs<float>? received = null;
        ctrl.PoseChanged += (_, e) => received = e;
        ctrl.Start();
        received.Should().NotBeNull();
        received!.Progress.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void PoseChanged_FiresOnSeek()
    {
        var ctrl = SimpleTraj().Control();
        PoseEventArgs<float>? received = null;
        ctrl.PoseChanged += (_, e) => received = e;
        ctrl.Seek(1f);
        received.Should().NotBeNull();
        received!.Pose.X.Should().BeApproximately(5f, Tol);
        received!.Progress.Should().BeApproximately(0.5f, Tol);
    }

    [Fact]
    public void PoseChanged_FiresOnStop()
    {
        var ctrl = SimpleTraj().Control().Start();
        PoseEventArgs<float>? received = null;
        ctrl.PoseChanged += (_, e) => received = e;
        ctrl.Stop();
        received.Should().NotBeNull();
        received!.Progress.Should().BeApproximately(0f, Tol);
    }

    // ─────────────────────────────────────────────
    // Fluent API
    // ─────────────────────────────────────────────

    [Fact]
    public void FluentChaining_AllMethodsReturnThis()
    {
        var ctrl = SimpleTraj().Control();
        ctrl.Start().Should().BeSameAs(ctrl);
        ctrl.Pause().Should().BeSameAs(ctrl);
        ctrl.Resume().Should().BeSameAs(ctrl);
        ctrl.Stop().Should().BeSameAs(ctrl);
        ctrl.Seek(0f).Should().BeSameAs(ctrl);
    }

    // ─────────────────────────────────────────────
    // Control() factory method
    // ─────────────────────────────────────────────

    [Fact]
    public void Trajectory_Control_CreatesController()
    {
        var traj = SimpleTraj();
        var ctrl = traj.Control();
        ctrl.Trajectory.Should().Be(traj);
    }

    // ─────────────────────────────────────────────
    // Elapsed
    // ─────────────────────────────────────────────

    [Fact]
    public void Elapsed_IncreasesWhileRunning()
    {
        var ctrl = SimpleTraj().Control().Start();
        Thread.Sleep(50);
        ctrl.Elapsed.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Elapsed_FrozenWhenPaused()
    {
        var ctrl = SimpleTraj().Control().Start();
        Thread.Sleep(50);
        ctrl.Pause();
        var elapsed1 = ctrl.Elapsed;
        Thread.Sleep(50);
        var elapsed2 = ctrl.Elapsed;
        elapsed2.Should().Be(elapsed1);
    }
}
