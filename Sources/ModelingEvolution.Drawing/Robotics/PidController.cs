using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing.Robotics;

/// <summary>
/// Industrial PID controller with derivative-on-measurement, low-pass filtered
/// derivative, output saturation, and back-calculation anti-windup.
/// </summary>
/// <remarks>
/// <para>
/// Control law (parallel form):
/// <c>u = Kp·e + Ki·∫e dt − Kd·(dy/dt)_filtered</c>,
/// where <c>e = setpoint − measurement</c> and <c>y = measurement</c>.
/// </para>
/// <para>
/// The derivative is taken on the measurement (not the error) to avoid the
/// "derivative kick" — a large transient produced when the setpoint changes
/// in a step. With derivative-on-measurement the D term reacts only to the
/// process moving, not to operator commands.
/// </para>
/// <para>
/// A first-order low-pass with time constant <see cref="DerivativeFilterTime"/>
/// (Tf) is applied to the derivative path to suppress noise amplification:
/// <c>α = Tf / (Tf + dt); d_filt = α·d_filt_prev + (1 − α)·dy_raw</c>.
/// As a rule of thumb pick <c>Tf ≈ Kd / (Kp · N)</c> with N ∈ [5..20];
/// larger N gives lighter filtering. Setting Tf to zero disables filtering.
/// </para>
/// <para>
/// Anti-windup uses back-calculation: when the output saturates the
/// integrator is bled by <c>Kaw · (u_unclamped − u_clamped)</c> so it cannot
/// accumulate while clamped. <see cref="AntiWindupGain"/> is dimensionless on
/// [0..1]; 1 cancels the saturation excess in a single step, 0 disables
/// anti-windup entirely. Note that Kaw is <em>not</em> normalised by dt: if
/// you change the loop rate, re-tune Kaw at the new rate. (The Åström
/// formulation <c>I -= (1/Tt)·(u_raw − u)·dt</c> would be dt-invariant; this
/// class deliberately uses the simpler one-step bleed.)
/// </para>
/// <para>
/// <strong>Finding PID parameters.</strong>
/// </para>
/// <para>
/// <em>Ziegler–Nichols (sustained-oscillation method).</em> Set Ki = Kd = 0
/// and raise Kp until the closed loop sustains steady oscillations. Record
/// the critical gain Ku and oscillation period Tu, then read off:
/// Kp = 0.6·Ku, Ti = 0.5·Tu, Td = 0.125·Tu, with Ki = Kp/Ti and Kd = Kp·Td.
/// Quick but aggressive — typically ~25% overshoot. Don't use on plants that
/// can't tolerate marginal-stability tests (food, pharma, anything fragile).
/// </para>
/// <para>
/// <em>Tyreus–Luyben.</em> Same Ku/Tu measurement, gentler gains:
/// Kp = 0.45·Ku, Ti = 2.2·Tu, Td = Tu/6.3. Less overshoot, slower response —
/// preferred when oscillation is costly.
/// </para>
/// <para>
/// <em>Cohen–Coon (open-loop step test).</em> Apply a step input, fit a
/// first-order-plus-dead-time model (gain K, time constant τ, dead time L)
/// to the response, then:
/// Kp = (1/K)(τ/L)(4/3 + L/(4τ)),  Ti = L·(32 + 6L/τ)/(13 + 8L/τ),
/// Td = 4L/(11 + 2L/τ). Useful when closed-loop oscillation testing is unsafe.
/// </para>
/// <para>
/// <em>Manual heuristic.</em>
/// (1) Start with Ki = Kd = 0; raise Kp until the step response is fast with
/// ~10% overshoot.
/// (2) Add Ki to remove steady-state error; increase until the integrator
/// settles within roughly 2× the rise time.
/// (3) Add Kd to dampen overshoot/ringing; back off if it starts amplifying
/// measurement noise.
/// (4) Pick <see cref="DerivativeFilterTime"/> ≈ Kd/(Kp·N) with N ∈ [5..20];
/// N = 10 is a good default (see <see cref="Create(T,T,T,T,T,int)"/>).
/// </para>
/// <para>
/// <em>Software auto-tuning.</em> MATLAB's <c>pidtune</c>, Python's
/// <c>scipy.signal</c> / <c>python-control</c>, and most commercial PLC
/// vendors offer relay-feedback or model-fit auto-tuners that pick gains
/// from a short experiment. Prefer these when a plant model is available.
/// </para>
/// <para>
/// <strong>Thread-safety.</strong> Not thread-safe; serialize
/// <see cref="Compute(T,T,TimeSpan)"/> calls and gain mutations externally.
/// </para>
/// </remarks>
/// <example>
/// Speed loop running at 100 Hz on a DC motor:
/// <code>
/// var pid = PidController&lt;double&gt;.Create(
///     kp: 0.8, ki: 2.0, kd: 0.05,
///     outputMin: -100.0, outputMax: 100.0);
///
/// pid.Prime(motor.Rpm);                     // avoid first-tick derivative spike
/// var dt = TimeSpan.FromMilliseconds(10);
///
/// while (running)
/// {
///     var command = pid.Compute(targetRpm, motor.Rpm, dt);
///     motor.PowerPercent = command;
///     await Task.Delay(dt);
/// }
/// </code>
/// </example>
/// <typeparam name="T">Numeric type used for gains, signals, and time steps.</typeparam>
public sealed class PidController<T>
    where T : INumber<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    private T _integral;
    private T _dFiltered;
    private T _prevMeasurement;
    private bool _hasPrev;

    /// <summary>Proportional gain.</summary>
    public T Kp { get; set; }

    /// <summary>Integral gain (equivalent to Kp / Ti).</summary>
    public T Ki { get; set; }

    /// <summary>Derivative gain (equivalent to Kp · Td).</summary>
    public T Kd { get; set; }

    /// <summary>Output lower saturation limit.</summary>
    public T OutputMin { get; set; }

    /// <summary>Output upper saturation limit.</summary>
    public T OutputMax { get; set; }

    /// <summary>
    /// Derivative low-pass filter time constant, in seconds.
    /// Zero disables filtering (raw difference).
    /// </summary>
    public T DerivativeFilterTime { get; set; }

    /// <summary>
    /// Back-calculation anti-windup gain on [0..1].
    /// Zero disables anti-windup; one cancels the saturation excess in one step.
    /// </summary>
    public T AntiWindupGain { get; set; }

    /// <summary>Most recent error (setpoint − measurement).</summary>
    public T LastError { get; private set; }

    /// <summary>Current accumulated integral term.</summary>
    public T IntegralSum => _integral;

    /// <summary>Filtered derivative state (on measurement, not error).</summary>
    public T FilteredDerivative => _dFiltered;

    /// <summary>True when the last <see cref="Compute(T,T,TimeSpan)"/> call produced a saturated output.</summary>
    public bool IsSaturated { get; private set; }

    /// <summary>
    /// Creates a PID controller. Pass <c>T.Zero</c> for <paramref name="derivativeFilterTime"/>
    /// to disable D-filtering, or for <paramref name="antiWindupGain"/> to disable anti-windup.
    /// </summary>
    /// <param name="kp">Proportional gain.</param>
    /// <param name="ki">Integral gain.</param>
    /// <param name="kd">Derivative gain.</param>
    /// <param name="outputMin">Output lower saturation limit. Must be strictly less than <paramref name="outputMax"/>.</param>
    /// <param name="outputMax">Output upper saturation limit.</param>
    /// <param name="derivativeFilterTime">Derivative low-pass time constant (seconds). Zero = unfiltered.</param>
    /// <param name="antiWindupGain">Back-calculation gain on [0..1]. Zero = no anti-windup.</param>
    public PidController(T kp, T ki, T kd, T outputMin, T outputMax,
                         T derivativeFilterTime, T antiWindupGain)
    {
        if (outputMin >= outputMax)
            throw new ArgumentException("outputMin must be strictly less than outputMax.", nameof(outputMin));
        if (derivativeFilterTime < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(derivativeFilterTime), "Must be non-negative.");
        if (antiWindupGain < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(antiWindupGain), "Must be non-negative.");

        Kp = kp;
        Ki = ki;
        Kd = kd;
        OutputMin = outputMin;
        OutputMax = outputMax;
        DerivativeFilterTime = derivativeFilterTime;
        AntiWindupGain = antiWindupGain;
    }

    /// <summary>
    /// Creates a PID controller with sensible industrial defaults: back-calculation
    /// gain <c>Kaw = 1</c> (one-step bleed) and derivative-filter time
    /// <c>Tf = |Kd| / (Kp · N)</c> with N defaulted to 10. When Kp is zero
    /// (pure-D or pure-I controller) Tf falls back to 50 ms — a noise-friendly
    /// default; override via the full constructor if your sampling rate or noise
    /// profile demands something else.
    /// </summary>
    /// <param name="kp">Proportional gain.</param>
    /// <param name="ki">Integral gain.</param>
    /// <param name="kd">Derivative gain.</param>
    /// <param name="outputMin">Output lower saturation limit.</param>
    /// <param name="outputMax">Output upper saturation limit.</param>
    /// <param name="n">Derivative-filter ratio. Typical range [5..20]; larger = lighter filtering.</param>
    public static PidController<T> Create(T kp, T ki, T kd, T outputMin, T outputMax, int n = 10)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Derivative-filter ratio must be positive.");

        T tf;
        if (kd == T.Zero)
            tf = T.Zero;                                       // no D term — no filter needed
        else if (kp == T.Zero)
            tf = T.CreateChecked(0.05);                        // pure-D fallback: 50 ms
        else
            tf = T.Abs(kd / (kp * T.CreateChecked(n)));        // Tf = |Kd| / (Kp · N)

        return new PidController<T>(kp, ki, kd, outputMin, outputMax, tf, T.One);
    }

    /// <summary>
    /// Clears integrator, filtered derivative, and the previous-measurement memory.
    /// Call when the loop is re-armed after being stopped or after a manual override.
    /// </summary>
    public void Reset()
    {
        _integral = T.Zero;
        _dFiltered = T.Zero;
        _hasPrev = false;          // _prevMeasurement is gated by this flag, no need to clear it
        LastError = T.Zero;
        IsSaturated = false;
    }

    /// <summary>
    /// Primes the previous-measurement state without producing an output.
    /// Use this when handing the controller a known current measurement before the
    /// first <see cref="Compute(T,T,TimeSpan)"/> call so the initial derivative is zero
    /// (instead of being relative to <c>T.Zero</c>).
    /// </summary>
    public void Prime(T measurement)
    {
        _prevMeasurement = measurement;
        _hasPrev = true;
    }

    /// <summary>
    /// Computes the next control output.
    /// </summary>
    /// <remarks>
    /// Converts <paramref name="dt"/> via <c>T.CreateTruncating(dt.TotalSeconds)</c> on
    /// every call — free for <c>T = double</c>, one narrowing cast for <c>T = float</c>.
    /// In hot inner loops with a fixed tick rate, hoist the conversion once and call
    /// <see cref="Compute(T,T,T)"/> directly.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Compute(T setpoint, T measurement, TimeSpan dt)
        => Compute(setpoint, measurement, T.CreateTruncating(dt.TotalSeconds));

    /// <summary>
    /// Computes the next control output. Overload accepting <paramref name="dt"/> directly in T —
    /// prefer this overload in hot loops with a fixed tick rate (see <see cref="Compute(T,T,TimeSpan)"/>).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Compute(T setpoint, T measurement, T dt)
    {
        if (dt <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(dt), "dt must be positive.");
        if (!T.IsFinite(setpoint) || !T.IsFinite(measurement))
            throw new ArgumentException(
                "setpoint and measurement must be finite numbers; NaN or Infinity would poison the integrator.");

        var error = setpoint - measurement;
        LastError = error;

        var p = Kp * error;

        // Derivative on measurement (negated). On the first sample we have no
        // previous measurement, so dy is zero — avoids a spurious startup spike.
        var dyRaw = _hasPrev ? (measurement - _prevMeasurement) / dt : T.Zero;

        if (DerivativeFilterTime > T.Zero)
        {
            var alpha = DerivativeFilterTime / (DerivativeFilterTime + dt);
            _dFiltered = alpha * _dFiltered + (T.One - alpha) * dyRaw;
        }
        else
        {
            _dFiltered = dyRaw;
        }

        var d = -Kd * _dFiltered;

        _integral += Ki * error * dt;

        var uRaw = p + _integral + d;
        var u = uRaw < OutputMin ? OutputMin : uRaw > OutputMax ? OutputMax : uRaw;
        IsSaturated = u != uRaw;

        if (IsSaturated && AntiWindupGain > T.Zero)
            _integral -= AntiWindupGain * (uRaw - u);

        _prevMeasurement = measurement;
        _hasPrev = true;

        return u;
    }
}
