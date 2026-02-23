using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a point in cylindrical coordinate system (radius, angle, height).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinate values.</typeparam>
[DebuggerDisplay("[{RadialDistance},{Angle},{Z}]")]
public readonly record struct CylindricalPoint<T>
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the angle component of the cylindrical point in radians.
    /// </summary>
    public Radian<T> Angle { get; }
    /// <summary>
    /// Gets the radial distance from the Z-axis.
    /// </summary>
    public T RadialDistance { get; }
    /// <summary>
    /// Gets the height (Z-coordinate) of the point.
    /// </summary>
    public T Z { get; }
    /// <summary>
    /// Initializes a new instance of the CylindricalPoint struct.
    /// </summary>
    /// <param name="angle">The angle in radians.</param>
    /// <param name="r">The radial distance from the Z-axis.</param>
    /// <param name="z">The height (Z-coordinate).</param>
    public CylindricalPoint(Radian<T> angle, T r, T z)
    {
        Angle = angle;
        RadialDistance = r;
        Z = z;
    }

    /// <summary>
    /// Implicitly converts a polar point to a cylindrical point with Z=0.
    /// </summary>
    /// <param name="point">The polar point to convert.</param>
    /// <returns>A cylindrical point with the same angle and radius, and Z=0.</returns>
    public static implicit operator CylindricalPoint<T>(PolarPoint<T> point)
    {
        return new CylindricalPoint<T>(point.Angle, point.Radius, T.Zero);
    }
    /// <summary>
    /// Implicitly converts a cylindrical point to a Vector3 in Cartesian coordinates.
    /// </summary>
    /// <param name="point">The cylindrical point to convert.</param>
    /// <returns>A Vector3 representing the point in Cartesian coordinates.</returns>
    public static implicit operator Vector3(CylindricalPoint<T> point)
    {
        T x = point.RadialDistance * T.Cos((T)point.Angle);
        T y = point.RadialDistance * T.Sin((T)point.Angle);
        return new Vector3(float.CreateTruncating(x), float.CreateTruncating(y), float.CreateTruncating(point.Z));
    }
    /// <summary>
    /// Returns a string representation of this cylindrical point.
    /// </summary>
    /// <returns>A string representation in the format [RadialDistance,Angle,Z].</returns>
    public override string ToString()
    {
        return $"[{RadialDistance},{Angle},{Z}]";
    }
}

/// <summary>
/// Represents a point in polar coordinate system (radius, angle).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinate values.</typeparam>
[DebuggerDisplay("[{Angle},{Radius}]")]
public readonly record struct PolarPoint<T>
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// Gets the angle component of the polar point in radians.
    /// </summary>
    public Radian<T> Angle { get; }
    /// <summary>
    /// Gets the radial distance from the origin.
    /// </summary>
    public T Radius { get; }

    /// <summary>
    /// Initializes a new instance of the PolarPoint struct.
    /// </summary>
    /// <param name="angle">The angle in radians.</param>
    /// <param name="r">The radial distance from the origin.</param>
    public PolarPoint(Radian<T> angle, T r)
    {
        Angle = angle;
        Radius = r;
    }

    /// <summary>
    /// Explicitly converts a polar point to a Vector2 in Cartesian coordinates.
    /// </summary>
    /// <param name="point">The polar point to convert.</param>
    /// <returns>A Vector2 representing the point in Cartesian coordinates.</returns>
    public static explicit operator Vector2(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Vector2(float.CreateTruncating(x), float.CreateTruncating(y));
    }
    /// <summary>
    /// Explicitly converts a polar point to a generic Vector in Cartesian coordinates.
    /// </summary>
    /// <param name="point">The polar point to convert.</param>
    /// <returns>A Vector representing the point in Cartesian coordinates.</returns>
    public static explicit operator Vector<T>(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Vector<T>(T.CreateTruncating(x), T.CreateTruncating(y));
    }
    /// <summary>
    /// Implicitly converts a Cartesian point to a polar point.
    /// </summary>
    /// <param name="point">The Cartesian point to convert.</param>
    /// <returns>A polar point representing the same location.</returns>
    public static implicit operator PolarPoint<T>(Point<T> point)
    {
        var x = T.CreateTruncating(point.X);
        var y = T.CreateTruncating(point.Y);
        var r = T.Sqrt(x * x + y*y);
        var alpha = T.Atan2(point.Y, point.X);
        var alphaConverted = T.CreateTruncating(alpha);
        var alphRad = Radian<T>.FromRadian(alphaConverted);
        return new PolarPoint<T>(alphRad, r);
    }
    /// <summary>
    /// Implicitly converts a polar point to a Cartesian point.
    /// </summary>
    /// <param name="point">The polar point to convert.</param>
    /// <returns>A Point representing the same location in Cartesian coordinates.</returns>
    public static implicit operator Point<T>(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Point<T>(T.CreateTruncating(x), T.CreateTruncating(y));
    }

    /// <summary>
    /// Returns a string representation of this polar point.
    /// </summary>
    /// <returns>A string representation in the format [Radius,Angle].</returns>
    public override string ToString()
    {
        return $"[{Radius},{Angle}]";
    }
}

