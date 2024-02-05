namespace ModelingEvolution.Drawing;
using VectorF = Vector<float>;
public readonly record struct RectangleAreaF
{
    private readonly RectangleF _result;
    private readonly bool _isEmpty = true;

    public RectangleAreaF()
    {
        _isEmpty = true;
        _result = RectangleF.Empty;
    }

    private RectangleAreaF(RectangleF result)
    {
        _isEmpty = false;
        _result = result;
    }

    public VectorF Offset => _result.Location;
    public float Value => _result.Height * _result.Width;
    public static implicit operator SizeF(RectangleAreaF rectangleAreaF) => rectangleAreaF._result.Size;
    public static implicit operator float(RectangleAreaF rectangleAreaF) => rectangleAreaF.Value;
    public static explicit operator RectangleF(RectangleAreaF rectangleAreaF) => rectangleAreaF._result;
    public static RectangleAreaF operator +(RectangleAreaF left, RectangleF right)
    {
        if (right.Size == SizeF.Empty) return left;

        return left._isEmpty ? new RectangleAreaF(right) : 
            new RectangleAreaF(RectangleF.Union(right, left._result));
    }

    public override string ToString()
    {
        return _isEmpty ? "[-]": _result.ToString();
    }
}