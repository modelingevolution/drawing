using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ModelingEvolution.Drawing;

[VectorJsonConverterAttribute]
public struct Vector<T> : IFormattable, IEquatable<Vector<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>
{
    public static Vector<T> Random()
    {
        T length = T.One;
        var angle = T.CreateTruncating(System.Random.Shared.NextSingle()) * T.Pi * T.CreateTruncating(2); 

        var x = T.Cos(angle) * length;
        var y = T.Sin(angle) * length;
        return new Vector<T>(x, y);
    }
    public static Vector<T> Multiply(Vector<T> vector, Matrix<T> matrix)
    {
        return new Vector<T>(vector.X * matrix.M11 + vector.Y * matrix.M21,
            vector.X * matrix.M12 + vector.Y * matrix.M22);
    }

    public static Vector<T> From(T x, T y) => new Vector<T>(x, y);
    public static Vector<T> From(T s) => new Vector<T>(s, s);
    public Vector(T x, T y)
    {
        this._x = x;
        this._y = y;
    }

    public bool Equals(Vector<T> value)
    {
        return _x == value.X && _y == value.Y;
    }

    public Vector<T> MirrorOX() => new Vector<T>(_x, -_y);
    public Vector<T> MirrorOY() => new Vector<T>(-_x, _y);
    public override bool Equals(object o)
    {
        if (!(o is Vector<T>))
            return false;

        return Equals((Vector<T>)o);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
        }
    }

    public static bool Equals(Vector<T> vector1, Vector<T> vector2)
    {
        return vector1.Equals(vector2);
    }

    public static Point<T> Add(Vector<T> vector, Point<T> point)
    {
        return new Point<T>(vector.X + point.X, vector.Y + point.Y);
    }

    public static Vector<T> Add(Vector<T> vector1, Vector<T> vector2)
    {
        return new Vector<T>(vector1.X + vector2.X,
            vector1.Y + vector2.Y);
    }

    private static readonly T t180 = T.CreateTruncating((180));
    public static T AngleBetween(Vector<T> vector1, Vector<T> vector2)
    {
        T cos_theta = (vector1.X * vector2.X + vector1.Y * vector2.Y) / (vector1.Length * vector2.Length);

        return T.Acos(cos_theta) / T.Pi * t180;
    }

    public static T CrossProduct(Vector<T> vector1, Vector<T> vector2)
    {
        // ... what operation is this exactly?
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    public static T Determinant(Vector<T> vector1, Vector<T> vector2)
    {
        // same as CrossProduct, it appears.
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    public static Vector<T> Divide(Vector<T> vector, T scalar)
    {
        return new Vector<T>(vector.X / scalar, vector.Y / scalar);
    }

    public static T Multiply(Vector<T> vector1, Vector<T> vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y;
    }
    /// <summary>
    /// Projection of current vector onto another vector.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Vector<T> Projection(Vector<T> direction)
    {
        return (this * direction) / (direction * direction) * direction;
    }
    //public static Vector<T> Multiply(Vector<T> vector, Matrix matrix)
    //{
    //    return new Vector<T>(vector.X * matrix.M11 + vector.Y * matrix.M21,
    //        vector.X * matrix.M12 + vector.Y * matrix.M22);
    //}

    public static Vector<T> Multiply(T scalar, Vector<T> vector)
    {
        return new Vector<T>(scalar * vector.X, scalar * vector.Y);
    }

    public static Vector<T> Multiply(Vector<T> vector, T scalar)
    {
        return new Vector<T>(scalar * vector.X, scalar * vector.Y);
    }

    public void Negate()
    {
        _x = -_x;
        _y = -_y;
    }

    public Vector<T> Normalize()
    {
        T ls = LengthSquared;
        if (T.Abs(ls - T.One) < T.Epsilon)
            return this;
        if (ls == T.Zero) throw new ArgumentException("Cannot normalize zero.");
        T l = T.Sqrt(ls);
        return this / l;
    }

    public static Vector<T> Subtract(Vector<T> vector1, Vector<T> vector2)
    {
        return new Vector<T>(vector1.X - vector2.X, vector1.Y - vector2.Y);
    }

    public static Vector<T> Parse(string source)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
        T x;
        T y;
        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out y))
        {
            throw new FormatException(string.Format("Invalid Vector<T> format: {0}", source));
        }
        if (!tokenizer.HasNoMoreTokens())
        {
            throw new InvalidOperationException("Invalid Vector<T> format: " + source);
        }
        return new Vector<T>(x, y);
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
    public T Length
    {
        get { return T.Sqrt(LengthSquared); }
    }
    [JsonIgnore]
    public T LengthSquared
    {
        get { return _x * _x + _y * _y; }
    }

    
    public T X
    {
        get => _x;
        set => _x = value;
    }
    
    public T Y
    {
        get => _y;
        set => _y = value;
    }

    /* operators */
    public static explicit operator Point<T>(Vector<T> vector)
    {
        return new Point<T>(vector.X, vector.Y);
    }



    public static Vector<T> operator -(Vector<T> vector1, Vector<T> vector2)
    {
        return Subtract(vector1, vector2);
    }

    public static Vector<T> operator -(Vector<T> vector)
    {
        Vector<T> result = vector;
        result.Negate();
        return result;
    }

    public static bool operator !=(Vector<T> vector1, Vector<T> vector2)
    {
        return !Equals(vector1, vector2);
    }

    public static bool operator ==(Vector<T> vector1, Vector<T> vector2)
    {
        return Equals(vector1, vector2);
    }

    public static T operator *(Vector<T> vector1, Vector<T> vector2)
    {
        return Multiply(vector1, vector2);
    }

    //public static Vector<T> operator *(Vector<T> vector, Matrix matrix)
    //{
    //    return Multiply(vector, matrix);
    //}

    public static Vector<T> operator *(T scalar, Vector<T> vector)
    {
        return Multiply(scalar, vector);
    }

    public static Vector<T> operator *(Vector<T> vector, T scalar)
    {
        return Multiply(vector, scalar);
    }
   

    public static Vector<T> operator /(Vector<T> vector, T scalar)
    {
        return Divide(vector, scalar);
    }

    public static Point<T> operator +(Vector<T> vector, Point<T> point)
    {
        return Add(vector, point);
    }

    public static Vector<T> operator +(Vector<T> vector1, Vector<T> vector2)
    {
        return Add(vector1, vector2);
    }

    T _x;
    T _y;
    public static readonly Vector<T> EX = new Vector<T>(T.One, T.Zero);
    public static readonly Vector<T> EY = new Vector<T>(T.Zero, T.One);
    public static readonly Vector<T> Zero = new Vector<T>(T.Zero, T.Zero);
}