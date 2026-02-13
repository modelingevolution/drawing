using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 2D vector with X and Y components, using generic numeric types.
/// </summary>
/// <typeparam name="T">The numeric type used for vector components.</typeparam>
[ProtoContract]
[VectorJsonConverterAttribute]
[Svg.SvgExporter(typeof(VectorSvgExporterFactory))]
public struct Vector<T> : IFormattable, IEquatable<Vector<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Creates a random unit vector with a random angle.
    /// </summary>
    /// <returns>A random unit vector.</returns>
    public static Vector<T> Random()
    {
        T length = T.One;
        var angle = T.CreateTruncating(System.Random.Shared.NextSingle()) * T.Pi * T.CreateTruncating(2); 

        var x = T.Cos(angle) * length;
        var y = T.Sin(angle) * length;
        return new Vector<T>(x, y);
    }
    /// <summary>
    /// Transforms a vector by a matrix.
    /// </summary>
    /// <param name="vector">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector<T> Multiply(Vector<T> vector, Matrix<T> matrix)
    {
        return new Vector<T>(vector.X * matrix.M11 + vector.Y * matrix.M21,
            vector.X * matrix.M12 + vector.Y * matrix.M22);
    }

    /// <summary>
    /// Creates a vector from the specified X and Y components.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <returns>A new vector with the specified components.</returns>
    public static Vector<T> From(T x, T y) => new Vector<T>(x, y);
    /// <summary>
    /// Creates a vector with both X and Y components set to the same value.
    /// </summary>
    /// <param name="s">The value for both X and Y components.</param>
    /// <returns>A new vector with equal X and Y components.</returns>
    public static Vector<T> From(T s) => new Vector<T>(s, s);
    /// <summary>
    /// Initializes a new instance of the Vector struct with the specified X and Y components.
    /// </summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    public Vector(T x, T y)
    {
        this._x = x;
        this._y = y;
    }

    /// <summary>
    /// Determines whether the specified vector is equal to this vector.
    /// </summary>
    /// <param name="value">The vector to compare with this vector.</param>
    /// <returns>true if the specified vector is equal to this vector; otherwise, false.</returns>
    public bool Equals(Vector<T> value)
    {
        return _x == value.X && _y == value.Y;
    }

    /// <summary>
    /// Creates a vector that is mirrored across the X-axis (negates the Y component).
    /// </summary>
    /// <returns>A vector mirrored across the X-axis.</returns>
    public Vector<T> MirrorOX() => new Vector<T>(_x, -_y);
    /// <summary>
    /// Creates a vector that is mirrored across the Y-axis (negates the X component).
    /// </summary>
    /// <returns>A vector mirrored across the Y-axis.</returns>
    public Vector<T> MirrorOY() => new Vector<T>(-_x, _y);
    /// <summary>
    /// Determines whether the specified object is equal to this vector.
    /// </summary>
    /// <param name="o">The object to compare with this vector.</param>
    /// <returns>true if the specified object is equal to this vector; otherwise, false.</returns>
    public override bool Equals(object o)
    {
        if (!(o is Vector<T>))
            return false;

        return Equals((Vector<T>)o);
    }

    /// <summary>
    /// Returns the hash code for this vector.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
        }
    }

    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    /// <param name="vector1">The first vector to compare.</param>
    /// <param name="vector2">The second vector to compare.</param>
    /// <returns>true if the vectors are equal; otherwise, false.</returns>
    public static bool Equals(Vector<T> vector1, Vector<T> vector2)
    {
        return vector1.Equals(vector2);
    }

    /// <summary>
    /// Adds a vector to a point, resulting in a translated point.
    /// </summary>
    /// <param name="vector">The vector to add.</param>
    /// <param name="point">The point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Point<T> Add(Vector<T> vector, Point<T> point)
    {
        return new Point<T>(vector.X + point.X, vector.Y + point.Y);
    }

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="vector1">The first vector to add.</param>
    /// <param name="vector2">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    public static Vector<T> Add(Vector<T> vector1, Vector<T> vector2)
    {
        return new Vector<T>(vector1.X + vector2.X,
            vector1.Y + vector2.Y);
    }

    private static readonly T t180 = T.CreateTruncating((180));
    /// <summary>
    /// Calculates the angle between two vectors in degrees.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The angle between the vectors in degrees.</returns>
    public static T AngleBetween(Vector<T> vector1, Vector<T> vector2)
    {
        T cos_theta = (vector1.X * vector2.X + vector1.Y * vector2.Y) / (vector1.Length * vector2.Length);

        return T.Acos(cos_theta) / T.Pi * t180;
    }

    /// <summary>
    /// Calculates the cross product of two 2D vectors (returns the Z component of the 3D cross product).
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The cross product value.</returns>
    public static T CrossProduct(Vector<T> vector1, Vector<T> vector2)
    {
        // ... what operation is this exactly?
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    /// <summary>
    /// Calculates the determinant of the matrix formed by two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The determinant value.</returns>
    public static T Determinant(Vector<T> vector1, Vector<T> vector2)
    {
        // same as CrossProduct, it appears.
        return vector1.X * vector2.Y - vector1.Y * vector2.X;
    }

    /// <summary>
    /// Divides a vector by a scalar value.
    /// </summary>
    /// <param name="vector">The vector to divide.</param>
    /// <param name="scalar">The scalar value to divide by.</param>
    /// <returns>The divided vector.</returns>
    public static Vector<T> Divide(Vector<T> vector, T scalar)
    {
        return new Vector<T>(vector.X / scalar, vector.Y / scalar);
    }

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static T Multiply(Vector<T> vector1, Vector<T> vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y;
    }
    /// <summary>
    /// Calculates the projection of this vector onto another vector.
    /// </summary>
    /// <param name="direction">The vector to project onto.</param>
    /// <returns>The projection of this vector onto the specified direction.</returns>
    public Vector<T> Projection(Vector<T> direction)
    {
        return (this * direction) / (direction * direction) * direction;
    }
    //public static Vector<T> Multiply(Vector<T> vector, Matrix matrix)
    //{
    //    return new Vector<T>(vector.X * matrix.M11 + vector.Y * matrix.M21,
    //        vector.X * matrix.M12 + vector.Y * matrix.M22);
    //}

    /// <summary>
    /// Multiplies a vector by a scalar value.
    /// </summary>
    /// <param name="scalar">The scalar value to multiply by.</param>
    /// <param name="vector">The vector to multiply.</param>
    /// <returns>The scaled vector.</returns>
    public static Vector<T> Multiply(T scalar, Vector<T> vector)
    {
        return new Vector<T>(scalar * vector.X, scalar * vector.Y);
    }

    /// <summary>
    /// Multiplies a vector by a scalar value.
    /// </summary>
    /// <param name="vector">The vector to multiply.</param>
    /// <param name="scalar">The scalar value to multiply by.</param>
    /// <returns>The scaled vector.</returns>
    public static Vector<T> Multiply(Vector<T> vector, T scalar)
    {
        return new Vector<T>(scalar * vector.X, scalar * vector.Y);
    }

    /// <summary>
    /// Negates this vector (reverses its direction).
    /// </summary>
    public void Negate()
    {
        _x = -_x;
        _y = -_y;
    }

    /// <summary>
    /// Returns a normalized (unit) vector in the same direction as this vector.
    /// </summary>
    /// <returns>A normalized vector.</returns>
    /// <exception cref="ArgumentException">Thrown when attempting to normalize a zero vector.</exception>
    public Vector<T> Normalize()
    {
        T ls = LengthSquared;
        if (T.Abs(ls - T.One) < T.Epsilon)
            return this;
        if (ls == T.Zero) throw new ArgumentException("Cannot normalize zero.");
        T l = T.Sqrt(ls);
        return this / l;
    }

    /// <summary>
    /// Subtracts one vector from another.
    /// </summary>
    /// <param name="vector1">The vector to subtract from.</param>
    /// <param name="vector2">The vector to subtract.</param>
    /// <returns>The difference between the two vectors.</returns>
    public static Vector<T> Subtract(Vector<T> vector1, Vector<T> vector2)
    {
        return new Vector<T>(vector1.X - vector2.X, vector1.Y - vector2.Y);
    }
    /// <summary>
    /// Converts this vector to a different numeric type by truncating the components.
    /// </summary>
    /// <typeparam name="U">The target numeric type.</typeparam>
    /// <returns>A vector with components in the target type.</returns>
    public Vector<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>,
        IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Vector<U>(U.CreateTruncating(this._x), U.CreateTruncating(_y));
    }

    /// <summary>
    /// Parses a string representation of a vector.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>The parsed vector.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in a valid format.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the string contains invalid vector data.</exception>
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

    /// <summary>
    /// Returns a string representation of this vector.
    /// </summary>
    /// <returns>A string representation of the vector.</returns>
    public override string ToString()
    {
        return ToString(null);
    }

    /// <summary>
    /// Returns a string representation of this vector using the specified format provider.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the vector.</returns>
    public string ToString(IFormatProvider provider)
    {
        return ToString(null, provider);
    }

    /// <summary>
    /// Returns a string representation of this vector using the specified format and format provider.
    /// </summary>
    /// <param name="format">A format string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the vector.</returns>
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
    /// <summary>
    /// Gets the length (magnitude) of this vector.
    /// </summary>
    [JsonIgnore]
    public T Length
    {
        get { return T.Sqrt(LengthSquared); }
    }
    /// <summary>
    /// Gets the squared length of this vector. This is more efficient than Length when you only need to compare lengths.
    /// </summary>
    [JsonIgnore]
    public T LengthSquared
    {
        get { return _x * _x + _y * _y; }
    }

    /// <summary>
    /// Gets or sets the X component of this vector.
    /// </summary>
    [ProtoMember(1)]
    public T X
    {
        get => _x;
        set => _x = value;
    }
    /// <summary>
    /// Gets or sets the Y component of this vector.
    /// </summary>
    [ProtoMember(2)]
    public T Y
    {
        get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Explicitly converts a vector to a point.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <returns>A point with the same X and Y coordinates as the vector.</returns>
    public static explicit operator Point<T>(Vector<T> vector)
    {
        return new Point<T>(vector.X, vector.Y);
    }

    /// <summary>
    /// Implicitly converts a tuple to a Vector.
    /// </summary>
    /// <param name="tuple">The tuple containing X and Y components.</param>
    /// <returns>A Vector with the specified components.</returns>
    public static implicit operator Vector<T>((T x, T y) tuple)
    {
        return new Vector<T>(tuple.x, tuple.y);
    }

    /// <summary>
    /// Implicitly converts a Vector to a tuple.
    /// </summary>
    /// <param name="vector">The Vector to convert.</param>
    /// <returns>A tuple containing the X and Y components.</returns>
    public static implicit operator (T x, T y)(Vector<T> vector)
    {
        return (vector._x, vector._y);
    }



    /// <summary>
    /// Subtracts one vector from another.
    /// </summary>
    /// <param name="vector1">The vector to subtract from.</param>
    /// <param name="vector2">The vector to subtract.</param>
    /// <returns>The difference between the two vectors.</returns>
    public static Vector<T> operator -(Vector<T> vector1, Vector<T> vector2)
    {
        return Subtract(vector1, vector2);
    }

    /// <summary>
    /// Negates a vector (returns the opposite direction).
    /// </summary>
    /// <param name="vector">The vector to negate.</param>
    /// <returns>The negated vector.</returns>
    public static Vector<T> operator -(Vector<T> vector)
    {
        Vector<T> result = vector;
        result.Negate();
        return result;
    }

    /// <summary>
    /// Determines whether two vectors are not equal.
    /// </summary>
    /// <param name="vector1">The first vector to compare.</param>
    /// <param name="vector2">The second vector to compare.</param>
    /// <returns>true if the vectors are not equal; otherwise, false.</returns>
    public static bool operator !=(Vector<T> vector1, Vector<T> vector2)
    {
        return !Equals(vector1, vector2);
    }

    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    /// <param name="vector1">The first vector to compare.</param>
    /// <param name="vector2">The second vector to compare.</param>
    /// <returns>true if the vectors are equal; otherwise, false.</returns>
    public static bool operator ==(Vector<T> vector1, Vector<T> vector2)
    {
        return Equals(vector1, vector2);
    }

    /// <summary>
    /// Computes the dot product of two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static T operator *(Vector<T> vector1, Vector<T> vector2)
    {
        return Multiply(vector1, vector2);
    }

    /// <summary>
    /// Transforms a vector by a matrix.
    /// </summary>
    /// <param name="vector">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector<T> operator *(Vector<T> vector, Matrix<T> matrix)
    {
        return Multiply(vector, matrix);
    }

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    /// <param name="scalar">The scalar value.</param>
    /// <param name="vector">The vector to scale.</param>
    /// <returns>The scaled vector.</returns>
    public static Vector<T> operator *(T scalar, Vector<T> vector)
    {
        return Multiply(scalar, vector);
    }

    /// <summary>
    /// Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="vector">The vector to scale.</param>
    /// <param name="scalar">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    public static Vector<T> operator *(Vector<T> vector, T scalar)
    {
        return Multiply(vector, scalar);
    }
    /// <summary>
    /// Scales a vector by the width component of a size.
    /// </summary>
    /// <param name="vector">The vector to scale.</param>
    /// <param name="ratio">The size whose width is used as the scaling factor.</param>
    /// <returns>The scaled vector.</returns>
    public static Vector<T> operator *(Vector<T> vector, Size<T> ratio)
    {
        return new Vector<T>(vector.X * ratio.Width, vector.Y * ratio.Width);
    }
    /// <summary>
    /// Divides a vector by the width component of a size.
    /// </summary>
    /// <param name="vector">The vector to divide.</param>
    /// <param name="ratio">The size whose width is used as the divisor.</param>
    /// <returns>The divided vector.</returns>
    public static Vector<T> operator /(Vector<T> vector, Size<T> ratio)
    {
        return new Vector<T>(vector.X / ratio.Width, vector.Y / ratio.Width);
    }

    /// <summary>
    /// Divides a vector by a scalar value.
    /// </summary>
    /// <param name="vector">The vector to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The divided vector.</returns>
    public static Vector<T> operator /(Vector<T> vector, T scalar)
    {
        return Divide(vector, scalar);
    }

    /// <summary>
    /// Adds a vector to a point, producing a translated point.
    /// </summary>
    /// <param name="vector">The vector to add.</param>
    /// <param name="point">The point to translate.</param>
    /// <returns>The translated point.</returns>
    public static Point<T> operator +(Vector<T> vector, Point<T> point)
    {
        return Add(vector, point);
    }

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="vector1">The first vector to add.</param>
    /// <param name="vector2">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    public static Vector<T> operator +(Vector<T> vector1, Vector<T> vector2)
    {
        return Add(vector1, vector2);
    }

    T _x;
    T _y;

    /// <summary>
    /// Maps this 2D vector onto a 3D plane perpendicular to the given normal.
    /// For axis-aligned normals:
    /// - Z normal (top-down): (x, y) → (x, y, 0)
    /// - Y normal (front): (x, y) → (x, 0, y)
    /// - X normal (side): (x, y) → (0, x, y)
    /// </summary>
    /// <param name="planeNormal">The normal vector of the target plane.</param>
    /// <returns>A 3D vector lying on the plane.</returns>
    public Vector3<T> MapOnPlane(Vector3<T> planeNormal)
    {
        var absX = T.Abs(planeNormal.X);
        var absY = T.Abs(planeNormal.Y);
        var absZ = T.Abs(planeNormal.Z);

        if (absZ >= absX && absZ >= absY)
        {
            // Normal is mostly Z, plane is XY
            return new Vector3<T>(_x, _y, T.Zero);
        }
        else if (absY >= absX)
        {
            // Normal is mostly Y, plane is XZ
            return new Vector3<T>(_x, T.Zero, _y);
        }
        else
        {
            // Normal is mostly X, plane is YZ
            return new Vector3<T>(T.Zero, _x, _y);
        }
    }

    /// <summary>
    /// Computes the signed angle from this vector to another vector.
    /// The result is in the range (-π, π].
    /// Positive means counter-clockwise rotation.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The signed angle between the vectors in radians.</returns>
    public Radian<T> AngleBetween(in Vector<T> other)
    {
        var t1 = T.Atan2(_y, _x);
        var t2 = T.Atan2(other._y, other._x);
        var diff = t2 - t1;

        // Normalize to (-π, π]
        var pi = T.Pi;
        if (diff > pi) diff -= pi + pi;
        if (diff <= -pi) diff += pi + pi;

        return Radian<T>.FromRadian(diff);
    }

    /// <summary>
    /// Rotates this vector by the given angle.
    /// </summary>
    public Vector<T> Rotate(Degree<T> angle)
    {
        var rad = (Radian<T>)angle;
        var cos = T.Cos((T)rad);
        var sin = T.Sin((T)rad);
        return new Vector<T>(cos * _x - sin * _y, sin * _x + cos * _y);
    }

    /// <summary>
    /// Returns the perpendicular vector rotated 90 degrees clockwise.
    /// </summary>
    public Vector<T> PerpendicularCW => new Vector<T>(_y, -_x);

    /// <summary>
    /// Returns the perpendicular vector rotated 90 degrees counter-clockwise.
    /// </summary>
    public Vector<T> PerpendicularCCW => new Vector<T>(-_y, _x);

    /// <summary>
    /// Returns this vector reflected across the given normal.
    /// </summary>
    public Vector<T> Reflect(Vector<T> normal)
    {
        var d = _x * normal._x + _y * normal._y;
        var two = T.CreateTruncating(2);
        return new Vector<T>(_x - two * d * normal._x, _y - two * d * normal._y);
    }

    /// <summary>
    /// Computes the dot product of this vector with another.
    /// </summary>
    public T Dot(in Vector<T> other) => _x * other._x + _y * other._y;

    /// <summary>
    /// Linearly interpolates between this vector and another.
    /// </summary>
    public Vector<T> Lerp(in Vector<T> other, T t) =>
        new Vector<T>(_x + (other._x - _x) * t, _y + (other._y - _y) * t);

    /// <summary>
    /// Represents the unit vector in the X direction (1, 0).
    /// </summary>
    public static readonly Vector<T> EX = new Vector<T>(T.One, T.Zero);
    /// <summary>
    /// Represents the unit vector in the Y direction (0, 1).
    /// </summary>
    public static readonly Vector<T> EY = new Vector<T>(T.Zero, T.One);
    /// <summary>
    /// Represents the zero vector (0, 0).
    /// </summary>
    public static readonly Vector<T> Zero = new Vector<T>(T.Zero, T.Zero);
}