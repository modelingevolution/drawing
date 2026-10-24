using System.Reflection;
using FluentAssertions;

namespace ModelingEvolution.Drawing.Tests;

/// <summary>
/// TS-129-55: <see cref="Pose3{T}"/> is a wire and edit format only (epic-129 design C1, ADR-11). Its maths lives in
/// <see cref="Frame3{T}"/>; a new member fails this test until the kept list is changed on purpose.
/// </summary>
public class Pose3SurfaceTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Theory]
    [InlineData("TransformPoint")]
    [InlineData("TransformVector")]
    [InlineData("Inverse")]
    [InlineData("Multiply")]
    [InlineData("op_Multiply")]
    [InlineData("op_Addition")]
    [InlineData("op_Subtraction")]
    [InlineData("Lerp")]
    [InlineData("Distance")]
    [InlineData("FromSurface")]
    public void DeletedMathsMember_DoesNotExist(string name)
    {
        typeof(Pose3<double>).GetMembers(All).Where(m => m.Name == name).Should().BeEmpty();
    }

    [Fact]
    public void PublicMembers_AreExactlyTheWireAndEditSet()
    {
        var actual = typeof(Pose3<double>)
            .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(Describe)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        string[] kept =
        [
            // constructors
            ".ctor(Point3`1, Rotation3`1)",
            ".ctor(Double, Double, Double, Degree`1, Degree`1, Degree`1)",
            ".ctor(Double, Double, Double, Double, Double, Double)",
            // identity
            "Identity",
            // factories (named constructors)
            "From(Point3`1, Rotation3`1)",
            "From(Vector3`1, Rotation3`1)",
            // properties and their accessors (Position/Rotation are init-only for `with`)
            "IsIdentity", "get_IsIdentity()",
            "Position", "get_Position()", "set_Position(Point3`1)",
            "Rotation", "get_Rotation()", "set_Rotation(Rotation3`1)",
            "Rx", "get_Rx()", "Ry", "get_Ry()", "Rz", "get_Rz()",
            "X", "get_X()", "Y", "get_Y()", "Z", "get_Z()",
            // collection-expression literals ([x, y, z, rx, ry, rz])
            "Enumerator", "GetEnumerator()",
            // parsing and formatting
            "Parse(String, IFormatProvider)",
            "TryParse(String, IFormatProvider, Pose3`1&)",
            "ToString()",
            // tuple conversions and deconstruction
            "op_Implicit(Pose3`1)",
            "op_Implicit(ValueTuple`6)",
            "Deconstruct(Double&, Double&, Double&, Double&, Double&, Double&)",
            "Deconstruct(Point3`1&, Rotation3`1&)",
            // equality
            "Equals(Object)", "Equals(Pose3`1)", "GetHashCode()", "op_Equality(Pose3`1, Pose3`1)", "op_Inequality(Pose3`1, Pose3`1)",
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
