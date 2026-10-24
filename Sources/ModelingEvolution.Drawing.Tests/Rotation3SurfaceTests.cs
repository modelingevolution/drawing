using System.Reflection;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// Epic-129 Q7: <see cref="Rotation3{T}"/> is a wire and edit format only, like <see cref="Pose3{T}"/>. Its rotation
/// algebra is deleted; rotation maths uses <see cref="Quaternion{T}"/> or <see cref="Frame3{T}"/>. A new member
/// fails this test until the kept list is changed on purpose.
/// </summary>
public class Rotation3SurfaceTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Theory]
    [InlineData("Combine")]
    [InlineData("op_Addition")]
    [InlineData("Inverse")]
    [InlineData("op_UnaryNegation")]
    [InlineData("Slerp")]
    [InlineData("Rotate")]
    [InlineData("ToMatrix3x3")]
    [InlineData("op_Explicit")]
    [InlineData("Negate")]
    public void DeletedMathsMember_DoesNotExist(string name)
    {
        typeof(Rotation3<double>).GetMembers(All).Where(m => m.Name == name).Should().BeEmpty();
    }

    [Fact]
    public void PublicMembers_AreExactlyTheWireAndEditSet()
    {
        var actual = typeof(Rotation3<double>)
            .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(Describe)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        string[] kept =
        [
            // constructors and named constructors
            ".ctor(Degree`1, Degree`1, Degree`1)",
            "FromDegrees(Double, Double, Double)",
            "FromRadians(Radian`1, Radian`1, Radian`1)",
            // identity
            "Identity",
            "IsIdentity", "get_IsIdentity()",
            // angles (init-only for `with`)
            "Rx", "get_Rx()", "set_Rx(Degree`1)",
            "Ry", "get_Ry()", "set_Ry(Degree`1)",
            "Rz", "get_Rz()", "set_Rz(Degree`1)",
            "ToRadians()",
            // collection-expression literals ([rx, ry, rz])
            "Enumerator", "GetEnumerator()",
            // the conversions to and from the maths types (one Euler formula each way)
            "ToQuaternion()",
            "FromQuaternion(Quaternion`1)",
            "FromMatrix3x3(Matrix3x3`1)",
            // parsing and formatting
            "Parse(String, IFormatProvider)",
            "TryParse(String, IFormatProvider, Rotation3`1&)",
            "ToString()",
            // tuple conversions
            "op_Implicit(ValueTuple`3)",
            "op_Implicit(Rotation3`1)",
            // equality
            "Equals(Object)", "Equals(Rotation3`1)", "GetHashCode()", "op_Equality(Rotation3`1, Rotation3`1)", "op_Inequality(Rotation3`1, Rotation3`1)",
            // numeric type conversion
            "Truncating()",
        ];

        actual.Should().Equal(kept.OrderBy(s => s, StringComparer.Ordinal));
    }

    private static string Describe(MemberInfo m) => m switch
    {
        MethodBase mb => $"{mb.Name}({string.Join(", ", mb.GetParameters().Select(p => p.ParameterType.Name))})",
        _ => m.Name,
    };
}
