using System.Numerics;

namespace ModelingEvolution.Drawing
{
    /// <summary>
    /// Represents thickness values for all four sides of a rectangular area (Top, Right, Bottom, Left).
    /// </summary>
    /// <typeparam name="T">The numeric type for thickness values.</typeparam>
    public readonly record struct Thickness<T> where T : IFloatingPointIeee754<T>
    {

        /// <summary>
        /// Initializes a new instance with the same thickness value for all sides.
        /// </summary>
        /// <param name="v">The thickness value to apply to all sides.</param>
        public Thickness(T v)
        {
            Top = Right = Bottom = Left = v;
        }
        /// <summary>
        /// Initializes a new instance with default thickness values (zero for all sides).
        /// </summary>
        public Thickness() {}

        /// <summary>
        /// Initializes a new instance with horizontal and vertical thickness values.
        /// </summary>
        /// <param name="x">The horizontal thickness (applied to Left and Right).</param>
        /// <param name="y">The vertical thickness (applied to Top and Bottom).</param>
        public Thickness(T x, T y)
        {
            Top = Bottom = y;
            Left = Right = x;
        }
        /// <summary>
        /// Gets the top thickness value.
        /// </summary>
        public T Top { get; init; }
        /// <summary>
        /// Gets the right thickness value.
        /// </summary>
        public T Right { get; init; }
        /// <summary>
        /// Gets the bottom thickness value.
        /// </summary>
        public T Bottom { get; init; }
        /// <summary>
        /// Gets the left thickness value.
        /// </summary>
        public T Left { get; init; }

        /// <summary>
        /// Converts the thickness to a CSS-style string representation.
        /// </summary>
        /// <returns>A string in CSS format: "top right bottom left" with pixel units.</returns>
        public string ToStyle()
        {
            return $"{Top.ToPx()} {Right.ToPx()} {Bottom.ToPx()} {Left.ToPx()}";
        }
    }
}
