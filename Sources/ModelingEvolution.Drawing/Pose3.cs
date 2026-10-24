using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D pose (position + orientation) using generic numeric types: the wire and edit format
/// (for example mm plus ZYX degrees). It carries no maths; convert it to <see cref="Frame3{T}"/> with
/// <see cref="Frame3{T}.FromPose3"/> to compose, invert, transform or interpolate.
/// Combines Point3{T} (x, y, z) and Rotation3{T} (rx, ry, rz).
/// </summary>
/// <typeparam name="T">The numeric type used for coordinates and angles.</typeparam>
[ProtoContract]
[Pose3JsonConverterAttribute]
[System.Runtime.CompilerServices.CollectionBuilder(typeof(Pose3CollectionBuilder), nameof(Pose3CollectionBuilder.Create))]
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
    /// Returns a struct enumerator over the six components (X, Y, Z, Rx, Ry, Rz) in order,
    /// with rotation angles in degrees. Required by <see cref="System.Runtime.CompilerServices.CollectionBuilderAttribute"/>
    /// so collection-expression literals (<c>Pose3&lt;double&gt; p = [120, 50, 200, 180, 0, 0];</c>) resolve the element type.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Struct enumerator over the six components of a <see cref="Pose3{T}"/>.</summary>
    public struct Enumerator
    {
        private readonly Pose3<T> _p;
        private int _i;
        internal Enumerator(Pose3<T> p) { _p = p; _i = -1; }
        public T Current => _i switch
        {
            0 => _p._position.X,
            1 => _p._position.Y,
            2 => _p._position.Z,
            3 => (T)_p._rotation.Rx,
            4 => (T)_p._rotation.Ry,
            5 => (T)_p._rotation.Rz,
            _ => throw new InvalidOperationException()
        };
        public bool MoveNext() => ++_i < 6;
    }

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
