using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a triangle in 2D space defined by three vertices.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly struct Triangle<T> : IEquatable<Triangle<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T Two = T.CreateTruncating(2);
    private static readonly T Three = T.CreateTruncating(3);

    private readonly Point<T> _a;
    private readonly Point<T> _b;
    private readonly Point<T> _c;

    /// <summary>
    /// Initializes a new triangle with the specified vertices.
    /// </summary>
    public Triangle(Point<T> a, Point<T> b, Point<T> c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    /// <summary>First vertex of the triangle.</summary>
    public Point<T> A => _a;

    /// <summary>Second vertex of the triangle.</summary>
    public Point<T> B => _b;

    /// <summary>Third vertex of the triangle.</summary>
    public Point<T> C => _c;

    /// <summary>
    /// Gets the centroid (center of mass) of the triangle.
    /// </summary>
    public Point<T> Centroid => new(
        (_a.X + _b.X + _c.X) / Three,
        (_a.Y + _b.Y + _c.Y) / Three);

    /// <summary>
    /// Gets the area of the triangle using the cross product formula.
    /// </summary>
    public T Area
    {
        get
        {
            // Area = |AB × AC| / 2
            // For 2D: AB × AC = (bx-ax)(cy-ay) - (by-ay)(cx-ax)
            var cross = (_b.X - _a.X) * (_c.Y - _a.Y) - (_b.Y - _a.Y) * (_c.X - _a.X);
            return T.Abs(cross) / Two;
        }
    }

    /// <summary>
    /// Gets the perimeter of the triangle.
    /// </summary>
    public T Perimeter =>
        Distance(_a, _b) + Distance(_b, _c) + Distance(_c, _a);

    /// <summary>
    /// Determines whether the specified point lies inside the triangle.
    /// Uses barycentric coordinate method.
    /// </summary>
    public bool Contains(Point<T> point)
    {
        // Compute vectors
        var v0 = _c - _a;
        var v1 = _b - _a;
        var v2 = point - _a;

        // Compute dot products (using * operator which calls Multiply)
        var dot00 = v0 * v0;
        var dot01 = v0 * v1;
        var dot02 = v0 * v2;
        var dot11 = v1 * v1;
        var dot12 = v1 * v2;

        // Compute barycentric coordinates
        var invDenom = T.One / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return u >= T.Zero && v >= T.Zero && (u + v) <= T.One;
    }

    private static T Distance(Point<T> a, Point<T> b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return T.Sqrt(dx * dx + dy * dy);
    }

    #region Operators

    /// <summary>Translates the triangle by the specified vector.</summary>
    public static Triangle<T> operator +(Triangle<T> t, Vector<T> v) =>
        new(t._a + v, t._b + v, t._c + v);

    /// <summary>Translates the triangle by the negation of the specified vector.</summary>
    public static Triangle<T> operator -(Triangle<T> t, Vector<T> v) =>
        new(t._a - v, t._b - v, t._c - v);

    public static bool operator ==(Triangle<T> left, Triangle<T> right) => left.Equals(right);
    public static bool operator !=(Triangle<T> left, Triangle<T> right) => !left.Equals(right);

    #endregion

    #region Equality & Formatting

    public bool Equals(Triangle<T> other) =>
        _a == other._a && _b == other._b && _c == other._c;

    public override bool Equals(object? obj) =>
        obj is Triangle<T> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(_a, _b, _c);

    public override string ToString() =>
        $"Triangle({_a}, {_b}, {_c})";

    #endregion
}
