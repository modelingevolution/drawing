using System.Numerics;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D velocity vector (direction + magnitude) with generic numeric type.
/// The magnitude (speed) has units of distance per second.
/// </summary>
/// <typeparam name="T">The numeric type used for components.</typeparam>
[ProtoContract]
public readonly record struct Velocity3<T>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>,
    IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    [ProtoMember(1)]
    private readonly Vector3<T> _vector;

    /// <summary>
    /// Gets a zero velocity.
    /// </summary>
    public static Velocity3<T> Zero { get; } = new(Vector3<T>.Zero);

    /// <summary>
    /// Initializes a new velocity from a vector.
    /// </summary>
    public Velocity3(Vector3<T> vector) => _vector = vector;

    /// <summary>
    /// Initializes a new velocity from components.
    /// </summary>
    public Velocity3(T x, T y, T z) => _vector = new Vector3<T>(x, y, z);

    /// <summary>
    /// Creates a velocity from a direction and speed.
    /// </summary>
    public static Velocity3<T> From(Vector3<T> direction, Speed<T> speed)
        => new(direction.Normalize() * (T)speed);

    /// <summary>Gets the X component.</summary>
    public T X => _vector.X;

    /// <summary>Gets the Y component.</summary>
    public T Y => _vector.Y;

    /// <summary>Gets the Z component.</summary>
    public T Z => _vector.Z;

    /// <summary>
    /// Gets the speed (magnitude) of this velocity.
    /// </summary>
    public Speed<T> Speed => Speed<T>.From(_vector.Length);

    /// <summary>
    /// Gets the normalized direction of this velocity. Returns zero vector if speed is zero.
    /// </summary>
    public Vector3<T> Direction
    {
        get
        {
            var len = _vector.Length;
            return len == T.Zero ? Vector3<T>.Zero : _vector / len;
        }
    }

    /// <summary>
    /// Gets the underlying vector.
    /// </summary>
    public Vector3<T> Vector => _vector;

    #region Operators

    public static Velocity3<T> operator +(Velocity3<T> a, Velocity3<T> b) => new(a._vector + b._vector);
    public static Velocity3<T> operator -(Velocity3<T> a, Velocity3<T> b) => new(a._vector - b._vector);
    public static Velocity3<T> operator -(Velocity3<T> a) => new(-a._vector);
    public static Velocity3<T> operator *(Velocity3<T> a, T scalar) => new(a._vector * scalar);
    public static Velocity3<T> operator *(T scalar, Velocity3<T> a) => new(a._vector * scalar);
    public static Velocity3<T> operator /(Velocity3<T> a, T scalar) => new(a._vector / scalar);

    /// <summary>
    /// Computes the displacement vector for traveling at this velocity for the given time.
    /// </summary>
    public Vector3<T> DisplacementIn(T time) => _vector * time;

    #endregion

    #region Conversions

    public static implicit operator Velocity3<T>(Vector3<T> v) => new(v);
    public static explicit operator Vector3<T>(Velocity3<T> v) => v._vector;

    #endregion

    public override string ToString() => $"Velocity3({_vector.X}, {_vector.Y}, {_vector.Z})";
}
