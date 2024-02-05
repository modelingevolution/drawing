namespace ModelingEvolution.Drawing
{
    public readonly record struct ThicknessF
    {

        public ThicknessF(float v)
        {
            Top = Right = Bottom = Left = v;
        }
        public ThicknessF() {}

        public ThicknessF(float x, float y)
        {
            Top = Bottom = y;
            Left = Right = x;
        }
        public float Top { get; init; }
        public float Right { get; init; }
        public float Bottom { get; init; }
        public float Left { get; init; }

        public string ToStyle()
        {
            return $"{Top.ToPx()} {Right.ToPx()} {Bottom.ToPx()} {Left.ToPx()}";
        }
    }
}
