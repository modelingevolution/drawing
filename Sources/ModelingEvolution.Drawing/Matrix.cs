using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;
using static ModelingEvolution.Drawing.Radian;

public struct Matrix<T> : IFormattable
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{

    T _m11;
    T _m12;
    T _m21;
    T _m22;
    T _offsetX;
    T _offsetY;

    public Matrix(T m11,
        T m12,
        T m21,
        T m22,
        T offsetX,
        T offsetY)
    {
        _m11 = m11;
        _m12 = m12;
        _m21 = m21;
        _m22 = m22;
        _offsetX = offsetX;
        _offsetY = offsetY;
    }

    public void Append(Matrix<T> other)
    {
        T _m11;
        T _m21;
        T _m12;
        T _m22;
        T _offsetX;
        T _offsetY;

        _m11 = this._m11 * other.M11 + this._m12 * other.M21;
        _m12 = this._m11 * other.M12 + this._m12 * other.M22;
        _m21 = this._m21 * other.M11 + this._m22 * other.M21;
        _m22 = this._m21 * other.M12 + this._m22 * other.M22;

        _offsetX = this._offsetX * other.M11 + this._offsetY * other.M21 + other.OffsetX;
        _offsetY = this._offsetX * other.M12 + this._offsetY * other.M22 + other.OffsetY;

        this._m11 = _m11;
        this._m12 = _m12;
        this._m21 = _m21;
        this._m22 = _m22;
        this._offsetX = _offsetX;
        this._offsetY = _offsetY;
    }

    public bool Equals(Matrix<T> value)
    {
        return _m11 == value.M11 &&
                _m12 == value.M12 &&
                _m21 == value.M21 &&
                _m22 == value.M22 &&
                _offsetX == value.OffsetX &&
                _offsetY == value.OffsetY;
    }

    public override bool Equals(object o)
    {
        if (!(o is Matrix<T>))
            return false;

        return Equals((Matrix<T>)o);
    }

    public static bool Equals(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return matrix1.Equals(matrix2);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _m11.GetHashCode();
            hashCode = hashCode * 397 ^ _m12.GetHashCode();
            hashCode = hashCode * 397 ^ _m21.GetHashCode();
            hashCode = hashCode * 397 ^ _m22.GetHashCode();
            hashCode = hashCode * 397 ^ _offsetX.GetHashCode();
            hashCode = hashCode * 397 ^ _offsetY.GetHashCode();
            return hashCode;
        }
    }

    public void Invert()
    {
        if (!HasInverse)
            throw new InvalidOperationException("Transform is not invertible.");

        T d = Determinant;

        /* 1/(ad-bc)[d -b; -c a] */

        T _m11 = this._m22;
        T _m12 = -this._m12;
        T _m21 = -this._m21;
        T _m22 = this._m11;

        T _offsetX = this._m21 * this._offsetY - this._m22 * this._offsetX;
        T _offsetY = this._m12 * this._offsetX - this._m11 * this._offsetY;

        this._m11 = _m11 / d;
        this._m12 = _m12 / d;
        this._m21 = _m21 / d;
        this._m22 = _m22 / d;
        this._offsetX = _offsetX / d;
        this._offsetY = _offsetY / d;
    }

    public static Matrix<T> Multiply(Matrix<T> trans1,
        Matrix<T> trans2)
    {
        Matrix<T> m = trans1;
        m.Append(trans2);
        return m;
    }

    public static bool operator ==(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return matrix1.Equals(matrix2);
    }

    public static bool operator !=(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return !matrix1.Equals(matrix2);
    }

    public static Matrix<T> operator *(Matrix<T> trans1,
        Matrix<T> trans2)
    {
        Matrix<T> result = trans1;
        result.Append(trans2);
        return result;
    }

    public static Matrix<T> Parse(string source)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        Matrix<T> value;
        if (source.Trim() == "Identity")
        {
            value = Identity;
        }
        else
        {
            var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
            T m11;
            T m12;
            T m21;
            T m22;
            T offsetX;
            T offsetY;
            if (T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m11)
                && T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m12)
                && T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m21)
                && T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m22)
                && T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetX)
                && T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetY))
            {
                if (!tokenizer.HasNoMoreTokens())
                {
                    throw new InvalidOperationException("Invalid Matrix format: " + source);
                }
                value = new Matrix<T>(m11, m12, m21, m22, offsetX, offsetY);
            }
            else
            {
                throw new FormatException(string.Format("Invalid Matrix format: {0}", source));
            }
        }
        return value;
    }

    public void Prepend(Matrix<T> other)
    {
        T _m11;
        T _m21;
        T _m12;
        T _m22;
        T _offsetX;
        T _offsetY;

        _m11 = other.M11 * this._m11 + other.M12 * this._m21;
        _m12 = other.M11 * this._m12 + other.M12 * this._m22;
        _m21 = other.M21 * this._m11 + other.M22 * this._m21;
        _m22 = other.M21 * this._m12 + other.M22 * this._m22;

        _offsetX = other.OffsetX * this._m11 + other.OffsetY * this._m21 + this._offsetX;
        _offsetY = other.OffsetX * this._m12 + other.OffsetY * this._m22 + this._offsetY;

        this._m11 = _m11;
        this._m12 = _m12;
        this._m21 = _m21;
        this._m22 = _m22;
        this._offsetX = _offsetX;
        this._offsetY = _offsetY;
    }

    public Matrix<T> Rotate(Degree<T> angle) => Rotate((Radian<T>)angle);
    public Matrix<T> Rotate(Radian<T> theta)
    {
        Matrix<T> r_theta = new Matrix<T>(Cos(theta), Sin(theta),
            -Sin(theta), Cos(theta),
            T.Zero, T.Zero);

        Append(r_theta);
        return this;
    }

    public Matrix<T> RotateAt(T angle,
        T centerX,
        T centerY)
    {
        Translate(-centerX, -centerY);
        Rotate(angle);
        Translate(centerX, centerY);
        return this;
    }

    public Matrix<T> RotateAtPrepend(T angle,
        T centerX,
        T centerY)
    {
        Matrix<T> m = Identity;
        m.RotateAt(angle, centerX, centerY);
        Prepend(m);
        return this;
    }

    public Matrix<T> RotatePrepend(T angle)
    {
        Matrix<T> m = Identity;
        m.Rotate(angle);
        Prepend(m);
        return this;
    }


    public Matrix<T> Scale(T scaleX,
        T scaleY)
    {
        Matrix<T> scale = new Matrix<T>(scaleX, T.Zero,
            T.Zero, scaleY,
            T.Zero, T.Zero);

        Append(scale);
        return this;
    }

    public Matrix<T> ScaleAt(T scaleX,
        T scaleY,
        T centerX,
        T centerY)
    {
        Translate(-centerX, -centerY);
        Scale(scaleX, scaleY);
        Translate(centerX, centerY);
        return this;
    }

    public Matrix<T> ScaleAtPrepend(T scaleX,
        T scaleY,
        T centerX,
        T centerY)
    {
        Matrix<T> m = Identity;
        m.ScaleAt(scaleX, scaleY, centerX, centerY);
        Prepend(m);
        return this;
    }

    public Matrix<T> ScalePrepend(T scaleX,
        T scaleY)
    {
        Matrix<T> m = Identity;
        m.Scale(scaleX, scaleY);
        Prepend(m);
        return this;
    }

    public void SetIdentity()
    {
        _m11 = _m22 = T.One;
        _m12 = _m21 = T.Zero;
        _offsetX = _offsetY = T.Zero;
    }
    readonly static T deg180 = T.CreateTruncating(180);
    public Matrix<T> Skew(T skewX,
        T skewY)
    {
        Matrix<T> skew_m = new Matrix<T>(T.One, T.Tan(skewY * T.Pi / deg180),
            T.Tan(skewX * T.Pi / deg180), T.One,
            T.Zero, T.Zero);
        Append(skew_m);
        return this;
    }

    public Matrix<T> SkewPrepend(T skewX,
        T skewY)
    {
        Matrix<T> m = Identity;
        m.Skew(skewX, skewY);
        Prepend(m);
        return this;
    }

    public override string ToString()
    {
        return ToString(null);
    }

    public string ToString(IFormatProvider provider)
    {
        return ToString(null, provider);
    }

    string IFormattable.ToString(string format,
        IFormatProvider provider)
    {
        return ToString(provider);
    }

    private string ToString(string format, IFormatProvider provider)
    {
        if (IsIdentity)
            return "Identity";

        if (provider == null)
            provider = CultureInfo.CurrentCulture;

        if (format == null)
            format = string.Empty;

        var separator = NumericListTokenizer.GetSeparator(provider);

        var matrix = string.Format(
            "{{0:{0}}}{1}{{1:{0}}}{1}{{2:{0}}}{1}{{3:{0}}}{1}{{4:{0}}}{1}{{5:{0}}}",
            format, separator);
        return string.Format(provider, matrix,
            _m11, _m12, _m21, _m22, _offsetX, _offsetY);
    }

    public Point<T> Transform(Point<T> point)
    {
        return Point<T>.Multiply(point, this);
    }
    
    public Point<T>[] Transform(Point<T>[] points)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = Transform(points[i]);
        return points;
    }

    public Vector<T> Transform(Vector<T> vector)
    {
        return Vector<T>.Multiply(vector, this);
    }

    public void Transform(Vector<T>[] vectors)
    {
        for (int i = 0; i < vectors.Length; i++)
            vectors[i] = Transform(vectors[i]);
    }

    public Matrix<T> Translate(T offsetX,
        T offsetY)
    {
        _offsetX += offsetX;
        _offsetY += offsetY;
        return this;
    }

    public Matrix<T> TranslatePrepend(T offsetX,
        T offsetY)
    {
        Matrix<T> m = Identity;
        m.Translate(offsetX, offsetY);
        Prepend(m);
        return this;
    }

    public T Determinant
    {
        get { return _m11 * _m22 - _m12 * _m21; }
    }

    public bool HasInverse
    {
        get { return Determinant != T.Zero; }
    }

    public static Matrix<T> Identity
    {
        get { return new Matrix<T>(T.One, T.Zero, T.Zero, T.One, T.Zero, T.Zero); }
    }

    public bool IsIdentity
    {
        get { return Equals(Identity); }
    }

    public T M11
    {
        get { return _m11; }
        set { _m11 = value; }
    }
    public T M12
    {
        get { return _m12; }
        set { _m12 = value; }
    }
    public T M21
    {
        get { return _m21; }
        set { _m21 = value; }
    }
    public T M22
    {
        get { return _m22; }
        set { _m22 = value; }
    }
    public T OffsetX
    {
        get { return _offsetX; }
        set { _offsetX = value; }
    }
    public T OffsetY
    {
        get { return _offsetY; }
        set { _offsetY = value; }
    }

    public Matrix<T> Translate(Vector<T> offset)
    {
        Translate(offset.X, offset.Y);
        return this;
    }


}