using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

[DebuggerDisplay("[{RadialDistance},{Angle},{Z}]")]
public readonly record struct CylindricalPoint<T>
    where T : IFloatingPointIeee754<T>
{
    public Radian<T> Angle { get; }
    public T RadialDistance { get; }
    public T Z { get; }
    public CylindricalPoint(Radian<T> angle, T r, T z)
    {
        Angle = angle;
        RadialDistance = r;
        Z = z;
    }

    public static implicit operator CylindricalPoint<T>(PolarPoint<T> point)
    {
        return new CylindricalPoint<T>(point.Angle, point.Radius, T.Zero);
    }
    public static implicit operator Vector3(CylindricalPoint<T> point)
    {
        T x = point.RadialDistance * T.Cos((T)point.Angle);
        T y = point.RadialDistance * T.Sin((T)point.Angle);
        return new Vector3(float.CreateTruncating(x), float.CreateTruncating(y), float.CreateTruncating(point.Z));
    }
    public override string ToString()
    {
        return $"[{RadialDistance},{Angle},{Z}]";
    }
}

[DebuggerDisplay("[{Angle},{Radius}]")]
public readonly record struct PolarPoint<T>
    where T : IFloatingPointIeee754<T>
{
    public Radian<T> Angle { get; }
    public T Radius { get; }

    public PolarPoint(Radian<T> angle, T r)
    {
        Angle = angle;
        Radius = r;
    }

    public static explicit operator Vector2(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Vector2(float.CreateTruncating(x), float.CreateTruncating(y));
    }
    public static explicit operator Vector<T>(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Vector<T>(T.CreateTruncating(x), T.CreateTruncating(y));
    }
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
    public static implicit operator Point<T>(PolarPoint<T> point)
    {
        T x = point.Radius * T.Cos((T)point.Angle);
        T y = point.Radius * T.Sin((T)point.Angle);
        return new Point<T>(T.CreateTruncating(x), T.CreateTruncating(y));
    }

    public override string ToString()
    {
        return $"[{Radius},{Angle}]";
    }
}