using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModelingEvolution.Drawing;

/// <summary>
/// A rigid 3D frame: a position and a unit-quaternion orientation. All pose maths composes <see cref="Frame3{T}"/>;
/// <see cref="Pose3{T}"/> is only the wire and edit format and is converted once, by
/// <see cref="FromPose3"/> and <see cref="ToPose3"/> (epic-129 design § "Drawing: the kinematic chain").
/// The frame carries no unit: its lengths are in whatever unit its producer chose.
/// </summary>
/// <param name="Position">The origin of the frame in its parent frame.</param>
/// <param name="Rotation">The orientation of the frame in its parent frame; a unit quaternion.</param>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly record struct Frame3<T>(Point3<T> Position, Quaternion<T> Rotation)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>The identity frame: origin at zero, no rotation.</summary>
    public static Frame3<T> Identity { get; } = new(Point3<T>.Zero, Quaternion<T>.Identity);

    /// <summary>Composes this frame with a child frame expressed in it: <c>this · child</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame3<T> Multiply(in Frame3<T> child) =>
        new(Position + Rotation.Rotate(child.Position), Rotation * child.Rotation);

    /// <summary>Composes two frames: <c>a · b</c>, where <paramref name="b"/> is expressed in <paramref name="a"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Frame3<T> operator *(in Frame3<T> a, in Frame3<T> b) => a.Multiply(b);

    /// <summary>Returns the inverse frame, so that <c>f · f.Inverse()</c> is the identity.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame3<T> Inverse()
    {
        var inv = Rotation.Conjugate();
        return new Frame3<T>((Point3<T>)(-inv.Rotate(Position)), inv);
    }

    /// <summary>Maps a point from this frame's local coordinates into its parent's coordinates.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3<T> TransformPoint(Point3<T> p) => Position + Rotation.Rotate(p);

    /// <summary>Rotates a vector from this frame's local coordinates into its parent's (no translation).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<T> TransformVector(Vector3<T> v) => Rotation.Rotate(v);

    /// <summary>
    /// Interpolates between two frames: the position linearly, the rotation by spherical linear interpolation
    /// along the shorter arc. <paramref name="t"/> = 0 gives <paramref name="a"/>, 1 gives <paramref name="b"/>.
    /// </summary>
    public static Frame3<T> Interpolate(in Frame3<T> a, in Frame3<T> b, T t) =>
        new(Point3<T>.Lerp(a.Position, b.Position, t), Quaternion<T>.Slerp(a.Rotation, b.Rotation, t));

    /// <summary>
    /// The distance between two frames: the position error |p_a − p_b| and the angle error
    /// 2·acos(|q_a·q_b|) in radians (computed in its well-conditioned atan2 form).
    /// </summary>
    public static (T Position, T Angle) Distance(in Frame3<T> a, in Frame3<T> b)
    {
        var d = a.Rotation.Conjugate() * b.Rotation;
        var s = T.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
        return (Point3<T>.Distance(a.Position, b.Position), Two * T.Atan2(s, T.Abs(d.W)));
    }

    /// <summary>
    /// Converts a wire/edit pose (ZYX degrees) into a frame: position scaled by <paramref name="lengthScale"/>
    /// (0.001 turns mm into m), rotation by <see cref="Quaternion{T}.FromRpy"/>. This is the one Euler rule.
    /// </summary>
    public static Frame3<T> FromPose3(Pose3<T> p, T lengthScale)
    {
        var (rx, ry, rz) = p.Rotation.ToRadians();
        return new Frame3<T>(p.Position * lengthScale, Quaternion<T>.FromRpy((T)rx, (T)ry, (T)rz));
    }

    /// <summary>
    /// Converts this frame into a wire/edit pose: position scaled by <paramref name="lengthScale"/>
    /// (1000 turns m into mm), rotation as ZYX degrees (the inverse of <see cref="Quaternion{T}.FromRpy"/>).
    /// </summary>
    public Pose3<T> ToPose3(T lengthScale) =>
        new(Position * lengthScale, Rotation3<T>.FromQuaternion(Rotation));

    #region FromSurface

    /// <summary>
    /// Creates a frame from three points defining a surface plane using the right-hand rule.
    /// Z direction is determined by (b-a) x (c-a). Reversing point order reverses Z direction.
    /// </summary>
    /// <param name="a">First point - becomes the origin, also defines X-axis direction with b.</param>
    /// <param name="b">Second point - defines X-axis direction from a.</param>
    /// <param name="c">Third point - completes the plane; a-b-c order determines Z via right-hand rule.</param>
    /// <returns>A frame whose origin is at a, X-axis along a-b, Z-axis per right-hand rule.</returns>
    /// <exception cref="ArgumentException">Thrown when points a, b, c are collinear.</exception>
    public static Frame3<T> FromSurface(Point3<T> a, Point3<T> b, Point3<T> c)
    {
        var epsilon = T.CreateTruncating(1e-9);
        var ab = b - a;
        var ac = c - a;

        var zAxis = Vector3<T>.Cross(ab, ac);
        var normalLength = zAxis.Length;
        if (normalLength < epsilon)
            throw new ArgumentException("Points a, b, c are collinear and do not define a plane.");
        zAxis = zAxis / normalLength;

        var xAxis = ab.Normalize();
        var yAxis = Vector3<T>.Cross(zAxis, xAxis);
        return new Frame3<T>(a, Quaternion<T>.FromAxes(xAxis, yAxis, zAxis));
    }

    /// <summary>
    /// Creates a frame from three points defining a surface plane and a hint point indicating the Z direction.
    /// </summary>
    /// <param name="a">First point on the surface (also defines X-axis origin direction with b).</param>
    /// <param name="b">Second point on the surface (defines X-axis direction from a).</param>
    /// <param name="c">Third point on the surface (completes the plane definition).</param>
    /// <param name="h">Hint point off the surface - Z-axis will point toward this point.</param>
    /// <returns>A frame whose origin is the projection of h onto the plane, X-Y plane is the surface, and Z points toward h.</returns>
    /// <exception cref="ArgumentException">Thrown when h lies on the surface plane, or when a, b, c are collinear.</exception>
    public static Frame3<T> FromSurface(Point3<T> a, Point3<T> b, Point3<T> c, Point3<T> h)
    {
        var epsilon = T.CreateTruncating(1e-9);
        var ab = b - a;
        var ac = c - a;

        var normal = Vector3<T>.Cross(ab, ac);
        var normalLength = normal.Length;
        if (normalLength < epsilon)
            throw new ArgumentException("Points a, b, c are collinear and do not define a plane.");
        normal = normal / normalLength;

        var d = Vector3<T>.Dot(h - a, normal);
        if (T.Abs(d) < epsilon)
            throw new ArgumentException("Point h lies on the surface plane. Cannot determine Z direction.", nameof(h));

        var origin = new Point3<T>(h.X - d * normal.X, h.Y - d * normal.Y, h.Z - d * normal.Z);
        var zAxis = d > T.Zero ? normal : -normal;
        var xAxis = ab.Normalize();
        var yAxis = Vector3<T>.Cross(zAxis, xAxis);
        return new Frame3<T>(origin, Quaternion<T>.FromAxes(xAxis, yAxis, zAxis));
    }

    /// <summary>
    /// Creates a frame from N points defining a surface plane using least-squares best-fit.
    /// Origin is the centroid. Z-axis is the best-fit normal (oriented via right-hand rule
    /// of the first three points). X-axis is the projection of edge a→b onto the plane.
    /// </summary>
    /// <param name="points">At least 3 non-collinear points on or near the surface.</param>
    /// <returns>A frame whose origin is the centroid and whose Z-axis is the best-fit surface normal.</returns>
    /// <exception cref="ArgumentException">Thrown when fewer than 3 points or points are collinear.</exception>
    public static Frame3<T> FromSurface(ReadOnlySpan<Point3<T>> points)
    {
        if (points.Length < 3)
            throw new ArgumentException("At least 3 points are required to define a surface.", nameof(points));
        if (points.Length == 3)
            return FromSurface(points[0], points[1], points[2]);

        var epsilon = T.CreateTruncating(1e-9);
        var n = T.CreateTruncating(points.Length);

        T cx = T.Zero, cy = T.Zero, cz = T.Zero;
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

        // Scatter matrix (symmetric 3x3): S = Σ (pi - centroid)(pi - centroid)^T
        T sxx = T.Zero, sxy = T.Zero, sxz = T.Zero, syy = T.Zero, syz = T.Zero, szz = T.Zero;
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

        // Normal = eigenvector of the smallest eigenvalue, oriented by the right-hand rule of the first three points.
        var zAxis = SmallestEigenvector3x3Symmetric(sxx, sxy, sxz, syy, syz, szz);
        var ab = points[1] - points[0];
        var ac = points[2] - points[0];
        if (Vector3<T>.Dot(Vector3<T>.Cross(ab, ac), zAxis) < T.Zero)
            zAxis = -zAxis;

        // X-axis: edge a→b projected onto the plane.
        var proj = Vector3<T>.Dot(ab, zAxis);
        var xAxis = new Vector3<T>(ab.X - proj * zAxis.X, ab.Y - proj * zAxis.Y, ab.Z - proj * zAxis.Z);
        var xLen = xAxis.Length;
        if (xLen < epsilon)
            throw new ArgumentException("Cannot determine X-axis: first two points project to the same location on the best-fit plane.");
        xAxis = xAxis / xLen;

        var yAxis = Vector3<T>.Cross(zAxis, xAxis);
        return new Frame3<T>(centroid, Quaternion<T>.FromAxes(xAxis, yAxis, zAxis));
    }

    /// <summary>
    /// Finds the eigenvector of the smallest eigenvalue of a 3×3 symmetric matrix: the analytical trigonometric
    /// method for the eigenvalue, then the cross product of rows for the eigenvector.
    /// </summary>
    private static Vector3<T> SmallestEigenvector3x3Symmetric(T sxx, T sxy, T sxz, T syy, T syz, T szz)
    {
        var three = T.CreateTruncating(3);
        var six = T.CreateTruncating(6);
        var epsilon = T.CreateTruncating(1e-12);

        var m = (sxx + syy + szz) / three;
        var kxx = sxx - m;
        var kyy = syy - m;
        var kzz = szz - m;

        var q = (kxx * kxx + kyy * kyy + kzz * kzz + Two * (sxy * sxy + sxz * sxz + syz * syz)) / six;
        var p = T.Sqrt(q);
        if (p < epsilon)
            throw new ArgumentException("Points are collinear or coincident and do not define a plane.");

        var invP = T.One / p;
        var bxx = kxx * invP;
        var bxy = sxy * invP;
        var bxz = sxz * invP;
        var byy = kyy * invP;
        var byz = syz * invP;
        var bzz = kzz * invP;

        var halfDetB = (bxx * (byy * bzz - byz * byz)
                        - bxy * (bxy * bzz - byz * bxz)
                        + bxz * (bxy * byz - byy * bxz)) / Two;
        if (halfDetB > T.One) halfDetB = T.One;
        if (halfDetB < -T.One) halfDetB = -T.One;

        var phi = T.Acos(halfDetB) / three;
        var e3 = m + Two * p * T.Cos(phi + Two * T.Pi / three);

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

    #endregion
}
