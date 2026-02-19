using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D pose (position + orientation) using generic numeric types.
/// Combines Point3{T} (x, y, z) and Rotation3{T} (rx, ry, rz).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and angles.</typeparam>
[ProtoContract]
[Pose3JsonConverterAttribute]
public struct Pose3<T> : IEquatable<Pose3<T>>, IParsable<Pose3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private Point3<T> _position;
    private Rotation3<T> _rotation;

    /// <summary>
    /// Represents identity pose (origin with no rotation).
    /// </summary>
    public static readonly Pose3<T> Identity = new(Point3<T>.Zero, Rotation3<T>.Identity);

    /// <summary>
    /// Initializes a new pose with the specified position and rotation.
    /// </summary>
    public Pose3(Point3<T> position, Rotation3<T> rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    /// <summary>
    /// Initializes a new pose with the specified components.
    /// </summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="z">Z position.</param>
    /// <param name="rx">Rotation around X axis in degrees.</param>
    /// <param name="ry">Rotation around Y axis in degrees.</param>
    /// <param name="rz">Rotation around Z axis in degrees.</param>
    public Pose3(T x, T y, T z, T rx, T ry, T rz)
    {
        _position = new Point3<T>(x, y, z);
        _rotation = new Rotation3<T>(rx, ry, rz);
    }

    /// <summary>
    /// Gets or sets the position component.
    /// </summary>
    [ProtoMember(1)]
    public Point3<T> Position
    {
        readonly get => _position;
        set => _position = value;
    }

    /// <summary>
    /// Gets or sets the rotation component.
    /// </summary>
    [ProtoMember(2)]
    public Rotation3<T> Rotation
    {
        readonly get => _rotation;
        set => _rotation = value;
    }

    /// <summary>
    /// Gets or sets the X position.
    /// </summary>
    public T X
    {
        readonly get => _position.X;
        set => _position.X = value;
    }

    /// <summary>
    /// Gets or sets the Y position.
    /// </summary>
    public T Y
    {
        readonly get => _position.Y;
        set => _position.Y = value;
    }

    /// <summary>
    /// Gets or sets the Z position.
    /// </summary>
    public T Z
    {
        readonly get => _position.Z;
        set => _position.Z = value;
    }

    /// <summary>
    /// Gets or sets the rotation around X axis in degrees.
    /// </summary>
    public Degree<T> Rx
    {
        readonly get => _rotation.Rx;
        set => _rotation.Rx = value;
    }

    /// <summary>
    /// Gets or sets the rotation around Y axis in degrees.
    /// </summary>
    public Degree<T> Ry
    {
        readonly get => _rotation.Ry;
        set => _rotation.Ry = value;
    }

    /// <summary>
    /// Gets or sets the rotation around Z axis in degrees.
    /// </summary>
    public Degree<T> Rz
    {
        readonly get => _rotation.Rz;
        set => _rotation.Rz = value;
    }

    /// <summary>
    /// Gets a value indicating whether this pose is identity (origin with no rotation).
    /// </summary>
    public readonly bool IsIdentity => _position.IsEmpty && _rotation.IsIdentity;

    /// <summary>
    /// Transforms a point from local coordinates to world coordinates using this pose.
    /// </summary>
    public readonly Point3<T> TransformPoint(Point3<T> localPoint)
    {
        var rotated = _rotation.Rotate(localPoint);
        return rotated + (Vector3<T>)_position;
    }

    /// <summary>
    /// Transforms a vector from local coordinates to world coordinates using this pose's rotation.
    /// </summary>
    public readonly Vector3<T> TransformVector(Vector3<T> localVector)
    {
        return _rotation.Rotate(localVector);
    }

    /// <summary>
    /// Returns the inverse of this pose.
    /// </summary>
    public readonly Pose3<T> Inverse()
    {
        var invRotation = _rotation.Inverse();
        var negPosition = new Vector3<T>(-_position.X, -_position.Y, -_position.Z);
        var invPosition = invRotation.Rotate(negPosition);
        return new Pose3<T>((Point3<T>)invPosition, invRotation);
    }

    /// <summary>
    /// Combines two poses (this * other).
    /// </summary>
    public readonly Pose3<T> Multiply(Pose3<T> other)
    {
        var newRotation = Rotation3<T>.Combine(_rotation, other._rotation);
        var rotatedPosition = _rotation.Rotate(other._position);
        var newPosition = _position + (Vector3<T>)rotatedPosition;
        return new Pose3<T>(newPosition, newRotation);
    }

    /// <summary>
    /// Linearly interpolates between two poses.
    /// </summary>
    public static Pose3<T> Lerp(Pose3<T> a, Pose3<T> b, T t)
    {
        var position = Point3<T>.Lerp(a._position, b._position, t);
        var rotation = Rotation3<T>.Slerp(a._rotation, b._rotation, t);
        return new Pose3<T>(position, rotation);
    }

    /// <summary>
    /// Calculates the distance between two poses (position only).
    /// </summary>
    public static T Distance(Pose3<T> a, Pose3<T> b)
    {
        return Point3<T>.Distance(a._position, b._position);
    }

    /// <summary>
    /// Converts this pose to a different numeric type.
    /// </summary>
    public Pose3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Pose3<U>(_position.Truncating<U>(), _rotation.Truncating<U>());
    }

    #region Operators

    public static Pose3<T> operator *(Pose3<T> a, Pose3<T> b) => a.Multiply(b);
    public static Pose3<T> operator +(Pose3<T> pose, Vector3<T> offset) => new(pose._position + offset, pose._rotation);
    public static Pose3<T> operator -(Pose3<T> pose, Vector3<T> offset) => new(pose._position - offset, pose._rotation);
    public static Pose3<T> operator +(Pose3<T> pose, Rotation3<T> rotation) => new(pose._position, Rotation3<T>.Combine(pose._rotation, rotation));
    public static bool operator ==(Pose3<T> a, Pose3<T> b) => a._position == b._position && a._rotation == b._rotation;
    public static bool operator !=(Pose3<T> a, Pose3<T> b) => !(a == b);

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a pose from position and rotation.
    /// </summary>
    public static Pose3<T> From(Point3<T> position, Rotation3<T> rotation) => new(position, rotation);

    /// <summary>
    /// Creates a pose from position vector and rotation.
    /// </summary>
    public static Pose3<T> From(Vector3<T> position, Rotation3<T> rotation) => new((Point3<T>)position, rotation);

    /// <summary>
    /// Creates a pose from three points defining a surface plane using the right-hand rule.
    /// Z direction is determined by (b-a) x (c-a). Reversing point order reverses Z direction.
    /// </summary>
    /// <param name="a">First point - becomes the origin, also defines X-axis direction with b.</param>
    /// <param name="b">Second point - defines X-axis direction from a.</param>
    /// <param name="c">Third point - completes the plane; a-b-c order determines Z via right-hand rule.</param>
    /// <returns>A pose where origin is at a, X-axis along a-b, Z-axis per right-hand rule.</returns>
    /// <exception cref="ArgumentException">Thrown when points a, b, c are collinear.</exception>
    public static Pose3<T> FromSurface(Point3<T> a, Point3<T> b, Point3<T> c)
    {
        var epsilon = T.CreateTruncating(1e-9);

        // Edge vectors
        var ab = b - a;
        var ac = c - a;

        // Plane normal via right-hand rule: Z = (b-a) x (c-a)
        var zAxis = Vector3<T>.Cross(ab, ac);
        var normalLength = zAxis.Length;

        if (normalLength < epsilon)
            throw new ArgumentException("Points a, b, c are collinear and do not define a plane.");

        zAxis = zAxis / normalLength; // Normalize

        // X-axis along edge a-b, normalized
        var xAxis = ab.Normalize();

        // Y-axis completes right-hand system: Y = Z x X
        var yAxis = Vector3<T>.Cross(zAxis, xAxis);

        // Convert orthonormal basis to Euler angles (ZYX convention)
        var rotation = RotationFromAxes(xAxis, yAxis, zAxis);

        return new Pose3<T>(a, rotation);
    }

    /// <summary>
    /// Creates a pose from three points defining a surface plane and a hint point indicating the Z direction.
    /// </summary>
    /// <param name="a">First point on the surface (also defines X-axis origin direction with b).</param>
    /// <param name="b">Second point on the surface (defines X-axis direction from a).</param>
    /// <param name="c">Third point on the surface (completes the plane definition).</param>
    /// <param name="h">Hint point off the surface - Z-axis will point toward this point.</param>
    /// <returns>A pose where origin is the projection of h onto the plane, X-Y plane is the surface, and Z points toward h.</returns>
    /// <exception cref="ArgumentException">Thrown when h lies on the surface plane.</exception>
    /// <exception cref="ArgumentException">Thrown when points a, b, c are collinear.</exception>
    public static Pose3<T> FromSurface(Point3<T> a, Point3<T> b, Point3<T> c, Point3<T> h)
    {
        var epsilon = T.CreateTruncating(1e-9);

        // Edge vectors
        var ab = b - a;
        var ac = c - a;

        // Plane normal (unsigned)
        var normal = Vector3<T>.Cross(ab, ac);
        var normalLength = normal.Length;

        if (normalLength < epsilon)
            throw new ArgumentException("Points a, b, c are collinear and do not define a plane.");

        normal = normal / normalLength; // Normalize

        // Signed distance from h to plane
        var ah = h - a;
        var d = Vector3<T>.Dot(ah, normal);

        if (T.Abs(d) < epsilon)
            throw new ArgumentException("Point h lies on the surface plane. Cannot determine Z direction.", nameof(h));

        // Origin = foot of perpendicular from h to plane
        var origin = new Point3<T>(
            h.X - d * normal.X,
            h.Y - d * normal.Y,
            h.Z - d * normal.Z);

        // Z-axis points toward h
        var zAxis = d > T.Zero ? normal : -normal;

        // X-axis along edge a-b, normalized
        var xAxis = ab.Normalize();

        // Y-axis completes right-hand system: Y = Z x X
        var yAxis = Vector3<T>.Cross(zAxis, xAxis);

        // Convert orthonormal basis to Euler angles (ZYX convention)
        var rotation = RotationFromAxes(xAxis, yAxis, zAxis);

        return new Pose3<T>(origin, rotation);
    }

    /// <summary>
    /// Creates a Rotation3 from orthonormal basis vectors (X, Y, Z axes).
    /// The resulting rotation satisfies: Rotate(EX)=X, Rotate(EY)=Y, Rotate(EZ)=Z.
    /// </summary>
    private static Rotation3<T> RotationFromAxes(Vector3<T> xAxis, Vector3<T> yAxis, Vector3<T> zAxis)
    {
        var one = T.One;
        var epsilon = T.CreateTruncating(1e-6);

        // Clamp to [-1, 1] to avoid NaN from asin
        var sinPitch = -xAxis.Z;
        if (sinPitch > one) sinPitch = one;
        if (sinPitch < -one) sinPitch = -one;

        var ry = T.Asin(sinPitch); // Pitch
        var cosPitch = T.Cos(ry);

        T rx, rz;

        if (T.Abs(cosPitch) > epsilon)
        {
            // Normal case
            rx = T.Atan2(yAxis.Z, zAxis.Z); // Roll
            rz = T.Atan2(xAxis.Y, xAxis.X); // Yaw
        }
        else
        {
            // Gimbal lock - pitch is +/-90 deg
            rx = T.Atan2(-zAxis.Y, yAxis.Y);
            rz = T.Zero;
        }

        // rx, ry, rz are in radians — convert via Radian<T> → Degree<T>
        return new Rotation3<T>(
            (Degree<T>)Radian<T>.FromRadian(rx),
            (Degree<T>)Radian<T>.FromRadian(ry),
            (Degree<T>)Radian<T>.FromRadian(rz));
    }

    #endregion

    #region Conversions

    public static implicit operator Pose3<T>((T x, T y, T z, T rx, T ry, T rz) tuple) =>
        new(tuple.x, tuple.y, tuple.z, tuple.rx, tuple.ry, tuple.rz);

    public static implicit operator (T x, T y, T z, T rx, T ry, T rz)(Pose3<T> pose) =>
        (pose.X, pose.Y, pose.Z, (T)pose.Rx, (T)pose.Ry, (T)pose.Rz);

    /// <summary>
    /// Deconstructs into position and rotation.
    /// </summary>
    public readonly void Deconstruct(out Point3<T> position, out Rotation3<T> rotation)
    {
        position = _position;
        rotation = _rotation;
    }

    /// <summary>
    /// Deconstructs into individual components.
    /// </summary>
    public readonly void Deconstruct(out T x, out T y, out T z, out T rx, out T ry, out T rz)
    {
        x = X;
        y = Y;
        z = Z;
        rx = (T)Rx;
        ry = (T)Ry;
        rz = (T)Rz;
    }

    #endregion

    #region Equality & Formatting

    public readonly bool Equals(Pose3<T> other) => this == other;
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Pose3<T> p && Equals(p);
    public override readonly int GetHashCode() => HashCode.Combine(_position, _rotation);

    public override readonly string ToString() =>
        $"{{X={X}, Y={Y}, Z={Z}, Rx={(T)Rx}, Ry={(T)Ry}, Rz={(T)Rz}}}";

    #endregion

    #region Parsing

    public static Pose3<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz))
            throw new FormatException($"Invalid Pose3<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Pose3<T> format: {source}");

        return new Pose3<T>(x, y, z, rx, ry, rz);
    }

    public static bool TryParse(string? source, IFormatProvider? provider, out Pose3<T> result)
    {
        result = Identity;
        if (source == null) return false;

        try
        {
            var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

            if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
                !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz) ||
                !tokenizer.HasNoMoreTokens())
                return false;

            result = new Pose3<T>(x, y, z, rx, ry, rz);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
