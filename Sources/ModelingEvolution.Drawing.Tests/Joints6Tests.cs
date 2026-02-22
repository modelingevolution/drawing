using System.Text.Json;
using FluentAssertions;
using ProtoBuf;
using Xunit.Abstractions;

namespace ModelingEvolution.Drawing.Tests;

public class Joints6Tests
{
    private readonly ITestOutputHelper _output;

    public Joints6Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static Joints6<double> MakeJoints(double j1, double j2, double j3, double j4, double j5, double j6) =>
        new(Degree<double>.Create(j1), Degree<double>.Create(j2), Degree<double>.Create(j3),
            Degree<double>.Create(j4), Degree<double>.Create(j5), Degree<double>.Create(j6));

    #region Construction

    [Fact]
    public void Zero_AllJointsAreZero()
    {
        var j = Joints6<double>.Zero;

        j.J1.Should().Be(Degree<double>.Zero);
        j.J2.Should().Be(Degree<double>.Zero);
        j.J3.Should().Be(Degree<double>.Zero);
        j.J4.Should().Be(Degree<double>.Zero);
        j.J5.Should().Be(Degree<double>.Zero);
        j.J6.Should().Be(Degree<double>.Zero);
    }

    [Fact]
    public void Constructor_SetsAllJoints()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        ((double)(Degree<double>)j.J1).Should().Be(10);
        ((double)(Degree<double>)j.J2).Should().Be(20);
        ((double)(Degree<double>)j.J3).Should().Be(30);
        ((double)(Degree<double>)j.J4).Should().Be(40);
        ((double)(Degree<double>)j.J5).Should().Be(50);
        ((double)(Degree<double>)j.J6).Should().Be(60);
    }

    #endregion

    #region Indexer

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 20)]
    [InlineData(2, 30)]
    [InlineData(3, 40)]
    [InlineData(4, 50)]
    [InlineData(5, 60)]
    public void Indexer_ReturnsCorrectJoint(int index, double expected)
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        ((double)(Degree<double>)j[index]).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Indexer_OutOfRange_Throws(int index)
    {
        var j = Joints6<double>.Zero;

        var act = () => j[index];

        act.Should().Throw<IndexOutOfRangeException>();
    }

    #endregion

    #region Operators

    [Fact]
    public void Addition_AddsElementWise()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(1, 2, 3, 4, 5, 6);

        var result = a + b;

        result.Should().Be(MakeJoints(11, 22, 33, 44, 55, 66));
    }

    [Fact]
    public void Subtraction_SubtractsElementWise()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(1, 2, 3, 4, 5, 6);

        var result = a - b;

        result.Should().Be(MakeJoints(9, 18, 27, 36, 45, 54));
    }

    [Fact]
    public void UnaryNegation_NegatesAll()
    {
        var j = MakeJoints(10, -20, 30, -40, 50, -60);

        var result = -j;

        result.Should().Be(MakeJoints(-10, 20, -30, 40, -50, 60));
    }

    [Fact]
    public void MultiplyByScalar_ScalesAll()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = j * 2.0;

        result.Should().Be(MakeJoints(20, 40, 60, 80, 100, 120));
    }

    [Fact]
    public void ScalarMultiply_Commutative()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        (j * 3.0).Should().Be(3.0 * j);
    }

    [Fact]
    public void DivideByScalar_DividesAll()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = j / 2.0;

        result.Should().Be(MakeJoints(5, 10, 15, 20, 25, 30));
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 60);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 61);

        (a != b).Should().BeTrue();
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 60);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region Lerp

    [Fact]
    public void Lerp_AtZero_ReturnsFirst()
    {
        var a = MakeJoints(0, 0, 0, 0, 0, 0);
        var b = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = Joints6<double>.Lerp(a, b, 0.0);

        result.Should().Be(a);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsSecond()
    {
        var a = MakeJoints(0, 0, 0, 0, 0, 0);
        var b = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = Joints6<double>.Lerp(a, b, 1.0);

        result.Should().Be(b);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        var a = MakeJoints(0, 0, 0, 0, 0, 0);
        var b = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = Joints6<double>.Lerp(a, b, 0.5);

        result.Should().Be(MakeJoints(5, 10, 15, 20, 25, 30));
    }

    #endregion

    #region MaxAbsDelta / IsWithin

    [Fact]
    public void MaxAbsDelta_ReturnsLargestDifference()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 70);

        var delta = a.MaxAbsDelta(b);

        ((double)(Degree<double>)delta).Should().Be(10);
    }

    [Fact]
    public void MaxAbsDelta_WithNegatives_UsesAbsolute()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 40);

        var delta = a.MaxAbsDelta(b);

        ((double)(Degree<double>)delta).Should().Be(20);
    }

    [Fact]
    public void IsWithin_WithinTolerance_ReturnsTrue()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10.5, 20.5, 30.5, 40.5, 50.5, 60.5);

        a.IsWithin(b, Degree<double>.Create(1.0)).Should().BeTrue();
    }

    [Fact]
    public void IsWithin_OutsideTolerance_ReturnsFalse()
    {
        var a = MakeJoints(10, 20, 30, 40, 50, 60);
        var b = MakeJoints(10, 20, 30, 40, 50, 62);

        a.IsWithin(b, Degree<double>.Create(1.0)).Should().BeFalse();
    }

    #endregion

    #region ToArray / FromArray / CopyTo / FromSpan

    [Fact]
    public void ToArray_Returns6Elements()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        var arr = j.ToArray();

        arr.Should().HaveCount(6);
        ((double)(Degree<double>)arr[0]).Should().Be(10);
        ((double)(Degree<double>)arr[5]).Should().Be(60);
    }

    [Fact]
    public void CopyTo_FillsSpan()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);
        Span<Degree<double>> span = stackalloc Degree<double>[6];

        j.CopyTo(span);

        ((double)(Degree<double>)span[0]).Should().Be(10);
        ((double)(Degree<double>)span[5]).Should().Be(60);
    }

    [Fact]
    public void CopyTo_SpanTooSmall_Throws()
    {
        var j = Joints6<double>.Zero;
        var arr = new Degree<double>[3];

        var act = () => j.CopyTo(arr);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromSpan_RoundTrips()
    {
        var original = MakeJoints(10, 20, 30, 40, 50, 60);
        var arr = new Degree<double>[6];
        original.CopyTo(arr);

        var result = Joints6<double>.FromSpan(arr);

        result.Should().Be(original);
    }

    [Fact]
    public void FromSpan_WrongLength_Throws()
    {
        var arr = new Degree<double>[3];

        var act = () => Joints6<double>.FromSpan(arr);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromArray_RoundTrips()
    {
        var original = MakeJoints(10, 20, 30, 40, 50, 60);

        var result = Joints6<double>.FromArray(original.ToArray());

        result.Should().Be(original);
    }

    [Fact]
    public void FromArray_WrongLength_Throws()
    {
        var act = () => Joints6<double>.FromArray(new Degree<double>[5]);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Deconstruct

    [Fact]
    public void Deconstruct_ExtractsAllJoints()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        var (j1, j2, j3, j4, j5, j6) = j;

        j1.Should().Be(j.J1);
        j2.Should().Be(j.J2);
        j3.Should().Be(j.J3);
        j4.Should().Be(j.J4);
        j5.Should().Be(j.J5);
        j6.Should().Be(j.J6);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void JsonSerialization_Double_RoundTrips()
    {
        var original = MakeJoints(10.5, 20.3, 15.7, 5.2, 30.1, 45.0);
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[10.5,20.3,15.7,5.2,30.1,45]");

        var deserialized = JsonSerializer.Deserialize<Joints6<double>>(json);
        deserialized.Should().Be(original);
    }

    [Fact]
    public void JsonSerialization_Float_RoundTrips()
    {
        var original = new Joints6<float>(
            Degree<float>.Create(10f), Degree<float>.Create(20f), Degree<float>.Create(30f),
            Degree<float>.Create(40f), Degree<float>.Create(50f), Degree<float>.Create(60f));
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        json.Should().Be("[10,20,30,40,50,60]");

        var deserialized = JsonSerializer.Deserialize<Joints6<float>>(json);
        deserialized.Should().Be(original);
    }

    record FooJoints6(Joints6<double> Joints);

    [Fact]
    public void JsonSerialization_InRecord_RoundTrips()
    {
        var original = new FooJoints6(MakeJoints(1, 2, 3, 4, 5, 6));
        var json = JsonSerializer.Serialize(original);
        _output.WriteLine($"JSON: {json}");

        var deserialized = JsonSerializer.Deserialize<FooJoints6>(json);
        deserialized.Should().Be(original);
    }

    #endregion

    #region Protobuf Serialization

    [Fact]
    public void ProtobufSerialization_RoundTrips()
    {
        var original = MakeJoints(10.5, 20.3, 15.7, 5.2, 30.1, 45.0);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<Joints6<double>>(ms);

        deserialized.Should().Be(original);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Parse_CommaDelimited_Works()
    {
        var result = Joints6<double>.Parse("10.5, 20.3, 15.7, 5.2, 30.1, 45.0");

        result.Should().Be(MakeJoints(10.5, 20.3, 15.7, 5.2, 30.1, 45.0));
    }

    [Fact]
    public void Parse_SpaceDelimited_Works()
    {
        var result = Joints6<double>.Parse("10 20 30 40 50 60");

        result.Should().Be(MakeJoints(10, 20, 30, 40, 50, 60));
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrue()
    {
        var success = Joints6<double>.TryParse("1, 2, 3, 4, 5, 6", null, out var result);

        success.Should().BeTrue();
        result.Should().Be(MakeJoints(1, 2, 3, 4, 5, 6));
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        var success = Joints6<double>.TryParse("invalid", null, out var result);

        success.Should().BeFalse();
        result.Should().Be(Joints6<double>.Zero);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var success = Joints6<double>.TryParse(null, null, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_TooFewValues_ReturnsFalse()
    {
        var success = Joints6<double>.TryParse("1, 2, 3", null, out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_TooManyValues_ReturnsFalse()
    {
        var success = Joints6<double>.TryParse("1, 2, 3, 4, 5, 6, 7", null, out _);

        success.Should().BeFalse();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_FormatsWithDegreeSymbol()
    {
        var j = MakeJoints(10, 20, 30, 40, 50, 60);

        var s = j.ToString();

        s.Should().Contain("10");
        s.Should().Contain("60");
        s.Should().Contain("\u00b0");
    }

    #endregion
}
