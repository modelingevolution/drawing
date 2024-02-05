using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public interface ISize<T>
{
    T Width { get; }
    T Height { get; }
}

public struct Size<T> : IEquatable<Size<T>>, IParsable<Size<T>>, ISize<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    public static readonly Size<T> Max = new Size<T>(T.MaxValue, T.MaxValue);
    public static readonly Size<T> Empty;
    private T width; // Do not rename (binary serialization)
    private T height; // Do not rename (binary serialization)

    public Size(T f) :this(f, f){}
    public Size(Size<T> size)
    {
        width = size.width;
        height = size.height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.Size<T>'/> class from the specified
    /// <see cref='System.Drawing.Point<T>'/>.
    /// </summary>
    public Size(Point<T> pt)
    {
        width = pt.X;
        height = pt.Y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.Size<T>'/> struct from the specified
    /// <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public Size(Vector<T> vector)
    {
        width = vector.X;
        height = vector.Y;
    }

    

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.Size<T>'/> class from the specified dimensions.
    /// </summary>
    public Size(T width, T height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Converts the specified <see cref="System.Drawing.Size<T>"/> to a <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public static explicit operator Vector<T>(Size<T> size) => new Vector<T>(size.Width, size.Height);

    

    /// <summary>
    /// Performs vector addition of two <see cref='System.Drawing.Size<T>'/> objects.
    /// </summary>
    public static Size<T> operator +(Size<T> sz1, Size<T> sz2) => Add(sz1, sz2);

    /// <summary>
    /// Contracts a <see cref='System.Drawing.Size<T>'/> by another <see cref='System.Drawing.Size<T>'/>
    /// </summary>
    public static Size<T> operator -(Size<T> sz1, Size<T> sz2) => Subtract(sz1, sz2);

    /// <summary>
    /// Multiplies <see cref="Size<T>"/> by a <see cref="T"/> producing <see cref="Size<T>"/>.
    /// </summary>
    /// <param name="left">Multiplier of type <see cref="T"/>.</param>
    /// <param name="right">Multiplicand of type <see cref="Size<T>"/>.</param>
    /// <returns>Product of type <see cref="Size<T>"/>.</returns>
    public static Size<T> operator *(T left, Size<T> right) => Multiply(right, left);

    /// <summary>
    /// Multiplies <see cref="Size<T>"/> by a <see cref="T"/> producing <see cref="Size<T>"/>.
    /// </summary>
    /// <param name="left">Multiplicand of type <see cref="Size<T>"/>.</param>
    /// <param name="right">Multiplier of type <see cref="T"/>.</param>
    /// <returns>Product of type <see cref="Size<T>"/>.</returns>
    public static Size<T> operator *(Size<T> left, T right) => Multiply(left, right);

    /// <summary>
    /// Divides <see cref="Size<T>"/> by a <see cref="T"/> producing <see cref="Size<T>"/>.
    /// </summary>
    /// <param name="left">Dividend of type <see cref="Size<T>"/>.</param>
    /// <param name="right">Divisor of type <see cref="int"/>.</param>
    /// <returns>Result of type <see cref="Size<T>"/>.</returns>
    public static Size<T> operator /(Size<T> left, T right)
        => new Size<T>(left.width / right, left.height / right);

    /// <summary>
    /// Tests whether two <see cref='System.Drawing.Size<T>'/> objects are identical.
    /// </summary>
    public static bool operator ==(Size<T> sz1, Size<T> sz2) => T.Abs(sz1.Width - sz2.Width) < T.Epsilon 
                                                            && T.Abs(sz1.Height - sz2.Height) < T.Epsilon;

    /// <summary>
    /// Tests whether two <see cref='System.Drawing.Size<T>'/> objects are different.
    /// </summary>
    public static bool operator !=(Size<T> sz1, Size<T> sz2) => !(sz1 == sz2);

    /// <summary>
    /// Converts the specified <see cref='System.Drawing.Size<T>'/> to a <see cref='System.Drawing.Point<T>'/>.
    /// </summary>
    public static explicit operator Point<T>(Size<T> size) => new Point<T>(size.Width, size.Height);

    /// <summary>
    /// Tests whether this <see cref='System.Drawing.Size<T>'/> has zero width and height.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => width == T.Zero && height == T.Zero;

    /// <summary>
    /// Represents the horizontal component of this <see cref='System.Drawing.Size<T>'/>.
    /// </summary>
    public T Width
    {
        readonly get => width;
        set => width = value;
    }

    /// <summary>
    /// Represents the vertical component of this <see cref='System.Drawing.Size<T>'/>.
    /// </summary>
    public T Height
    {
        readonly get => height;
        set => height = value;
    }

    /// <summary>
    /// Performs vector addition of two <see cref='System.Drawing.Size<T>'/> objects.
    /// </summary>
    public static Size<T> Add(Size<T> sz1, Size<T> sz2) => new Size<T>(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

    /// <summary>
    /// Contracts a <see cref='System.Drawing.Size<T>'/> by another <see cref='System.Drawing.Size<T>'/>.
    /// </summary>
    public static Size<T> Subtract(Size<T> sz1, Size<T> sz2) => new Size<T>(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

    /// <summary>
    /// Tests to see whether the specified object is a <see cref='System.Drawing.Size<T>'/>  with the same dimensions
    /// as this <see cref='System.Drawing.Size<T>'/>.
    /// </summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Size<T> && Equals((Size<T>)obj);

    public readonly bool Equals(Size<T> other) => this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);

    

    /// <summary>
    /// Creates a human-readable string that represents this <see cref='System.Drawing.Size<T>'/>.
    /// </summary>
    public override readonly string ToString() => $"[{width} {height}]";

    /// <summary>
    /// Multiplies <see cref="Size<T>"/> by a <see cref="T"/> producing <see cref="Size<T>"/>.
    /// </summary>
    /// <param name="size">Multiplicand of type <see cref="Size<T>"/>.</param>
    /// <param name="multiplier">Multiplier of type <see cref="T"/>.</param>
    /// <returns>Product of type Size<T>.</returns>
    private static Size<T> Multiply(Size<T> size, T multiplier) =>
        new Size<T>(size.width * multiplier, size.height * multiplier);

    public static Size<T> Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var r))
            return r;
        throw new ArgumentException($"String {s} could not be parsed to size.");
    }
    private static readonly char[] splitChars = new[] { ' ', ',' };
    public static bool TryParse(string? s, IFormatProvider? provider, out Size<T> result)
    {
        var items = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
        switch(items.Length)
        {
            
            case 2:
                if (T.TryParse(items[0], NumberStyles.Float,null, out var w1)
                    && T.TryParse(items[1], NumberStyles.Float, null, out var h1))
                {
                    result = new Size<T>(w1, h1);
                    return true;
                }

                break;
            default:
                break;
        }
        result = Empty;
        return false;
    }
}