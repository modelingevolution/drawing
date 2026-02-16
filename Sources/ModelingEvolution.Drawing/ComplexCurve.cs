using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using ModelingEvolution.Drawing.Svg;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a composite curve composed of mixed Bezier curves and polyline segments,
/// stored in a contiguous packed byte buffer for zero-copy decoding.
/// </summary>
/// <remarks>
/// Memory layout: <c>[segment₀][segment₁]...[segmentₙ₋₁]</c>
/// <para>Bezier segment:  <c>[0x00 : byte][BezierCurve&lt;T&gt; : 8×sizeof(T) bytes]</c></para>
/// <para>Polyline segment: <c>[0x01 : byte][count : ushort][Point&lt;T&gt;₀]...[Point&lt;T&gt;ₙ₋₁]</c></para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
[JsonConverter(typeof(ComplexCurveJsonConverterFactory))]
[SvgExporter(typeof(ComplexCurveSvgExporterFactory))]
[ProtoContract]
public readonly record struct ComplexCurve<T> : IBoundingBox<T>, IParsable<ComplexCurve<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    internal const byte TagBezier = 0;
    internal const byte TagPolyline = 1;

    private readonly ReadOnlyMemory<byte> _data;
    private readonly int _segmentCount;

    [ProtoMember(1)]
    private byte[] ProtoData
    {
        get => MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)_data, out var seg)
                   && seg.Offset == 0 && seg.Count == seg.Array!.Length
               ? seg.Array
               : _data.ToArray();
        init => _data = value ?? ReadOnlyMemory<byte>.Empty;
    }

    [ProtoMember(2)]
    private int ProtoSegmentCount
    {
        get => _segmentCount;
        init => _segmentCount = value;
    }

    internal ComplexCurve(ReadOnlyMemory<byte> data, int segmentCount)
    {
        _data = data;
        _segmentCount = segmentCount;
    }

    /// <summary>Gets whether this curve contains no segments.</summary>
    public bool IsEmpty => _data.IsEmpty;

    /// <summary>Gets the number of segments in this curve.</summary>
    public int SegmentCount => _segmentCount;

    /// <summary>Gets the total byte length of the packed buffer.</summary>
    public int ByteLength => _data.Length;

    /// <summary>Returns an enumerator that decodes segments from the packed buffer.</summary>
    public Enumerator GetEnumerator() => new(_data.Span);

    // ════════════════════════════════════════════════
    //  Operators
    // ════════════════════════════════════════════════

    /// <summary>Translates the curve by adding a vector.</summary>
    public static ComplexCurve<T> operator +(in ComplexCurve<T> c, in Vector<T> v)
    { var v2 = v; return c.Rebuild(p => p + v2); }

    /// <summary>Translates the curve by subtracting a vector.</summary>
    public static ComplexCurve<T> operator -(in ComplexCurve<T> c, in Vector<T> v)
    { var v2 = v; return c.Rebuild(p => p - v2); }

    /// <summary>Scales the curve by a size.</summary>
    public static ComplexCurve<T> operator *(in ComplexCurve<T> c, in Size<T> s)
    { var s2 = s; return c.Rebuild(p => p * s2); }

    /// <summary>Scales the curve by dividing by a size.</summary>
    public static ComplexCurve<T> operator /(in ComplexCurve<T> c, in Size<T> s)
    { var s2 = s; return c.Rebuild(p => p / s2); }

    /// <summary>Rotates the curve around the origin by the given angle.</summary>
    public static ComplexCurve<T> operator +(in ComplexCurve<T> c, Degree<T> angle)
        => c.Rotate(angle);

    /// <summary>Rotates the curve around the origin by the negation of the given angle.</summary>
    public static ComplexCurve<T> operator -(in ComplexCurve<T> c, Degree<T> angle)
        => c.Rotate(-angle);

    /// <summary>Rotates the curve around the origin by the given radian angle.</summary>
    public static ComplexCurve<T> operator +(in ComplexCurve<T> c, Radian<T> angle)
        => c.Rotate((Degree<T>)angle);

    /// <summary>Rotates the curve around the origin by the negation of the given radian angle.</summary>
    public static ComplexCurve<T> operator -(in ComplexCurve<T> c, Radian<T> angle)
        => c.Rotate(-(Degree<T>)angle);

    /// <summary>
    /// Rotates the curve around the specified origin by the given angle.
    /// </summary>
    public ComplexCurve<T> Rotate(Degree<T> angle, Point<T> origin = default)
        => Rebuild(p => p.Rotate(angle, origin));

    // ════════════════════════════════════════════════
    //  Length
    // ════════════════════════════════════════════════

    /// <summary>
    /// Computes the approximate total arc length of the curve.
    /// Delegates to <see cref="BezierCurve{T}.Length"/> and polyline edge sums.
    /// </summary>
    public T Length()
    {
        var total = T.Zero;
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                total += seg.AsBezier().Length();
            else
            {
                var pts = seg.AsPoints();
                for (int i = 1; i < pts.Length; i++)
                    total += (pts[i] - pts[i - 1]).Length;
            }
        }
        return total;
    }

    // ════════════════════════════════════════════════
    //  Densify
    // ════════════════════════════════════════════════

    /// <summary>
    /// Densifies this curve by placing points along it at most 1 unit apart.
    /// Delegates to <see cref="BezierCurve{T}.Densify"/> for Bezier segments
    /// and <see cref="Densification.Densify{T}"/> for polyline segments.
    /// </summary>
    public Polyline<T> Densify()
    {
        if (_data.IsEmpty) return new Polyline<T>();

        var builder = new PolylineBuilder<T>();
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                builder.AddRange(seg.AsBezier().Densify());
            else
                builder.AddRange(Densification.Densify(seg.AsPoints()));
        }
        return builder.Build();
    }

    // ════════════════════════════════════════════════
    //  BoundingBox
    // ════════════════════════════════════════════════

    /// <summary>
    /// Computes the axis-aligned bounding box of this curve.
    /// Uses control points for Bezier segments (conservative hull).
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        var minX = T.MaxValue;
        var minY = T.MaxValue;
        var maxX = T.MinValue;
        var maxY = T.MinValue;

        foreach (var seg in this)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                ExpandBounds(b.Start, ref minX, ref minY, ref maxX, ref maxY);
                ExpandBounds(b.C0, ref minX, ref minY, ref maxX, ref maxY);
                ExpandBounds(b.C1, ref minX, ref minY, ref maxX, ref maxY);
                ExpandBounds(b.End, ref minX, ref minY, ref maxX, ref maxY);
            }
            else
            {
                var pts = seg.AsPoints();
                for (int i = 0; i < pts.Length; i++)
                    ExpandBounds(pts[i], ref minX, ref minY, ref maxX, ref maxY);
            }
        }

        if (minX > maxX) return default;
        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExpandBounds(Point<T> p, ref T minX, ref T minY, ref T maxX, ref T maxY)
    {
        if (p.X < minX) minX = p.X;
        if (p.Y < minY) minY = p.Y;
        if (p.X > maxX) maxX = p.X;
        if (p.Y > maxY) maxY = p.Y;
    }

    // ════════════════════════════════════════════════
    //  TransformBy
    // ════════════════════════════════════════════════

    /// <summary>
    /// Transforms all points in this curve by the specified matrix, returning a new curve.
    /// </summary>
    public ComplexCurve<T> TransformBy(Matrix<T> m)
        => Rebuild(p => m.Transform(p));

    // ════════════════════════════════════════════════
    //  AlignTo
    // ════════════════════════════════════════════════

    /// <summary>
    /// Computes a PCA-based rigid alignment that maps this curve onto the target point cloud.
    /// Densifies the curve first so that Bezier segments and polyline edges produce
    /// uniformly-spaced points suitable for PCA.
    /// </summary>
    /// <param name="target">The target point cloud to align to.</param>
    public AlignmentResult<T> AlignTo(ReadOnlySpan<Point<T>> target)
    {
        return Alignment.Pca(Densify().AsSpan(), target);
    }

    // ════════════════════════════════════════════════
    //  ComputeCorrections
    // ════════════════════════════════════════════════

    /// <summary>
    /// Computes corrections to deform this template curve onto a measured target polyline.
    /// First applies rigid alignment (PCA), then per-segment local correction:
    /// polyline vertices are projected onto the target; Bezier control points are
    /// fitted via least-squares to the corresponding target sub-section.
    /// </summary>
    public CorrectionResult<T> ComputeCorrections(in Polyline<T> target)
    {
        if (_data.IsEmpty || target.Count < 2)
            return new CorrectionResult<T>(default, this, T.Zero);

        var targetSpan = target.AsSpan();

        // Step 1: Rigid alignment
        var alignment = AlignTo(targetSpan);
        var alignedCurve = Rebuild(p => alignment.ApplyTo(p));

        // Step 2: Build KD-tree for fast NN queries
        var targetTree = KdTree<T>.Build(targetSpan);

        // Step 3: Per-segment correction
        var builder = new ComplexCurveBuilder<T>(_data.Length);
        var totalResidual = T.Zero;

        foreach (var seg in alignedCurve)
        {
            if (seg.IsPolyline)
            {
                var pts = seg.AsPoints();
                var corrected = new Point<T>[pts.Length];
                for (int i = 0; i < pts.Length; i++)
                {
                    var proj = CurveCorrection.ProjectOntoPolyline(pts[i], targetSpan, targetTree);
                    corrected[i] = proj.ClosestPoint;
                    totalResidual += proj.Distance * proj.Distance;
                }
                builder.AddPoints(corrected);
            }
            else // IsBezier
            {
                var bezier = seg.AsBezier();

                // Project endpoints
                var projStart = CurveCorrection.ProjectOntoPolyline(bezier.Start, targetSpan, targetTree);
                var projEnd = CurveCorrection.ProjectOntoPolyline(bezier.End, targetSpan, targetTree);

                var p0 = projStart.ClosestPoint;
                var p3 = projEnd.ClosestPoint;

                // Extract target sub-section between projected endpoints
                var subPts = CurveCorrection.ExtractTargetSubSection(
                    p0, projStart.EdgeIndex, projStart.EdgeParam,
                    p3, projEnd.EdgeIndex, projEnd.EdgeParam,
                    targetSpan);

                // Filter to points within oriented rectangle around chord
                subPts = CurveCorrection.FilterByOrientedRect(subPts);

                // Fit Bezier with Schneider's iterative re-parameterization
                var fitted = BezierCurve<T>.Fit(subPts.Span);

                // Accumulate residual
                totalResidual += CurveCorrection.ComputeBezierResidual(fitted, subPts.Span);

                builder.AddBezier(fitted);
            }
        }

        return new CorrectionResult<T>(alignment, builder.Build(), totalResidual);
    }

    // ════════════════════════════════════════════════
    //  Intersections
    // ════════════════════════════════════════════════

    /// <summary>
    /// Returns all intersection points of this curve with an infinite line.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Line<T> line)
    {
        if (_data.IsEmpty) return ReadOnlyMemory<Point<T>>.Empty;
        var results = new List<Point<T>>();
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                AddHits(results, seg.AsBezier().Intersect(line));
            else
            {
                var pts = seg.AsPoints();
                for (int i = 1; i < pts.Length; i++)
                {
                    var hit = Intersections.Of(line, new Segment<T>(pts[i - 1], pts[i]));
                    if (hit != null) results.Add(hit.Value);
                }
            }
        }
        return ToMemory(results);
    }

    /// <summary>
    /// Returns all intersection points of this curve with a finite segment.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Segment<T> segment)
    {
        if (_data.IsEmpty) return ReadOnlyMemory<Point<T>>.Empty;
        var results = new List<Point<T>>();
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                AddHits(results, seg.AsBezier().Intersect(segment));
            else
            {
                var pts = seg.AsPoints();
                for (int i = 1; i < pts.Length; i++)
                {
                    var hit = Intersections.Of(new Segment<T>(pts[i - 1], pts[i]), segment);
                    if (hit != null) results.Add(hit.Value);
                }
            }
        }
        return ToMemory(results);
    }

    /// <summary>
    /// Returns all intersection points of this curve with a circle boundary.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Circle<T> circle)
    {
        if (_data.IsEmpty) return ReadOnlyMemory<Point<T>>.Empty;
        var results = new List<Point<T>>();
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                AddHits(results, seg.AsBezier().Intersect(circle));
            else
            {
                var pts = seg.AsPoints();
                for (int i = 1; i < pts.Length; i++)
                    AddHits(results, Intersections.SegmentCirclePoints(new Segment<T>(pts[i - 1], pts[i]), circle));
            }
        }
        return ToMemory(results);
    }

    /// <summary>
    /// Returns all intersection points of this curve with a rectangle's edges.
    /// </summary>
    public ReadOnlyMemory<Point<T>> Intersect(in Rectangle<T> rect)
    {
        if (_data.IsEmpty) return ReadOnlyMemory<Point<T>>.Empty;
        var results = new List<Point<T>>();
        var rectEdges = new Segment<T>[]
        {
            new(new Point<T>(rect.X, rect.Y), new Point<T>(rect.Right, rect.Y)),
            new(new Point<T>(rect.Right, rect.Y), new Point<T>(rect.Right, rect.Bottom)),
            new(new Point<T>(rect.Right, rect.Bottom), new Point<T>(rect.X, rect.Bottom)),
            new(new Point<T>(rect.X, rect.Bottom), new Point<T>(rect.X, rect.Y)),
        };
        foreach (var seg in this)
        {
            if (seg.IsBezier)
                AddHits(results, seg.AsBezier().Intersect(rect));
            else
            {
                var pts = seg.AsPoints();
                for (int i = 1; i < pts.Length; i++)
                {
                    var edge = new Segment<T>(pts[i - 1], pts[i]);
                    for (int j = 0; j < rectEdges.Length; j++)
                    {
                        var hit = Intersections.Of(edge, rectEdges[j]);
                        if (hit != null) results.Add(hit.Value);
                    }
                }
            }
        }
        return ToMemory(results);
    }

    // ════════════════════════════════════════════════
    //  IParsable / ToString (SVG path data)
    // ════════════════════════════════════════════════

    private static string FormatNumber(T value) =>
        Convert.ToDouble(value).ToString("G", CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the SVG path data string representation.
    /// Bezier segments emit C commands; polyline segments emit L commands.
    /// </summary>
    public override string ToString()
    {
        if (_data.IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        bool needMove = true;
        Point<T> currentPos = default;

        foreach (var seg in this)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                if (needMove || !PointsEqual(currentPos, b.Start))
                    sb.Append($"M {FormatNumber(b.Start.X)} {FormatNumber(b.Start.Y)}");
                needMove = false;
                sb.Append($" C {FormatNumber(b.C0.X)} {FormatNumber(b.C0.Y)},");
                sb.Append($" {FormatNumber(b.C1.X)} {FormatNumber(b.C1.Y)},");
                sb.Append($" {FormatNumber(b.End.X)} {FormatNumber(b.End.Y)}");
                currentPos = b.End;
            }
            else
            {
                var pts = seg.AsPoints();
                if (pts.Length == 0) continue;
                if (needMove || !PointsEqual(currentPos, pts[0]))
                    sb.Append($"M {FormatNumber(pts[0].X)} {FormatNumber(pts[0].Y)}");
                needMove = false;
                for (int i = 1; i < pts.Length; i++)
                    sb.Append($" L {FormatNumber(pts[i].X)} {FormatNumber(pts[i].Y)}");
                currentPos = pts[pts.Length - 1];
            }
        }
        return sb.ToString();
    }

    private static bool PointsEqual(Point<T> a, Point<T> b)
    {
        var eps = T.CreateTruncating(0.0001);
        return T.Abs(a.X - b.X) < eps && T.Abs(a.Y - b.Y) < eps;
    }

    /// <summary>
    /// Parses an SVG path data string into a ComplexCurve.
    /// Supports M (move), C (cubic Bezier), and L (line) commands.
    /// </summary>
    public static ComplexCurve<T> Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;
        throw new FormatException($"Unable to parse ComplexCurve SVG path data: '{s}'.");
    }

    /// <summary>
    /// Attempts to parse an SVG path data string into a ComplexCurve.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out ComplexCurve<T> result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return true;
        }

        try
        {
            var builder = new ComplexCurveBuilder<T>();
            var currentPos = Point<T>.Zero;
            var lineAccum = new List<Point<T>>();

            var tokens = TokenizePath(s);
            var i = 0;

            while (i < tokens.Count)
            {
                var command = tokens[i++];
                switch (command.ToUpperInvariant())
                {
                    case "M":
                        FlushLineAccum(lineAccum, builder);
                        currentPos = ParsePoint(tokens, ref i, provider);
                        break;

                    case "C":
                        FlushLineAccum(lineAccum, builder);
                        var c0 = ParsePoint(tokens, ref i, provider);
                        var c1 = ParsePoint(tokens, ref i, provider);
                        var end = ParsePoint(tokens, ref i, provider);
                        builder.AddBezier(new BezierCurve<T>(currentPos, c0, c1, end));
                        currentPos = end;
                        break;

                    case "L":
                        if (lineAccum.Count == 0)
                            lineAccum.Add(currentPos);
                        var lineTo = ParsePoint(tokens, ref i, provider);
                        lineAccum.Add(lineTo);
                        currentPos = lineTo;
                        break;

                    case "Z":
                        FlushLineAccum(lineAccum, builder);
                        break;

                    default:
                        // Skip unknown commands by consuming any numeric tokens
                        while (i < tokens.Count && double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                            i++;
                        break;
                }
            }

            FlushLineAccum(lineAccum, builder);
            result = builder.Build();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void FlushLineAccum(List<Point<T>> lineAccum, ComplexCurveBuilder<T> builder)
    {
        if (lineAccum.Count >= 2)
        {
            builder.AddPoints(lineAccum.ToArray());
        }
        lineAccum.Clear();
    }

    private static Point<T> ParsePoint(List<string> tokens, ref int i, IFormatProvider? provider)
    {
        var x = T.Parse(tokens[i++], provider ?? CultureInfo.InvariantCulture);
        var y = T.Parse(tokens[i++], provider ?? CultureInfo.InvariantCulture);
        return new Point<T>(x, y);
    }

    private static List<string> TokenizePath(string s)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();

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
            else if (ch == '-' && sb.Length > 0 && sb[sb.Length - 1] != 'e' && sb[sb.Length - 1] != 'E')
            {
                // Negative sign starts a new number (unless after exponent)
                tokens.Add(sb.ToString()); sb.Clear();
                sb.Append(ch);
            }
            else
            {
                sb.Append(ch);
            }
        }
        if (sb.Length > 0) tokens.Add(sb.ToString());

        return tokens;
    }

    // ════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════

    private ComplexCurve<T> Rebuild(Func<Point<T>, Point<T>> transform)
    {
        if (_data.IsEmpty) return this;
        var builder = new ComplexCurveBuilder<T>(_data.Length);
        foreach (var seg in this)
        {
            if (seg.IsBezier)
            {
                var b = seg.AsBezier();
                builder.AddBezier(new BezierCurve<T>(
                    transform(b.Start), transform(b.C0),
                    transform(b.C1), transform(b.End)));
            }
            else
            {
                var pts = seg.AsPoints();
                var arr = new Point<T>[pts.Length];
                for (int i = 0; i < pts.Length; i++)
                    arr[i] = transform(pts[i]);
                builder.AddPoints(arr);
            }
        }
        return builder.Build();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddHits(List<Point<T>> results, ReadOnlyMemory<Point<T>> hits)
    {
        var span = hits.Span;
        for (int i = 0; i < span.Length; i++)
            results.Add(span[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlyMemory<Point<T>> ToMemory(List<Point<T>> results)
    {
        if (results.Count == 0) return ReadOnlyMemory<Point<T>>.Empty;
        var mem = Alloc.Memory<Point<T>>(results.Count);
        results.CopyTo(mem.Span);
        return mem;
    }

    // ════════════════════════════════════════════════
    //  Nested types
    // ════════════════════════════════════════════════

    /// <summary>
    /// Zero-copy view into one segment of the packed buffer.
    /// </summary>
    public readonly ref struct CurveSegment
    {
        private readonly ReadOnlySpan<byte> _payload;
        private readonly byte _tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CurveSegment(byte tag, ReadOnlySpan<byte> payload)
        {
            _tag = tag;
            _payload = payload;
        }

        /// <summary>True if this segment is a cubic Bezier curve.</summary>
        public bool IsBezier => _tag == TagBezier;

        /// <summary>True if this segment is a polyline (sequence of straight points).</summary>
        public bool IsPolyline => _tag == TagPolyline;

        /// <summary>
        /// Decodes this segment as a BezierCurve. Only valid when <see cref="IsBezier"/> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BezierCurve<T> AsBezier()
            => MemoryMarshal.Read<BezierCurve<T>>(_payload);

        /// <summary>
        /// Decodes this segment as a span of points. Only valid when <see cref="IsPolyline"/> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<Point<T>> AsPoints()
        {
            var count = MemoryMarshal.Read<ushort>(_payload);
            return MemoryMarshal.Cast<byte, Point<T>>(
                _payload.Slice(sizeof(ushort), count * Unsafe.SizeOf<Point<T>>()));
        }

        /// <summary>
        /// Gets the number of points: 4 for Bezier, N for polyline.
        /// </summary>
        public int PointCount => IsBezier ? 4 : MemoryMarshal.Read<ushort>(_payload);
    }

    /// <summary>
    /// Ref struct enumerator that walks the packed byte buffer, decoding segments on the fly.
    /// Supports duck-typed foreach (no IEnumerable — ref structs can't implement interfaces).
    /// </summary>
    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _position;
        private CurveSegment _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<byte> data)
        {
            _data = data;
            _position = 0;
            _current = default;
        }

        /// <summary>Gets the current segment.</summary>
        public CurveSegment Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        /// <summary>Advances to the next segment in the buffer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_position >= _data.Length) return false;

            var tag = _data[_position];
            var payload = _data.Slice(_position + 1);
            int payloadSize;

            if (tag == TagBezier)
            {
                payloadSize = Unsafe.SizeOf<BezierCurve<T>>();
            }
            else
            {
                var count = MemoryMarshal.Read<ushort>(payload);
                payloadSize = sizeof(ushort) + count * Unsafe.SizeOf<Point<T>>();
            }

            _current = new CurveSegment(tag, payload.Slice(0, payloadSize));
            _position += 1 + payloadSize;
            return true;
        }
    }
}

