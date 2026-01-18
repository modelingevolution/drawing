using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a triangle in 3D space defined by three vertices.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly struct Triangle3<T> : IEquatable<Triangle3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Three = T.CreateTruncating(3);

    private readonly Point3<T> _a;
    private readonly Point3<T> _b;
    private readonly Point3<T> _c;

    /// <summary>
    /// Initializes a new triangle with the specified vertices.
    /// </summary>
    public Triangle3(Point3<T> a, Point3<T> b, Point3<T> c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    /// <summary>First vertex of the triangle.</summary>
    public Point3<T> A => _a;

    /// <summary>Second vertex of the triangle.</summary>
    public Point3<T> B => _b;

    /// <summary>Third vertex of the triangle.</summary>
    public Point3<T> C => _c;

    /// <summary>
    /// Gets the centroid (center of mass) of the triangle.
    /// </summary>
    public Point3<T> Centroid => new(
        (_a.X + _b.X + _c.X) / Three,
        (_a.Y + _b.Y + _c.Y) / Three,
        (_a.Z + _b.Z + _c.Z) / Three);

    /// <summary>
    /// Gets the unit normal vector of the triangle plane (right-hand rule: A→B→C).
    /// </summary>
    public Vector3<T> Normal
    {
        get
        {
            var ab = _b - _a;
            var ac = _c - _a;
            return Vector3<T>.Cross(ab, ac).Normalize();
        }
    }

    /// <summary>
    /// Gets the area of the triangle using the cross product formula.
    /// </summary>
    public T Area
    {
        get
        {
            // Area = |AB × AC| / 2
            var ab = _b - _a;
            var ac = _c - _a;
            var cross = Vector3<T>.Cross(ab, ac);
            return cross.Length / Two;
        }
    }

    /// <summary>
    /// Gets the perimeter of the triangle.
    /// </summary>
    public T Perimeter =>
        Point3<T>.Distance(_a, _b) +
        Point3<T>.Distance(_b, _c) +
        Point3<T>.Distance(_c, _a);

    /// <summary>
    /// Creates a pose at the triangle's centroid with Z-axis along the normal.
    /// X-axis is along edge A→B, Y-axis completes the right-handed system.
    /// </summary>
    /// <returns>A pose representing the triangle's plane orientation at its center.</returns>
    public Pose3<T> ToPose()
    {
        // Get rotation from surface, then create pose at centroid
        var surfacePose = Pose3<T>.FromSurface(_a, _b, _c);
        return new Pose3<T>(Centroid, surfacePose.Rotation);
    }

    #region Operators

    /// <summary>Translates the triangle by the specified vector.</summary>
    public static Triangle3<T> operator +(Triangle3<T> t, Vector3<T> v) =>
        new(t._a + v, t._b + v, t._c + v);

    /// <summary>Translates the triangle by the negation of the specified vector.</summary>
    public static Triangle3<T> operator -(Triangle3<T> t, Vector3<T> v) =>
        new(t._a - v, t._b - v, t._c - v);

    public static bool operator ==(Triangle3<T> left, Triangle3<T> right) => left.Equals(right);
    public static bool operator !=(Triangle3<T> left, Triangle3<T> right) => !left.Equals(right);

    #endregion

    #region Equality & Formatting

    public bool Equals(Triangle3<T> other) =>
        _a == other._a && _b == other._b && _c == other._c;

    public override bool Equals(object? obj) =>
        obj is Triangle3<T> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(_a, _b, _c);

    public override string ToString() =>
        $"Triangle3({_a}, {_b}, {_c})";

    #endregion
}
