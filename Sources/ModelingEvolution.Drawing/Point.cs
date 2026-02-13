using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a point in 2D space with generic numeric coordinates.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
[ProtoContract]
[PointJsonConverter()]
[Svg.SvgExporter(typeof(PointSvgExporterFactory))]
public struct Point<T> : IEquatable<Point<T>>, IParsable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Represents a point with coordinates (0, 0).
    /// </summary>
    public static readonly Point<T> Zero = new Point<T>(T.Zero, T.Zero);
    /// <summary>
    /// Generates a random point with coordinates between 0 and 1.
    /// </summary>
    /// <returns>A random point with coordinates in the range [0, 1].</returns>
    public static Point<T> Random()
    {
        T width = T.One;
        T height = T.One;
        return Random(width, height);
    }
    /// <summary>
    /// Explicitly converts a generic Point to a System.Drawing.Point.
    /// </summary>
    /// <param name="point">The point to convert.</param>
    /// <returns>A System.Drawing.Point with integer coordinates.</returns>
    public static explicit operator System.Drawing.Point(Point<T> point)
    {
        
        return new System.Drawing.Point(Convert.ToInt32(point.X), Convert.ToInt32(point.Y));
    }
    
    /// <summary>
    /// Implicitly converts a System.Drawing.Point to a generic Point.
    /// </summary>
    /// <param name="point">The System.Drawing.Point to convert.</param>
    /// <returns>A generic Point with the same coordinates.</returns>
    public static implicit operator Point<T>(System.Drawing.Point point)
    {
        return new Point<T>(T.CreateTruncating(point.X), T.CreateTruncating(point.Y));
    }

    /// <summary>
    /// Implicitly converts a tuple to a Point.
    /// </summary>
    /// <param name="tuple">The tuple containing X and Y coordinates.</param>
    /// <returns>A Point with the specified coordinates.</returns>
    public static implicit operator Point<T>((T x, T y) tuple)
    {
        return new Point<T>(tuple.x, tuple.y);
    }

    /// <summary>
    /// Implicitly converts a Point to a tuple.
    /// </summary>
    /// <param name="point">The Point to convert.</param>
    /// <returns>A tuple containing the X and Y coordinates.</returns>
    public static implicit operator (T x, T y)(Point<T> point)
    {
        return (point.x, point.y);
    }
    /// <summary>
    /// Generates a random point within the specified dimensions.
    /// </summary>
    /// <param name="width">The maximum width for the random point.</param>
    /// <param name="height">The maximum height for the random point.</param>
    /// <returns>A random point with coordinates in the range [0, width] x [0, height].</returns>
    public static Point<T> Random(T width, T height)
    {
        var t1 = T.CreateTruncating(System.Random.Shared.NextDouble());
        var t2 = T.CreateTruncating(System.Random.Shared.NextDouble());
        return new Point<T>(t1 * width, t2* height);
    }

    /// <summary>
    /// Clamps this point to be within the specified rectangular area.
    /// </summary>
    /// <param name="area">The rectangular area to clamp to.</param>
    /// <returns>A point that is guaranteed to be within the specified area.</returns>
    public Point<T> Clamp(Rectangle<T> area)
    {
        var clampedX = T.Max(area.X, T.Min(area.Right, x));
        var clampedY = T.Max(area.Y, T.Min(area.Bottom, y));
        return new Point<T>(clampedX, clampedY);
    }
    private static readonly T Two = T.CreateTruncating(2);
    /// <summary>
    /// Calculates the midpoint between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The point that is halfway between the two specified points.</returns>
    public static Point<T> Middle(Point<T> a, Point<T> b)
    {
        return new Point<T>((a.x + b.x) / Two, (a.y + b.y) / Two);
    }
    
    /// <summary>
    /// Transforms a point using the specified matrix.
    /// </summary>
    /// <param name="point">The point to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed point.</returns>
    public static Point<T> Multiply(Point<T> point, Matrix<T> matrix)
    {
        return new Point<T>(point.X * matrix.M11 + point.Y * matrix.M21 + matrix.OffsetX,
            point.X * matrix.M12 + point.Y * matrix.M22 + matrix.OffsetY);
    }
    /// <summary>
    /// Tries to parse a string representation of a point.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="p">An optional format provider.</param>
    /// <param name="result">When this method returns, contains the parsed point if successful; otherwise, Point.Zero.</param>
    /// <returns>true if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? source, IFormatProvider? p,out Point<T> result)
    {
        if (source == null)
        {
            result = Point<T>.Zero;
            return false;
        }
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        
        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || 
            !tokenizer.HasNoMoreTokens())
        {
            result = Point<T>.Zero;
            return false;
        }
        result = new Point<T>(x, y);
        return true;
    }
    /// <summary>
    /// Parses a string representation of a point.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <param name="p">An optional format provider.</param>
    /// <returns>The parsed point.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the string contains invalid point data.</exception>
    public static Point<T> Parse(string source, IFormatProvider? p = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            throw new FormatException(string.Format("Invalid Point<T> format: {0}", source));
        
        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException("Invalid Point<T> format: " + source);
        
        return new Point<T>(x, y);
    }
    /// <summary>
    /// Generates a random point within the specified size dimensions.
    /// </summary>
    /// <param name="size">The size defining the maximum dimensions for the random point.</param>
    /// <returns>A random point with coordinates within the specified size.</returns>
    public static Point<T> Random(System.Drawing.SizeF size) => Random(T.CreateTruncating(size.Width), T.CreateTruncating(size.Height));
    
    private T x; // Do not rename (binary serialization)
    private T y; // Do not rename (binary serialization)
    
    /// <summary>
    /// Initializes a new instance of the Point struct with the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point(T x, T y)
    {
        this.x = x;
        this.y = y;
    }
    
    /// <summary>
    /// Initializes a new instance of the Point struct from a Vector2.
    /// </summary>
    /// <param name="vector">The Vector2 to convert to a point.</param>
    public Point(Vector2 vector)
    {
        x = T.CreateTruncating(vector.X);
        y = T.CreateTruncating(vector.Y);
    }


    /// <summary>
    /// Converts this point to a vector of a different numeric type by truncating the coordinates.
    /// </summary>
    /// <typeparam name="U">The target numeric type.</typeparam>
    /// <returns>A vector with the truncated coordinates in the target type.</returns>
    public Vector<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>,
        IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return  new Vector<U>(U.CreateTruncating( x),U.CreateTruncating(  y));
    }
    

    /// <summary>
    /// Implicitly converts a point to a vector.
    /// </summary>
    /// <param name="f">The point to convert.</param>
    /// <returns>A vector with the same coordinates as the point.</returns>
    public static implicit operator Vector<T>(Point<T> f)
    {
        return new Vector<T>(f.x, f.y);
    }

    /// <summary>
    /// Gets a value indicating whether this point is empty (both coordinates are zero).
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => x == T.Zero && y == T.Zero;


    /// <summary>
    /// Gets or sets the X coordinate of this point.
    /// </summary>
    [ProtoMember(1)]
    public T X
    {
        readonly get => x;
        set => x = value;
    }

    /// <summary>
    /// Gets or sets the Y coordinate of this point.
    /// </summary>
    [ProtoMember(2)]
    public T Y
    {
        readonly get => y;
        set => y = value;
    }

    
    /// <summary>
    /// Explicitly converts a Vector2 to a Point.
    /// </summary>
    /// <param name="vector">The Vector2 to convert.</param>
    /// <returns>A Point with coordinates from the vector.</returns>
    public static explicit operator Point<T>(Vector2 vector) => new Point<T>(vector);

    /// <summary>
    /// Multiplies a point by a size, scaling each coordinate.
    /// </summary>
    /// <param name="pt">The point to scale.</param>
    /// <param name="sz">The size to scale by.</param>
    /// <returns>A point with scaled coordinates.</returns>
    public static Point<T> operator *(Point<T> pt, ISize<T> sz) => new Point<T>(pt.X * sz.Width, pt.Y * sz.Height);
    /// <summary>
    /// Divides a point by a size, scaling each coordinate.
    /// </summary>
    /// <param name="pt">The point to scale.</param>
    /// <param name="sz">The size to divide by.</param>
    /// <returns>A point with scaled coordinates.</returns>
    public static Point<T> operator /(Point<T> pt, ISize<T> sz) => new Point<T>(pt.X / sz.Width, pt.Y / sz.Height);
    /// <summary>
    /// Adds a size to a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The size to add.</param>
    /// <returns>A point translated by the specified size.</returns>
    public static Point<T> operator +(Point<T> pt, ISize<T> sz) => Add(pt, sz);
    /// <summary>
    /// Adds a vector to a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The vector to add.</param>
    /// <returns>A point translated by the specified vector.</returns>
    public static Point<T> operator +(Point<T> pt, Vector<T> sz) => new Point<T>(pt.x + sz.X, pt.y + sz.Y);
    /// <summary>
    /// Subtracts a vector from a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The vector to subtract.</param>
    /// <returns>A point translated by the negative of the specified vector.</returns>
    public static Point<T> operator -(Point<T> pt, Vector<T> sz) => new Point<T>(pt.x - sz.X, pt.y - sz.Y);

    /// <summary>
    /// Subtracts a size from a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The size to subtract.</param>
    /// <returns>A point translated by the negative of the specified size.</returns>
    public static Point<T> operator -(Point<T> pt, ISize<T> sz) => Subtract(pt, sz);

    /// <summary>
    /// Subtracts one point from another, resulting in a vector.
    /// </summary>
    /// <param name="pt">The point to subtract from.</param>
    /// <param name="sz">The point to subtract.</param>
    /// <returns>A vector representing the difference between the two points.</returns>
    public static Vector<T> operator -(Point<T> pt, Point<T> sz) => new Vector<T>(pt.x - sz.x, pt.y -sz.y);

    /// <summary>
    /// Determines whether two points are equal.
    /// </summary>
    /// <param name="left">The first point to compare.</param>
    /// <param name="right">The second point to compare.</param>
    /// <returns>true if the points are equal; otherwise, false.</returns>
    public static bool operator ==(Point<T> left, Point<T> right) => left.X == right.X && left.Y == right.Y;

   
    /// <summary>
    /// Determines whether two points are not equal.
    /// </summary>
    /// <param name="left">The first point to compare.</param>
    /// <param name="right">The second point to compare.</param>
    /// <returns>true if the points are not equal; otherwise, false.</returns>
    public static bool operator !=(Point<T> left, Point<T> right) => !(left == right);

    
    /// <summary>
    /// Adds a size to a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The size to add.</param>
    /// <returns>A point translated by the specified size.</returns>
    public static Point<T> Add(Point<T> pt, ISize<T> sz) => new Point<T>(pt.X + sz.Width, pt.Y + sz.Height);

   
    /// <summary>
    /// Subtracts a size from a point, translating the point.
    /// </summary>
    /// <param name="pt">The point to translate.</param>
    /// <param name="sz">The size to subtract.</param>
    /// <returns>A point translated by the negative of the specified size.</returns>
    public static Point<T> Subtract(Point<T> pt, ISize<T> sz) => new Point<T>(pt.X -sz.Width, pt.Y - sz.Height);

    /// <summary>
    /// Determines whether the specified object is equal to this point.
    /// </summary>
    /// <param name="obj">The object to compare with this point.</param>
    /// <returns>true if the specified object is equal to this point; otherwise, false.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Point<T> && Equals((Point<T>)obj);

    /// <summary>
    /// Determines whether the specified point is equal to this point.
    /// </summary>
    /// <param name="other">The point to compare with this point.</param>
    /// <returns>true if the specified point is equal to this point; otherwise, false.</returns>
    public readonly bool Equals(Point<T> other) => this == other;

    /// <summary>
    /// Returns the hash code for this point.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode());

    /// <summary>
    /// Rotates this point around the specified origin by the given angle.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="origin">The center of rotation. Defaults to the origin (0, 0).</param>
    /// <returns>The rotated point.</returns>
    public readonly Point<T> Rotate(Degree<T> angle, Point<T> origin = default)
    {
        var rad = (Radian<T>)angle;
        var cos = T.Cos((T)rad);
        var sin = T.Sin((T)rad);
        var dx = x - origin.X;
        var dy = y - origin.Y;
        return new Point<T>(
            cos * dx - sin * dy + origin.X,
            sin * dx + cos * dy + origin.Y);
    }

    /// <summary>
    /// Returns a string representation of this point.
    /// </summary>
    /// <returns>A string representation of the point in the format {X=value, Y=value}.</returns>
    /// <summary>
    /// Computes the Euclidean distance from this point to another.
    /// </summary>
    public readonly T DistanceTo(Point<T> other)
    {
        var dx = other.x - x;
        var dy = other.y - y;
        return T.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Linearly interpolates between this point and another.
    /// </summary>
    public readonly Point<T> Lerp(Point<T> other, T t) =>
        new Point<T>(x + (other.x - x) * t, y + (other.y - y) * t);

    /// <summary>
    /// Reflects this point across the specified center point.
    /// </summary>
    public readonly Point<T> Reflect(Point<T> center) =>
        new Point<T>(center.x + center.x - x, center.y + center.y - y);

    public override readonly string ToString() => $"{{X={x}, Y={y}}}";
}