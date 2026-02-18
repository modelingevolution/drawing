using System.Numerics;

namespace ModelingEvolution.Drawing;

/// <summary>
/// Describes the corner behavior at a trajectory waypoint.
/// <c>Sharp</c> means no blending (the path passes through the exact waypoint).
/// <c>Round(radius)</c> creates a smooth arc that starts <paramref name="radius"/> units
/// before the waypoint and ends <paramref name="radius"/> units after it, so the path
/// never passes through the exact waypoint position.
/// </summary>
/// <typeparam name="T">The numeric type used for the blend radius.</typeparam>
public readonly record struct Corner<T> where T : INumber<T>
{
    /// <summary>Gets the blend radius. Zero means sharp (no blending).</summary>
    public T Radius { get; }

    /// <summary>Gets whether this corner is sharp (no blending).</summary>
    public bool IsSharp => Radius == T.Zero;

    private Corner(T radius) => Radius = radius;

    /// <summary>A sharp corner — the trajectory passes through the exact waypoint.</summary>
    public static Corner<T> Sharp { get; } = new(T.Zero);

    /// <summary>
    /// Creates a rounded corner with the given blend radius.
    /// The trajectory will start blending <paramref name="radius"/> units before
    /// the waypoint and finish <paramref name="radius"/> units after it.
    /// </summary>
    public static Corner<T> Round(T radius)
    {
        if (radius < T.Zero)
            throw new ArgumentOutOfRangeException(nameof(radius), "Blend radius must be non-negative.");
        return new(radius);
    }

    public override string ToString() => IsSharp ? "Sharp" : $"Round({Radius})";
}
