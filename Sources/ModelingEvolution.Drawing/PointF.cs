using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public readonly record struct TimeSpanF
{
    private readonly float _seconds;
    public static readonly TimeSpanF Zero = new TimeSpanF(0f);
    public TimeSpanF(float seconds)
    {
        this._seconds = seconds;
    }

    public static TimeSpanF operator +(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds + sz._seconds);
    public static TimeSpanF operator -(TimeSpanF pt, TimeSpanF sz) => new TimeSpanF(pt._seconds - sz._seconds);
    public static implicit operator TimeSpanF(TimeSpan span) => new TimeSpanF((float)span.TotalSeconds);
    public static implicit operator float(TimeSpanF t) => t._seconds;

}

public struct PointF : IEquatable<PointF>, IParsable<PointF>
{
    public static readonly PointF Zero = new PointF(0, 0);
    public static PointF Random(float width = 1.0f, float height=1.0f)
    {
        return new PointF(System.Random.Shared.NextSingle() * width,
            System.Random.Shared.NextSingle() * height);
    }

    public static PointF Middle(PointF a, PointF b)
    {
        return new PointF((a.x + b.x) / 2, (a.y + b.y) / 2);
    }
    public static PointF Multiply(PointF point, MatrixF matrix)
    {
        return new PointF(point.X * matrix.M11 + point.Y * matrix.M21 + matrix.OffsetX,
            point.X * matrix.M12 + point.Y * matrix.M22 + matrix.OffsetY);
    }
    public static bool TryParse(string? source, IFormatProvider? p,out PointF result)
    {
        if (source == null)
        {
            result = PointF.Zero;
            return false;
        }
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        
        if (!float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || 
            !tokenizer.HasNoMoreTokens())
        {
            result = PointF.Zero;
            return false;
        }
        result = new PointF(x, y);
        return true;
    }
    public static PointF Parse(string source, IFormatProvider? p = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        if (!float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            throw new FormatException(string.Format("Invalid PointF format: {0}", source));
        
        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException("Invalid PointF format: " + source);
        
        return new PointF(x, y);
    }
    public static PointF Random(SizeF size) => Random(size.Width, size.Height);
    
    private float x; // Do not rename (binary serialization)
    private float y; // Do not rename (binary serialization)
    
    public PointF(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    public PointF(Vector2 vector)
    {
        x = vector.X;
        y = vector.Y;
    }

    
    public Vector2 AsVector2() => new Vector2(x, y);
    public VectorF AsVectorF() => new VectorF(x, y);

    [Browsable(false)]
    public readonly bool IsEmpty => x == 0f && y == 0f;

   
    public float X
    {
        readonly get => x;
        set => x = value;
    }

    /// <summary>
    /// Gets the y-coordinate of this <see cref='System.Drawing.PointF'/>.
    /// </summary>
    public float Y
    {
        readonly get => y;
        set => y = value;
    }

    
    public static explicit operator Vector2(PointF point) => point.AsVector2();

    public static explicit operator PointF(Vector2 vector) => new PointF(vector);

   
    public static PointF operator +(PointF pt, SizeF sz) => Add(pt, sz);
    public static PointF operator +(PointF pt, VectorF sz) => new PointF(pt.x + sz.X, pt.y + sz.Y);
    public static PointF operator -(PointF pt, VectorF sz) => new PointF(pt.x - sz.X, pt.y - sz.Y);

    public static PointF operator -(PointF pt, SizeF sz) => Subtract(pt, sz);

    public static VectorF operator -(PointF pt, PointF sz) => new VectorF(pt.x - sz.x, pt.y -sz.y);

    public static bool operator ==(PointF left, PointF right) => left.X == right.X && left.Y == right.Y;

   
    public static bool operator !=(PointF left, PointF right) => !(left == right);

    
    public static PointF Add(PointF pt, SizeF sz) => new PointF(pt.X + sz.Width, pt.Y + sz.Height);

   
    public static PointF Subtract(PointF pt, SizeF sz) => new PointF(pt.X - sz.Width, pt.Y - sz.Height);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is PointF && Equals((PointF)obj);

    public readonly bool Equals(PointF other) => this == other;

    public override readonly int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode());

    public override readonly string ToString() => $"{{X={x}, Y={y}}}";
}