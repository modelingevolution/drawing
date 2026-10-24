using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ProtoBuf;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents a 3D rotation using Euler angles (Roll, Pitch, Yaw) in degrees.
/// Convention: Rotation order is Z-Y-X (Yaw, Pitch, Roll) - also known as Tait-Bryan angles.
/// Since 2.0 this is a wire and edit format only, like <see cref="Pose3{T}"/>: rotation maths uses
/// <see cref="Quaternion{T}"/> (<see cref="ToQuaternion"/>) or <see cref="Frame3{T}"/> (epic-129 design, rule 2).
/// </summary>
/// <typeparam name="T">The numeric type used for angles.</typeparam>
[ProtoContract]
[Rotation3JsonConverterAttribute]
[System.Runtime.CompilerServices.CollectionBuilder(typeof(Rotation3CollectionBuilder), nameof(Rotation3CollectionBuilder.Create))]
public readonly struct Rotation3<T> : IEquatable<Rotation3<T>>, IParsable<Rotation3<T>>
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Degree<T> _rx; // Roll - rotation around X axis
    private readonly Degree<T> _ry; // Pitch - rotation around Y axis
    private readonly Degree<T> _rz; // Yaw - rotation around Z axis

    private static readonly T Two = T.CreateTruncating(2);

    /// <summary>
    /// Represents zero rotation (identity).
    /// </summary>
    public static readonly Rotation3<T> Identity = new(T.Zero, T.Zero, T.Zero);

    /// <summary>
    /// Initializes a new instance with the specified Euler angles in degrees.
    /// </summary>
    /// <param name="rx">Roll - rotation around X axis in degrees.</param>
    /// <param name="ry">Pitch - rotation around Y axis in degrees.</param>
    /// <param name="rz">Yaw - rotation around Z axis in degrees.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rotation3(Degree<T> rx, Degree<T> ry, Degree<T> rz)
    {
        _rx = rx;
        _ry = ry;
        _rz = rz;
    }

    /// <summary>
    /// Gets or initializes Roll (rotation around X axis) in degrees.
    /// </summary>
    public Degree<T> Rx
    {
        get => _rx;
        init => _rx = value;
    }

    /// <summary>
    /// Gets or initializes Pitch (rotation around Y axis) in degrees.
    /// </summary>
    public Degree<T> Ry
    {
        get => _ry;
        init => _ry = value;
    }

    /// <summary>
    /// Gets or initializes Yaw (rotation around Z axis) in degrees.
    /// </summary>
    public Degree<T> Rz
    {
        get => _rz;
        init => _rz = value;
    }

    /// <summary>
    /// Returns a struct enumerator over the three angles (Rx, Ry, Rz) in degrees, in order.
    /// Required by <see cref="System.Runtime.CompilerServices.CollectionBuilderAttribute"/>
    /// so collection-expression literals (<c>Rotation3&lt;double&gt; r = [0, 0, 5];</c>) resolve the element type.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Struct enumerator over the three Euler angles (in degrees) of a <see cref="Rotation3{T}"/>.</summary>
    public struct Enumerator
    {
        private readonly Rotation3<T> _r;
        private int _i;
        internal Enumerator(Rotation3<T> r) { _r = r; _i = -1; }
        public T Current => _i switch { 0 => (T)_r._rx, 1 => (T)_r._ry, 2 => (T)_r._rz, _ => throw new InvalidOperationException() };
        public bool MoveNext() => ++_i < 3;
    }

    // Protobuf shadow properties — serialize as raw T for wire compatibility
    [ProtoMember(1)]
    private T ProtoRx
    {
        get => (T)_rx;
        init => _rx = value;
    }

    [ProtoMember(2)]
    private T ProtoRy
    {
        get => (T)_ry;
        init => _ry = value;
    }

    [ProtoMember(3)]
    private T ProtoRz
    {
        get => (T)_rz;
        init => _rz = value;
    }

    /// <summary>
    /// Gets a value indicating whether this rotation is identity (no rotation).
    /// </summary>
    public bool IsIdentity => _rx == Degree<T>.Zero && _ry == Degree<T>.Zero && _rz == Degree<T>.Zero;

    /// <summary>
    /// Creates a rotation from degrees.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotation3<T> FromDegrees(T rx, T ry, T rz) => new(rx, ry, rz);

    /// <summary>
    /// Creates a rotation from radians.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotation3<T> FromRadians(Radian<T> rx, Radian<T> ry, Radian<T> rz) => new((Degree<T>)rx, (Degree<T>)ry, (Degree<T>)rz);

    /// <summary>
    /// Gets the rotation angles in radians.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Radian<T> rx, Radian<T> ry, Radian<T> rz) ToRadians() => ((Radian<T>)_rx, (Radian<T>)_ry, (Radian<T>)_rz);

    /// <summary>
    /// Converts to a quaternion representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion<T> ToQuaternion()
    {
        var (rx, ry, rz) = ToRadians();
        return Quaternion<T>.FromRpy((T)rx, (T)ry, (T)rz);
    }

    /// <summary>
    /// Creates a rotation from a quaternion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotation3<T> FromQuaternion(Quaternion<T> q)
    {
        // Extract Euler angles from quaternion (ZYX order)
        var sinp = Two * (q.W * q.Y - q.Z * q.X);
        T rx, ry, rz;

        var gimbalThreshold = T.One - T.CreateTruncating(1e-6);

        if (T.Abs(sinp) >= gimbalThreshold)
        {
            // Gimbal lock: Ry ~ +/-90 deg. Rx and Rz share one degree of freedom.
            // Convention: set Rz = 0, recover Rx from rotation matrix R[0][1], R[0][2].
            ry = T.CopySign(T.Pi / Two, sinp);
            rz = T.Zero;

            var r01 = Two * (q.X * q.Y - q.W * q.Z);
            var r02 = Two * (q.X * q.Z + q.W * q.Y);

            rx = sinp > T.Zero
                ? T.Atan2(r01, r02)
                : T.Atan2(-r01, -r02);
        }
        else
        {
            // Normal case
            var sinrCosp = Two * (q.W * q.X + q.Y * q.Z);
            var cosrCosp = T.One - Two * (q.X * q.X + q.Y * q.Y);
            rx = T.Atan2(sinrCosp, cosrCosp);

            ry = T.Asin(sinp);

            var sinyCosp = Two * (q.W * q.Z + q.X * q.Y);
            var cosyCosp = T.One - Two * (q.Y * q.Y + q.Z * q.Z);
            rz = T.Atan2(sinyCosp, cosyCosp);
        }

        // rx, ry, rz are in radians — wrap in Radian<T> then implicit-convert to Degree<T>
        return new Rotation3<T>(
            (Degree<T>)Radian<T>.FromRadian(rx),
            (Degree<T>)Radian<T>.FromRadian(ry),
            (Degree<T>)Radian<T>.FromRadian(rz));
    }

    /// <summary>
    /// Converts this rotation to a different numeric type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rotation3<U> Truncating<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>, ISignedNumber<U>, IFloatingPointIeee754<U>, IMinMaxValue<U>
    {
        return new Rotation3<U>(_rx.Truncate<U>(), _ry.Truncate<U>(), _rz.Truncate<U>());
    }

    #region Operators

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Rotation3<T> a, Rotation3<T> b) => a._rx == b._rx && a._ry == b._ry && a._rz == b._rz;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Rotation3<T> a, Rotation3<T> b) => !(a == b);

    #endregion

    /// <summary>
    /// Creates a rotation from a 3x3 rotation matrix: <see cref="Quaternion{T}.FromMatrix3x3"/>, then
    /// <see cref="FromQuaternion"/> (the one rotation → Euler formula).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotation3<T> FromMatrix3x3(Matrix3x3<T> m) => FromQuaternion(Quaternion<T>.FromMatrix3x3(m));

    #region Conversions

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Rotation3<T>((T rx, T ry, T rz) tuple) => new(tuple.rx, tuple.ry, tuple.rz);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (T rx, T ry, T rz)(Rotation3<T> r) => ((T)r._rx, (T)r._ry, (T)r._rz);

    #endregion

    #region Equality & Formatting

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Rotation3<T> other) => this == other;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Rotation3<T> r && Equals(r);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(_rx, _ry, _rz);
    public override string ToString() => $"{{Rx={(T)_rx}, Ry={(T)_ry}, Rz={(T)_rz}}}";

    #endregion

    #region Parsing

    public static Rotation3<T> Parse(string source, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz))
            throw new FormatException($"Invalid Rotation3<T> format: {source}");

        if (!tokenizer.HasNoMoreTokens())
            throw new InvalidOperationException($"Invalid Rotation3<T> format: {source}");

        return new Rotation3<T>(rx, ry, rz);
    }

    public static bool TryParse(string? source, IFormatProvider? provider, out Rotation3<T> result)
    {
        result = Identity;
        if (source == null) return false;

        var tokenizer = new NumericListTokenizer(source, CultureInfo.InvariantCulture);

        if (!T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
            !T.TryParse(tokenizer.GetNextToken(), NumberStyles.Float, CultureInfo.InvariantCulture, out var rz) ||
            !tokenizer.HasNoMoreTokens())
            return false;

        result = new Rotation3<T>(rx, ry, rz);
        return true;
    }

    #endregion
}
