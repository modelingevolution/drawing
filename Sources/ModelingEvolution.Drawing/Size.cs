using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a size with width and height components, using generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type used for width and height values.</typeparam>
[ProtoContract]
public struct Size<T> : IEquatable<Size<T>>, IParsable<Size<T>>, ISize<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>, IParsable<T>
{
    /// <summary>
    /// Represents a Size with maximum width and height values.
    /// </summary>
    public static readonly Size<T> Max = new Size<T>(T.MaxValue, T.MaxValue);
    /// <summary>
    /// Represents a Size with zero width and height.
    /// </summary>
    public static readonly Size<T> Empty;
    [ProtoMember(1)]
    private T width; // Do not rename (binary serialization)
    [ProtoMember(2)]
    private T height; // Do not rename (binary serialization)

    /// <summary>
    /// Initializes a new instance of the Size struct with equal width and height.
    /// </summary>
    /// <param name="f">The value for both width and height.</param>
    public Size(T f) :this(f, f){}
    /// <summary>
    /// Initializes a new instance of the Size struct by copying from another size.
    /// </summary>
    /// <param name="size">The size to copy from.</param>
    public Size(Size<T> size)
    {
        width = size.width;
        height = size.height;
    }
    /// <summary>
    /// Implicitly converts a generic Size to a System.Drawing.Size.
    /// </summary>
    /// <param name="size">The size to convert.</param>
    /// <returns>A System.Drawing.Size with integer dimensions.</returns>
    public static implicit operator System.Drawing.Size(Size<T> size)
    {
        return new System.Drawing.Size(
            Convert.ToInt32(size.Width),
            Convert.ToInt32(size.Height)
        );
    }
    /// <summary>
    /// Implicitly converts a System.Drawing.Size to a generic Size.
    /// </summary>
    /// <param name="size">The System.Drawing.Size to convert.</param>
    /// <returns>A generic Size with the same dimensions.</returns>
    public static implicit operator Size<T>(System.Drawing.Size size)
    {
        return new Size<T>(
            T.CreateTruncating(size.Width),
            T.CreateTruncating(size.Height)
        );
    }

    /// <summary>
    /// Implicitly converts a tuple to a Size.
    /// </summary>
    /// <param name="tuple">The tuple containing Width and Height values.</param>
    /// <returns>A Size with the specified dimensions.</returns>
    public static implicit operator Size<T>((T width, T height) tuple)
    {
        return new Size<T>(tuple.width, tuple.height);
    }

    /// <summary>
    /// Implicitly converts a Size to a tuple.
    /// </summary>
    /// <param name="size">The Size to convert.</param>
    /// <returns>A tuple containing the Width and Height values.</returns>
    public static implicit operator (T width, T height)(Size<T> size)
    {
        return (size.width, size.height);
    }
    /// <summary>
    /// Initializes a new instance of the <see cref='Size{T}'/> class from the specified
    /// <see cref='Point{T}'/>.
    /// </summary>
    public Size(Point<T> pt)
    {
        width = pt.X;
        height = pt.Y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='Size{T}'/> struct from the specified
    /// <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public Size(Vector<T> vector)
    {
        width = vector.X;
        height = vector.Y;
    }

    

    /// <summary>
    /// Initializes a new instance of the <see cref='Size{T}'/> class from the specified dimensions.
    /// </summary>
    public Size(T width, T height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Converts the specified <see cref="Size{T}"/> to a <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public static explicit operator Vector<T>(Size<T> size) => new Vector<T>(size.Width, size.Height);

    

    /// <summary>
    /// Performs vector addition of two <see cref='Size{T}'/> objects.
    /// </summary>
    public static Size<T> operator +(Size<T> sz1, Size<T> sz2) => Add(sz1, sz2);

    /// <summary>
    /// Contracts a <see cref='Size{T}'/> by another <see cref='Size{T}'/>
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
    /// Divides one Size by another, component-wise.
    /// </summary>
    /// <param name="left">The dividend size.</param>
    /// <param name="right">The divisor size.</param>
    /// <returns>A size with each component divided.</returns>
    public static Size<T> operator /(Size<T> left, Size<T> right)
        => new Size<T>(left.width / right.width, left.height / right.height);

    /// <summary>
    /// Multiplies two Sizes together, component-wise.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>A size with each component multiplied.</returns>
    public static Size<T> operator *(Size<T> left, Size<T> right)
        => new Size<T>(left.width* right.width, left.height * right.height);
    /// <summary>
    /// Tests whether two <see cref='Size{T}'/> objects are identical.
    /// </summary>
    public static bool operator ==(Size<T> sz1, Size<T> sz2) => T.Abs(sz1.Width - sz2.Width) < T.Epsilon 
                                                            && T.Abs(sz1.Height - sz2.Height) < T.Epsilon;

    /// <summary>
    /// Tests whether two <see cref='Size{T}'/> objects are different.
    /// </summary>
    public static bool operator !=(Size<T> sz1, Size<T> sz2) => !(sz1 == sz2);

    /// <summary>
    /// Converts the specified <see cref='Size{T}'/> to a <see cref='Point{T}'/>.
    /// </summary>
    public static explicit operator Point<T>(Size<T> size) => new Point<T>(size.Width, size.Height);

    /// <summary>
    /// Tests whether this <see cref='Size{T}'/> has zero width and height.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => width == T.Zero && height == T.Zero;

    /// <summary>
    /// Represents the horizontal component of this <see cref='Size{T}'/>.
    /// </summary>
    public T Width
    {
        readonly get => width;
        set => width = value;
    }

    /// <summary>
    /// Represents the vertical component of this <see cref='Size{T}'/>.
    /// </summary>
    public T Height
    {
        readonly get => height;
        set => height = value;
    }

    /// <summary>
    /// Performs vector addition of two <see cref='Size{T}'/> objects.
    /// </summary>
    public static Size<T> Add(Size<T> sz1, Size<T> sz2) => new Size<T>(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

    /// <summary>
    /// Contracts a <see cref='Size{T}'/> by another <see cref='Size{T}'/>.
    /// </summary>
    public static Size<T> Subtract(Size<T> sz1, Size<T> sz2) => new Size<T>(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

    /// <summary>
    /// Tests to see whether the specified object is a <see cref='Size{T}'/>  with the same dimensions
    /// as this <see cref='Size{T}'/>.
    /// </summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Size<T> && Equals((Size<T>)obj);

    /// <summary>
    /// Determines whether this Size is equal to another Size by comparing their width and height values.
    /// </summary>
    /// <param name="other">The other Size to compare with this instance.</param>
    /// <returns>true if the width and height of both sizes are equal; otherwise, false.</returns>
    public readonly bool Equals(Size<T> other) => this == other;

    /// <summary>
    /// Returns the hash code for this Size.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);

    

    /// <summary>
    /// Creates a human-readable string that represents this <see cref='Size{T}'/>.
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

    /// <summary>
    /// Parses a string representation of a size.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <returns>The parsed size.</returns>
    /// <exception cref="ArgumentException">Thrown when the string cannot be parsed as a size.</exception>
    public static Size<T> Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var r))
            return r;
        throw new ArgumentException($"String {s} could not be parsed to size.");
    }
    private static readonly char[] splitChars = new[] { ' ', ',' };
    /// <summary>
    /// Tries to parse a string representation of a size.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider.</param>
    /// <param name="result">When this method returns, contains the parsed size if successful; otherwise, Empty.</param>
    /// <returns>true if parsing was successful; otherwise, false.</returns>
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