/// <summary>
/// Mutable builder for constructing <see cref="ComplexCurve{T}"/> instances.
/// Writes segments into a packed byte buffer.
/// </summary>
public sealed class ComplexCurveBuilder<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>,
              IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    private byte[] _buffer;
    private int _length;
    private int _segmentCount;

    public ComplexCurveBuilder(int initialCapacity = 256)
    {
        _buffer = new byte[initialCapacity];
        _length = 0;
        _segmentCount = 0;
    }

    /// <summary>Appends a cubic Bezier curve segment.</summary>
    public ComplexCurveBuilder<T> AddBezier(in BezierCurve<T> curve)
    {
        var curveSize = Unsafe.SizeOf<BezierCurve<T>>();
        var totalSize = 1 + curveSize;
        EnsureCapacity(totalSize);

        _buffer[_length] = ComplexCurve<T>.TagBezier;
        Unsafe.WriteUnaligned(ref _buffer[_length + 1], curve);
        _length += totalSize;
        _segmentCount++;
        return this;
    }

    /// <summary>Appends a polyline segment from a span of points.</summary>
    public ComplexCurveBuilder<T> AddPoints(ReadOnlySpan<Point<T>> points)
    {
        var pointBytes = Unsafe.SizeOf<Point<T>>() * points.Length;
        var totalSize = 1 + sizeof(ushort) + pointBytes;
        EnsureCapacity(totalSize);

        _buffer[_length] = ComplexCurve<T>.TagPolyline;
        var count = (ushort)points.Length;
        Unsafe.WriteUnaligned(ref _buffer[_length + 1], count);
        MemoryMarshal.AsBytes(points).CopyTo(_buffer.AsSpan(_length + 1 + sizeof(ushort)));
        _length += totalSize;
        _segmentCount++;
        return this;
    }

    /// <summary>Appends a line segment as a 2-point polyline.</summary>
    public ComplexCurveBuilder<T> AddSegment(in Segment<T> segment)
    {
        ReadOnlySpan<Point<T>> pts = [segment.Start, segment.End];
        return AddPoints(pts);
    }

    /// <summary>
    /// Builds an immutable <see cref="ComplexCurve{T}"/> from the accumulated segments.
    /// Uses <see cref="Alloc"/> to respect active <see cref="AllocationScope"/>.
    /// </summary>
    public ComplexCurve<T> Build()
    {
        var mem = Alloc.Memory<byte>(_length);
        _buffer.AsSpan(0, _length).CopyTo(mem.Span);
        return new ComplexCurve<T>(mem, _segmentCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int additionalBytes)
    {
        var required = _length + additionalBytes;
        if (required <= _buffer.Length) return;
        var newSize = _buffer.Length;
        while (newSize < required) newSize *= 2;
        Array.Resize(ref _buffer, newSize);
    }
}
