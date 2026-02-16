using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ModelingEvolution.Drawing.Equations;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a cubic Bezier curve defined by four control points.
/// Layout is guaranteed sequential for safe memory casting.
/// </summary>
/// <typeparam name="T">The numeric type for the coordinates.</typeparam>
[Svg.SvgExporter(typeof(BezierCurveSvgExporterFactory))]
[JsonConverter(typeof(BezierCurveJsonConverterFactory))]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BezierCurve<T> : IEnumerable<Point<T>>, IBoundingBox<T>, IParsable<BezierCurve<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private readonly Point<T> _start;
    private readonly Point<T> _c0;
    private readonly Point<T> _c1;
    private readonly Point<T> _end;

    /// <summary>
    /// Gets the starting point of the Bezier curve.
    /// </summary>
    public Point<T> Start => _start;

    /// <summary>
    /// Gets the first control point of the Bezier curve.
    /// </summary>
    public Point<T> C0 => _c0;

    /// <summary>
    /// Gets the second control point of the Bezier curve.
    /// </summary>
    public Point<T> C1 => _c1;

    /// <summary>
    /// Gets the ending point of the Bezier curve.
    /// </summary>
    public Point<T> End => _end;

    /// <summary>
    /// Initializes a new instance of the BezierCurve struct.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="c0">The first control point.</param>
    /// <param name="c1">The second control point.</param>
    /// <param name="end">The ending point.</param>
    public BezierCurve(Point<T> start, Point<T> c0, Point<T> c1, Point<T> end)
    {
        _start = start;
        _c0 = c0;
        _c1 = c1;
        _end = end;
    }

    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points.
    /// </summary>
    /// <param name="points">The points to create curves through.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>>)points, T.One / (T.One + T.One));
    }
    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points with a specified coefficient.
    /// </summary>
    /// <param name="coef">The coefficient controlling the curve's smoothness.</param>
    /// <param name="points">The points to create curves through.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(T coef, params Point<T>[] points)
    {
        return Create((IReadOnlyList<Point<T>> )points, coef);
    }
    /// <summary>
    /// Creates a series of Bezier curves that pass through the given points with a specified coefficient.
    /// </summary>
    /// <param name="points">The points to create curves through.</param>
    /// <param name="coef">The coefficient controlling the curve's smoothness.</param>
    /// <returns>A collection of Bezier curves.</returns>
    public static IEnumerable<BezierCurve<T>> Create(IReadOnlyList<Point<T>> points, T coef)
    {
        if (points.Count >= 2)
        {
            var p0 = points[0];
            var p1 = points[1];
            if (points.Count >= 3)
            {
                var p2 = points[2];
                var d = p1 - p0;
                yield return new BezierCurve<T>(p0,
                    d * coef + p0,
                    p1 - coef*((p2 - p0).Normalize()) * d.Length,
                    p1);
            }
            else
            {
                var d = p1 - p0;
                yield return new BezierCurve<T>(p0, p0 + d * coef, p1 - d * coef, p1);
            }

            for (int i = 0; i < points.Count-3; i++)
            {
                p0 = points[i];
                p1 = points[i + 1];
                var p2 = points[i + 2];
                var p3 = points[i + 3];

                var c0 = (p2 - p0).Normalize();
                var c1 = (p3 - p1).Normalize();
                var d = (p2 - p1).Length;
                yield return new BezierCurve<T>(p1,
                    p1 + c0 * coef * d,
                    p2 - c1 * coef * d,
                    p2);
            }

            if (points.Count >= 3)
            {
                var p3 = points[^1];
                var p2 = points[^2];
                p1 = points[^3];
                var d = p3 - p2;
                var c0 = (p3 - p1).Normalize();
                yield return new BezierCurve<T>(p2, p2 + c0 * coef * d.Length, p3 - d * coef, p3);
            }
        }
        else yield break;
    }
    /// <summary>
    /// Initializes a new instance of the BezierCurve struct from an array of points.
    /// </summary>
    /// <param name="points">Array containing exactly 4 points: start, control1, control2, end.</param>
    public BezierCurve(params Point<T>[] points) : this(points[0], points[1], points[2], points[3]) { }

    // ════════════════════════════════════════════════
    //  Operators
    // ════════════════════════════════════════════════

    /// <summary>Translates the curve by adding a vector.</summary>
    public static BezierCurve<T> operator +(BezierCurve<T> c, Vector<T> v)
        => new(c.Start + v, c.C0 + v, c.C1 + v, c.End + v);

    /// <summary>Translates the curve by subtracting a vector.</summary>
    public static BezierCurve<T> operator -(BezierCurve<T> c, Vector<T> v)
        => new(c.Start - v, c.C0 - v, c.C1 - v, c.End - v);

    /// <summary>Scales the curve by a size (component-wise).</summary>
    public static BezierCurve<T> operator *(BezierCurve<T> c, Size<T> s)
        => new(c.Start * s, c.C0 * s, c.C1 * s, c.End * s);

    /// <summary>Scales the curve by dividing by a size (component-wise).</summary>
    public static BezierCurve<T> operator /(BezierCurve<T> c, Size<T> s)
        => new(c.Start / s, c.C0 / s, c.C1 / s, c.End / s);

    /// <summary>Rotates the curve around the origin by the given angle.</summary>
    public static BezierCurve<T> operator +(BezierCurve<T> c, Degree<T> angle)
        => c.Rotate(angle);

    /// <summary>Rotates the curve around the origin by the negation of the given angle.</summary>
    public static BezierCurve<T> operator -(BezierCurve<T> c, Degree<T> angle)
        => c.Rotate(-angle);

    /// <summary>
    /// Rotates the curve around the specified origin by the given angle.
    /// </summary>
    public BezierCurve<T> Rotate(Degree<T> angle, Point<T> origin = default)
        => new(Start.Rotate(angle, origin), C0.Rotate(angle, origin),
               C1.Rotate(angle, origin), End.Rotate(angle, origin));

    // ════════════════════════════════════════════════
    //  TransformBy
    // ════════════════════════════════════════════════

    /// <summary>
    /// Transforms the Bezier curve by the specified matrix.
    /// </summary>
    /// <param name="m">The transformation matrix.</param>
    /// <returns>A new transformed Bezier curve.</returns>
    public BezierCurve<T> TransformBy(Matrix<T> m)
        => new(m.Transform(Start), m.Transform(C0), m.Transform(C1), m.Transform(End));

    // ════════════════════════════════════════════════
    //  Length / BoundingBox
    // ════════════════════════════════════════════════

    /// <summary>
    /// Computes the approximate arc length of the Bezier curve by sampling.
    /// </summary>
    public T Length()
    {
        var total = T.Zero;
        var prev = Start;
        const int samples = 20;
        for (int i = 1; i <= samples; i++)
        {
            var t = T.CreateChecked(i) / T.CreateChecked(samples);
            var curr = Evaluate(t);
            total += (curr - prev).Length;
            prev = curr;
        }
        return total;
    }

    /// <summary>
    /// Computes the axis-aligned bounding box using the control point hull.
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        var minX = T.Min(T.Min(Start.X, C0.X), T.Min(C1.X, End.X));
        var minY = T.Min(T.Min(Start.Y, C0.Y), T.Min(C1.Y, End.Y));
        var maxX = T.Max(T.Max(Start.X, C0.X), T.Max(C1.X, End.X));
        var maxY = T.Max(T.Max(Start.Y, C0.Y), T.Max(C1.Y, End.Y));
        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    // ════════════════════════════════════════════════
    //  IParsable / ToString (SVG path data)
    // ════════════════════════════════════════════════

    private static string Fmt(T value) =>
        Convert.ToDouble(value).ToString("G", CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the SVG path data string: M sx sy C c0x c0y, c1x c1y, ex ey
    /// </summary>
    public override string ToString()
        => $"M {Fmt(Start.X)} {Fmt(Start.Y)} C {Fmt(C0.X)} {Fmt(C0.Y)}, {Fmt(C1.X)} {Fmt(C1.Y)}, {Fmt(End.X)} {Fmt(End.Y)}";

    /// <summary>
    /// Parses an SVG path data string (M ... C ...) into a BezierCurve.
    /// </summary>
    public static BezierCurve<T> Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;
        throw new FormatException($"Unable to parse BezierCurve SVG path data: '{s}'.");
    }

    /// <summary>
    /// Attempts to parse an SVG path data string into a BezierCurve.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out BezierCurve<T> result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        try
        {
            var fmt = provider ?? CultureInfo.InvariantCulture;
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (char.IsLetter(ch))
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    tokens.Add(ch.ToString());
                }
                else if (ch == ',' || char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                }
                else if (ch == '-' && sb.Length > 0 && sb[^1] != 'e' && sb[^1] != 'E')
                {
                    tokens.Add(sb.ToString()); sb.Clear();
                    sb.Append(ch);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());

            // Expect: M x y C x y, x y, x y
            int idx = 0;
            if (idx >= tokens.Count || tokens[idx].ToUpperInvariant() != "M") return false;
            idx++;
            var sx = T.Parse(tokens[idx++], fmt);
            var sy = T.Parse(tokens[idx++], fmt);
            if (idx >= tokens.Count || tokens[idx].ToUpperInvariant() != "C") return false;
            idx++;
            var c0x = T.Parse(tokens[idx++], fmt);
            var c0y = T.Parse(tokens[idx++], fmt);
            var c1x = T.Parse(tokens[idx++], fmt);
            var c1y = T.Parse(tokens[idx++], fmt);
            var ex = T.Parse(tokens[idx++], fmt);
            var ey = T.Parse(tokens[idx++], fmt);

            result = new BezierCurve<T>(
                new Point<T>(sx, sy), new Point<T>(c0x, c0y),
                new Point<T>(c1x, c1y), new Point<T>(ex, ey));
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ════════════════════════════════════════════════
    //  Extremum / Intersection / Evaluate / Split
    // ════════════════════════════════════════════════

    private static readonly T tree = T.CreateTruncating(3);
    private static readonly T six = T.CreateTruncating(6);
    private static readonly T two = T.CreateTruncating(2);

    /// <summary>
    /// Calculates the extremum points (local minima and maxima) of the Bezier curve.
    /// </summary>
    /// <returns>An array of extremum points.</returns>
    public ReadOnlyMemory<Point<T>> CalculateExtremumPoints()
    {
        var D0 = tree * (C0 - Start);
        var D1 = tree * (C1 - C0);
        var D2 = tree * (End - C1);

        // Coefficients for the quadratic equation
        var a = D2 - two * D1 + D0;
        var b = two * (D1 - D0);
        var c = D0;

        var tValuesX = new QuadraticEquation<T>(a.X, b.X, c.X).ZeroPoints();
        var tValuesY = new QuadraticEquation<T>(a.Y, b.Y, c.Y).ZeroPoints();

        var result = new List<Point<T>>();
        var seen = new HashSet<T>();
        var xSpan = tValuesX.Span;
        for (int i = 0; i < xSpan.Length; i++)
        {
            var t = xSpan[i];
            if (t >= T.Zero && t <= T.One && seen.Add(t))
                result.Add(Evaluate(t));
        }
        var ySpan = tValuesY.Span;
        for (int i = 0; i < ySpan.Length; i++)
        {
            var t = ySpan[i];
            if (t >= T.Zero && t <= T.One && seen.Add(t))
                result.Add(Evaluate(t));
        }

        var mem = Alloc.Memory<Point<T>>(result.Count);
        result.CopyTo(mem.Span);
        return mem;
    }

    private readonly static T half = T.CreateTruncating(0.5);
    /// <summary>
    /// Calculates the intersection point of the Bezier curve with a linear equation.
    /// </summary>
    /// <param name="f">The linear equation to intersect with.</param>
    /// <returns>The intersection point.</returns>
    public Point<T> Intersection(LinearEquation<T> f)
    {
        var a = Start.X * f.A - Start.Y - tree * C0.X * f.A + tree * C0.Y + tree * C1.X * f.A - tree * C1.Y - End.X * f.A + End.Y;
        var b = -tree * Start.X * f.A + tree * Start.Y + six * C0.X * f.A - six * C0.Y - tree * C1.X * f.A + tree * C1.Y;
        var c = tree * Start.X * f.A - tree * Start.Y - tree * C0.X * f.A + tree * C0.Y;
        var d = -Start.X * f.A + Start.Y - f.B;
        CubicEquation<T> ex = new CubicEquation<T>(a, b, c, d);
        var zer = ex.FindRoot(half);

        return Evaluate(zer);
    }

    /// <summary>
    /// Evaluates the Bezier curve at the specified parameter value.
    /// </summary>
    /// <param name="t">The parameter value (must be between 0 and 1).</param>
    /// <returns>The point on the curve at parameter t.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when t is not between 0 and 1.</exception>
    public Point<T> Evaluate(T t)
    {
        if (t < T.Zero || t > T.One)
        {
            throw new ArgumentOutOfRangeException(nameof(t), "Parameter t should be between 0 and 1.");
        }

        // Cubic Bézier formula
        T oneMinusT = T.One - t;
        T oneMinusTSquare = oneMinusT * oneMinusT;
        T oneMinusTCube = oneMinusTSquare * oneMinusT;
        T tSquare = t * t;
        T tCube = tSquare * t;

        T x = oneMinusTCube * Start.X +
              tree * oneMinusTSquare * t * C0.X +
              tree * oneMinusT * tSquare * C1.X +
              tCube * End.X;

        T y = oneMinusTCube * Start.Y +
              tree * oneMinusTSquare * t * C0.Y +
              tree * oneMinusT * tSquare * C1.Y +
              tCube * End.Y;

        return new Point<T>(x, y);
    }

    /// <summary>
    /// Splits the curve at parameter t into two curves using De Casteljau's algorithm.
    /// </summary>
    /// <param name="t">The parameter value at which to split (0 to 1).</param>
    /// <returns>A tuple of (Left, Right) curves.</returns>
    public (BezierCurve<T> Left, BezierCurve<T> Right) Split(T t)
    {
        var q0 = LerpPoint(Start, C0, t);
        var q1 = LerpPoint(C0, C1, t);
        var q2 = LerpPoint(C1, End, t);
        var r0 = LerpPoint(q0, q1, t);
        var r1 = LerpPoint(q1, q2, t);
        var s = LerpPoint(r0, r1, t);
        return (new BezierCurve<T>(Start, q0, r0, s), new BezierCurve<T>(s, r1, q2, End));
    }

    /// <summary>
    /// Returns the sub-curve between parameters t0 and t1.
    /// </summary>
    /// <param name="t0">The start parameter (0 to 1).</param>
    /// <param name="t1">The end parameter (t0 to 1).</param>
    /// <returns>The sub-curve between t0 and t1.</returns>
    public BezierCurve<T> SubCurve(T t0, T t1)
    {
        if (t0 == T.Zero && t1 == T.One) return this;
        if (t0 == T.Zero) return Split(t1).Left;
        var (_, right) = Split(t0);
        if (t1 == T.One) return right;
        var remapped = (t1 - t0) / (T.One - t0);
        return right.Split(remapped).Left;
    }

    /// <summary>
    /// Finds all parameter values in (0,1) where this curve crosses the edges of a rectangle.
    /// </summary>
    internal ReadOnlyMemory<T> FindEdgeCrossings(Rectangle<T> rect)
    {
        var eps = T.CreateTruncating(1e-9);
        var results = new List<T>();

        // Left edge: x = rect.X
        AddAxisCrossings(Start.X, C0.X, C1.X, End.X, rect.X,
            Start.Y, C0.Y, C1.Y, End.Y, rect.Y, rect.Bottom, results, eps);
        // Right edge: x = rect.Right
        AddAxisCrossings(Start.X, C0.X, C1.X, End.X, rect.Right,
            Start.Y, C0.Y, C1.Y, End.Y, rect.Y, rect.Bottom, results, eps);
        // Top edge: y = rect.Y
        AddAxisCrossings(Start.Y, C0.Y, C1.Y, End.Y, rect.Y,
            Start.X, C0.X, C1.X, End.X, rect.X, rect.Right, results, eps);
        // Bottom edge: y = rect.Bottom
        AddAxisCrossings(Start.Y, C0.Y, C1.Y, End.Y, rect.Bottom,
            Start.X, C0.X, C1.X, End.X, rect.X, rect.Right, results, eps);

        results.Sort();
        // Deduplicate within epsilon
        for (int i = results.Count - 1; i > 0; i--)
            if (results[i] - results[i - 1] < eps)
                results.RemoveAt(i);

        var mem = Alloc.Memory<T>(results.Count);
        results.CopyTo(mem.Span);
        return mem;
    }

    /// <summary>
    /// Finds t-values where the Bezier polynomial in one axis equals a constant,
    /// and the other axis is within the edge bounds.
    /// </summary>
    private static void AddAxisCrossings(
        T p0, T p1, T p2, T p3, T k,
        T q0, T q1, T q2, T q3, T lo, T hi,
        List<T> results, T eps)
    {
        // Cubic coefficients: B(t) = at³ + bt² + ct + d, solve B(t) = k
        var a = -p0 + tree * p1 - tree * p2 + p3;
        var b = tree * p0 - six * p1 + tree * p2;
        var c = -tree * p0 + tree * p1;
        var d = p0 - k;

        // If all coefficients are ~zero, the component is constant → no crossings
        if (T.Abs(a) < eps && T.Abs(b) < eps && T.Abs(c) < eps)
            return;

        ReadOnlyMemory<T> roots;
        try { roots = new CubicEquation<T>(a, b, c, d).ZeroPoints(); }
        catch { return; }

        var rootsSpan = roots.Span;
        for (int ri = 0; ri < rootsSpan.Length; ri++)
        {
            var t = rootsSpan[ri];
            if (t > -eps && t < T.One + eps)
            {
                var clamped = T.Max(T.Zero, T.Min(T.One, t));
                // Check the other axis is within edge bounds
                var other = EvalComponent(q0, q1, q2, q3, clamped);
                if (other >= lo - eps && other <= hi + eps)
                    results.Add(clamped);
            }
        }
    }

    /// <summary>
    /// Evaluates one component of a cubic Bezier at parameter t.
    /// </summary>
    private static T EvalComponent(T p0, T p1, T p2, T p3, T t)
    {
        T u = T.One - t;
        return u * u * u * p0 + tree * u * u * t * p1 + tree * u * t * t * p2 + t * t * t * p3;
    }

    private static Point<T> LerpPoint(Point<T> a, Point<T> b, T t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>
    /// Densifies this Bezier curve by placing points along it at most 1 unit apart (arc-length based).
    /// </summary>
    public ReadOnlyMemory<Point<T>> Densify() => Densify(T.One);

    /// <summary>
    /// Densifies this Bezier curve by placing points along it at most <paramref name="unit"/> apart (arc-length based).
    /// </summary>
    public ReadOnlyMemory<Point<T>> Densify(T unit)
    {
        var s = unit;
        var ctrlLen = Start.DistanceTo(C0) + C0.DistanceTo(C1) + C1.DistanceTo(End);
        int oversamples = int.Max(20, int.CreateChecked(T.Ceiling(ctrlLen)) * 4);

        // Oversample to get dense intermediate points
        var samples = new List<Point<T>>(oversamples + 1);
        for (int i = 0; i <= oversamples; i++)
        {
            var t = T.CreateTruncating(i) / T.CreateTruncating(oversamples);
            var pt = Evaluate(t);
            if (samples.Count == 0 || !ApproxEqual(samples[^1], pt))
                samples.Add(pt);
        }

        // Walk samples, emit a point every s units of arc length
        var result = new List<Point<T>> { samples[0] };
        var accum = T.Zero;
        for (int i = 1; i < samples.Count; i++)
        {
            var prev = samples[i - 1];
            var cur = samples[i];
            var d = prev.DistanceTo(cur);
            accum += d;
            while (accum >= s)
            {
                var overshoot = accum - s;
                var frac = (d - overshoot) / d;
                result.Add(new Point<T>(
                    prev.X + (cur.X - prev.X) * frac,
                    prev.Y + (cur.Y - prev.Y) * frac));
                accum -= s;
            }
        }
        if (!ApproxEqual(result[^1], samples[^1]))
            result.Add(samples[^1]);

        var mem = Alloc.Memory<Point<T>>(result.Count);
        result.CopyTo(mem.Span);
        return mem;
    }

    private static bool ApproxEqual(Point<T> a, Point<T> b)
    {
        var eps = T.CreateTruncating(1e-9);
        return T.Abs(a.X - b.X) < eps && T.Abs(a.Y - b.Y) < eps;
    }

    // ════════════════════════════════════════════════
    //  Fit (least-squares)
    // ════════════════════════════════════════════════

    /// <summary>
    /// Fits a cubic Bezier curve to a sequence of points using iterative least-squares
    /// with Schneider's re-parameterization. The first point becomes <c>Start</c>,
    /// the last becomes <c>End</c>. Control points <c>C0</c> and <c>C1</c> are computed
    /// by iteratively solving least-squares and refining parameter values via
    /// Newton-Raphson projection onto the fitted curve.
    /// </summary>
    /// <param name="points">At least 2 points. First and last are used as fixed endpoints.</param>
    /// <returns>The best-fit cubic Bezier curve.</returns>
    public static BezierCurve<T> Fit(ReadOnlySpan<Point<T>> points)
    {
        if (points.Length < 2)
            throw new ArgumentException("At least 2 points are required.", nameof(points));

        var p0 = points[0];
        var p3 = points[^1];

        // 2 points: straight Bezier with 1/3-rule control points
        if (points.Length == 2)
            return FallbackBezier(p0, p3);

        // Compute initial chord-length parameters
        var n = points.Length;
        var t = new T[n];
        t[0] = T.Zero;
        for (int i = 1; i < n; i++)
            t[i] = t[i - 1] + points[i - 1].DistanceTo(points[i]);

        var totalLen = t[n - 1];
        if (totalLen <= T.CreateTruncating(1e-12))
            return FallbackBezier(p0, p3);

        for (int i = 1; i < n; i++)
            t[i] /= totalLen;

        // Iterative Schneider: solve → re-parameterize → repeat
        var curve = SolveLeastSquares(p0, p3, points, t);
        const int maxIterations = 8;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Re-parameterize: for each data point, find closest t on the fitted curve
            Reparameterize(curve, points, t);
            curve = SolveLeastSquares(p0, p3, points, t);
        }

        return curve;
    }

    /// <summary>
    /// Solves the 2x2 least-squares system for C0, C1 given fixed P0, P3 and parameters.
    /// </summary>
    private static BezierCurve<T> SolveLeastSquares(
        Point<T> p0, Point<T> p3,
        ReadOnlySpan<Point<T>> points, T[] t)
    {
        var n = points.Length;
        var a11 = T.Zero;
        var a12 = T.Zero;
        var a22 = T.Zero;
        var rhsX1 = T.Zero;
        var rhsX2 = T.Zero;
        var rhsY1 = T.Zero;
        var rhsY2 = T.Zero;

        for (int i = 1; i < n - 1; i++)
        {
            var ti = t[i];
            var u = T.One - ti;

            var alpha = tree * u * u * ti;
            var beta = tree * u * ti * ti;

            var u3 = u * u * u;
            var t3 = ti * ti * ti;
            var rx = points[i].X - u3 * p0.X - t3 * p3.X;
            var ry = points[i].Y - u3 * p0.Y - t3 * p3.Y;

            a11 += alpha * alpha;
            a12 += alpha * beta;
            a22 += beta * beta;
            rhsX1 += alpha * rx;
            rhsX2 += beta * rx;
            rhsY1 += alpha * ry;
            rhsY2 += beta * ry;
        }

        var det = a11 * a22 - a12 * a12;
        var eps = T.CreateTruncating(1e-12);

        if (T.Abs(det) > eps)
        {
            // Full-rank 2x2 solve via Cramer's rule
            var c0x = (a22 * rhsX1 - a12 * rhsX2) / det;
            var c1x = (a11 * rhsX2 - a12 * rhsX1) / det;
            var c0y = (a22 * rhsY1 - a12 * rhsY2) / det;
            var c1y = (a11 * rhsY2 - a12 * rhsY1) / det;
            return new BezierCurve<T>(p0, new Point<T>(c0x, c0y), new Point<T>(c1x, c1y), p3);
        }

        // Rank-deficient (e.g. 1 interior point): minimum-norm pseudoinverse
        // A⁺ = A / ||A||²_F for rank-1 symmetric A
        var two = T.CreateTruncating(2);
        var frobSq = a11 * a11 + two * a12 * a12 + a22 * a22;
        if (frobSq > eps)
        {
            var c0x = (a11 * rhsX1 + a12 * rhsX2) / frobSq;
            var c1x = (a12 * rhsX1 + a22 * rhsX2) / frobSq;
            var c0y = (a11 * rhsY1 + a12 * rhsY2) / frobSq;
            var c1y = (a12 * rhsY1 + a22 * rhsY2) / frobSq;
            return new BezierCurve<T>(p0, new Point<T>(c0x, c0y), new Point<T>(c1x, c1y), p3);
        }

        return FallbackBezier(p0, p3);
    }

    /// <summary>
    /// Newton-Raphson re-parameterization: for each data point, find the t value
    /// that minimizes ||B(t) - point||² using the derivative of the distance function.
    /// </summary>
    private static void Reparameterize(
        BezierCurve<T> curve, ReadOnlySpan<Point<T>> points, T[] t)
    {
        var two = T.CreateTruncating(2);
        var six = T.CreateTruncating(6);
        var eps = T.CreateTruncating(1e-12);

        var p0 = curve.Start;
        var c0 = curve.C0;
        var c1 = curve.C1;
        var p3 = curve.End;

        for (int i = 1; i < points.Length - 1; i++)
        {
            var ti = t[i];
            var px = points[i].X;
            var py = points[i].Y;

            // Newton-Raphson: minimize f(t) = ||B(t) - P||²
            // f'(t) = 2·(B(t)-P)·B'(t)
            // f''(t) = 2·(B'(t)·B'(t) + (B(t)-P)·B''(t))
            // t_new = t - f'(t)/f''(t)

            for (int j = 0; j < 5; j++)
            {
                var u = T.One - ti;
                var u2 = u * u;
                var u3 = u2 * u;
                var ti2 = ti * ti;
                var ti3 = ti2 * ti;

                // B(t)
                var bx = u3 * p0.X + tree * u2 * ti * c0.X + tree * u * ti2 * c1.X + ti3 * p3.X;
                var by = u3 * p0.Y + tree * u2 * ti * c0.Y + tree * u * ti2 * c1.Y + ti3 * p3.Y;

                // B'(t) = 3[(1-t)²(C0-P0) + 2(1-t)t(C1-C0) + t²(P3-C1)]
                var d1x = tree * (u2 * (c0.X - p0.X) + two * u * ti * (c1.X - c0.X) + ti2 * (p3.X - c1.X));
                var d1y = tree * (u2 * (c0.Y - p0.Y) + two * u * ti * (c1.Y - c0.Y) + ti2 * (p3.Y - c1.Y));

                // B''(t) = 6[(1-t)(C1-2C0+P0) + t(P3-2C1+C0)]
                var d2x = six * (u * (c1.X - two * c0.X + p0.X) + ti * (p3.X - two * c1.X + c0.X));
                var d2y = six * (u * (c1.Y - two * c0.Y + p0.Y) + ti * (p3.Y - two * c1.Y + c0.Y));

                var diffX = bx - px;
                var diffY = by - py;

                var numerator = diffX * d1x + diffY * d1y;
                var denominator = d1x * d1x + d1y * d1y + diffX * d2x + diffY * d2y;

                var relEps = T.Max(eps, T.CreateTruncating(1e-6) * (d1x * d1x + d1y * d1y));
                if (T.Abs(denominator) < relEps)
                    break;

                ti -= numerator / denominator;
                // Clamp to [0, 1]
                if (ti < T.Zero) ti = T.Zero;
                else if (ti > T.One) ti = T.One;
            }

            t[i] = ti;
        }
    }

    private static BezierCurve<T> FallbackBezier(Point<T> p0, Point<T> p3)
    {
        var third = T.One / tree;
        var c0 = new Point<T>(p0.X + (p3.X - p0.X) * third, p0.Y + (p3.Y - p0.Y) * third);
        var c1 = new Point<T>(p3.X - (p3.X - p0.X) * third, p3.Y - (p3.Y - p0.Y) * third);
        return new BezierCurve<T>(p0, c0, c1, p3);
    }

    // ════════════════════════════════════════════════
    //  Intersections
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all intersection points of this Bezier curve with an infinite line.
    /// Solves the cubic equation analytically.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Line<T> line)
    {
        var results = new List<Point<T>>();
        IntersectLine(line, results);
        var mem = Alloc.Memory<Point<T>>(results.Count);
        results.CopyTo(mem.Span);
        return mem;
    }

    /// <summary>
    /// Returns all intersection points of this Bezier curve with a finite segment.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Segment<T> segment)
    {
        // Quick bounding box check
        if (!BoundingBox().IntersectsWith(segment.BoundingBox()))
            return ReadOnlyMemory<Point<T>>.Empty;

        var lineResults = new List<Point<T>>();
        IntersectLine(segment.ToLine(), lineResults);

        var eps = T.CreateTruncating(1e-7);
        var d = segment.Direction;
        var lenSq = d.LengthSquared;
        var results = new List<Point<T>>();
        foreach (var pt in lineResults)
        {
            var ap = pt - segment.Start;
            var t = (ap.X * d.X + ap.Y * d.Y) / lenSq;
            if (t >= T.Zero - eps && t <= T.One + eps)
                results.Add(pt);
        }

        var mem = Alloc.Memory<Point<T>>(results.Count);
        results.CopyTo(mem.Span);
        return mem;
    }

    /// <summary>
    /// Returns all intersection points of this Bezier curve with a circle boundary.
    /// Approximated by densifying the curve and testing each sub-edge.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Circle<T> circle)
    {
        // Quick bounding box check
        if (!BoundingBox().IntersectsWith(circle.BoundingBox()))
            return ReadOnlyMemory<Point<T>>.Empty;

        var results = new List<Point<T>>();
        var densified = Densify();
        var pts = densified.Span;
        for (int i = 1; i < pts.Length; i++)
        {
            var edge = new Segment<T>(pts[i - 1], pts[i]);
            var hits = Intersections.SegmentCirclePoints(edge, circle);
            var hitsSpan = hits.Span;
            for (int j = 0; j < hitsSpan.Length; j++)
                results.Add(hitsSpan[j]);
        }

        var mem = Alloc.Memory<Point<T>>(results.Count);
        results.CopyTo(mem.Span);
        return mem;
    }

    /// <summary>
    /// Returns all intersection points of this Bezier curve with a rectangle's edges.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Rectangle<T> rect)
    {
        // Quick bounding box check
        if (!BoundingBox().IntersectsWith(rect))
            return ReadOnlyMemory<Point<T>>.Empty;

        var crossings = FindEdgeCrossings(rect);
        var span = crossings.Span;
        var mem = Alloc.Memory<Point<T>>(span.Length);
        var pts = mem.Span;
        for (int i = 0; i < span.Length; i++)
            pts[i] = Evaluate(span[i]);
        return mem;
    }

    /// <summary>
    /// Accumulates intersection points with an infinite line into the provided list.
    /// Used by both <see cref="Intersect(in Line{T})"/> and <see cref="Intersect(in Segment{T})"/>.
    /// </summary>
    internal void IntersectLine(in Line<T> line, List<Point<T>> results)
    {
        var eps = T.CreateTruncating(1e-9);

        if (line.IsVertical)
        {
            var vx = line.VerticalX;
            var a = -Start.X + tree * C0.X - tree * C1.X + End.X;
            var bCoeff = tree * Start.X - two * tree * C0.X + tree * C1.X;
            var c = -tree * Start.X + tree * C0.X;
            var d = Start.X - vx;
            SolveCubicAndAdd(a, bCoeff, c, d, eps, results);
        }
        else
        {
            var eq = line.Equation;
            var ax = -Start.X + tree * C0.X - tree * C1.X + End.X;
            var bx = tree * Start.X - six * C0.X + tree * C1.X;
            var cx = -tree * Start.X + tree * C0.X;
            var dx = Start.X;

            var ay = -Start.Y + tree * C0.Y - tree * C1.Y + End.Y;
            var by = tree * Start.Y - six * C0.Y + tree * C1.Y;
            var cy = -tree * Start.Y + tree * C0.Y;
            var dy = Start.Y;

            var a3 = eq.A * ax - ay;
            var a2 = eq.A * bx - by;
            var a1 = eq.A * cx - cy;
            var a0 = eq.A * dx - dy + eq.B;

            SolveCubicAndAdd(a3, a2, a1, a0, eps, results);
        }
    }

    private void SolveCubicAndAdd(T a, T bCoeff, T c, T d, T eps, List<Point<T>> results)
    {
        if (T.Abs(a) < eps && T.Abs(bCoeff) < eps && T.Abs(c) < eps)
            return;

        ReadOnlyMemory<T> roots;
        try { roots = new CubicEquation<T>(a, bCoeff, c, d).ZeroPoints(); }
        catch { return; }

        var rootsSpan = roots.Span;
        for (int i = 0; i < rootsSpan.Length; i++)
        {
            var t = rootsSpan[i];
            if (t >= -eps && t <= T.One + eps)
            {
                var clamped = T.Max(T.Zero, T.Min(T.One, t));
                results.Add(Evaluate(clamped));
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the control points of the curve.
    /// </summary>
    /// <returns>An enumerator for the control points.</returns>
    public IEnumerator<Point<T>> GetEnumerator()
    {
        yield return Start;
        yield return C0;
        yield return C1;
        yield return End;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
