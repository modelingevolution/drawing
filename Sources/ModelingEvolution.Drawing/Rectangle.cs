using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

[RectangleJsonConverterAttribute]
[ProtoContract]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Rectangle<T> : IEquatable<Rectangle<T>>, IParsable<Rectangle<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Initializes a new instance of the  class.
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
    /// Initializes a new instance of the  class with the specified location
    /// and size.
    /// </summary>
    public Rectangle(T x, T y, T width, T height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Initializes a new instance of the  class with the specified location
    /// and size.
    /// </summary>
    public Rectangle(Point<T> location, Size<T> size)
    {
        x = location.X;
        y = location.Y;
        width = size.Width;
        height = size.Height;
    }
    
    public static implicit operator System.Drawing.Rectangle(Rectangle<T> rectangle)
    {
        return new System.Drawing.Rectangle(
            Convert.ToInt32(rectangle.X),
            Convert.ToInt32(rectangle.Y),
            Convert.ToInt32(rectangle.Width),
            Convert.ToInt32(rectangle.Height)
        );
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<T> MoveTo(Vector<T> point) => MoveTo((Point<T>)point);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle<T> MoveTo(Point<T> point) => new(point, Size);

    /// <summary>
    /// Initializes a new instance of the  struct from the specified
    /// <see cref="System.Numerics.Vector4"/>.
    /// </summary>
    public Rectangle(Vector4 vector)
    {
        x =  T.CreateTruncating(vector.X);
        y = T.CreateTruncating(vector.Y);
        width = T.CreateTruncating(vector.Z);
        height = T.CreateTruncating(vector.W);
    }

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
    /// Creates a new <see cref="System.Numerics.Vector4"/> from this <see cref="System.Drawing.Rectangle<T>"/>.
    /// </summary>
    public Vector4 ToVector4() => new Vector4(float.CreateTruncating(x), float.CreateTruncating(y), float.CreateTruncating(width), float.CreateTruncating(height));

    /// <summary>
    /// Converts the specified <see cref="System.Drawing.Rectangle<T>"/> to a <see cref="System.Numerics.Vector4"/>.
    /// </summary>
    public static explicit operator Vector4(Rectangle<T> rectangle) => rectangle.ToVector4();

    /// <summary>
    /// Converts the specified <see cref="System.Numerics.Vector2"/> to a <see cref="System.Drawing.Rectangle<T>"/>.
    /// </summary>
    public static explicit operator Rectangle<T>(Vector4 vector) => new Rectangle<T>(vector);

    /// <summary>
    /// Creates a new  with the specified location and size.
    /// </summary>
    public static Rectangle<T> FromLTRB(T left, T top, T right, T bottom) =>
        new Rectangle<T>(left, top, right - left, bottom - top);

    public static Rectangle<T> operator +(Rectangle<T> rect, Vector<T> vector)
    {
        return new Rectangle<T>(rect.Location + vector, rect.Size);
    }
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
    /// Gets the x-coordinate of the upper-left corner of the rectangular region defined by this
    
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
    /// Tests whether this  has a <see cref='System.Drawing.Rectangle<T>.Width'/> or a <see cref='System.Drawing.Rectangle<T>.Height'/> of 0.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => (Width <= T.Zero) || (Height <= T.Zero);

    /// <summary>
    /// Tests whether <paramref name="obj"/> is a  with the same location and
    /// size of this 
    /// </summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rectangle<T> && Equals((Rectangle<T>)obj);

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
    /// Creates a rectangle that represents the union between a and b.
    /// </summary>
    public static Rectangle<T> Union(Rectangle<T> a, Rectangle<T> b)
    {
        T x1 = T.Min(a.X, b.X);
        T x2 = T.Max(a.X + a.Width, b.X + b.Width);
        T y1 = T.Min(a.Y, b.Y);
        T y2 = T.Max(a.Y + a.Height, b.Y + b.Height);

        return new Rectangle<T>(x1, y1, x2 - x1, y2 - y1);
    }
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
    /// Converts the <see cref='System.Drawing.Rectangle<T>.Location'/> and <see cref='System.Drawing.Rectangle<T>.Size'/>
    /// of this  to a human-readable string.
    /// </summary>
    public override readonly string ToString() => $"[{X} {Y} {Width} {Height}]";

    private static readonly char[] splitChars = new[] { ' ', ',' };
    public static Rectangle<T> Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var r))
            return r;
        

        throw new ArgumentException($"Could not parse string {s} as rectangle.");
    }

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

    public static Rectangle<T> Random()
    {
        return new Rectangle<T>(Point<T>.Random(), new Size<T>(T.CreateTruncating(System.Random.Shared.NextDouble())));
    }
}