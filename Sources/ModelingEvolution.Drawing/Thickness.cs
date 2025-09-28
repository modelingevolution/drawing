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
        /// Initializes a new instance with all four thickness values.
        /// </summary>
        /// <param name="left">The left thickness value.</param>
        /// <param name="top">The top thickness value.</param>
        /// <param name="right">The right thickness value.</param>
        /// <param name="bottom">The bottom thickness value.</param>
        public Thickness(T left, T top, T right, T bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
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
        /// Implicitly converts a tuple to a Thickness.
        /// </summary>
        /// <param name="tuple">The tuple containing Left, Top, Right, and Bottom thickness values.</param>
        /// <returns>A Thickness with the specified values.</returns>
        public static implicit operator Thickness<T>((T left, T top, T right, T bottom) tuple)
        {
            return new Thickness<T>(tuple.left, tuple.top, tuple.right, tuple.bottom);
        }

        /// <summary>
        /// Implicitly converts a Thickness to a tuple.
        /// </summary>
        /// <param name="thickness">The Thickness to convert.</param>
        /// <returns>A tuple containing the Left, Top, Right, and Bottom thickness values.</returns>
        public static implicit operator (T left, T top, T right, T bottom)(Thickness<T> thickness)
        {
            return (thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
        }

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
