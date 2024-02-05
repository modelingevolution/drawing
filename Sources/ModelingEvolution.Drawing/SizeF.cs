using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public struct SizeF : IEquatable<SizeF>, IParsable<SizeF>
{
    public static readonly SizeF Max = new SizeF(float.MaxValue, float.MaxValue);
    public static readonly SizeF Empty;
    private float width; // Do not rename (binary serialization)
    private float height; // Do not rename (binary serialization)

    public SizeF(float f) :this(f, f){}
    public SizeF(SizeF size)
    {
        width = size.width;
        height = size.height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from the specified
    /// <see cref='System.Drawing.PointF'/>.
    /// </summary>
    public SizeF(PointF pt)
    {
        width = pt.X;
        height = pt.Y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.SizeF'/> struct from the specified
    /// <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public SizeF(Vector2 vector)
    {
        width = vector.X;
        height = vector.Y;
    }

    /// <summary>
    /// Creates a new <see cref="System.Numerics.Vector2"/> from this <see cref="System.Drawing.SizeF"/>.
    /// </summary>
    public Vector2 ToVector2() => new Vector2(width, height);

    /// <summary>
    /// Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from the specified dimensions.
    /// </summary>
    public SizeF(float width, float height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Converts the specified <see cref="System.Drawing.SizeF"/> to a <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public static explicit operator Vector2(SizeF size) => size.ToVector2();

    /// <summary>
    /// Converts the specified <see cref="System.Numerics.Vector2"/> to a <see cref="System.Drawing.SizeF"/>.
    /// </summary>
    public static explicit operator SizeF(Vector2 vector) => new SizeF(vector);

    /// <summary>
    /// Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
    /// </summary>
    public static SizeF operator +(SizeF sz1, SizeF sz2) => Add(sz1, sz2);

    /// <summary>
    /// Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
    /// </summary>
    public static SizeF operator -(SizeF sz1, SizeF sz2) => Subtract(sz1, sz2);

    /// <summary>
    /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
    /// </summary>
    /// <param name="left">Multiplier of type <see cref="float"/>.</param>
    /// <param name="right">Multiplicand of type <see cref="SizeF"/>.</param>
    /// <returns>Product of type <see cref="SizeF"/>.</returns>
    public static SizeF operator *(float left, SizeF right) => Multiply(right, left);

    /// <summary>
    /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
    /// </summary>
    /// <param name="left">Multiplicand of type <see cref="SizeF"/>.</param>
    /// <param name="right">Multiplier of type <see cref="float"/>.</param>
    /// <returns>Product of type <see cref="SizeF"/>.</returns>
    public static SizeF operator *(SizeF left, float right) => Multiply(left, right);

    /// <summary>
    /// Divides <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
    /// </summary>
    /// <param name="left">Dividend of type <see cref="SizeF"/>.</param>
    /// <param name="right">Divisor of type <see cref="int"/>.</param>
    /// <returns>Result of type <see cref="SizeF"/>.</returns>
    public static SizeF operator /(SizeF left, float right)
        => new SizeF(left.width / right, left.height / right);

    /// <summary>
    /// Tests whether two <see cref='System.Drawing.SizeF'/> objects are identical.
    /// </summary>
    public static bool operator ==(SizeF sz1, SizeF sz2) => Math.Abs(sz1.Width - sz2.Width) < float.Epsilon 
                                                            && Math.Abs(sz1.Height - sz2.Height) < float.Epsilon;

    /// <summary>
    /// Tests whether two <see cref='System.Drawing.SizeF'/> objects are different.
    /// </summary>
    public static bool operator !=(SizeF sz1, SizeF sz2) => !(sz1 == sz2);

    /// <summary>
    /// Converts the specified <see cref='System.Drawing.SizeF'/> to a <see cref='System.Drawing.PointF'/>.
    /// </summary>
    public static explicit operator PointF(SizeF size) => new PointF(size.Width, size.Height);

    /// <summary>
    /// Tests whether this <see cref='System.Drawing.SizeF'/> has zero width and height.
    /// </summary>
    [Browsable(false)]
    public readonly bool IsEmpty => width == 0 && height == 0;

    /// <summary>
    /// Represents the horizontal component of this <see cref='System.Drawing.SizeF'/>.
    /// </summary>
    public float Width
    {
        readonly get => width;
        set => width = value;
    }

    /// <summary>
    /// Represents the vertical component of this <see cref='System.Drawing.SizeF'/>.
    /// </summary>
    public float Height
    {
        readonly get => height;
        set => height = value;
    }

    /// <summary>
    /// Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
    /// </summary>
    public static SizeF Add(SizeF sz1, SizeF sz2) => new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

    /// <summary>
    /// Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>.
    /// </summary>
    public static SizeF Subtract(SizeF sz1, SizeF sz2) => new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

    /// <summary>
    /// Tests to see whether the specified object is a <see cref='System.Drawing.SizeF'/>  with the same dimensions
    /// as this <see cref='System.Drawing.SizeF'/>.
    /// </summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is SizeF && Equals((SizeF)obj);

    public readonly bool Equals(SizeF other) => this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);

    public readonly PointF ToPointF() => (PointF)this;
    public static Size Truncate(SizeF value) => new Size(unchecked((int)value.Width), unchecked((int)value.Height));

    public readonly Size ToSize() => Truncate(this);

    /// <summary>
    /// Creates a human-readable string that represents this <see cref='System.Drawing.SizeF'/>.
    /// </summary>
    public override readonly string ToString() => $"[{width} {height}]";

    /// <summary>
    /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
    /// </summary>
    /// <param name="size">Multiplicand of type <see cref="SizeF"/>.</param>
    /// <param name="multiplier">Multiplier of type <see cref="float"/>.</param>
    /// <returns>Product of type SizeF.</returns>
    private static SizeF Multiply(SizeF size, float multiplier) =>
        new SizeF(size.width * multiplier, size.height * multiplier);

    public static SizeF Parse(string s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var r))
            return r;
        throw new ArgumentException($"String {s} could not be parsed to size.");
    }
    private static readonly char[] splitChars = new[] { ' ', ',' };
    public static bool TryParse(string? s, IFormatProvider? provider, out SizeF result)
    {
        var items = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
        switch(items.Length)
        {
            
            case 2:
                if (float.TryParse(items[0], out var w1)
                    && float.TryParse(items[1], out var h1))
                {
                    result = new SizeF(w1, h1);
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