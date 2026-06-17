using System.Numerics;

namespace ModelingEvolution.Drawing.Units;

/// <summary>
/// A unit of measure for a quantity of dimension represented in numeric type <typeparamref name="T"/>.
/// Conversion to/from the SI base unit is expressed as a pair of (ideally inverse) functions,
/// which lets the same interface model both linear units (scale) and affine units (scale + offset).
/// </summary>
/// <typeparam name="T">The backing numeric type (e.g. <see cref="double"/>).</typeparam>
public interface IUnit<T> where T : INumber<T>
{
    /// <summary>Convert a value expressed in this unit to the SI base unit.</summary>
    static abstract T ToSi(T value);

    /// <summary>Convert a value expressed in the SI base unit to this unit.</summary>
    static abstract T FromSi(T si);

    /// <summary>The unit symbol used for formatting/parsing (e.g. "mm/s").</summary>
    static abstract string Symbol { get; }
}
