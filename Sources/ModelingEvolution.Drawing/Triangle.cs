using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a triangle in 2D space defined by three vertices.
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates.</typeparam>
public readonly struct Triangle<T> : IEquatable<Triangle<T>>, IShape<T, Triangle<T>>
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
    public Point<T> Centroid() => new(
        (_a.X + _b.X + _c.X) / Three,
        (_a.Y + _b.Y + _c.Y) / Three);

    /// <summary>
    /// Gets the area of the triangle using the cross product formula.
    /// </summary>
    public T Area()
    {
        // Area = |AB × AC| / 2
        // For 2D: AB × AC = (bx-ax)(cy-ay) - (by-ay)(cx-ax)
        var cross = (_b.X - _a.X) * (_c.Y - _a.Y) - (_b.Y - _a.Y) * (_c.X - _a.X);
        return T.Abs(cross) / Two;
    }

    /// <summary>
    /// Gets the perimeter of the triangle.
    /// </summary>
    public T Perimeter() =>
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

    /// <summary>
    /// Determines whether this triangle is similar to another triangle (same shape, possibly different size).
    /// Uses the SSS ratio criterion: all three pairs of sorted side lengths have the same ratio.
    /// </summary>
    /// <param name="other">The other triangle.</param>
    /// <param name="epsilon">Tolerance for floating-point comparison. Defaults to 1e-9.</param>
    /// <returns>True if the triangles are similar.</returns>
    public bool IsSimilarTo(in Triangle<T> other) => IsSimilarTo(other, T.CreateTruncating(1e-9));

    /// <inheritdoc cref="IsSimilarTo(in Triangle{T})"/>
    public bool IsSimilarTo(in Triangle<T> other, T epsilon)
    {
        var eps = epsilon;

        // Get sorted side lengths for both triangles
        Sort3(Distance(_a, _b), Distance(_b, _c), Distance(_c, _a),
              out var s1a, out var s1b, out var s1c);
        Sort3(Distance(other._a, other._b), Distance(other._b, other._c), Distance(other._c, other._a),
              out var s2a, out var s2b, out var s2c);

        // Degenerate triangle check
        if (s1a < eps || s2a < eps)
            return false;

        // Check that ratios are equal: s1[0]/s2[0] == s1[1]/s2[1] == s1[2]/s2[2]
        // Using cross-multiplication to avoid division: s1[i]*s2[0] == s1[0]*s2[i]
        var r = s1a * s2b - s1b * s2a;
        if (T.Abs(r) > eps * s1c) return false;

        r = s1a * s2c - s1c * s2a;
        return T.Abs(r) <= eps * s1c;
    }

    private static void Sort3(T a, T b, T c, out T lo, out T mid, out T hi)
    {
        if (a > b) (a, b) = (b, a);
        if (b > c) (b, c) = (c, b);
        if (a > b) (a, b) = (b, a);
        lo = a; mid = b; hi = c;
    }

    /// <summary>
    /// Determines whether this triangle is congruent to another triangle (same shape and size).
    /// Uses the SSS criterion: all three pairs of sorted side lengths are equal.
    /// </summary>
    /// <param name="other">The other triangle.</param>
    /// <returns>True if the triangles are congruent.</returns>
    public bool IsCongruentTo(in Triangle<T> other) => IsCongruentTo(other, T.CreateTruncating(1e-9));

    /// <inheritdoc cref="IsCongruentTo(in Triangle{T})"/>
    public bool IsCongruentTo(in Triangle<T> other, T epsilon)
    {
        Sort3(Distance(_a, _b), Distance(_b, _c), Distance(_c, _a),
              out var s1a, out var s1b, out var s1c);
        Sort3(Distance(other._a, other._b), Distance(other._b, other._c), Distance(other._c, other._a),
              out var s2a, out var s2b, out var s2c);

        return T.Abs(s1a - s2a) <= epsilon
            && T.Abs(s1b - s2b) <= epsilon
            && T.Abs(s1c - s2c) <= epsilon;
    }

    /// <summary>
    /// Returns the incircle (inscribed circle) of this triangle.
    /// The incircle is the largest circle that fits inside the triangle.
    /// Its center is the incenter (intersection of angle bisectors).
    /// </summary>
    public Circle<T> Incircle()
    {
        var ab = Distance(_a, _b);
        var bc = Distance(_b, _c);
        var ca = Distance(_c, _a);
        var p = ab + bc + ca;

        // Incenter = (a*|BC| + b*|CA| + c*|AB|) / perimeter
        var cx = (bc * _a.X + ca * _b.X + ab * _c.X) / p;
        var cy = (bc * _a.Y + ca * _b.Y + ab * _c.Y) / p;

        // Inradius = 2 * Area / Perimeter
        var r = Two * Area() / p;

        return new Circle<T>(new Point<T>(cx, cy), r);
    }

    /// <summary>
    /// Returns the circumcircle (circumscribed circle) of this triangle.
    /// The circumcircle passes through all three vertices.
    /// Its center is the circumcenter (intersection of perpendicular bisectors).
    /// </summary>
    public Circle<T> Circumcircle()
    {
        // Using the determinant formula for circumcenter
        var ax = _a.X; var ay = _a.Y;
        var bx = _b.X; var by = _b.Y;
        var cx = _c.X; var cy = _c.Y;

        var d = Two * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

        var aSq = ax * ax + ay * ay;
        var bSq = bx * bx + by * by;
        var cSq = cx * cx + cy * cy;

        var ux = (aSq * (by - cy) + bSq * (cy - ay) + cSq * (ay - by)) / d;
        var uy = (aSq * (cx - bx) + bSq * (ax - cx) + cSq * (bx - ax)) / d;

        var center = new Point<T>(ux, uy);
        var radius = Distance(center, _a);

        return new Circle<T>(center, radius);
    }

    private static T Distance(Point<T> a, Point<T> b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return T.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Gets the orthocenter of the triangle (intersection of altitudes).
    /// </summary>
    public Point<T> Orthocenter
    {
        get
        {
            var lineBC = Line<T>.From(_b, _c);
            var altA = lineBC.PerpendicularAt(_a);
            var lineAC = Line<T>.From(_a, _c);
            var altB = lineAC.PerpendicularAt(_b);
            return altA.Intersect(altB) ?? _a;
        }
    }

    /// <summary>
    /// Gets the three edges of the triangle as segments.
    /// </summary>
    public (Segment<T> AB, Segment<T> BC, Segment<T> CA) Edges =>
        (new Segment<T>(_a, _b), new Segment<T>(_b, _c), new Segment<T>(_c, _a));

    /// <summary>
    /// Gets the interior angles of the triangle at each vertex.
    /// </summary>
    public (Radian<T> AtA, Radian<T> AtB, Radian<T> AtC) Angles
    {
        get
        {
            var ab = Distance(_a, _b);
            var bc = Distance(_b, _c);
            var ca = Distance(_c, _a);

            var cosA = (ca * ca + ab * ab - bc * bc) / (Two * ca * ab);
            var cosB = (ab * ab + bc * bc - ca * ca) / (Two * ab * bc);
            var cosC = (bc * bc + ca * ca - ab * ab) / (Two * bc * ca);

            return (
                Radian<T>.FromRadian(T.Acos(T.Max(-T.One, T.Min(T.One, cosA)))),
                Radian<T>.FromRadian(T.Acos(T.Max(-T.One, T.Min(T.One, cosB)))),
                Radian<T>.FromRadian(T.Acos(T.Max(-T.One, T.Min(T.One, cosC))))
            );
        }
    }

    /// <summary>
    /// Determines whether this triangle is a right triangle (has a 90 degree angle).
    /// </summary>
    public bool IsRight() => IsRight(T.CreateTruncating(1e-9));

    /// <inheritdoc cref="IsRight()"/>
    public bool IsRight(T epsilon)
    {
        var halfPi = T.Pi / Two;
        var (a, b, c) = Angles;
        return T.Abs((T)a - halfPi) < epsilon || T.Abs((T)b - halfPi) < epsilon || T.Abs((T)c - halfPi) < epsilon;
    }

    /// <summary>
    /// Determines whether this triangle is acute (all angles less than 90 degrees).
    /// </summary>
    public bool IsAcute()
    {
        var halfPi = T.Pi / Two;
        var (a, b, c) = Angles;
        return (T)a < halfPi && (T)b < halfPi && (T)c < halfPi;
    }

    /// <summary>
    /// Determines whether this triangle is obtuse (has an angle greater than 90 degrees).
    /// </summary>
    public bool IsObtuse()
    {
        var halfPi = T.Pi / Two;
        var (a, b, c) = Angles;
        return (T)a > halfPi || (T)b > halfPi || (T)c > halfPi;
    }

    /// <summary>
    /// Determines whether this triangle is equilateral (all sides equal).
    /// </summary>
    public bool IsEquilateral() => IsEquilateral(T.CreateTruncating(1e-9));

    /// <inheritdoc cref="IsEquilateral()"/>
    public bool IsEquilateral(T epsilon)
    {
        var ab = Distance(_a, _b);
        var bc = Distance(_b, _c);
        var ca = Distance(_c, _a);
        return T.Abs(ab - bc) <= epsilon && T.Abs(bc - ca) <= epsilon;
    }

    /// <summary>
    /// Determines whether this triangle is isosceles (at least two sides equal).
    /// </summary>
    public bool IsIsosceles() => IsIsosceles(T.CreateTruncating(1e-9));

    /// <inheritdoc cref="IsIsosceles()"/>
    public bool IsIsosceles(T epsilon)
    {
        var ab = Distance(_a, _b);
        var bc = Distance(_b, _c);
        var ca = Distance(_c, _a);
        return T.Abs(ab - bc) <= epsilon || T.Abs(bc - ca) <= epsilon || T.Abs(ca - ab) <= epsilon;
    }

    /// <summary>
    /// Determines whether this triangle is scalene (all sides different).
    /// </summary>
    public bool IsScalene() => !IsIsosceles();

    /// <summary>
    /// Returns the portion of this triangle that lies inside the rectangle.
    /// The result is a polygon (3 to 6 vertices) since clipping a triangle by a rectangle
    /// can produce various convex polygons.
    /// </summary>
    public Polygon<T> Intersect(Rectangle<T> rect)
    {
        var poly = (Polygon<T>)this;
        var results = poly.Intersect(rect);
        using var enumerator = results.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : new Polygon<T>();
    }

    /// <summary>
    /// Returns the axis-aligned bounding box of the triangle.
    /// </summary>
    public Rectangle<T> BoundingBox()
    {
        var minX = T.Min(T.Min(_a.X, _b.X), _c.X);
        var minY = T.Min(T.Min(_a.Y, _b.Y), _c.Y);
        var maxX = T.Max(T.Max(_a.X, _b.X), _c.X);
        var maxY = T.Max(T.Max(_a.Y, _b.Y), _c.Y);
        return new Rectangle<T>(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Returns a new triangle scaled uniformly around its centroid.
    /// </summary>
    public Triangle<T> Scale(T factor)
    {
        var c = Centroid();
        return new Triangle<T>(
            new Point<T>(c.X + (_a.X - c.X) * factor, c.Y + (_a.Y - c.Y) * factor),
            new Point<T>(c.X + (_b.X - c.X) * factor, c.Y + (_b.Y - c.Y) * factor),
            new Point<T>(c.X + (_c.X - c.X) * factor, c.Y + (_c.Y - c.Y) * factor));
    }

    #region Operators

    /// <summary>Translates the triangle by the specified vector.</summary>
    public static Triangle<T> operator +(Triangle<T> t, Vector<T> v) =>
        new(t._a + v, t._b + v, t._c + v);

    /// <summary>Translates the triangle by the negation of the specified vector.</summary>
    public static Triangle<T> operator -(Triangle<T> t, Vector<T> v) =>
        new(t._a - v, t._b - v, t._c - v);

    /// <summary>Rotates the triangle around the specified origin by the given angle.</summary>
    public Triangle<T> Rotate(Degree<T> angle, Point<T> origin = default) =>
        new(_a.Rotate(angle, origin), _b.Rotate(angle, origin), _c.Rotate(angle, origin));

    /// <summary>Rotates the triangle around the origin by the given angle.</summary>
    public static Triangle<T> operator +(Triangle<T> t, Degree<T> angle) =>
        t.Rotate(angle);

    /// <summary>Rotates the triangle around the origin by the negation of the given angle.</summary>
    public static Triangle<T> operator -(Triangle<T> t, Degree<T> angle) =>
        t.Rotate(-angle);

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
