using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A 3×3 matrix stored as nine fields (row-major). Supports linear algebra
/// operations, 3D rotation matrix construction, and Euler/quaternion conversions.
/// </summary>
[Matrix3x3JsonConverter]
public readonly record struct Matrix3x3<T>(
    T M11, T M12, T M13,
    T M21, T M22, T M23,
    T M31, T M32, T M33)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>,
              ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>The 3×3 identity matrix.</summary>
    public static Matrix3x3<T> Identity => new(
        T.One, T.Zero, T.Zero,
        T.Zero, T.One, T.Zero,
        T.Zero, T.Zero, T.One);

    /// <summary>The 3×3 zero matrix.</summary>
    public static Matrix3x3<T> Zero => new(
        T.Zero, T.Zero, T.Zero,
        T.Zero, T.Zero, T.Zero,
        T.Zero, T.Zero, T.Zero);

    /// <summary>The determinant of this 3×3 matrix.</summary>
    public T Determinant
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => M11 * (M22 * M33 - M23 * M32)
             - M12 * (M21 * M33 - M23 * M31)
             + M13 * (M21 * M32 - M22 * M31);
    }

    /// <summary>The trace: M11 + M22 + M33.</summary>
    public T Trace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => M11 + M22 + M33;
    }

    /// <summary>Returns the transpose of this matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3x3<T> Transpose() => new(
        M11, M21, M31,
        M12, M22, M32,
        M13, M23, M33);

    /// <summary>
    /// Returns the inverse of this matrix via adjugate/determinant.
    /// Throws if the matrix is singular (determinant ≈ 0).
    /// </summary>
    public Matrix3x3<T> Inverse()
    {
        var det = Determinant;
        if (T.Abs(det) < T.CreateTruncating(1e-15))
            throw new InvalidOperationException("Matrix is singular.");

        var invDet = T.One / det;

        return new(
            (M22 * M33 - M23 * M32) * invDet,
            (M13 * M32 - M12 * M33) * invDet,
            (M12 * M23 - M13 * M22) * invDet,
            (M23 * M31 - M21 * M33) * invDet,
            (M11 * M33 - M13 * M31) * invDet,
            (M13 * M21 - M11 * M23) * invDet,
            (M21 * M32 - M22 * M31) * invDet,
            (M12 * M31 - M11 * M32) * invDet,
            (M11 * M22 - M12 * M21) * invDet);
    }

    /// <summary>Transforms a vector by this matrix: result = M · v.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<T> Transform(Vector3<T> v) => new(
        M11 * v.X + M12 * v.Y + M13 * v.Z,
        M21 * v.X + M22 * v.Y + M23 * v.Z,
        M31 * v.X + M32 * v.Y + M33 * v.Z);

    /// <summary>Transforms a point by this matrix: result = M · p.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3<T> Transform(Point3<T> p) => new(
        M11 * p.X + M12 * p.Y + M13 * p.Z,
        M21 * p.X + M22 * p.Y + M23 * p.Z,
        M31 * p.X + M32 * p.Y + M33 * p.Z);

    #region Operators

    /// <summary>Matrix multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> operator *(in Matrix3x3<T> a, in Matrix3x3<T> b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
        a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
        a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
        a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
        a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
        a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
        a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);

    /// <summary>Matrix-vector multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<T> operator *(in Matrix3x3<T> m, Vector3<T> v) => m.Transform(v);

    /// <summary>Matrix-point multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3<T> operator *(in Matrix3x3<T> m, Point3<T> p) => m.Transform(p);

    /// <summary>Scalar multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> operator *(in Matrix3x3<T> m, T s) => new(
        m.M11 * s, m.M12 * s, m.M13 * s,
        m.M21 * s, m.M22 * s, m.M23 * s,
        m.M31 * s, m.M32 * s, m.M33 * s);

    /// <summary>Scalar multiplication (commutative).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> operator *(T s, in Matrix3x3<T> m) => m * s;

    /// <summary>Matrix addition.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> operator +(in Matrix3x3<T> a, in Matrix3x3<T> b) => new(
        a.M11 + b.M11, a.M12 + b.M12, a.M13 + b.M13,
        a.M21 + b.M21, a.M22 + b.M22, a.M23 + b.M23,
        a.M31 + b.M31, a.M32 + b.M32, a.M33 + b.M33);

    /// <summary>Matrix subtraction.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> operator -(in Matrix3x3<T> a, in Matrix3x3<T> b) => new(
        a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13,
        a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23,
        a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33);

    #endregion

    #region Rotation Factories

    /// <summary>Creates a rotation matrix around the X axis.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> RotationX(Degree<T> angle)
    {
        var rad = (Radian<T>)angle;
        var c = T.Cos((T)rad);
        var s = T.Sin((T)rad);
        return new(
            T.One, T.Zero, T.Zero,
            T.Zero, c, -s,
            T.Zero, s, c);
    }

    /// <summary>Creates a rotation matrix around the Y axis.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> RotationY(Degree<T> angle)
    {
        var rad = (Radian<T>)angle;
        var c = T.Cos((T)rad);
        var s = T.Sin((T)rad);
        return new(
            c, T.Zero, s,
            T.Zero, T.One, T.Zero,
            -s, T.Zero, c);
    }

    /// <summary>Creates a rotation matrix around the Z axis.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> RotationZ(Degree<T> angle)
    {
        var rad = (Radian<T>)angle;
        var c = T.Cos((T)rad);
        var s = T.Sin((T)rad);
        return new(
            c, -s, T.Zero,
            s, c, T.Zero,
            T.Zero, T.Zero, T.One);
    }

    /// <summary>
    /// Creates a ZYX Euler rotation matrix: R = Rz(rz) * Ry(ry) * Rx(rx).
    /// This matches the convention used by <see cref="Rotation3{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> RotationZYX(Degree<T> rx, Degree<T> ry, Degree<T> rz)
    {
        var cRx = T.Cos((T)(Radian<T>)rx); var sRx = T.Sin((T)(Radian<T>)rx);
        var cRy = T.Cos((T)(Radian<T>)ry); var sRy = T.Sin((T)(Radian<T>)ry);
        var cRz = T.Cos((T)(Radian<T>)rz); var sRz = T.Sin((T)(Radian<T>)rz);

        return new(
            cRy * cRz, sRx * sRy * cRz - cRx * sRz, cRx * sRy * cRz + sRx * sRz,
            cRy * sRz, sRx * sRy * sRz + cRx * cRz, cRx * sRy * sRz - sRx * cRz,
            -sRy,      sRx * cRy,                    cRx * cRy);
    }

    #endregion

    #region Other Factories

    /// <summary>Creates a 3D scaling matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> Scale(T sx, T sy, T sz) => new(
        sx, T.Zero, T.Zero,
        T.Zero, sy, T.Zero,
        T.Zero, T.Zero, sz);

    /// <summary>Creates a matrix from three column vectors.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> FromColumns(Vector3<T> c0, Vector3<T> c1, Vector3<T> c2) => new(
        c0.X, c1.X, c2.X,
        c0.Y, c1.Y, c2.Y,
        c0.Z, c1.Z, c2.Z);

    /// <summary>Creates a matrix from three row vectors.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3x3<T> FromRows(Vector3<T> r0, Vector3<T> r1, Vector3<T> r2) => new(
        r0.X, r0.Y, r0.Z,
        r1.X, r1.Y, r1.Z,
        r2.X, r2.Y, r2.Z);

    #endregion

    #region Euler & Quaternion Conversions

    /// <summary>
    /// Extracts ZYX Euler angles (in degrees) from this rotation matrix.
    /// Assumes R = Rz(rz) * Ry(ry) * Rx(rx). Handles gimbal lock at ry = ±90°.
    /// </summary>
    public (Degree<T> Rx, Degree<T> Ry, Degree<T> Rz) ToEulerZYX()
    {
        var epsilon = T.CreateTruncating(1e-6);
        var sy = -M31;

        if (sy > T.One) sy = T.One;
        if (sy < -T.One) sy = -T.One;

        var ry = T.Asin(sy);
        var cy = T.Cos(ry);

        T rx, rz;
        if (T.Abs(cy) > epsilon)
        {
            rx = T.Atan2(M32, M33);
            rz = T.Atan2(M21, M11);
        }
        else
        {
            // Gimbal lock
            rx = T.Atan2(-M23, M22);
            rz = T.Zero;
        }

        return (
            (Degree<T>)Radian<T>.FromRadian(rx),
            (Degree<T>)Radian<T>.FromRadian(ry),
            (Degree<T>)Radian<T>.FromRadian(rz));
    }

    /// <summary>
    /// Converts this rotation matrix to a unit quaternion using Shepperd's method
    /// (numerically stable for all orientations).
    /// </summary>
    public Quaternion<T> ToQuaternion()
    {
        // Shepperd's method: pick the largest diagonal element to avoid division by small numbers
        var tr = Trace;
        T w, x, y, z;

        if (tr > T.Zero)
        {
            var s = T.Sqrt(tr + T.One) * Two;
            w = s / (T.CreateTruncating(4));
            x = (M32 - M23) / s;
            y = (M13 - M31) / s;
            z = (M21 - M12) / s;
        }
        else if (M11 > M22 && M11 > M33)
        {
            var s = T.Sqrt(T.One + M11 - M22 - M33) * Two;
            w = (M32 - M23) / s;
            x = s / (T.CreateTruncating(4));
            y = (M12 + M21) / s;
            z = (M13 + M31) / s;
        }
        else if (M22 > M33)
        {
            var s = T.Sqrt(T.One + M22 - M11 - M33) * Two;
            w = (M13 - M31) / s;
            x = (M12 + M21) / s;
            y = s / (T.CreateTruncating(4));
            z = (M23 + M32) / s;
        }
        else
        {
            var s = T.Sqrt(T.One + M33 - M11 - M22) * Two;
            w = (M21 - M12) / s;
            x = (M13 + M31) / s;
            y = (M23 + M32) / s;
            z = s / (T.CreateTruncating(4));
        }

        return new Quaternion<T>(w, x, y, z);
    }

    #endregion

    public override string ToString() =>
        $"[{M11}, {M12}, {M13}; {M21}, {M22}, {M23}; {M31}, {M32}, {M33}]";
}
