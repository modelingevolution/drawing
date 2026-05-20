using FluentAssertions;
using ModelingEvolution.Drawing.Robotics;

namespace ModelingEvolution.Drawing.Tests.Robotics;

public class PidControllerTests
{
    private const double Dt = 0.01;          // 10 ms loop
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(Dt);

    private static PidController<double> NewPid(
        double kp = 1.0, double ki = 0.0, double kd = 0.0,
        double min = -100.0, double max = 100.0)
        => PidController<double>.Create(kp, ki, kd, min, max);

    [Fact]
    public void Constructor_RejectsInvertedLimits()
    {
        Action act = () => new PidController<double>(1, 0, 0, 1.0, 1.0, 0, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_RejectsNegativeFilterTime()
    {
        Action act = () => new PidController<double>(1, 0, 0, -1, 1, -0.001, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Compute_ZeroError_ProducesZeroOutput()
    {
        var pid = NewPid(kp: 2.0, ki: 1.0, kd: 0.5);
        var u = pid.Compute(5.0, 5.0, Step);
        u.Should().Be(0.0);
    }

    [Fact]
    public void Compute_ProportionalOnly_OutputIsKpTimesError()
    {
        var pid = NewPid(kp: 3.0);
        var u = pid.Compute(10.0, 4.0, Step);
        u.Should().BeApproximately(18.0, 1e-9);
    }

    [Fact]
    public void Compute_IntegralOnly_AccumulatesOverTime()
    {
        // Ki=1, e=2 constant, dt=10ms → after 100 ticks (1 s), integral ≈ 2.0
        var pid = NewPid(kp: 0.0, ki: 1.0);
        double u = 0;
        for (int i = 0; i < 100; i++)
            u = pid.Compute(2.0, 0.0, Step);
        u.Should().BeApproximately(2.0, 1e-9);
    }

    [Fact]
    public void DerivativeOnMeasurement_HasNoKickOnSetpointStep()
    {
        // With derivative-on-measurement, stepping the setpoint while the
        // process value sits still must NOT produce a D-term contribution.
        // Output should be purely Kp · error.
        var pid = NewPid(kp: 1.0, kd: 100.0);
        pid.Compute(0.0, 0.0, Step);          // settle prev-measurement at 0
        var u = pid.Compute(50.0, 0.0, Step); // setpoint step, measurement unchanged
        u.Should().BeApproximately(50.0, 1e-6, "D acts on measurement, not error");
    }

    [Fact]
    public void DerivativeFilter_AttenuatesHighFrequencyNoise()
    {
        // Compare unfiltered vs filtered controllers fed the same noisy
        // measurement sequence (alternating ±1). The filtered controller
        // must produce a markedly smaller peak D-term contribution.
        var unfiltered = new PidController<double>(0.0, 0.0, 1.0, -1e6, 1e6, 0.0, 0.0);
        var filtered = new PidController<double>(0.0, 0.0, 1.0, -1e6, 1e6, 0.05, 0.0); // Tf=50ms

        double peakRaw = 0, peakFilt = 0;
        for (int i = 0; i < 200; i++)
        {
            var noisy = (i % 2 == 0) ? 1.0 : -1.0;
            var uRaw = Math.Abs(unfiltered.Compute(0.0, noisy, Step));
            var uFilt = Math.Abs(filtered.Compute(0.0, noisy, Step));
            if (i > 1) // skip startup transient
            {
                peakRaw = Math.Max(peakRaw, uRaw);
                peakFilt = Math.Max(peakFilt, uFilt);
            }
        }

        // Theoretical Nyquist attenuation with Tf=50ms, dt=10ms is
        // (1−α)/(1+α) ≈ 0.09; assert with margin.
        peakFilt.Should().BeLessThan(peakRaw * 0.15,
            "the low-pass filter must heavily attenuate Nyquist-rate noise");
    }

    [Fact]
    public void Saturation_ClampsOutput()
    {
        var pid = NewPid(kp: 100.0, min: -10.0, max: 10.0);
        var u = pid.Compute(5.0, 0.0, Step); // raw P = 500
        u.Should().Be(10.0);
        pid.IsSaturated.Should().BeTrue();
    }

    [Fact]
    public void AntiWindup_PreventsIntegratorBlowup()
    {
        // Drive the controller into saturation for 5 s with a large constant
        // error, then bring the setpoint back. Without anti-windup the
        // integrator would have accumulated ki·e·t = 5·5 = 25 of headroom and
        // taken seconds to bleed off. With Kaw=1 the integrator stays bounded.
        var withAw = new PidController<double>(1.0, 5.0, 0.0, -10.0, 10.0, 0.0, 1.0);
        var noAw = new PidController<double>(1.0, 5.0, 0.0, -10.0, 10.0, 0.0, 0.0);

        for (int i = 0; i < 500; i++)
        {
            withAw.Compute(100.0, 0.0, Step);
            noAw.Compute(100.0, 0.0, Step);
        }

        withAw.IntegralSum.Should().BeLessThan(noAw.IntegralSum * 0.1,
            "back-calculation must keep the integrator near the saturation boundary");
    }

    [Fact]
    public void AntiWindup_RecoversQuicklyAfterSaturation()
    {
        // After a long saturation episode the controller must unsaturate as soon
        // as the setpoint comes back into a reachable range. Measured by the
        // number of ticks until IsSaturated turns false.
        var pid = new PidController<double>(1.0, 5.0, 0.0, -10.0, 10.0, 0.0, 1.0);
        for (int i = 0; i < 500; i++)
            pid.Compute(100.0, 0.0, Step);

        int ticksToUnsaturate = 0;
        while (pid.IsSaturated && ticksToUnsaturate < 50)
        {
            pid.Compute(0.0, 0.0, Step);
            ticksToUnsaturate++;
        }

        pid.IsSaturated.Should().BeFalse();
        ticksToUnsaturate.Should().BeLessThan(5,
            "anti-windup must let the controller exit saturation within a few ticks");
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var pid = NewPid(kp: 1.0, ki: 1.0, kd: 1.0);
        for (int i = 0; i < 100; i++)
            pid.Compute(10.0, 0.0, Step);

        pid.IntegralSum.Should().NotBe(0.0);
        pid.Reset();

        pid.IntegralSum.Should().Be(0.0);
        pid.FilteredDerivative.Should().Be(0.0);
        pid.LastError.Should().Be(0.0);
        pid.IsSaturated.Should().BeFalse();
    }

    [Fact]
    public void Prime_SuppressesStartupDerivativeSpike()
    {
        // If the process starts at y=50 and the controller's first Compute call
        // sees that 50 with no prior reference, a naïve implementation would
        // attribute a (50 − 0)/dt derivative spike to the first sample.
        // Prime() lets the caller hand in the current measurement so the first
        // derivative is taken against it (and is therefore zero).
        var pid = new PidController<double>(0.0, 0.0, 1.0, -1e6, 1e6, 0.0, 0.0);
        pid.Prime(50.0);
        var u = pid.Compute(50.0, 50.0, Step);
        u.Should().Be(0.0);
    }

    [Fact]
    public void Compute_RejectsNaNInputs()
    {
        var pid = NewPid(kp: 1.0, ki: 1.0);
        Action sp = () => pid.Compute(double.NaN, 0.0, Step);
        Action mv = () => pid.Compute(0.0, double.NaN, Step);
        Action inf = () => pid.Compute(double.PositiveInfinity, 0.0, Step);
        sp.Should().Throw<ArgumentException>();
        mv.Should().Throw<ArgumentException>();
        inf.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClosedLoop_RejectsConstantDisturbance()
    {
        // Same first-order plant as the convergence test, but with a constant
        // load disturbance d injected into the dynamics:
        //   y_{k+1} = y_k + dt · (u_k − y_k + d) / τ
        // With Ki > 0 the integrator must absorb d and drive the steady-state
        // error to zero. P-only would settle to setpoint − d/(1+Kp).
        const double tau = 0.2;
        const double disturbance = 0.5;
        const double setpoint = 1.0;

        var pid = PidController<double>.Create(kp: 4.0, ki: 8.0, kd: 0.2,
                                                   outputMin: -5.0, outputMax: 5.0);
        double y = 0.0;
        for (int i = 0; i < 4000; i++) // 40 s simulated
        {
            var u = pid.Compute(setpoint, y, Step);
            y += Dt * (u - y + disturbance) / tau;
        }

        y.Should().BeApproximately(setpoint, 0.01,
            "integral action must cancel a constant disturbance in steady state");
    }

    [Fact]
    public void ClosedLoop_FirstOrderPlant_ConvergesToSetpoint()
    {
        // Toy first-order plant: y_{k+1} = y_k + dt · (u_k − y_k) / τ
        // PID brings y from 0 to setpoint 1.0 within reasonable time.
        const double tau = 0.2;
        var pid = PidController<double>.Create(kp: 4.0, ki: 8.0, kd: 0.2,
                                                   outputMin: -5.0, outputMax: 5.0);
        double y = 0.0;
        const double setpoint = 1.0;

        for (int i = 0; i < 2000; i++) // 20 s of simulated time
        {
            var u = pid.Compute(setpoint, y, Step);
            y += Dt * (u - y) / tau;
        }

        y.Should().BeApproximately(setpoint, 0.01, "closed loop should settle within 1%");
    }
}
