using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public struct Point<T> : IEquatable<Point<T>>, IParsable<Point<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    public static readonly Point<T> Zero = new Point<T>(T.Zero, T.Zero);
    public static Point<T> Random()
    {
        T width = T.One;
        T height = T.One;
        return Random(width, height);
    }
    public static Point<T> Random(T width, T height)
    {
        var t1 = T.CreateTruncating(System.Random.Shared.NextDouble());
        var t2 = T.CreateTruncating(System.Random.Shared.NextDouble());
        return new Point<T>(t1 * width, t2* height);
    }

    private static readonly T Two = T.CreateTruncating(2);
    public static Point<T> Middle(Point<T> a, Point<T> b)
    {
        return new Point<T>((a.x + b.x) / Two, (a.y + b.y) / Two);
    }
    
    public static Point<T> Multiply(Point<T> point, Matrix<T> matrix)
    {
        return new Point<T>(point.X * matrix.M11 + point.Y * matrix.M21 + matrix.OffsetX,
            point.X * matrix.M12 + point.Y * matrix.M22 + matrix.OffsetY);
    }
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
    public static Point<T> Random(System.Drawing.SizeF size) => Random(T.CreateTruncating(size.Width), T.CreateTruncating(size.Height));
    
    private T x; // Do not rename (binary serialization)
    private T y; // Do not rename (binary serialization)
    
    public Point(T x, T y)
    {
        this.x = x;
        this.y = y;
    }
    
    public Point(Vector2 vector)
    {
        x = T.CreateTruncating(vector.X);
        y = T.CreateTruncating(vector.Y);
    }


    public Vector<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>,
        IFloatingPointIeee754<U>
    {
        return  new Vector<U>(U.CreateTruncating( x),U.CreateTruncating(  y));
    }
    

    public static implicit operator Vector<T>(Point<T> f)
    {
        return new Vector<T>(f.x, f.y);
    }

    [Browsable(false)]
    public readonly bool IsEmpty => x == T.Zero && y == T.Zero;

   
    public T X
    {
        readonly get => x;
        set => x = value;
    }

    /// <summary>
    /// Gets the y-coordinate of this <see cref='System.Drawing.Point<T>'/>.
    /// </summary>
    public T Y
    {
        readonly get => y;
        set => y = value;
    }

    
    public static explicit operator Point<T>(Vector2 vector) => new Point<T>(vector);

   
    public static Point<T> operator +(Point<T> pt, System.Drawing.SizeF sz) => Add(pt, sz);
    public static Point<T> operator +(Point<T> pt, Vector<T> sz) => new Point<T>(pt.x + sz.X, pt.y + sz.Y);
    public static Point<T> operator -(Point<T> pt, Vector<T> sz) => new Point<T>(pt.x - sz.X, pt.y - sz.Y);

    public static Point<T> operator -(Point<T> pt, System.Drawing.SizeF sz) => Subtract(pt, sz);

    public static Vector<T> operator -(Point<T> pt, Point<T> sz) => new Vector<T>(pt.x - sz.x, pt.y -sz.y);

    public static bool operator ==(Point<T> left, Point<T> right) => left.X == right.X && left.Y == right.Y;

   
    public static bool operator !=(Point<T> left, Point<T> right) => !(left == right);

    
    public static Point<T> Add(Point<T> pt, System.Drawing.SizeF sz) => new Point<T>(pt.X + T.CreateTruncating(sz.Width), pt.Y + T.CreateTruncating(sz.Height));

   
    public static Point<T> Subtract(Point<T> pt, System.Drawing.SizeF sz) => new Point<T>(pt.X - T.CreateTruncating(sz.Width), pt.Y - T.CreateTruncating(sz.Height));

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Point<T> && Equals((Point<T>)obj);

    public readonly bool Equals(Point<T> other) => this == other;

    public override readonly int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode());

    public override readonly string ToString() => $"{{X={x}, Y={y}}}";
}