/// <summary>
/// Represents a point in spherical coordinate system (radius, azimuth, inclination).
/// Uses ISO convention: azimuth is the angle in the XY plane from X-axis,
/// inclination is the angle from the Z-axis (0° = north pole, 180° = south pole).
/// Angles are stored in degrees, consistent with <see cref="Rotation3{T}"/>.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinate values.</typeparam>
[DebuggerDisplay("[{Radius},{Azimuth},{Inclination}]")]
public readonly record struct SphericalPoint<T>
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    /// <summary>
    /// A spherical point at the origin (radius = 0).
    /// </summary>
    public static readonly SphericalPoint<T> Zero = new(T.Zero, Degree<T>.Zero, Degree<T>.Zero);

    private static readonly T _180 = T.CreateTruncating(180);

    /// <summary>
    /// Gets the radial distance from the origin.
    /// </summary>
    public T Radius { get; }
    /// <summary>
    /// Gets the azimuth angle (in the XY plane, from the X-axis) in degrees.
    /// </summary>
    public Degree<T> Azimuth { get; }
    /// <summary>
    /// Gets the inclination angle (from the Z-axis) in degrees. 0° = north pole, 180° = south pole.
    /// </summary>
    public Degree<T> Inclination { get; }

    /// <summary>
    /// Initializes a new instance of the SphericalPoint struct.
    /// </summary>
    /// <param name="radius">The radial distance from the origin.</param>
    /// <param name="azimuth">The azimuth angle in the XY plane in degrees.</param>
    /// <param name="inclination">The inclination angle from the Z-axis in degrees.</param>
    public SphericalPoint(T radius, Degree<T> azimuth, Degree<T> inclination)
    {
        Radius = radius;
        Azimuth = azimuth;
        Inclination = inclination;
    }

    #region Static Factories

    /// <summary>
    /// Creates a spherical point on the equator (XY plane). Elevation = 0°, Inclination = 90°.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> OnEquator(T radius, Degree<T> azimuth)
        => new(radius, azimuth, Degree<T>.Create(T.CreateTruncating(90)));

    /// <summary>
    /// Creates a spherical point using elevation (angle from XY plane) instead of inclination (angle from Z-axis).
    /// Elevation 0° = equator, 90° = north pole, -90° = south pole.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> FromElevation(T radius, Degree<T> azimuth, Degree<T> elevation)
        => new(radius, azimuth, Degree<T>.Create(T.CreateTruncating(90) - (T)elevation));

    /// <summary>
    /// Creates a spherical point at the north pole (+Z direction).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> NorthPole(T radius)
        => new(radius, Degree<T>.Zero, Degree<T>.Zero);

    /// <summary>
    /// Creates a spherical point at the south pole (-Z direction).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> SouthPole(T radius)
        => new(radius, Degree<T>.Zero, Degree<T>.Create(T.CreateTruncating(180)));

    #endregion

    /// <summary>
    /// Returns a point on the unit sphere (radius = 1) with the same direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SphericalPoint<T> Normalized() => new(T.One, Azimuth, Inclination);

    /// <summary>
    /// Returns a new spherical point with the specified radius, preserving direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SphericalPoint<T> WithRadius(T radius) => new(radius, Azimuth, Inclination);

    /// <summary>
    /// Deconstructs this spherical point into its components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T radius, out Degree<T> azimuth, out Degree<T> inclination)
    {
        radius = Radius;
        azimuth = Azimuth;
        inclination = Inclination;
    }

    #region Rotation

    /// <summary>
    /// Rotates the azimuth angle (around the Z-axis). This is O(1) — just an angle addition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SphericalPoint<T> RotateAzimuth(Degree<T> delta)
        => new(Radius, Azimuth + delta, Inclination);

    /// <summary>
    /// Rotates the inclination angle (tilt toward/away from the pole).
    /// Result is clamped to [0°, 180°].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SphericalPoint<T> RotateInclination(Degree<T> delta)
    {
        T newInc = T.Clamp((T)Inclination + (T)delta, T.Zero, _180);
        return new(Radius, Azimuth, Degree<T>.Create(newInc));
    }

    /// <summary>
    /// Rotates both azimuth and inclination simultaneously.
    /// Inclination is clamped to [0°, 180°].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SphericalPoint<T> Rotate(Degree<T> deltaAzimuth, Degree<T> deltaInclination)
    {
        T newInc = T.Clamp((T)Inclination + (T)deltaInclination, T.Zero, _180);
        return new(Radius, Azimuth + deltaAzimuth, Degree<T>.Create(newInc));
    }

    #endregion

    #region Distance & Interpolation

    /// <summary>
    /// Calculates the angular distance (great-circle angle) between two directions,
    /// ignoring their radii. Result is in [0°, 180°].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Degree<T> AngularDistance(SphericalPoint<T> a, SphericalPoint<T> b)
    {
        // Vincenty formula — numerically stable for all angular separations.
        T sinA = Degree.Sin(a.Inclination);
        T cosA = Degree.Cos(a.Inclination);
        T sinB = Degree.Sin(b.Inclination);
        T cosB = Degree.Cos(b.Inclination);
        Radian<T> dAz = (Radian<T>)b.Azimuth - (Radian<T>)a.Azimuth;
        T cosDaz = T.Cos((T)dAz);
        T sinDaz = T.Sin((T)dAz);

        T num1 = sinB * sinDaz;
        T num2 = sinA * cosB - cosA * sinB * cosDaz;
        T num = T.Sqrt(num1 * num1 + num2 * num2);
        T den = cosA * cosB + sinA * sinB * cosDaz;

        return Radian<T>.FromRadian(T.Atan2(num, den));
    }

    /// <summary>
    /// Calculates the Euclidean distance between two spherical points in 3D space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Distance(SphericalPoint<T> a, SphericalPoint<T> b)
    {
        Point3<T> pa = a;
        Point3<T> pb = b;
        return Point3<T>.Distance(pa, pb);
    }

    /// <summary>
    /// Spherical linear interpolation (slerp) between two directions.
    /// Interpolates along the great circle at constant angular velocity.
    /// The radius is linearly interpolated.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> Slerp(SphericalPoint<T> a, SphericalPoint<T> b, T t)
    {
        // Convert to Cartesian, slerp via quaternion-style formula, convert back.
        Point3<T> pa = a;
        Point3<T> pb = b;

        T ra = a.Radius;
        T rb = b.Radius;

        // Normalize to unit vectors for directional slerp
        if (ra == T.Zero || rb == T.Zero)
        {
            // Degenerate — fall back to linear interpolation
            return Point3<T>.Lerp(pa, pb, t);
        }

        T dotVal = (pa.X * pb.X + pa.Y * pb.Y + pa.Z * pb.Z) / (ra * rb);
        dotVal = T.Clamp(dotVal, -T.One, T.One);
        T omega = T.Acos(dotVal);

        T rInterp = ra + (rb - ra) * t;

        if (omega < T.CreateTruncating(1e-6))
        {
            // Nearly identical directions — lerp
            Point3<T> lerped = Point3<T>.Lerp(pa, pb, t);
            SphericalPoint<T> result = lerped;
            return result.WithRadius(rInterp);
        }

        T sinOmega = T.Sin(omega);
        T factorA = T.Sin((T.One - t) * omega) / sinOmega;
        T factorB = T.Sin(t * omega) / sinOmega;

        // Slerp on unit sphere, then apply interpolated radius
        T ux = pa.X / ra * factorA + pb.X / rb * factorB;
        T uy = pa.Y / ra * factorA + pb.Y / rb * factorB;
        T uz = pa.Z / ra * factorA + pb.Z / rb * factorB;

        SphericalPoint<T> dir = new Point3<T>(ux, uy, uz);
        return dir.WithRadius(rInterp);
    }

    /// <summary>
    /// Linear interpolation of radius and angles. Faster than Slerp but does not
    /// follow the great circle for large angular separations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> Lerp(SphericalPoint<T> a, SphericalPoint<T> b, T t)
    {
        Point3<T> pa = a;
        Point3<T> pb = b;
        return Point3<T>.Lerp(pa, pb, t);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Scales the radius by a scalar value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> operator *(SphericalPoint<T> point, T scalar)
        => new(point.Radius * scalar, point.Azimuth, point.Inclination);

    /// <summary>
    /// Scales the radius by a scalar value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> operator *(T scalar, SphericalPoint<T> point)
        => new(point.Radius * scalar, point.Azimuth, point.Inclination);

    /// <summary>
    /// Divides the radius by a scalar value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SphericalPoint<T> operator /(SphericalPoint<T> point, T scalar)
        => new(point.Radius / scalar, point.Azimuth, point.Inclination);

    /// <summary>
    /// Offsets a Cartesian point by a spherical displacement.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3<T> operator +(Point3<T> point, SphericalPoint<T> offset)
        => point + (Vector3<T>)offset;

    /// <summary>
    /// Subtracts a spherical displacement from a Cartesian point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3<T> operator -(Point3<T> point, SphericalPoint<T> offset)
        => point - (Vector3<T>)offset;

    #endregion

    #region Conversions

    /// <summary>
    /// Converts a spherical point to a Cartesian Vector3 (direction + magnitude).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3<T>(SphericalPoint<T> point)
    {
        T sinInc = Degree.Sin(point.Inclination);
        T x = point.Radius * sinInc * Degree.Cos(point.Azimuth);
        T y = point.Radius * sinInc * Degree.Sin(point.Azimuth);
        T z = point.Radius * Degree.Cos(point.Inclination);
        return new Vector3<T>(x, y, z);
    }

    /// <summary>
    /// Converts a spherical point to a Cartesian Point3.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Point3<T>(SphericalPoint<T> point)
    {
        T sinInc = Degree.Sin(point.Inclination);
        T x = point.Radius * sinInc * Degree.Cos(point.Azimuth);
        T y = point.Radius * sinInc * Degree.Sin(point.Azimuth);
        T z = point.Radius * Degree.Cos(point.Inclination);
        return new Point3<T>(x, y, z);
    }

    /// <summary>
    /// Converts a Cartesian Point3 to a spherical point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SphericalPoint<T>(Point3<T> point)
    {
        T r = T.Sqrt(point.X * point.X + point.Y * point.Y + point.Z * point.Z);
        if (r == T.Zero)
            return Zero;

        Degree<T> inclination = Radian<T>.FromRadian(T.Acos(point.Z / r));
        Degree<T> azimuth = Radian<T>.FromRadian(T.Atan2(point.Y, point.X));
        return new SphericalPoint<T>(r, azimuth, inclination);
    }

    /// <summary>
    /// Converts a spherical point to a System.Numerics.Vector3 in Cartesian coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(SphericalPoint<T> point)
    {
        T sinInc = Degree.Sin(point.Inclination);
        T x = point.Radius * sinInc * Degree.Cos(point.Azimuth);
        T y = point.Radius * sinInc * Degree.Sin(point.Azimuth);
        T z = point.Radius * Degree.Cos(point.Inclination);
        return new Vector3(float.CreateTruncating(x), float.CreateTruncating(y), float.CreateTruncating(z));
    }

    /// <summary>
    /// Converts a spherical point to a cylindrical point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CylindricalPoint<T>(SphericalPoint<T> point)
    {
        T radialDistance = point.Radius * Degree.Sin(point.Inclination);
        T z = point.Radius * Degree.Cos(point.Inclination);
        return new CylindricalPoint<T>((Radian<T>)point.Azimuth, radialDistance, z);
    }

    /// <summary>
    /// Converts a cylindrical point to a spherical point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SphericalPoint<T>(CylindricalPoint<T> point)
    {
        T r = T.Sqrt(point.RadialDistance * point.RadialDistance + point.Z * point.Z);
        if (r == T.Zero)
            return Zero;

        Degree<T> inclination = Radian<T>.FromRadian(T.Atan2(point.RadialDistance, point.Z));
        Degree<T> azimuth = point.Angle;
        return new SphericalPoint<T>(r, azimuth, inclination);
    }

    #endregion

    /// <summary>
    /// Returns a string representation of this spherical point.
    /// </summary>
    public override string ToString()
    {
        return $"[{Radius},{Azimuth},{Inclination}]";
    }
}