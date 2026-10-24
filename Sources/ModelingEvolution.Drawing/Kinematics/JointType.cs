namespace ModelingEvolution.Drawing.Kinematics;

/// <summary>The motion a <see cref="Joint{T}"/> adds between its parent and child link.</summary>
public enum JointType : byte
{
    /// <summary>A rotation about the joint axis; the joint value is in radians.</summary>
    Revolute,

    /// <summary>A translation along the joint axis; the joint value is in metres.</summary>
    Prismatic
}
