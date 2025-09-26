using System.Diagnostics;
using System.Numerics;

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