using System.Numerics;

namespace ModelingEvolution.Drawing
{
    public readonly record struct Thickness<T> where T : IFloatingPointIeee754<T>
    {

        public Thickness(T v)
        {
            Top = Right = Bottom = Left = v;
        }
        public Thickness() {}

        public Thickness(T x, T y)
        {
            Top = Bottom = y;
            Left = Right = x;
        }
        public T Top { get; init; }
        public T Right { get; init; }
        public T Bottom { get; init; }
        public T Left { get; init; }

        public string ToStyle()
        {
            return $"{Top.ToPx()} {Right.ToPx()} {Bottom.ToPx()} {Left.ToPx()}";
        }
    }
}
