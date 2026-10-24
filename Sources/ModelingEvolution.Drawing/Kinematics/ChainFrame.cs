using System.Numerics;

namespace ModelingEvolution.Drawing.Kinematics;

/// <summary>A named frame fixed to a link of a <see cref="KinematicChain{T}"/> (for example <c>base</c> or <c>flange</c>).</summary>
/// <param name="Name">The frame name, unique in the chain.</param>
/// <param name="Link">The index of the link the frame is fixed to.</param>
/// <param name="InLink">The frame in the link frame (metres).</param>
/// <typeparam name="T">The numeric type.</typeparam>
public readonly record struct ChainFrame<T>(string Name, int Link, Frame3<T> InLink)
    where T : INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>;
