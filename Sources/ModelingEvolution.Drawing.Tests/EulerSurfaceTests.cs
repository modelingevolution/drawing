using System.Reflection;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// Epic-129 Q7: <see cref="Rotation3{T}"/> and <see cref="Pose3{T}"/> are wire and edit formats. The vector, point and
/// matrix maths types neither take nor return them (<c>Pose3.From(p, r)</c> builds a pose); direction → rotation is <see cref="Quaternion{T}.FromTo"/>.
/// </summary>
public class EulerSurfaceTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static bool IsWireType(Type t)
    {
        if (t.IsByRef) t = t.GetElementType()!;
        return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Rotation3<>) || t.GetGenericTypeDefinition() == typeof(Pose3<>));
    }

    private static IEnumerable<string> MembersTouchingWireTypes(Type type)
    {
        foreach (var m in type.GetMethods(All))
            if (IsWireType(m.ReturnType) || m.GetParameters().Any(p => IsWireType(p.ParameterType)))
                yield return m.ToString()!;
        foreach (var c in type.GetConstructors(All))
            if (c.GetParameters().Any(p => IsWireType(p.ParameterType)))
                yield return c.ToString()!;
        foreach (var p in type.GetProperties(All))
            if (IsWireType(p.PropertyType))
                yield return p.ToString()!;
        foreach (var f in type.GetFields(All))
            if (IsWireType(f.FieldType))
                yield return f.ToString()!;
    }

    [Fact]
    public void Vector3_HasNoMemberTakingOrReturningRotation3OrPose3()
    {
        MembersTouchingWireTypes(typeof(Vector3<double>)).Should().BeEmpty();
    }

    [Fact]
    public void Point3_HasNoMemberTakingOrReturningRotation3OrPose3()
    {
        MembersTouchingWireTypes(typeof(Point3<double>)).Should().BeEmpty();
    }

    [Fact]
    public void Matrix3x3_HasNoMemberTakingOrReturningRotation3OrPose3()
    {
        MembersTouchingWireTypes(typeof(Matrix3x3<double>)).Should().BeEmpty();
    }
}
