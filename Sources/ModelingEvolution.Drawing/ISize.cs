namespace ModelingEvolution.Drawing;

/// <summary>
/// Represents an object that has a width and height.
/// </summary>
/// <typeparam name="T">The numeric type used for dimensions.</typeparam>
public interface ISize<T>
{
    /// <summary>
    /// Gets the width of the object.
    /// </summary>
    T Width { get; }
    
    /// <summary>
    /// Gets the height of the object.
    /// </summary>
    T Height { get; }
}