using System.Diagnostics;
using System.Numerics;

namespace ModelingEvolution.Drawing;

public static class Degree
{
    public static T Sin<T>(Degree<T> degree) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => Radian.Sin((Radian<T>)degree);
    public static T Cos<T>(Degree<T> degree) 
        where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T> => Radian.Cos((Radian<T>)degree);
}
[DebuggerDisplay("{_val}")]
public readonly record struct Degree<T> : IComparisonOperators<Degree<T>, Degree<T>, bool>, IComparable<Degree<T>>, IComparable 
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>
{
    public static Degree<T> Zero { get; } = new Degree<T>();
    internal readonly T _val;
    public static implicit operator Degree<T>(T src) => new(src);
    public static Degree<T> Create(T degrees) => new Degree<T>(degrees);
    private Degree(T value)
    {
        _val = value;
    }

    public Degree<U> Truncate<U>()
        where U : INumber<U>, ITrigonometricFunctions<U>, IRootFunctions<U>, IFloatingPoint<U>
    {
        var v = U.CreateTruncating(this._val);
        return new Degree<U>(v);
    }
    
    public static explicit operator T(Degree<T> src)
    {
        return src._val;
    }
    public static Degree<T> operator -(Degree<T> a, Degree<T> b)
    {
        return new Degree<T>(a._val - b._val);
    }
    public static Degree<T> operator +(Degree<T> a, Degree<T> b)
    {
        return new Degree<T>(a._val + b._val);
    }
    public static bool operator <=(Degree<T> first, Degree<T> second)
    {
        return first._val <= second._val;
    }
    public static bool operator >=(Degree<T> first, Degree<T> second)
    {
        return first._val >= second._val;
    }

    public static bool operator <(Degree<T> first, Degree<T> second)
    {
        return first._val < second._val;
    }

    public static bool operator >(Degree<T> first, Degree<T> second)
    {
        return first._val > second._val;
    }

    public static implicit operator Degree<T>(Radian<T> src)
    {
        var tmp = (T)src / T.Pi * T.CreateChecked(180);
        return new Degree<T>(tmp);
    }

    public T GetValue()
    {
        return _val; 
    }
    

    public int CompareTo(Degree<T> other)
    {
        return _val.CompareTo(other._val);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is Degree<T> other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Degree<T>)}");
    }

    public override string ToString()
    {
        return $"{_val}\u00b0";
    }
}