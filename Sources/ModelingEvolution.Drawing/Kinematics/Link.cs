namespace ModelingEvolution.Drawing.Kinematics;

/// <summary>A rigid link of a <see cref="KinematicChain{T}"/>.</summary>
/// <param name="Name">The link name, unique in the chain.</param>
/// <param name="Parent">The index of the parent link; -1 for the root link (the chain base), otherwise lower than this link's index.</param>
public readonly record struct Link(string Name, int Parent);
