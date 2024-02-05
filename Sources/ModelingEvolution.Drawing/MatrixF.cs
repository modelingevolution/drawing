using System.Globalization;

namespace ModelingEvolution.Drawing;

public struct MatrixF : IFormattable
{

    float _m11;
    float _m12;
    float _m21;
    float _m22;
    float _offsetX;
    float _offsetY;

    public MatrixF(float m11,
        float m12,
        float m21,
        float m22,
        float offsetX,
        float offsetY)
    {
        _m11 = m11;
        _m12 = m12;
        _m21 = m21;
        _m22 = m22;
        _offsetX = offsetX;
        _offsetY = offsetY;
    }

    public void Append(MatrixF matrixF)
    {
        float _m11;
        float _m21;
        float _m12;
        float _m22;
        float _offsetX;
        float _offsetY;

        _m11 = this._m11 * matrixF.M11 + this._m12 * matrixF.M21;
        _m12 = this._m11 * matrixF.M12 + this._m12 * matrixF.M22;
        _m21 = this._m21 * matrixF.M11 + this._m22 * matrixF.M21;
        _m22 = this._m21 * matrixF.M12 + this._m22 * matrixF.M22;

        _offsetX = this._offsetX * matrixF.M11 + this._offsetY * matrixF.M21 + matrixF.OffsetX;
        _offsetY = this._offsetX * matrixF.M12 + this._offsetY * matrixF.M22 + matrixF.OffsetY;

        this._m11 = _m11;
        this._m12 = _m12;
        this._m21 = _m21;
        this._m22 = _m22;
        this._offsetX = _offsetX;
        this._offsetY = _offsetY;
    }

    public bool Equals(MatrixF value)
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
        if (!(o is MatrixF))
            return false;

