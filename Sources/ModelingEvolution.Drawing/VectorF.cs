using System.Globalization;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

public struct VectorF : IFormattable, IEquatable<VectorF>
{
    public static VectorF Random(float length = 1.0f)
    {
        var angle = System.Random.Shared.NextSingle() * MathF.PI * 2; 

        var x = MathF.Cos(angle) * length;
        var y = MathF.Sin(angle) * length;
        return new VectorF(x, y);
    }
    public static VectorF Multiply(VectorF vector, MatrixF matrix)
    {
        return new VectorF(vector.X * matrix.M11 + vector.Y * matrix.M21,
            vector.X * matrix.M12 + vector.Y * matrix.M22);
    }

    public static VectorF From(float x, float y) => new VectorF(x, y);
    public static VectorF From(float s) => new VectorF(s, s);
    public VectorF(float x, float y)
    {
        this._x = x;
        this._y = y;
    }

    public bool Equals(VectorF value)
    {
        return _x == value.X && _y == value.Y;
    }

    public VectorF MirrorOX() => new VectorF(_x, -_y);
    public VectorF MirrorOY() => new VectorF(-_x, _y);
    public override bool Equals(object o)
    {
        if (!(o is VectorF))
            return false;

        return Equals((VectorF)o);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
        }
    }

    public static bool Equals(VectorF vector1, VectorF vector2)
    {
        return vector1.Equals(vector2);
    }

    public static PointF Add(VectorF vector, PointF point)
    {
        return new PointF(vector.X + point.X, vector.Y + point.Y);
    }

    public static VectorF Add(VectorF vector1, VectorF vector2)
    {
        return new VectorF(vector1.X + vector2.X,
            vector1.Y + vector2.Y);
    }

    public static float AngleBetween(VectorF vector1, VectorF vector2)
    {
        float cos_theta = (vector1.X * vector2.X + vector1.Y * vector2.Y) / (vector1.Length * vector2.Length);

        return MathF.Acos(cos_theta) / MathF.PI * 180;
    }

    public static float CrossProduct(VectorF vector1, VectorF vector2)
    {
        // ... what operation is this exactly?
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    public static float Determinant(VectorF vector1, VectorF vector2)
    {
        // same as CrossProduct, it appears.
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    public static VectorF Divide(VectorF vector, float scalar)
    {
        return new VectorF(vector.X / scalar, vector.Y / scalar);
    }

    public static float Multiply(VectorF vector1, VectorF vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y;
    }
    /// <summary>
    /// Projection of current vector onto another vector.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public VectorF Projection(VectorF direction)
    {
        return (this * direction) / (direction * direction) * direction;
    }
    //public static VectorF Multiply(VectorF vector, Matrix matrix)
    //{
    //    return new VectorF(vector.X * matrix.M11 + vector.Y * matrix.M21,
    //        vector.X * matrix.M12 + vector.Y * matrix.M22);
    //}

    public static VectorF Multiply(float scalar, VectorF vector)
    {
        return new VectorF(scalar * vector.X, scalar * vector.Y);
    }

    public static VectorF Multiply(VectorF vector, float scalar)
    {
        return new VectorF(scalar * vector.X, scalar * vector.Y);
    }

    public void Negate()
    {
        _x = -_x;
        _y = -_y;
    }

    public VectorF Normalize()
    {
        float ls = LengthSquared;
        if (Math.Abs(ls - 1.0f) < float.Epsilon)
            return this;
        if (ls == 0.0f) throw new ArgumentException("Cannot normalize zero.");
        float l = MathF.Sqrt(ls);
        return this / l;
    }

    public static VectorF Subtract(VectorF vector1, VectorF vector2)
    {
        return new VectorF(vector1.X - vector2.X, vector1.Y - vector2.Y);
    }

    public static VectorF Parse(string source)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        float x;
        float y;
        if (!float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
            !float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out y))
        {
            throw new FormatException(string.Format("Invalid VectorF format: {0}", source));
        }
        if (!tokenizer.HasNoMoreTokens())
        {
            throw new InvalidOperationException("Invalid VectorF format: " + source);
        }
        return new VectorF(x, y);
    }

    public override string ToString()
    {
        return ToString(null);
    }

    public string ToString(IFormatProvider provider)
    {
        return ToString(null, provider);
    }

    string IFormattable.ToString(string format, IFormatProvider provider)
    {
        return ToString(format, provider);
    }

    private string ToString(string format, IFormatProvider formatProvider)
    {
        if (formatProvider == null)
            formatProvider = CultureInfo.CurrentCulture;
        if (format == null)
            format = string.Empty;
        var separator = NumericListTokenizer.GetSeparator(formatProvider);
        var vectorFormat = string.Format("{{0:{0}}}{1}{{1:{0}}}", format, separator);
        return string.Format(formatProvider, vectorFormat, _x, _y);
    }
    [JsonIgnore]
    public float Length
    {
        get { return MathF.Sqrt(LengthSquared); }
    }
    [JsonIgnore]
    public float LengthSquared
    {
        get { return _x * _x + _y * _y; }
    }

    
    public float X
    {
        get => _x;
        set => _x = value;
    }
    
    public float Y
    {
        get => _y;
        set => _y = value;
    }

    /* operators */
    public static explicit operator PointF(VectorF vector)
    {
        return new PointF(vector.X, vector.Y);
    }



    public static VectorF operator -(VectorF vector1, VectorF vector2)
    {
        return Subtract(vector1, vector2);
    }

    public static VectorF operator -(VectorF vector)
    {
        VectorF result = vector;
        result.Negate();
        return result;
    }

    public static bool operator !=(VectorF vector1, VectorF vector2)
    {
        return !Equals(vector1, vector2);
    }

    public static bool operator ==(VectorF vector1, VectorF vector2)
    {
        return Equals(vector1, vector2);
    }

    public static float operator *(VectorF vector1, VectorF vector2)
    {
        return Multiply(vector1, vector2);
    }

    //public static VectorF operator *(VectorF vector, Matrix matrix)
    //{
    //    return Multiply(vector, matrix);
    //}

    public static VectorF operator *(float scalar, VectorF vector)
    {
        return Multiply(scalar, vector);
    }

    public static VectorF operator *(VectorF vector, float scalar)
    {
        return Multiply(vector, scalar);
    }
   

    public static VectorF operator /(VectorF vector, float scalar)
    {
        return Divide(vector, scalar);
    }

    public static PointF operator +(VectorF vector, PointF point)
    {
        return Add(vector, point);
    }

    public static VectorF operator +(VectorF vector1, VectorF vector2)
    {
        return Add(vector1, vector2);
    }

    float _x;
    float _y;
    public static readonly VectorF EX = new VectorF(1, 0);
    public static readonly VectorF EY = new VectorF(0, 1);
    public static readonly VectorF Zero = new VectorF(0, 0);
}