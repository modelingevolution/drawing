using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D pose (position + orientation) using generic numeric types.
/// Combines Point3{T} (x, y, z) and Rotation3{T} (rx, ry, rz).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and angles.</typeparam>
[ProtoContract]
[Pose3JsonConverterAttribute]
public readonly struct Pose3<T> : IEquatable<Pose3<T>>, IParsable<Pose3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Point3<T> _position;
    private readonly Rotation3<T> _rotation;

    /// <summary>
    /// Represents identity pose (origin with no rotation).
    /// </summary>
    public static readonly Pose3<T> Identity = new(Point3<T>.Zero, Rotation3<T>.Identity);

    /// <summary>
    /// Initializes a new pose with the specified position and rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3(T x, T y, T z, T rx, T ry, T rz)
    {
        _position = new Point3<T>(x, y, z);
        _rotation = new Rotation3<T>(rx, ry, rz);
    }

    /// <summary>
    /// Initializes a new pose with the specified position and rotation angles in degrees.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3(T x, T y, T z, Degree<T> rx, Degree<T> ry, Degree<T> rz)
    {
        _position = new Point3<T>(x, y, z);
        _rotation = new Rotation3<T>(rx, ry, rz);
    }

    /// <summary>
    /// Gets or initializes the position component.
    /// </summary>
    [ProtoMember(1)]
    public Point3<T> Position
    {
        get => _position;
        init => _position = value;
    }

    /// <summary>
    /// Gets or initializes the rotation component.
    /// </summary>
    [ProtoMember(2)]
    public Rotation3<T> Rotation
    {
        get => _rotation;
        init => _rotation = value;
    }

    /// <summary>
    /// Gets the X position.
    /// </summary>
    public T X { get => _position.X; }

    /// <summary>
    /// Gets the Y position.
    /// </summary>
    public T Y { get => _position.Y; }

    /// <summary>
    /// Gets the Z position.
    /// </summary>
    public T Z { get => _position.Z; }

    /// <summary>
    /// Gets the rotation around X axis in degrees.
    /// </summary>
    public Degree<T> Rx { get => _rotation.Rx; }

    /// <summary>
    /// Gets the rotation around Y axis in degrees.
    /// </summary>
    public Degree<T> Ry { get => _rotation.Ry; }

    /// <summary>
    /// Gets the rotation around Z axis in degrees.
    /// </summary>
    public Degree<T> Rz { get => _rotation.Rz; }

    /// <summary>
    /// Gets a value indicating whether this pose is identity (origin with no rotation).
    /// </summary>
    public bool IsIdentity => _position.IsEmpty && _rotation.IsIdentity;

    /// <summary>
    /// Transforms a point from local coordinates to world coordinates using this pose.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3<T> TransformPoint(Point3<T> localPoint)
    {
        var rotated = _rotation.Rotate(localPoint);
        return rotated + (Vector3<T>)_position;
    }

    /// <summary>
    /// Transforms a vector from local coordinates to world coordinates using this pose's rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<T> TransformVector(Vector3<T> localVector)
    {
        return _rotation.Rotate(localVector);
    }

    /// <summary>
    /// Returns the inverse of this pose.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<T> Inverse()
    {
        var invRotation = _rotation.Inverse();
        var negPosition = new Vector3<T>(-_position.X, -_position.Y, -_position.Z);
        var invPosition = invRotation.Rotate(negPosition);
        return new Pose3<T>((Point3<T>)invPosition, invRotation);
    }

    /// <summary>
    /// Combines two poses (this * other).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<T> Multiply(Pose3<T> other)
    {
        var newRotation = Rotation3<T>.Combine(_rotation, other._rotation);
        var rotatedPosition = _rotation.Rotate(other._position);
        var newPosition = _position + (Vector3<T>)rotatedPosition;
        return new Pose3<T>(newPosition, newRotation);
    }

    /// <summary>
    /// Linearly interpolates between two poses.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> Lerp(Pose3<T> a, Pose3<T> b, T t)
    {
        var position = Point3<T>.Lerp(a._position, b._position, t);
        var rotation = Rotation3<T>.Slerp(a._rotation, b._rotation, t);
        return new Pose3<T>(position, rotation);
    }

    /// <summary>
    /// Calculates the distance between two poses (position only).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Distance(Pose3<T> a, Pose3<T> b)
    {
        return Point3<T>.Distance(a._position, b._position);
    }

    /// <summary>
    /// Converts this pose to a different numeric type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Pose3<U>(_position.Truncating<U>(), _rotation.Truncating<U>());
    }

    #region Operators

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> operator *(Pose3<T> a, Pose3<T> b) => a.Multiply(b);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> operator +(Pose3<T> pose, Vector3<T> offset) => new(pose._position + offset, pose._rotation);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> operator -(Pose3<T> pose, Vector3<T> offset) => new(pose._position - offset, pose._rotation);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> operator +(Pose3<T> pose, Rotation3<T> rotation) => new(pose._position, Rotation3<T>.Combine(pose._rotation, rotation));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Pose3<T> a, Pose3<T> b) => a._position == b._position && a._rotation == b._rotation;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Pose3<T> a, Pose3<T> b) => !(a == b);

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a pose from position and rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pose3<T> From(Point3<T> position, Rotation3<T> rotation) => new(position, rotation);

    /// <summary>
    /// Creates a pose from position vector and rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    /// Creates a pose from N points defining a surface plane using least-squares best-fit.
    /// Origin is the centroid. Z-axis is the best-fit normal (oriented via right-hand rule
    /// of the first three points). X-axis is the projection of edge a→b onto the plane.
    /// </summary>
    /// <param name="points">At least 3 non-collinear points on or near the surface.</param>
    /// <returns>A pose where origin is the centroid, Z-axis is the best-fit surface normal.</returns>
    /// <exception cref="ArgumentException">Thrown when fewer than 3 points or points are collinear.</exception>
    public static Pose3<T> FromSurface(ReadOnlySpan<Point3<T>> points)
    {
        if (points.Length < 3)
            throw new ArgumentException("At least 3 points are required to define a surface.", nameof(points));

        if (points.Length == 3)
            return FromSurface(points[0], points[1], points[2]);

        var epsilon = T.CreateTruncating(1e-9);
        var n = T.CreateTruncating(points.Length);

        // 1. Centroid
        var cx = T.Zero;
        var cy = T.Zero;
        var cz = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            cx += points[i].X;
            cy += points[i].Y;
            cz += points[i].Z;
        }
        cx /= n;
        cy /= n;
        cz /= n;
        var centroid = new Point3<T>(cx, cy, cz);

        // 2. Scatter matrix (symmetric 3x3): S = Σ (pi - centroid)(pi - centroid)^T
        var sxx = T.Zero;
        var sxy = T.Zero;
        var sxz = T.Zero;
        var syy = T.Zero;
        var syz = T.Zero;
        var szz = T.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            var dx = points[i].X - cx;
            var dy = points[i].Y - cy;
            var dz = points[i].Z - cz;
            sxx += dx * dx;
            sxy += dx * dy;
            sxz += dx * dz;
            syy += dy * dy;
            syz += dy * dz;
            szz += dz * dz;
        }

        // 3. Normal = eigenvector of smallest eigenvalue of scatter matrix
        var zAxis = SmallestEigenvector3x3Symmetric(sxx, sxy, sxz, syy, syz, szz);

        // 4. Orient via right-hand rule of first three points
        var ab = points[1] - points[0];
        var ac = points[2] - points[0];
        var cross = Vector3<T>.Cross(ab, ac);
        if (Vector3<T>.Dot(cross, zAxis) < T.Zero)
            zAxis = -zAxis;

        // 5. X-axis: project edge a→b onto the plane, normalize
        var proj = Vector3<T>.Dot(ab, zAxis);
        var xAxis = new Vector3<T>(
            ab.X - proj * zAxis.X,
            ab.Y - proj * zAxis.Y,
            ab.Z - proj * zAxis.Z);
        var xLen = xAxis.Length;
        if (xLen < epsilon)
            throw new ArgumentException("Cannot determine X-axis: first two points project to the same location on the best-fit plane.");
        xAxis = xAxis / xLen;

        // 6. Y = Z × X (right-hand system)
        var yAxis = Vector3<T>.Cross(zAxis, xAxis);

        var rotation = RotationFromAxes(xAxis, yAxis, zAxis);
        return new Pose3<T>(centroid, rotation);
    }

    /// <summary>
    /// Finds the eigenvector corresponding to the smallest eigenvalue of a 3×3 symmetric matrix.
    /// Uses the analytical trigonometric method for eigenvalues, then cross-product of rows for the eigenvector.
    /// </summary>
    private static Vector3<T> SmallestEigenvector3x3Symmetric(T sxx, T sxy, T sxz, T syy, T syz, T szz)
    {
        var two = T.CreateTruncating(2);
        var three = T.CreateTruncating(3);
        var six = T.CreateTruncating(6);
        var epsilon = T.CreateTruncating(1e-12);

        // Shift to zero-mean: K = S - m*I where m = trace/3
        var m = (sxx + syy + szz) / three;
        var kxx = sxx - m;
        var kyy = syy - m;
        var kzz = szz - m;

        // q = ||K||_F^2 / 6
        var q = (kxx * kxx + kyy * kyy + kzz * kzz
                 + two * (sxy * sxy + sxz * sxz + syz * syz)) / six;
        var p = T.Sqrt(q);

        if (p < epsilon)
        {
            // Scatter is (near-)isotropic — no well-defined plane.
            // Fall back: try cross-product of first available pair
            throw new ArgumentException("Points are collinear or coincident and do not define a plane.");
        }

        // B = K / p (normalized)
        var invP = T.One / p;
        var bxx = kxx * invP;
        var bxy = sxy * invP;
        var bxz = sxz * invP;
        var byy = kyy * invP;
        var byz = syz * invP;
        var bzz = kzz * invP;

        // halfDetB = det(B) / 2, clamped to [-1, 1]
        var halfDetB = (bxx * (byy * bzz - byz * byz)
                        - bxy * (bxy * bzz - byz * bxz)
                        + bxz * (bxy * byz - byy * bxz)) / two;
        if (halfDetB > T.One) halfDetB = T.One;
        if (halfDetB < -T.One) halfDetB = -T.One;

        var phi = T.Acos(halfDetB) / three;

        // Smallest eigenvalue: e3 = m + 2p·cos(φ + 2π/3)
        var twoPiOverThree = two * T.Pi / three;
        var e3 = m + two * p * T.Cos(phi + twoPiOverThree);

        // Eigenvector via cross-product of rows of (S - e3·I)
        var r0 = new Vector3<T>(sxx - e3, sxy, sxz);
        var r1 = new Vector3<T>(sxy, syy - e3, syz);
        var r2 = new Vector3<T>(sxz, syz, szz - e3);

        var c01 = Vector3<T>.Cross(r0, r1);
        var c02 = Vector3<T>.Cross(r0, r2);
        var c12 = Vector3<T>.Cross(r1, r2);

        var l01 = Vector3<T>.Dot(c01, c01);
        var l02 = Vector3<T>.Dot(c02, c02);
        var l12 = Vector3<T>.Dot(c12, c12);

        var best = l01;
        if (l02 > best) best = l02;
        if (l12 > best) best = l12;

        if (best < epsilon)
            throw new ArgumentException("Points are collinear or coincident and do not define a plane.");

        if (l01 >= l02 && l01 >= l12)
            return c01 / T.Sqrt(l01);
        if (l02 >= l12)
            return c02 / T.Sqrt(l02);
        return c12 / T.Sqrt(l12);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pose3<T>((T x, T y, T z, T rx, T ry, T rz) tuple) =>
        new(tuple.x, tuple.y, tuple.z, tuple.rx, tuple.ry, tuple.rz);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (T x, T y, T z, T rx, T ry, T rz)(Pose3<T> pose) =>
        (pose.X, pose.Y, pose.Z, (T)pose.Rx, (T)pose.Ry, (T)pose.Rz);

    /// <summary>
    /// Deconstructs into position and rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Point3<T> position, out Rotation3<T> rotation)
    {
        position = _position;
        rotation = _rotation;
    }

    /// <summary>
    /// Deconstructs into individual components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T x, out T y, out T z, out T rx, out T ry, out T rz)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Pose3<T> other) => this == other;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pose3<T> p && Equals(p);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(_position, _rotation);

    public override string ToString() =>
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
