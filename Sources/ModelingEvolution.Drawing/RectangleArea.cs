using System.Numerics;

namespace ModelingEvolution.Drawing;
using VectorF = Vector<float>;
public readonly record struct RectangleArea<T>
    where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private readonly Rectangle<T> _result;
    private readonly bool _isEmpty = true;

    public RectangleArea()
    {
        _isEmpty = true;
        _result = Rectangle<T>.Empty;
    }

    private RectangleArea(Rectangle<T> result)
    {
        _isEmpty = false;
        _result = result;
    }

    public Vector<T> Offset => _result.Location;
    public T Value => _result.Height * _result.Width;
    public static implicit operator Size<T>(RectangleArea<T> rectangleAreaF) => rectangleAreaF._result.Size;
    public static implicit operator T(RectangleArea<T> rectangleAreaF) => rectangleAreaF.Value;
    public static explicit operator Rectangle<T>(RectangleArea<T> rectangleAreaF) => rectangleAreaF._result;
    public static RectangleArea<T> operator +(RectangleArea<T> left, Rectangle<T> right)
    {
        if (right.Size == Size<T>.Empty) return left;

        return left._isEmpty ? new RectangleArea<T>(right) : 
            new RectangleArea<T>(Rectangle<T>.Bounds(right, left._result));
    }

    public override string ToString()
    {
        return _isEmpty ? "[-]": _result.ToString();
    }
}