        return Equals((MatrixF)o);
    }

    public static bool Equals(MatrixF matrix1,
        MatrixF matrix2)
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

        float d = Determinant;

        /* 1/(ad-bc)[d -b; -c a] */

        float _m11 = this._m22;
        float _m12 = -this._m12;
        float _m21 = -this._m21;
        float _m22 = this._m11;

        float _offsetX = this._m21 * this._offsetY - this._m22 * this._offsetX;
        float _offsetY = this._m12 * this._offsetX - this._m11 * this._offsetY;

        this._m11 = _m11 / d;
        this._m12 = _m12 / d;
        this._m21 = _m21 / d;
        this._m22 = _m22 / d;
        this._offsetX = _offsetX / d;
        this._offsetY = _offsetY / d;
    }

    public static MatrixF Multiply(MatrixF trans1,
        MatrixF trans2)
    {
        MatrixF m = trans1;
        m.Append(trans2);
        return m;
    }

    public static bool operator ==(MatrixF matrix1,
        MatrixF matrix2)
    {
        return matrix1.Equals(matrix2);
    }

    public static bool operator !=(MatrixF matrix1,
        MatrixF matrix2)
    {
        return !matrix1.Equals(matrix2);
    }

    public static MatrixF operator *(MatrixF trans1,
        MatrixF trans2)
    {
        MatrixF result = trans1;
        result.Append(trans2);
        return result;
    }

    public static MatrixF Parse(string source)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        MatrixF value;
        if (source.Trim() == "Identity")
        {
            value = Identity;
        }
        else
        {
            var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);
            float m11;
            float m12;
            float m21;
            float m22;
            float offsetX;
            float offsetY;
            if (float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m11)
                && float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m12)
                && float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m21)
                && float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out m22)
                && float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetX)
                && float.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetY))
            {
                if (!tokenizer.HasNoMoreTokens())
                {
                    throw new InvalidOperationException("Invalid Matrix format: " + source);
                }
                value = new MatrixF(m11, m12, m21, m22, offsetX, offsetY);
            }
            else
            {
                throw new FormatException(string.Format("Invalid Matrix format: {0}", source));
            }
        }
        return value;
    }

    public void Prepend(MatrixF matrixF)
    {
        float _m11;
        float _m21;
        float _m12;
        float _m22;
        float _offsetX;
        float _offsetY;

        _m11 = matrixF.M11 * this._m11 + matrixF.M12 * this._m21;
        _m12 = matrixF.M11 * this._m12 + matrixF.M12 * this._m22;
        _m21 = matrixF.M21 * this._m11 + matrixF.M22 * this._m21;
        _m22 = matrixF.M21 * this._m12 + matrixF.M22 * this._m22;

        _offsetX = matrixF.OffsetX * this._m11 + matrixF.OffsetY * this._m21 + this._offsetX;
        _offsetY = matrixF.OffsetX * this._m12 + matrixF.OffsetY * this._m22 + this._offsetY;

        this._m11 = _m11;
        this._m12 = _m12;
        this._m21 = _m21;
        this._m22 = _m22;
        this._offsetX = _offsetX;
        this._offsetY = _offsetY;
    }

    public MatrixF Rotate(float angle)
    {
        // R_theta==[costheta -sintheta; sintheta costheta],	
        float theta = angle * MathF.PI / 180;

        MatrixF r_theta = new MatrixF(MathF.Cos(theta), MathF.Sin(theta),
            -MathF.Sin(theta), MathF.Cos(theta),
            0, 0);

        Append(r_theta);
        return this;
    }

    public MatrixF RotateAt(float angle,
        float centerX,
        float centerY)
    {
        Translate(-centerX, -centerY);
        Rotate(angle);
        Translate(centerX, centerY);
        return this;
    }

    public MatrixF RotateAtPrepend(float angle,
        float centerX,
        float centerY)
    {
        MatrixF m = Identity;
        m.RotateAt(angle, centerX, centerY);
        Prepend(m);
        return this;
    }

    public MatrixF RotatePrepend(float angle)
    {
        MatrixF m = Identity;
        m.Rotate(angle);
        Prepend(m);
        return this;
    }


    public MatrixF Scale(float scaleX,
        float scaleY)
    {
        MatrixF scale = new MatrixF(scaleX, 0,
            0, scaleY,
            0, 0);

        Append(scale);
        return this;
    }

    public MatrixF ScaleAt(float scaleX,
        float scaleY,
        float centerX,
        float centerY)
    {
        Translate(-centerX, -centerY);
        Scale(scaleX, scaleY);
        Translate(centerX, centerY);
        return this;
    }

    public MatrixF ScaleAtPrepend(float scaleX,
        float scaleY,
        float centerX,
        float centerY)
    {
        MatrixF m = Identity;
        m.ScaleAt(scaleX, scaleY, centerX, centerY);
        Prepend(m);
        return this;
    }

    public MatrixF ScalePrepend(float scaleX,
        float scaleY)
    {
        MatrixF m = Identity;
        m.Scale(scaleX, scaleY);
        Prepend(m);
        return this;
    }

    public void SetIdentity()
    {
        _m11 = _m22 = 1.0f;
        _m12 = _m21 = 0.0f;
        _offsetX = _offsetY = 0.0f;
    }

    public MatrixF Skew(float skewX,
        float skewY)
    {
        MatrixF skew_m = new MatrixF(1, MathF.Tan(skewY * MathF.PI / 180),
            MathF.Tan(skewX * MathF.PI / 180), 1,
            0, 0);
        Append(skew_m);
        return this;
    }

    public MatrixF SkewPrepend(float skewX,
        float skewY)
    {
        MatrixF m = Identity;
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

        var matrixFormat = string.Format(
            "{{0:{0}}}{1}{{1:{0}}}{1}{{2:{0}}}{1}{{3:{0}}}{1}{{4:{0}}}{1}{{5:{0}}}",
            format, separator);
        return string.Format(provider, matrixFormat,
            _m11, _m12, _m21, _m22, _offsetX, _offsetY);
    }

    public PointF Transform(PointF point)
    {
        return PointF.Multiply(point, this);
    }
    
    public PointF[] Transform(PointF[] points)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = Transform(points[i]);
        return points;
    }

    public VectorF Transform(VectorF vector)
    {
        return VectorF.Multiply(vector, this);
    }

    public void Transform(VectorF[] vectors)
    {
        for (int i = 0; i < vectors.Length; i++)
            vectors[i] = Transform(vectors[i]);
    }

    public MatrixF Translate(float offsetX,
        float offsetY)
    {
        _offsetX += offsetX;
        _offsetY += offsetY;
        return this;
    }

    public MatrixF TranslatePrepend(float offsetX,
        float offsetY)
    {
        MatrixF m = Identity;
        m.Translate(offsetX, offsetY);
        Prepend(m);
        return this;
    }

    public float Determinant
    {
        get { return _m11 * _m22 - _m12 * _m21; }
    }

    public bool HasInverse
    {
        get { return Determinant != 0; }
    }

    public static MatrixF Identity
    {
        get { return new MatrixF(1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f); }
    }

    public bool IsIdentity
    {
        get { return Equals(Identity); }
    }

    public float M11
    {
        get { return _m11; }
        set { _m11 = value; }
    }
    public float M12
    {
        get { return _m12; }
        set { _m12 = value; }
    }
    public float M21
    {
        get { return _m21; }
        set { _m21 = value; }
    }
    public float M22
    {
        get { return _m22; }
        set { _m22 = value; }
    }
    public float OffsetX
    {
        get { return _offsetX; }
        set { _offsetX = value; }
    }
    public float OffsetY
    {
        get { return _offsetY; }
        set { _offsetY = value; }
    }

    public MatrixF Translate(VectorF offset)
    {
        Translate(offset.X, offset.Y);
        return this;
    }


}