using System.Collections.Immutable;
using System.Numerics;

namespace ModelingEvolution.Drawing.Kinematics;

/// <summary>
/// A tree of links connected by revolute and prismatic joints, with named frames fixed to the links.
/// Units are metres and radians. Link 0 is the root (the chain base frame); every other link is moved by exactly
/// one joint. <see cref="Forward"/> and <see cref="Jacobian"/> take spans in and write spans out and do not allocate.
/// (epic-129 design § "Drawing: the kinematic chain (feature 001)".)
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class KinematicChain<T>
    where T : struct, INumber<T>, ITrigonometricFunctions<T>, IRootFunctions<T>, IFloatingPoint<T>, ISignedNumber<T>, IFloatingPointIeee754<T>, IMinMaxValue<T>
{
    private static readonly T UnitTolerance = T.CreateTruncating(1e-6);

    // _jointOf[link] = index of the joint whose child is that link; -1 for the root.
    private readonly int[] _jointOf;

    /// <summary>
    /// Creates a chain and validates it: links in topological order (link 0 is the only root, each parent index is
    /// lower than its child's), one joint per non-root link, unit axes, Min ≤ Max, frames on existing links,
    /// unique link and frame names.
    /// </summary>
    /// <exception cref="ArgumentException">A rule is broken; the message names the offending link, joint or frame.</exception>
    public KinematicChain(ImmutableArray<Link> links, ImmutableArray<Joint<T>> joints, ImmutableArray<ChainFrame<T>> frames)
    {
        if (links.IsDefaultOrEmpty)
            throw new ArgumentException("A chain needs at least the root link.", nameof(links));
        if (joints.IsDefault)
            throw new ArgumentException("Joints must not be a default array.", nameof(joints));
        if (frames.IsDefault)
            throw new ArgumentException("Frames must not be a default array.", nameof(frames));

        var linkNames = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (string.IsNullOrEmpty(link.Name))
                throw new ArgumentException($"Link {i} has no name.", nameof(links));
            if (!linkNames.Add(link.Name))
                throw new ArgumentException($"Link '{link.Name}' (index {i}) has a duplicate name.", nameof(links));
            if (i == 0 && link.Parent != -1)
                throw new ArgumentException($"Link '{link.Name}' (index 0) is the root and must have parent -1, not {link.Parent}.", nameof(links));
            if (i > 0 && (link.Parent < 0 || link.Parent >= i))
                throw new ArgumentException($"Link '{link.Name}' (index {i}) has parent {link.Parent}; a parent index must be in [0, {i}) (topological order).", nameof(links));
        }

        var jointOf = new int[links.Length];
        Array.Fill(jointOf, -1);
        for (int j = 0; j < joints.Length; j++)
        {
            var joint = joints[j];
            var name = string.IsNullOrEmpty(joint.Name) ? $"#{j}" : joint.Name;
            if (string.IsNullOrEmpty(joint.Name))
                throw new ArgumentException($"Joint {name} has no name.", nameof(joints));
            if (joint.Child <= 0 || joint.Child >= links.Length)
                throw new ArgumentException($"Joint '{name}' has child link {joint.Child}; it must be in [1, {links.Length}).", nameof(joints));
            if (jointOf[joint.Child] != -1)
                throw new ArgumentException($"Joint '{name}' moves link '{links[joint.Child].Name}', which joint '{joints[jointOf[joint.Child]].Name}' already moves.", nameof(joints));
            if (joint.Type != JointType.Revolute && joint.Type != JointType.Prismatic)
                throw new ArgumentException($"Joint '{name}' has unknown type {joint.Type}.", nameof(joints));
            var axisLength = joint.Axis.Length;
            if (!(T.Abs(axisLength - T.One) <= UnitTolerance))
                throw new ArgumentException($"Joint '{name}' axis {joint.Axis} is not a unit vector (length {axisLength}).", nameof(joints));
            if (!(joint.Min <= joint.Max))
                throw new ArgumentException($"Joint '{name}' has Min {joint.Min} > Max {joint.Max}.", nameof(joints));
            jointOf[joint.Child] = j;
        }
        for (int i = 1; i < links.Length; i++)
            if (jointOf[i] == -1)
                throw new ArgumentException($"Link '{links[i].Name}' (index {i}) is moved by no joint.", nameof(joints));

        var frameNames = new HashSet<string>(StringComparer.Ordinal);
        for (int f = 0; f < frames.Length; f++)
        {
            var frame = frames[f];
            if (string.IsNullOrEmpty(frame.Name))
                throw new ArgumentException($"Frame {f} has no name.", nameof(frames));
            if (!frameNames.Add(frame.Name))
                throw new ArgumentException($"Frame '{frame.Name}' (index {f}) has a duplicate name.", nameof(frames));
            if (frame.Link < 0 || frame.Link >= links.Length)
                throw new ArgumentException($"Frame '{frame.Name}' is on link {frame.Link}; it must be in [0, {links.Length}).", nameof(frames));
        }

        Links = links;
        Joints = joints;
        Frames = frames;
        _jointOf = jointOf;
    }

    /// <summary>The links, in topological order; link 0 is the root.</summary>
    public ImmutableArray<Link> Links { get; }

    /// <summary>The joints; joint j reads the joint value <c>q[j]</c>.</summary>
    public ImmutableArray<Joint<T>> Joints { get; }

    /// <summary>The named frames fixed to links.</summary>
    public ImmutableArray<ChainFrame<T>> Frames { get; }

    /// <summary>The number of joint values: <c>Joints.Length</c>.</summary>
    public int Dof => Joints.Length;

    /// <summary>The index of the frame named <paramref name="name"/>, or -1 when absent.</summary>
    public int FrameIndex(string name)
    {
        for (int i = 0; i < Frames.Length; i++)
            if (string.Equals(Frames[i].Name, name, StringComparison.Ordinal))
                return i;
        return -1;
    }

    /// <summary>
    /// Forward kinematics: writes the pose of every link in the chain base frame,
    /// <c>links[c] = links[parent] · Origin · J(q)</c>. Does not allocate.
    /// </summary>
    /// <param name="q">The joint values, <c>q.Length == Dof</c> (radians or metres).</param>
    /// <param name="links">The output, <c>links.Length == Links.Length</c>.</param>
    /// <exception cref="ArgumentException">A span has the wrong length.</exception>
    public void Forward(ReadOnlySpan<T> q, Span<Frame3<T>> links)
    {
        if (q.Length != Joints.Length)
            throw new ArgumentException($"q has {q.Length} values; the chain has {Joints.Length} joints.", nameof(q));
        if (links.Length != Links.Length)
            throw new ArgumentException($"links has {links.Length} elements; the chain has {Links.Length} links.", nameof(links));

        links[0] = Frame3<T>.Identity;
        for (int c = 1; c < links.Length; c++)
        {
            int j = _jointOf[c];
            ref readonly var joint = ref Joints.ItemRef(j);
            var motion = joint.Type == JointType.Revolute
                ? new Frame3<T>(Point3<T>.Zero, Quaternion<T>.FromAxisAngle(joint.Axis, q[j]))
                : new Frame3<T>((Point3<T>)(joint.Axis * q[j]), Quaternion<T>.Identity);
            links[c] = links[Links.ItemRef(c).Parent] * joint.Origin * motion;
        }
    }

    /// <summary>The pose of frame <paramref name="frame"/> in the chain base frame, from link poses written by <see cref="Forward"/>.</summary>
    /// <exception cref="ArgumentException">A span has the wrong length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The frame index is out of range.</exception>
    public Frame3<T> FramePose(ReadOnlySpan<Frame3<T>> links, int frame)
    {
        if (links.Length != Links.Length)
            throw new ArgumentException($"links has {links.Length} elements; the chain has {Links.Length} links.", nameof(links));
        if ((uint)frame >= (uint)Frames.Length)
            throw new ArgumentOutOfRangeException(nameof(frame), frame, $"The chain has {Frames.Length} frames.");
        ref readonly var f = ref Frames.ItemRef(frame);
        return links[f.Link] * f.InLink;
    }

    /// <summary>
    /// The geometric Jacobian of frame <paramref name="frame"/> in the chain base frame: 6 × Dof, column-major,
    /// rows (vx vy vz ωx ωy ωz). Revolute: v = a × (p_frame − p_joint), ω = a. Prismatic: v = a, ω = 0.
    /// Joints that do not move the frame give zero columns. Does not allocate.
    /// </summary>
    /// <param name="links">Link poses written by <see cref="Forward"/>.</param>
    /// <param name="frame">The frame index (see <see cref="FrameIndex"/>).</param>
    /// <param name="j">The output, <c>j.Length == 6 * Dof</c>.</param>
    /// <exception cref="ArgumentException">A span has the wrong length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The frame index is out of range.</exception>
    public void Jacobian(ReadOnlySpan<Frame3<T>> links, int frame, Span<T> j)
    {
        if (j.Length != 6 * Joints.Length)
            throw new ArgumentException($"j has {j.Length} elements; a 6 × {Joints.Length} Jacobian needs {6 * Joints.Length}.", nameof(j));
        var p = FramePose(links, frame).Position;

        j.Clear();
        for (int c = Frames.ItemRef(frame).Link; c > 0; c = Links.ItemRef(c).Parent)
        {
            int k = _jointOf[c];
            ref readonly var joint = ref Joints.ItemRef(k);
            var a = links[c].TransformVector(joint.Axis);
            var col = j.Slice(6 * k, 6);
            if (joint.Type == JointType.Revolute)
            {
                var v = Vector3<T>.Cross(a, p - links[c].Position);
                col[0] = v.X; col[1] = v.Y; col[2] = v.Z;
                col[3] = a.X; col[4] = a.Y; col[5] = a.Z;
            }
            else
            {
                col[0] = a.X; col[1] = a.Y; col[2] = a.Z;
            }
        }
    }
}
