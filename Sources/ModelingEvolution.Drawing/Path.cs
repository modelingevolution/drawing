using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using ModelingEvolution.Drawing.Svg;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a path composed of multiple Bezier curve segments.
/// </summary>
/// <typeparam name="T">The numeric type for the coordinates.</typeparam>
[SvgExporter(typeof(PathSvgExporterFactory))]
public readonly record struct Path<T> : IParsable<Path<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Constants used throughout the Path implementation.
    /// </summary>
    private static class Constants
    {
        /// <summary>Default smoothing coefficient for curve generation (0.5).</summary>
        public static readonly T DefaultSmoothingCoefficient = T.One / (T.One + T.One);

        /// <summary>Default tolerance for point comparison (0.0001).</summary>
        public static readonly T DefaultTolerance = T.CreateTruncating(0.0001);

        /// <summary>Default number of samples per Bezier segment.</summary>
        public const int DefaultSamplesPerSegment = 20;

        /// <summary>Minimum samples per segment allowed.</summary>
        public const int MinSamplesPerSegment = 2;
    }

    private readonly ImmutableList<BezierCurve<T>> _segments;

    /// <summary>
    /// Gets the segments of the path.
    /// </summary>
    public ImmutableList<BezierCurve<T>> Segments => _segments ?? ImmutableList<BezierCurve<T>>.Empty;

    /// <summary>
    /// Gets a value indicating whether the path is empty.
    /// </summary>
    public bool IsEmpty => _segments?.Count == 0 || _segments == null;

    /// <summary>
    /// Gets the number of segments in the path.
    /// </summary>
    public int Count => _segments?.Count ?? 0;

    /// <summary>
    /// Initializes a new instance of the Path struct with the specified segments.
    /// </summary>
    /// <param name="segments">The Bezier curve segments that form the path.</param>
    public Path(ImmutableList<BezierCurve<T>> segments)
    {
        _segments = segments ?? ImmutableList<BezierCurve<T>>.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the Path struct with the specified segments.
    /// </summary>
    /// <param name="segments">The Bezier curve segments that form the path.</param>
    public Path(IEnumerable<BezierCurve<T>> segments)
    {
        _segments = segments?.ToImmutableList() ?? ImmutableList<BezierCurve<T>>.Empty;
    }

    /// <summary>
    /// Creates a path from a collection of Bezier curve segments.
    /// </summary>
    /// <param name="segments">The Bezier curve segments.</param>
    /// <returns>A new path containing the specified segments.</returns>
    public static Path<T> FromSegments(IEnumerable<BezierCurve<T>> segments)
    {
        return new Path<T>(segments);
    }

    /// <summary>
    /// Creates a path from a collection of Bezier curve segments.
    /// </summary>
    /// <param name="segments">The Bezier curve segments.</param>
    /// <returns>A new path containing the specified segments.</returns>
    public static Path<T> FromSegments(params BezierCurve<T>[] segments)
    {
        return new Path<T>(segments);
    }

    /// <summary>
    /// Creates a smooth path from a collection of points using default smoothing coefficient.
    /// </summary>
    /// <param name="points">The points to create the path through.</param>
    /// <returns>A new path passing through the specified points.</returns>
    public static Path<T> FromPoints(IEnumerable<Point<T>> points)
    {
        var pointList = points as IReadOnlyList<Point<T>> ?? points.ToList();
        return FromPoints(pointList, Constants.DefaultSmoothingCoefficient);
    }

    /// <summary>
    /// Creates a smooth path from a collection of points with a specified smoothing coefficient.
    /// </summary>
    /// <param name="points">The points to create the path through.</param>
    /// <param name="smoothingCoefficient">The coefficient controlling the curve's smoothness (0 to 1).</param>
    /// <returns>A new path passing through the specified points.</returns>
    public static Path<T> FromPoints(IEnumerable<Point<T>> points, T smoothingCoefficient)
    {
        var pointList = points as IReadOnlyList<Point<T>> ?? points.ToList();
        if (pointList.Count < 2)
        {
            return new Path<T>(ImmutableList<BezierCurve<T>>.Empty);
        }

        var curves = BezierCurve<T>.Create(pointList, smoothingCoefficient);
        return new Path<T>(curves);
    }

    /// <summary>
    /// Creates a smooth path from points using default smoothing.
    /// </summary>
    /// <param name="points">The points to create the path through.</param>
    /// <returns>A new path passing through the specified points.</returns>
    public static Path<T> FromPoints(params Point<T>[] points)
    {
        return FromPoints(points.AsEnumerable());
    }

    /// <summary>
    /// Calculates the bounding box of the path.
    /// </summary>
    /// <returns>The smallest rectangle that contains all points of the path.</returns>
    public Rectangle<T> BoundingBox()
    {
        if (IsEmpty)
        {
            return new Rectangle<T>(Point<T>.Zero, new Size<T>(T.Zero, T.Zero));
        }

        var firstSegment = Segments[0];
        var minX = firstSegment.Start.X;
        var minY = firstSegment.Start.Y;
        var maxX = minX;
        var maxY = minY;

        foreach (var segment in Segments)
        {
            UpdateBounds(segment.Start, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(segment.C0, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(segment.C1, ref minX, ref minY, ref maxX, ref maxY);
            UpdateBounds(segment.End, ref minX, ref minY, ref maxX, ref maxY);

            // Also include extremum points for more accurate bounds if available
            if (TryGetExtremumPoints(segment, out var extremumPoints))
            {
                foreach (var point in extremumPoints)
                {
                    UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
        }

        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Updates the bounding box coordinates with a point.
    /// </summary>
    private static void UpdateBounds(Point<T> point, ref T minX, ref T minY, ref T maxX, ref T maxY)
    {
        if (point.X < minX) minX = point.X;
        if (point.Y < minY) minY = point.Y;
        if (point.X > maxX) maxX = point.X;
        if (point.Y > maxY) maxY = point.Y;
    }

    /// <summary>
    /// Tries to get extremum points for a Bezier segment.
    /// </summary>
    private static bool TryGetExtremumPoints(BezierCurve<T> segment, out Point<T>[] extremumPoints)
    {
        try
        {
            extremumPoints = segment.CalculateExtremumPoints();
            return true;
        }
        catch
        {
            extremumPoints = Array.Empty<Point<T>>();
            return false;
        }
    }

    /// <summary>
    /// Translates the path by a vector.
    /// </summary>
    /// <param name="path">The path to translate.</param>
    /// <param name="vector">The translation vector.</param>
    /// <returns>A new translated path.</returns>
    public static Path<T> operator +(Path<T> path, Vector<T> vector)
    {
        if (path.IsEmpty)
            return path;

        var translatedSegments = path.Segments.Select(segment => segment + vector);
        return new Path<T>(translatedSegments);
    }

    /// <summary>
    /// Translates the path by subtracting a vector.
    /// </summary>
    /// <param name="path">The path to translate.</param>
    /// <param name="vector">The translation vector to subtract.</param>
    /// <returns>A new translated path.</returns>
    public static Path<T> operator -(Path<T> path, Vector<T> vector)
    {
        return path + (-vector);
    }

    /// <summary>
    /// Translates the path to a new position.
    /// </summary>
    /// <param name="path">The path to translate.</param>
    /// <param name="point">The point to translate by.</param>
    /// <returns>A new translated path.</returns>
    public static Path<T> operator +(Path<T> path, Point<T> point)
    {
        return path + new Vector<T>(point.X, point.Y);
    }

    /// <summary>
    /// Scales the path by a size factor.
    /// </summary>
    /// <param name="path">The path to scale.</param>
    /// <param name="size">The size to scale by.</param>
    /// <returns>A new scaled path.</returns>
    public static Path<T> operator *(Path<T> path, Size<T> size)
    {
        if (path.IsEmpty)
            return path;

        var scaledSegments = path.Segments.Select(segment =>
            new BezierCurve<T>(
                segment.Start * size,
                segment.C0 * size,
                segment.C1 * size,
                segment.End * size
            )
        );
        return new Path<T>(scaledSegments);
    }

    /// <summary>
    /// Scales the path by dividing by a size factor.
    /// </summary>
    /// <param name="path">The path to scale.</param>
    /// <param name="size">The size to divide by.</param>
    /// <returns>A new scaled path.</returns>
    public static Path<T> operator /(Path<T> path, Size<T> size)
    {
        if (path.IsEmpty)
            return path;

        var scaledSegments = path.Segments.Select(segment =>
            new BezierCurve<T>(
                segment.Start / size,
                segment.C0 / size,
                segment.C1 / size,
                segment.End / size
            )
        );
        return new Path<T>(scaledSegments);
    }

    /// <summary>
    /// Transforms the path by a transformation matrix.
    /// </summary>
    /// <param name="matrix">The transformation matrix to apply.</param>
    /// <returns>A new transformed path.</returns>
    public Path<T> Transform(Matrix<T> matrix)
    {
        if (IsEmpty)
            return this;

        var transformedSegments = Segments.Select(segment => segment.TransformBy(matrix));
        return new Path<T>(transformedSegments);
    }

    /// <summary>
    /// Reverses the direction of the path.
    /// </summary>
    /// <returns>A new path with segments in reverse order and direction.</returns>
    public Path<T> Reverse()
    {
        if (IsEmpty)
            return this;

        var reversedSegments = Segments
            .Reverse()
            .Select(segment => new BezierCurve<T>(segment.End, segment.C1, segment.C0, segment.Start));

        return new Path<T>(reversedSegments);
    }

    /// <summary>
    /// Appends another path to this path.
    /// </summary>
    /// <param name="other">The path to append.</param>
    /// <returns>A new path containing segments from both paths.</returns>
    public Path<T> Append(Path<T> other)
    {
        if (IsEmpty)
            return other;
        if (other.IsEmpty)
            return this;

        var combinedSegments = Segments.Concat(other.Segments);
        return new Path<T>(combinedSegments);
    }

    /// <summary>
    /// Concatenates two paths.
    /// </summary>
    /// <param name="first">The first path.</param>
    /// <param name="second">The second path to append.</param>
    /// <returns>A new path containing segments from both paths.</returns>
    public static Path<T> operator +(Path<T> first, Path<T> second)
    {
        return first.Append(second);
    }

    /// <summary>
    /// Converts the path to a polygon by sampling points along the Bezier curves and closing the path.
    /// </summary>
    /// <param name="samplesPerSegment">The number of sample points per Bezier segment (default: 20).</param>
    /// <returns>A closed polygon created from the sampled path points.</returns>
    public Polygon<T> Close(int samplesPerSegment = Constants.DefaultSamplesPerSegment)
    {
        if (IsEmpty)
        {
            return new Polygon<T>(new List<Point<T>>());
        }

        var points = SamplePoints(samplesPerSegment);

        // Ensure the path is closed by checking if first and last points are different
        if (points.Count > 0)
        {
            var first = points[0];
            var last = points[^1];

            // If the path isn't already closed (last point != first point), don't add duplicate
            if (T.Abs(first.X - last.X) > Constants.DefaultTolerance || T.Abs(first.Y - last.Y) > Constants.DefaultTolerance)
            {
                // Polygon will automatically close the shape
            }
        }

        return new Polygon<T>(points);
    }

    /// <summary>
    /// Converts the path to a polygon by sampling points along the Bezier curves without closing.
    /// </summary>
    /// <param name="samplesPerSegment">The number of sample points per Bezier segment (default: 20).</param>
    /// <returns>An open polygon created from the sampled path points.</returns>
    public Polygon<T> ToPolygon(int samplesPerSegment = Constants.DefaultSamplesPerSegment)
    {
        if (IsEmpty)
        {
            return new Polygon<T>(new List<Point<T>>());
        }

        var points = SamplePoints(samplesPerSegment);
        return new Polygon<T>(points);
    }

    /// <summary>
    /// Samples points along all segments of the path.
    /// </summary>
    /// <param name="samplesPerSegment">The number of sample points per Bezier segment.</param>
    /// <returns>A list of sampled points along the path.</returns>
    private List<Point<T>> SamplePoints(int samplesPerSegment)
    {
        if (samplesPerSegment < Constants.MinSamplesPerSegment)
        {
            throw new ArgumentException($"Samples per segment must be at least {Constants.MinSamplesPerSegment}.", nameof(samplesPerSegment));
        }

        var points = new List<Point<T>>();

        foreach (var segment in Segments)
        {
            // Sample points along the Bezier curve
            for (int i = 0; i < samplesPerSegment; i++)
            {
                var t = T.CreateTruncating(i) / T.CreateTruncating(samplesPerSegment - 1);
                var point = segment.Evaluate(t);

                // Avoid adding duplicate points at segment connections
                if (points.Count == 0 || !IsApproximatelyEqual(points[^1], point))
                {
                    points.Add(point);
                }
            }
        }

        return points;
    }

    /// <summary>
    /// Checks if two points are approximately equal within a small tolerance.
    /// </summary>
    /// <param name="p1">The first point.</param>
    /// <param name="p2">The second point.</param>
    /// <returns>True if the points are approximately equal; otherwise, false.</returns>
    private static bool IsApproximatelyEqual(Point<T> p1, Point<T> p2)
    {
        return T.Abs(p1.X - p2.X) < Constants.DefaultTolerance && T.Abs(p1.Y - p2.Y) < Constants.DefaultTolerance;
    }

    /// <summary>
    /// Converts the path to an SVG path data string.
    /// </summary>
    /// <returns>A string that can be used as the 'd' attribute of an SVG path element.</returns>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        bool isFirst = true;

        foreach (var segment in Segments)
        {
            if (isFirst)
            {
                // Move to the start point of the first segment
                sb.Append($"M {FormatNumber(segment.Start.X)} {FormatNumber(segment.Start.Y)}");
                isFirst = false;
            }

            // Add cubic Bezier curve command
            sb.Append($" C {FormatNumber(segment.C0.X)} {FormatNumber(segment.C0.Y)},");
            sb.Append($" {FormatNumber(segment.C1.X)} {FormatNumber(segment.C1.Y)},");
            sb.Append($" {FormatNumber(segment.End.X)} {FormatNumber(segment.End.Y)}");
        }

        return sb.ToString();
    }

    private static string FormatNumber(T value)
    {
        // Format with invariant culture to ensure consistent decimal separator
        return value.ToString("G", CultureInfo.InvariantCulture) ?? "0";
    }

    /// <summary>
    /// Parses an SVG path data string into a Path.
    /// </summary>
    /// <param name="s">The SVG path data string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <returns>A Path parsed from the string.</returns>
    public static Path<T> Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
        {
            return result;
        }
        throw new FormatException($"Unable to parse SVG path data: '{s}'. Supported commands: M (move), C (cubic Bezier), L (line), Z (close).");
    }

    /// <summary>
    /// Tries to parse an SVG path data string into a Path.
    /// </summary>
    /// <param name="s">The SVG path data string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <param name="result">When this method returns, contains the parsed Path if successful.</param>
    /// <returns>true if the parse was successful; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Path<T> result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
        {
            result = new Path<T>(ImmutableList<BezierCurve<T>>.Empty);
            return true;
        }

        var segments = new List<BezierCurve<T>>();
        var currentPos = Point<T>.Zero;
        var startPos = Point<T>.Zero;

        try
        {
            var tokens = TokenizePath(s);
            var i = 0;

            // Pre-calculate common fractions used in Bezier conversions
            var oneThird = T.One / T.CreateTruncating(3);
            var twoThirds = T.CreateTruncating(2) / T.CreateTruncating(3);

            while (i < tokens.Count)
            {
                var command = tokens[i++];

                switch (command.ToUpperInvariant())
                {
                    case "M": // Move to
                        currentPos = ParsePoint(tokens, ref i, provider);
                        startPos = currentPos;
                        break;

                    case "C": // Cubic Bezier
                        var c0 = ParsePoint(tokens, ref i, provider);
                        var c1 = ParsePoint(tokens, ref i, provider);
                        var end = ParsePoint(tokens, ref i, provider);
                        segments.Add(new BezierCurve<T>(currentPos, c0, c1, end));
                        currentPos = end;
                        break;

                    case "L": // Line to (convert to Bezier)
                        var lineTo = ParsePoint(tokens, ref i, provider);
                        // Convert line to cubic Bezier with control points at 1/3 and 2/3
                        var delta = lineTo - currentPos;

                        // Check for zero-length line
                        if (T.Abs(delta.X) < Constants.DefaultTolerance && T.Abs(delta.Y) < Constants.DefaultTolerance)
                        {
                            // Skip zero-length segments
                            currentPos = lineTo;
                            break;
                        }

                        var cp1 = currentPos + delta * oneThird;
                        var cp2 = currentPos + delta * twoThirds;
                        segments.Add(new BezierCurve<T>(currentPos, cp1, cp2, lineTo));
                        currentPos = lineTo;
                        break;

                    case "Z": // Close path
                        var distanceToStart = startPos - currentPos;
                        if (T.Abs(distanceToStart.X) > Constants.DefaultTolerance || T.Abs(distanceToStart.Y) > Constants.DefaultTolerance)
                        {
                            var closeDelta = startPos - currentPos;
                            var closeCP1 = currentPos + closeDelta * oneThird;
                            var closeCP2 = currentPos + closeDelta * twoThirds;
                            segments.Add(new BezierCurve<T>(currentPos, closeCP1, closeCP2, startPos));
                            currentPos = startPos;
                        }
                        break;

                    default:
                        // Skip unsupported commands
                        SkipCommand(tokens, ref i, command);
                        break;
                }
            }

            result = new Path<T>(segments);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> TokenizePath(string pathData)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (char c in pathData)
        {
            if (char.IsLetter(c) || c == ',' || char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                if (char.IsLetter(c))
                {
                    tokens.Add(c.ToString());
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static Point<T> ParsePoint(List<string> tokens, ref int index, IFormatProvider? provider)
    {
        if (index >= tokens.Count)
        {
            throw new FormatException($"Expected X coordinate at position {index} but reached end of path data.");
        }

        if (index + 1 >= tokens.Count)
        {
            throw new FormatException($"Expected Y coordinate at position {index + 1} but reached end of path data.");
        }

        var xStr = tokens[index++];
        var yStr = tokens[index++];

        if (!T.TryParse(xStr, NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var x))
        {
            throw new FormatException($"Invalid X coordinate value: '{xStr}'");
        }

        if (!T.TryParse(yStr, NumberStyles.Float, provider ?? CultureInfo.InvariantCulture, out var y))
        {
            throw new FormatException($"Invalid Y coordinate value: '{yStr}'");
        }

        return new Point<T>(x, y);
    }

    private static void SkipCommand(List<string> tokens, ref int index, string command)
    {
        // Skip parameters for unsupported commands
        // This is a simplified version - a full implementation would need to know
        // the parameter count for each SVG command
        while (index < tokens.Count && !char.IsLetter(tokens[index][0]))
        {
            index++;
        }
    }
}