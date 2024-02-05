using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

[DebuggerDisplay("{_val}rad")]
public readonly record struct Radian<T>: IComparisonOperators<Radian<T>, Radian<T>, bool>, IComparable<Radian<T>>, IComparable
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>
{
    public static Radian<T> Zero { get; } = new Radian<T>();
    private readonly T _val;
    private Radian(T value)
    {
        _val = value;
    }

    public T Sin() => T.Sin(_val);
    public T Cos() => T.Cos(_val);

    public Radian<U> Truncate<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>
    {
        var v = U.CreateTruncating(this._val);
        return new Radian<U>(v);
    }
    public static Radian<T> FromRadian(T radians) => new Radian<T>(radians);

    public static bool operator <=(Radian<T> first, Radian<T> second)
    {
        return first._val <= second._val;
    }
    public static bool operator >=(Radian<T> first, Radian<T> second)
    {
        return first._val >= second._val;
    }

    public static Radian<T> operator -(Radian<T> a, Radian<T> b)
    {
        return new Radian<T>(a._val - b._val);
    }
    public static Radian<T> operator +(Radian<T> a, Radian<T> b)
    {
        return new Radian<T>(a._val + b._val);
    }
    public static bool operator <(Radian<T> first, Radian<T> second)
    {
        return first._val < second._val;
    }
    public static bool operator >(Radian<T> first, Radian<T> second)
    {
        return first._val > second._val;
    }

    public static explicit operator T(Radian<T> src)
    {
        return src._val;
    }
    public static implicit operator Radian<T>(Degree<T> src)
    {
        var val = (T)src;
        var tmp = val / T.CreateChecked(180) * T.Pi;
        return new Radian<T>(tmp);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Radian<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Radian<T>)}");
    }

    public int CompareTo(Radian<T> other)
    {
        return _val.CompareTo(other._val);
    }

    public override string ToString()
    {
        return $"{_val}rad";
    }
}