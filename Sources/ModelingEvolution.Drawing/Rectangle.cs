using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a rectangle defined by its location and size, with generic numeric type support.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and dimensions.</typeparam>
[RectangleJsonConverterAttribute]
[ProtoContract]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Rectangle<T> : IEquatable<Rectangle<T>>, IParsable<Rectangle<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Represents a Rectangle with all properties set to zero.
    /// </summary>
    public static readonly Rectangle<T> Empty;

    [ProtoMember(1)]
    private T x; // Do not rename (binary serialization)
    [ProtoMember(2)]
    private T y; // D3 not rename (binary serialization)
    [ProtoMember(3)]
    private T width; // Do not rename (binary serialization)
    [ProtoMember(4)]
    private T height; // Do not rename (binary serialization)

    /// <summary>
    /// Initializes a new instance of the Rectangle struct with the specified location and size.
    /// </summary>
    /// <param name="x">The x-coordinate of the upper-left corner of the rectangle.</param>
    /// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rectangle(T x, T y, T width, T height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Gets the four corner points of this rectangle.
    /// </summary>
    /// <returns>An enumerable of the four corner points (top-left, top-right, bottom-left, bottom-right).</returns>
    public IEnumerable<Point<T>> Points()
    {
        yield return new Point<T>(x, y);
        yield return new Point<T>(x + width, y);
        yield return new Point<T>(x, y + height);
        yield return new Point<T>(x + width, y + height);
    }
    /// <summary>
    /// Initializes a new instance of the Rectangle struct with the specified location and size.
    /// </summary>
    /// <param name="location">The location of the upper-left corner of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    public Rectangle(Point<T> location, Size<T> size)
    {
        x = location.X;
        y = location.Y;
        width = size.Width;
        height = size.Height;
    }
    
    /// <summary>
    /// Implicitly converts a generic Rectangle to a System.Drawing.Rectangle.
    /// </summary>
    /// <param name="rectangle">The rectangle to convert.</param>
    /// <returns>A System.Drawing.Rectangle with integer coordinates.</returns>
    public static implicit operator System.Drawing.Rectangle(Rectangle<T> rectangle)
    {
        return new System.Drawing.Rectangle(
            Convert.ToInt32(rectangle.X),
            Convert.ToInt32(rectangle.Y),
            Convert.ToInt32(rectangle.Width),
            Convert.ToInt32(rectangle.Height)
        );
    }
    /// <summary>
    /// Creates a new rectangle with the same size but moved to the specified vector position.
    /// </summary>
    /// <param name="point">The new position for the rectangle.</param>
    /// <returns>A new rectangle with the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<T> MoveTo(Vector<T> point) => MoveTo((Point<T>)point);

    /// <summary>
    /// Creates a new rectangle with the same size but moved to the specified point.
    /// </summary>
    /// <param name="point">The new position for the rectangle.</param>
    /// <returns>A new rectangle with the specified position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<T> MoveTo(Point<T> point) => new(point, Size);

    /// <summary>
    /// Initializes a new instance of the Rectangle struct from the specified Vector4.
    /// </summary>
    /// <param name="vector">The Vector4 containing X, Y, Width, and Height values.</param>
    public Rectangle(Vector4 vector)
    {
        x =  T.CreateTruncating(vector.X);
        y = T.CreateTruncating(vector.Y);
        width = T.CreateTruncating(vector.Z);
        height = T.CreateTruncating(vector.W);
    }

    /// <summary>
    /// Divides this rectangle into tiles of the specified size, with optional overlap.
    /// </summary>
    /// <param name="tileSize">The size of each tile.</param>
    /// <param name="overlapPercentage">The percentage of overlap between adjacent tiles (0-1).</param>
    /// <returns>An array of rectangles representing the tiles.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when overlap percentage is not in range [0, 1).</exception>
    /// <exception cref="InvalidOperationException">Thrown when the tile count exceeds maximum allowable array size.</exception>
    public Rectangle<T>[] ComputeTiles( Size<T> tileSize, T overlapPercentage = default)
    {
        if (tileSize.Width <= T.Zero || tileSize.Height <= T.Zero || Width <= T.Zero || Height <= T.Zero)
            return Array.Empty<Rectangle<T>>();
        if (overlapPercentage < T.Zero || overlapPercentage >= T.One)
            throw new ArgumentOutOfRangeException(nameof(overlapPercentage), "Overlap percentage must be in the range [0, 1).");

        // Calculate step sizes with overlap
        T stepX = tileSize.Width * (T.One - overlapPercentage);
        T stepY = tileSize.Height * (T.One - overlapPercentage);
        // Ensure we don't have zero or negative steps
        if (stepX <= T.Zero) stepX = tileSize.Width;
        if (stepY <= T.Zero) stepY = tileSize.Height;
        // Calculate number of tiles needed in each direction
        T numTilesX = T.Ceiling((Width - tileSize.Width) / stepX) + T.One;
        T numTilesY = T.Ceiling((Height - tileSize.Height) / stepY) + T.One;
        int totalTiles = int.CreateTruncating(numTilesX * numTilesY);
        if (numTilesX > T.CreateChecked(int.MaxValue) || numTilesY > T.CreateChecked(int.MaxValue))
            throw new InvalidOperationException("Tile count exceeds maximum allowable array size.");

        var result = new Rectangle<T>[totalTiles];
        // Generate tiles
        int index = 0;
        for (T y = T.Zero; y < numTilesY; y++)
        {
            for (T x = T.Zero; x < numTilesX; x++)
            {
                // Calculate tile position
                T tileX = X + x * stepX;
                T tileY = Y + y * stepY;
                // Adjust last tiles in each row/column to not exceed original rectangle
                if (tileX + tileSize.Width > Right)
                    tileX = Right - tileSize.Width;
                if (tileY + tileSize.Height > Bottom)
                    tileY = Bottom - tileSize.Height;
                // Create and add the tile
                result[index++] = new Rectangle<T>(tileX, tileY, tileSize.Width, tileSize.Height);
            }
        }
        return result;
    }

    
    /// <summary>
    /// Creates a new <see cref="System.Numerics.Vector4"/> from this <see cref="Rectangle{T}"/>.
    /// </summary>
    public Vector4 ToVector4() => new Vector4(float.CreateTruncating(x), float.CreateTruncating(y), float.CreateTruncating(width), float.CreateTruncating(height));

    /// <summary>
    /// Converts the specified <see cref="Rectangle{T}"/> to a <see cref="System.Numerics.Vector4"/>.
    /// </summary>
    public static explicit operator Vector4(Rectangle<T> rectangle) => rectangle.ToVector4();

    /// <summary>
    /// Converts the specified <see cref="System.Numerics.Vector2"/> to a <see cref="Rectangle{T}"/>.
    /// </summary>
    public static explicit operator Rectangle<T>(Vector4 vector) => new Rectangle<T>(vector);

    /// <summary>
    /// Creates a new  with the specified location and size.
    /// </summary>
    public static Rectangle<T> FromLTRB(T left, T top, T right, T bottom) =>
        new Rectangle<T>(left, top, right - left, bottom - top);

    /// <summary>
    /// Translates a rectangle by a vector.
    /// </summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to translate by.</param>
    /// <returns>The translated rectangle.</returns>
    public static Rectangle<T> operator +(Rectangle<T> rect, Vector<T> vector)
    {
        return new Rectangle<T>(rect.Location + vector, rect.Size);
    }
    /// <summary>
    /// Translates a rectangle by the negative of a vector.
    /// </summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to translate by (negated).</param>
    /// <returns>The translated rectangle.</returns>
    public static Rectangle<T> operator -(Rectangle<T> rect, Vector<T> vector)
    {
        return new Rectangle<T>(rect.Location - vector, rect.Size);
    }
    
    /// <summary>
    /// Gets or sets the coordinates of the upper-left corner of the rectangular region represented by this
    /// 
    /// </summary>
    [Browsable(false)]
    public Point<T> Location
    {
        readonly get => new Point<T>(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    /// Gets or sets the size of this 
    /// </summary>
    [Browsable(false)]
    public Size<T> Size
    {
        readonly get => new Size<T>(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    /// <summary>
    /// Gets or sets the x-coordinate of the upper-left corner of the rectangular region defined by this
    /// 
    /// </summary>
    public T X
    {
        readonly get => x;
        set => x = value;
    }

    /// <summary>
    /// Gets or sets the y-coordinate of the upper-left corner of the rectangular region defined by this
    /// 
    /// </summary>
    public T Y
    {
        readonly get => y;
        set => y = value;
    }

    /// <summary>
    /// Gets or sets the width of the rectangular region defined by this 
    /// </summary>
    public T Width
    {
        readonly get => width;
        set => width = value;
    }

    /// <summary>
    /// Gets or sets the height of the rectangular region defined by this 
    /// </summary>
    public T Height
    {
        readonly get => height;
        set => height = value;
    }

    /// <summary>
    /// Gets the x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle.
    /// </summary>
    [Browsable(false)]
    public readonly T Left => X;

    /// <summary>
    /// Gets the y-coordinate of the upper-left corner of the rectangular region defined by this
    /// 
    /// </summary>
    [Browsable(false)]
    public readonly T Top => Y;

    /// <summary>
    /// Gets the x-coordinate of the lower-right corner of the rectangular region defined by this
    /// 
    /// </summary>
    [Browsable(false)]
    public readonly T Right => X + Width;

    /// <summary>
    /// Gets the y-coordinate of the lower-right corner of the rectangular region defined by this
    /// 
    /// </summary>
    [Browsable(false)]
    public readonly T Bottom => Y + Height;

    /// <summary>
    /// Tests whether this Rectangle has a <see cref='System.Drawing.Rectangle{T}.Width'/> or a <see cref='System.Drawing.Rectangle{T}.Height'/> of 0.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => (Width <= T.Zero) || (Height <= T.Zero);

    /// <summary>
    /// Tests whether <paramref name="obj"/> is a Rectangle with the same location and
    /// size of this Rectangle.
    /// </summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rectangle<T> && Equals((Rectangle<T>)obj);

    /// <summary>
    /// Tests whether the specified Rectangle has the same location and size as this Rectangle.
    /// </summary>
    /// <param name="other">The Rectangle to compare with this Rectangle.</param>
    /// <returns>true if the specified Rectangle has the same location and size as this Rectangle; otherwise, false.</returns>
    public readonly bool Equals(Rectangle<T> other) => this == other;

    /// <summary>
    /// Tests whether two  objects have equal location and size.
    /// </summary>
    public static bool operator ==(Rectangle<T> left, Rectangle<T> right) =>
        left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;

    /// <summary>
    /// Tests whether two  objects differ in location or size.
    /// </summary>
    public static bool operator !=(Rectangle<T> left, Rectangle<T> right) => !(left == right);

    /// <summary>
    /// Determines if the specified point is contained within the rectangular region defined by this
    /// <see cref='System.Drawing.Rectangle'/> .
    /// </summary>
    public readonly bool Contains(T x, T y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

    /// <summary>
    /// Determines if the specified point is contained within the rectangular region defined by this
    /// <see cref='System.Drawing.Rectangle'/> .
    /// </summary>
    public readonly bool Contains(Point<T> pt) => Contains(pt.X, pt.Y);

    /// <summary>
    /// Determines if the rectangular region represented by <paramref name="rect"/> is entirely contained within
    /// the rectangular region represented by this <see cref='System.Drawing.Rectangle'/> .
    /// </summary>
    public readonly bool Contains(Rectangle<T> rect) =>
        (X <= rect.X) && (rect.X + rect.Width <= X + Width) && (Y <= rect.Y) && (rect.Y + rect.Height <= Y + Height);

    /// <summary>
    /// Gets the hash code for this 
    /// </summary>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    /// <summary>
    /// Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
    /// </summary>
    public void Inflate(T x, T y)
    {
        X -= x;
        Y -= y;
        Width +=  (x+x);
        Height += (y+y);
    }

    /// <summary>
    /// Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
    /// </summary>
    public void Inflate(Size<T> size) => Inflate(size.Width, size.Height);

    /// <summary>
    /// Creates a <see cref='System.Drawing.Rectangle'/> that is inflated by the specified amount.
    /// </summary>
    public static Rectangle<T> Inflate(Rectangle<T> rect, T x, T y)
    {
        Rectangle<T> r = rect;
        r.Inflate(x, y);
        return r;
    }
    /// <summary>
    /// Implements the operator &amp; Returns intersection.
    /// </summary>
    /// <param name="a">a.</param>
    /// <param name="b">The b.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Rectangle<T> operator &(in Rectangle<T> a, in Rectangle<T> b) => Rectangle<T>.Intersect(a, b);

    /// <summary>
    /// Creates a Rectangle that represents the intersection between this Rectangle and rect.
    /// </summary>
    public Rectangle<T> Intersect(Rectangle<T> rect) => Intersect(rect, this);

    /// <summary>
    /// Creates a rectangle that represents the intersection between a and b. If there is no intersection, an
    /// empty rectangle is returned.
    /// </summary>
    public static Rectangle<T> Intersect(Rectangle<T> a, Rectangle<T> b)
    {
        T x1 = T.Max(a.X, b.X);
        T x2 = T.Min(a.X + a.Width, b.X + b.Width);
        T y1 = T.Max(a.Y, b.Y);
        T y2 = T.Min(a.Y + a.Height, b.Y + b.Height);

        if (x2 >= x1 && y2 >= y1)
        {
            return new Rectangle<T>(x1, y1, x2 - x1, y2 - y1);
        }

        return Empty;
    }

    /// <summary>
    /// Determines if this rectangle intersects with rect.
    /// </summary>
    public readonly bool IntersectsWith(Rectangle<T> rect) =>
        (rect.X < X + Width) && (X < rect.X + rect.Width) && (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

    /// <summary>
    /// Computes the bounding rectangle that contains all the specified rectangles.
    /// </summary>
    /// <param name="rects">The rectangles to compute bounds for.</param>
    /// <returns>The smallest rectangle that contains all specified rectangles.</returns>
    /// <exception cref="ArgumentNullException">Thrown when rects is null.</exception>
    public static Rectangle<T> Bounds(IEnumerable<Rectangle<T>> rects)
    {
        if (rects == null) throw new ArgumentNullException(nameof(rects));

        T x1 = T.MaxValue, x2 = T.MinValue, y1 = T.MaxValue, y2 = T.MinValue;
        foreach (var rect in rects)
        {
            x1 = T.Min(x1, rect.X);
            x2 = T.Max(x2, rect.X + rect.Width);
            y1 = T.Min(y1, rect.Y);
            y2 = T.Max(y2, rect.Y + rect.Height);
            
        }
        return new Rectangle<T>(x1, y1, x2 - x1, y2 - y1);
    }
    /// <summary>
    /// Creates a rectangle that represents the union between a and b.
    /// </summary>
    public static Rectangle<T> Bounds(Rectangle<T> a, Rectangle<T> b)
    {
        T x1 = T.Min(a.X, b.X);
        T x2 = T.Max(a.X + a.Width, b.X + b.Width);
        T y1 = T.Min(a.Y, b.Y);
        T y2 = T.Max(a.Y + a.Height, b.Y + b.Height);

        return new Rectangle<T>(x1, y1, x2 - x1, y2 - y1);
    }
    /// <summary>
    /// Gets the center point of this rectangle.
    /// </summary>
    /// <returns>The center point of the rectangle.</returns>
    public  Point<T> Center() 
    {
        var t2 = T.One + T.One;
        var centerX = (Left + Right) / t2;
        var centerY = (Top + Bottom) / t2;
        return new Point<T>(centerX, centerY);
    }
    /// <summary>
    /// Adjusts the location of this rectangle by the specified amount.
    /// </summary>
    public void Offset(Point<T> pos) => Offset(pos.X, pos.Y);

    /// <summary>
    /// Adjusts the location of this rectangle by the specified amount.
    /// </summary>
    public void Offset(T x, T y)
    {
        X += x;
        Y += y;
    }

    /// <summary>
    /// Converts the specified <see cref='System.Drawing.Rectangle'/> to a
    /// 
    /// </summary>
    public static implicit operator Rectangle<T>(Rectangle r) => new Rectangle<T>(T.CreateTruncating(r.X), 
        T.CreateTruncating(r.Y), T.CreateTruncating(r.Width), T.CreateTruncating(r.Height));

    /// <summary>
    /// Converts the <see cref='System.Drawing.Rectangle{T}.Location'/> and <see cref='System.Drawing.Rectangle{T}.Size'/>
    /// of this Rectangle to a human-readable string.
    /// </summary>
    public override readonly string ToString() => $"[{X} {Y} {Width} {Height}]";

    private static readonly char[] splitChars = new[] { ' ', ',' };
    /// <summary>
    /// Parses a string representation of a rectangle.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed rectangle.</returns>
    /// <exception cref="ArgumentException">Thrown when the string cannot be parsed as a rectangle.</exception>
    public static Rectangle<T> Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var r))
            return r;
        

        throw new ArgumentException($"Could not parse string {s} as rectangle.");
    }

    /// <summary>
    /// Tries to parse a string representation of a rectangle.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">When this method returns, contains the parsed rectangle if successful; otherwise, Empty.</param>
    /// <returns>true if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Rectangle<T> result)
    {
        var items = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
        switch(items.Length)
        {
            case 4:
                if (T.TryParse(items[0],NumberStyles.Float,null,out var x)
                    && T.TryParse(items[1], NumberStyles.Float, null, out var y)
                    && T.TryParse(items[2], NumberStyles.Float, null, out var w)
                    && T.TryParse(items[3], NumberStyles.Float, null, out var h))
                {
                    result = new Rectangle<T>(x, y, w, h);
                    return true;
                }
                break;
            case 2:
                if (T.TryParse(items[0], NumberStyles.Float, null, out var w1)
                    && T.TryParse(items[1], NumberStyles.Float, null, out var h1))
                {
                    result = new Rectangle<T>(T.Zero, T.Zero, w1, h1);
                    return true;
                }

                break;
            default:
                break;
        }
        result = Empty;
        return false;
    }

    /// <summary>
    /// Creates a rectangle with random position and size.
    /// </summary>
    /// <returns>A rectangle with random values.</returns>
    public static Rectangle<T> Random()
    {
        return new Rectangle<T>(Point<T>.Random(), new Size<T>(T.CreateTruncating(System.Random.Shared.NextDouble())));
    }
}