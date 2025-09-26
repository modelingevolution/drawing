using System.Globalization;
using System.Numerics;

namespace ModelingEvolution.Drawing;
using static ModelingEvolution.Drawing.Radian;

/// <summary>
/// Represents a 3x3 affine transformation matrix for 2D graphics operations.
/// </summary>
/// <typeparam name="T">The numeric type used for matrix elements.</typeparam>
public struct Matrix<T> : IFormattable
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{

    T _m11;
    T _m12;
    T _m21;
    T _m22;
    T _offsetX;
    T _offsetY;

    /// <summary>
    /// Initializes a new instance of the Matrix struct with the specified elements.
    /// </summary>
    /// <param name="m11">The element at row 1, column 1 of the matrix.</param>
    /// <param name="m12">The element at row 1, column 2 of the matrix.</param>
    /// <param name="m21">The element at row 2, column 1 of the matrix.</param>
    /// <param name="m22">The element at row 2, column 2 of the matrix.</param>
    /// <param name="offsetX">The X offset (translation) element.</param>
    /// <param name="offsetY">The Y offset (translation) element.</param>
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

    /// <summary>
    /// Multiplies this matrix by the specified matrix and stores the result in this matrix.
    /// </summary>
    /// <param name="other">The matrix to multiply by.</param>
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

    /// <summary>
    /// Determines whether the specified matrix is equal to this matrix.
    /// </summary>
    /// <param name="value">The matrix to compare.</param>
    /// <returns>true if the matrices are equal; otherwise, false.</returns>
    public bool Equals(Matrix<T> value)
    {
        return _m11 == value.M11 &&
                _m12 == value.M12 &&
                _m21 == value.M21 &&
                _m22 == value.M22 &&
                _offsetX == value.OffsetX &&
                _offsetY == value.OffsetY;
    }

    /// <summary>
    /// Determines whether the specified object is equal to this matrix.
    /// </summary>
    /// <param name="o">The object to compare.</param>
    /// <returns>true if the object is a matrix and is equal to this matrix; otherwise, false.</returns>
    public override bool Equals(object o)
    {
        if (!(o is Matrix<T>))
            return false;

        return Equals((Matrix<T>)o);
    }

    /// <summary>
    /// Determines whether two matrices are equal.
    /// </summary>
    /// <param name="matrix1">The first matrix to compare.</param>
    /// <param name="matrix2">The second matrix to compare.</param>
    /// <returns>true if the matrices are equal; otherwise, false.</returns>
    public static bool Equals(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return matrix1.Equals(matrix2);
    }

    /// <summary>
    /// Returns the hash code for this matrix.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
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

    /// <summary>
    /// Inverts this matrix if it is invertible.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the matrix is not invertible.</exception>
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

    /// <summary>
    /// Multiplies two matrices and returns the result.
    /// </summary>
    /// <param name="trans1">The first matrix to multiply.</param>
    /// <param name="trans2">The second matrix to multiply.</param>
    /// <returns>The product of the two matrices.</returns>
    public static Matrix<T> Multiply(Matrix<T> trans1,
        Matrix<T> trans2)
    {
        Matrix<T> m = trans1;
        m.Append(trans2);
        return m;
    }

    /// <summary>
    /// Determines whether two matrices are equal.
    /// </summary>
    /// <param name="matrix1">The first matrix to compare.</param>
    /// <param name="matrix2">The second matrix to compare.</param>
    /// <returns>true if the matrices are equal; otherwise, false.</returns>
    public static bool operator ==(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return matrix1.Equals(matrix2);
    }

    /// <summary>
    /// Determines whether two matrices are not equal.
    /// </summary>
    /// <param name="matrix1">The first matrix to compare.</param>
    /// <param name="matrix2">The second matrix to compare.</param>
    /// <returns>true if the matrices are not equal; otherwise, false.</returns>
    public static bool operator !=(Matrix<T> matrix1,
        Matrix<T> matrix2)
    {
        return !matrix1.Equals(matrix2);
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="trans1">The first matrix to multiply.</param>
    /// <param name="trans2">The second matrix to multiply.</param>
    /// <returns>The product of the two matrices.</returns>
    public static Matrix<T> operator *(Matrix<T> trans1,
        Matrix<T> trans2)
    {
        Matrix<T> result = trans1;
        result.Append(trans2);
        return result;
    }

    /// <summary>
    /// Parses a string representation of a matrix.
    /// </summary>
    /// <param name="source">The string representation of the matrix.</param>
    /// <returns>The parsed matrix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the source is null.</exception>
    /// <exception cref="FormatException">Thrown when the source is not in a valid format.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source contains invalid matrix data.</exception>
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

    /// <summary>
    /// Multiplies the specified matrix by this matrix and stores the result in this matrix.
    /// </summary>
    /// <param name="other">The matrix to multiply this matrix by.</param>
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

    /// <summary>
    /// Applies a rotation transformation to this matrix using degrees.
    /// </summary>
    /// <param name="angle">The rotation angle in degrees.</param>
    /// <returns>This matrix after the rotation transformation.</returns>
    public Matrix<T> Rotate(Degree<T> angle) => Rotate((Radian<T>)angle);
    /// <summary>
    /// Applies a rotation transformation to this matrix using radians.
    /// </summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <returns>This matrix after the rotation transformation.</returns>
    public Matrix<T> Rotate(Radian<T> theta)
    {
        Matrix<T> r_theta = new Matrix<T>(Cos(theta), Sin(theta),
            -Sin(theta), Cos(theta),
            T.Zero, T.Zero);

        Append(r_theta);
        return this;
    }

    /// <summary>
    /// Applies a rotation transformation around a specified point to this matrix.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="centerX">The X coordinate of the rotation center.</param>
    /// <param name="centerY">The Y coordinate of the rotation center.</param>
    /// <returns>This matrix after the rotation transformation.</returns>
    public Matrix<T> RotateAt(T angle,
        T centerX,
        T centerY)
    {
        Translate(-centerX, -centerY);
        Rotate(angle);
        Translate(centerX, centerY);
        return this;
    }

    /// <summary>
    /// Prepends a rotation transformation around a specified point to this matrix.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <param name="centerX">The X coordinate of the rotation center.</param>
    /// <param name="centerY">The Y coordinate of the rotation center.</param>
    /// <returns>This matrix after the rotation transformation.</returns>
    public Matrix<T> RotateAtPrepend(T angle,
        T centerX,
        T centerY)
    {
        Matrix<T> m = Identity;
        m.RotateAt(angle, centerX, centerY);
        Prepend(m);
        return this;
    }

    /// <summary>
    /// Prepends a rotation transformation to this matrix.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <returns>This matrix after the rotation transformation.</returns>
    public Matrix<T> RotatePrepend(T angle)
    {
        Matrix<T> m = Identity;
        m.Rotate(angle);
        Prepend(m);
        return this;
    }


    /// <summary>
    /// Applies a scaling transformation to this matrix.
    /// </summary>
    /// <param name="scaleX">The scale factor along the X-axis.</param>
    /// <param name="scaleY">The scale factor along the Y-axis.</param>
    /// <returns>This matrix after the scaling transformation.</returns>
    public Matrix<T> Scale(T scaleX,
        T scaleY)
    {
        Matrix<T> scale = new Matrix<T>(scaleX, T.Zero,
            T.Zero, scaleY,
            T.Zero, T.Zero);

        Append(scale);
        return this;
    }

    /// <summary>
    /// Applies a scaling transformation around a specified point to this matrix.
    /// </summary>
    /// <param name="scaleX">The scale factor along the X-axis.</param>
    /// <param name="scaleY">The scale factor along the Y-axis.</param>
    /// <param name="centerX">The X coordinate of the scaling center.</param>
    /// <param name="centerY">The Y coordinate of the scaling center.</param>
    /// <returns>This matrix after the scaling transformation.</returns>
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

    /// <summary>
    /// Prepends a scaling transformation around a specified point to this matrix.
    /// </summary>
    /// <param name="scaleX">The scale factor along the X-axis.</param>
    /// <param name="scaleY">The scale factor along the Y-axis.</param>
    /// <param name="centerX">The X coordinate of the scaling center.</param>
    /// <param name="centerY">The Y coordinate of the scaling center.</param>
    /// <returns>This matrix after the scaling transformation.</returns>
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

    /// <summary>
    /// Prepends a scaling transformation to this matrix.
    /// </summary>
    /// <param name="scaleX">The scale factor along the X-axis.</param>
    /// <param name="scaleY">The scale factor along the Y-axis.</param>
    /// <returns>This matrix after the scaling transformation.</returns>
    public Matrix<T> ScalePrepend(T scaleX,
        T scaleY)
    {
        Matrix<T> m = Identity;
        m.Scale(scaleX, scaleY);
        Prepend(m);
        return this;
    }

    /// <summary>
    /// Resets this matrix to the identity matrix.
    /// </summary>
    public void SetIdentity()
    {
        _m11 = _m22 = T.One;
        _m12 = _m21 = T.Zero;
        _offsetX = _offsetY = T.Zero;
    }
    readonly static T deg180 = T.CreateTruncating(180);
    /// <summary>
    /// Applies a skew transformation to this matrix.
    /// </summary>
    /// <param name="skewX">The skew angle along the X-axis in degrees.</param>
    /// <param name="skewY">The skew angle along the Y-axis in degrees.</param>
    /// <returns>This matrix after the skew transformation.</returns>
    public Matrix<T> Skew(T skewX,
        T skewY)
    {
        Matrix<T> skew_m = new Matrix<T>(T.One, T.Tan(skewY * T.Pi / deg180),
            T.Tan(skewX * T.Pi / deg180), T.One,
            T.Zero, T.Zero);
        Append(skew_m);
        return this;
    }

    /// <summary>
    /// Prepends a skew transformation to this matrix.
    /// </summary>
    /// <param name="skewX">The skew angle along the X-axis in degrees.</param>
    /// <param name="skewY">The skew angle along the Y-axis in degrees.</param>
    /// <returns>This matrix after the skew transformation.</returns>
    public Matrix<T> SkewPrepend(T skewX,
        T skewY)
    {
        Matrix<T> m = Identity;
        m.Skew(skewX, skewY);
        Prepend(m);
        return this;
    }

    /// <summary>
    /// Returns a string representation of this matrix.
    /// </summary>
    /// <returns>A string representation of the matrix.</returns>
    public override string ToString()
    {
        return ToString(null);
    }

    /// <summary>
    /// Returns a string representation of this matrix using the specified format provider.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix.</returns>
    public string ToString(IFormatProvider provider)
    {
        return ToString(null, provider);
    }

    /// <summary>
    /// Returns a string representation of this matrix using the specified format and format provider.
    /// </summary>
    /// <param name="format">A format string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix.</returns>
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

    /// <summary>
    /// Transforms the specified point using this matrix.
    /// </summary>
    /// <param name="point">The point to transform.</param>
    /// <returns>The transformed point.</returns>
    public Point<T> Transform(Point<T> point)
    {
        return Point<T>.Multiply(point, this);
    }
    
    /// <summary>
    /// Transforms an array of points using this matrix.
    /// </summary>
    /// <param name="points">The array of points to transform.</param>
    /// <returns>The array of transformed points.</returns>
    public Point<T>[] Transform(Point<T>[] points)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] = Transform(points[i]);
        return points;
    }

    /// <summary>
    /// Transforms the specified vector using this matrix.
    /// </summary>
    /// <param name="vector">The vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public Vector<T> Transform(Vector<T> vector)
    {
        return Vector<T>.Multiply(vector, this);
    }

    /// <summary>
    /// Transforms an array of vectors using this matrix.
    /// </summary>
    /// <param name="vectors">The array of vectors to transform.</param>
    public void Transform(Vector<T>[] vectors)
    {
        for (int i = 0; i < vectors.Length; i++)
            vectors[i] = Transform(vectors[i]);
    }

    /// <summary>
    /// Applies a translation transformation to this matrix.
    /// </summary>
    /// <param name="offsetX">The translation offset along the X-axis.</param>
    /// <param name="offsetY">The translation offset along the Y-axis.</param>
    /// <returns>This matrix after the translation transformation.</returns>
    public Matrix<T> Translate(T offsetX,
        T offsetY)
    {
        _offsetX += offsetX;
        _offsetY += offsetY;
        return this;
    }

    /// <summary>
    /// Prepends a translation transformation to this matrix.
    /// </summary>
    /// <param name="offsetX">The translation offset along the X-axis.</param>
    /// <param name="offsetY">The translation offset along the Y-axis.</param>
    /// <returns>This matrix after the translation transformation.</returns>
    public Matrix<T> TranslatePrepend(T offsetX,
        T offsetY)
    {
        Matrix<T> m = Identity;
        m.Translate(offsetX, offsetY);
        Prepend(m);
        return this;
    }

    /// <summary>
    /// Gets the determinant of this matrix.
    /// </summary>
    public T Determinant
    {
        get { return _m11 * _m22 - _m12 * _m21; }
    }

    /// <summary>
    /// Gets a value indicating whether this matrix is invertible.
    /// </summary>
    public bool HasInverse
    {
        get { return Determinant != T.Zero; }
    }

    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Matrix<T> Identity
    {
        get { return new Matrix<T>(T.One, T.Zero, T.Zero, T.One, T.Zero, T.Zero); }
    }

    /// <summary>
    /// Gets a value indicating whether this matrix is the identity matrix.
    /// </summary>
    public bool IsIdentity
    {
        get { return Equals(Identity); }
    }

    /// <summary>
    /// Gets or sets the element at row 1, column 1 of this matrix.
    /// </summary>
    public T M11
    {
        get { return _m11; }
        set { _m11 = value; }
    }
    /// <summary>
    /// Gets or sets the element at row 1, column 2 of this matrix.
    /// </summary>
    public T M12
    {
        get { return _m12; }
        set { _m12 = value; }
    }
    /// <summary>
    /// Gets or sets the element at row 2, column 1 of this matrix.
    /// </summary>
    public T M21
    {
        get { return _m21; }
        set { _m21 = value; }
    }
    /// <summary>
    /// Gets or sets the element at row 2, column 2 of this matrix.
    /// </summary>
    public T M22
    {
        get { return _m22; }
        set { _m22 = value; }
    }
    /// <summary>
    /// Gets or sets the X translation offset of this matrix.
    /// </summary>
    public T OffsetX
    {
        get { return _offsetX; }
        set { _offsetX = value; }
    }
    /// <summary>
    /// Gets or sets the Y translation offset of this matrix.
    /// </summary>
    public T OffsetY
    {
        get { return _offsetY; }
        set { _offsetY = value; }
    }

    /// <summary>
    /// Applies a translation transformation using a vector to this matrix.
    /// </summary>
    /// <param name="offset">The translation vector.</param>
    /// <returns>This matrix after the translation transformation.</returns>
    public Matrix<T> Translate(Vector<T> offset)
    {
        Translate(offset.X, offset.Y);
        return this;
    }


}