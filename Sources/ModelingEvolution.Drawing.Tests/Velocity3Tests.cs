using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class Velocity3Tests
{
    private const float Tol = 1e-4f;

    [Fact]
    public void Ctor_Components()
    {
        var v = new Velocity3<float>(1, 2, 3);
        v.X.Should().BeApproximately(1f, Tol);
        v.Y.Should().BeApproximately(2f, Tol);
        v.Z.Should().BeApproximately(3f, Tol);
    }

    [Fact]
    public void Speed_IsMagnitude()
    {
        var v = new Velocity3<float>(3, 4, 0);
        v.Speed.Value.Should().BeApproximately(5f, Tol);
    }

    [Fact]
    public void Direction_IsNormalized()
    {
        var v = new Velocity3<float>(10, 0, 0);
        var dir = v.Direction;
        dir.X.Should().BeApproximately(1f, Tol);
        dir.Y.Should().BeApproximately(0f, Tol);
        dir.Z.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void From_DirectionAndSpeed()
    {
        var v = Velocity3<float>.From(new Vector3<float>(0, 0, 1), Speed<float>.From(10f));
        v.Z.Should().BeApproximately(10f, Tol);
        v.X.Should().BeApproximately(0f, Tol);
    }

    [Fact]
    public void Addition()
    {
        var a = new Velocity3<float>(1, 0, 0);
        var b = new Velocity3<float>(0, 2, 0);
        var sum = a + b;
        sum.X.Should().BeApproximately(1f, Tol);
        sum.Y.Should().BeApproximately(2f, Tol);
    }

    [Fact]
    public void ScalarMultiply()
    {
        var v = new Velocity3<float>(1, 2, 3);
        var scaled = v * 2f;
        scaled.X.Should().BeApproximately(2f, Tol);
        scaled.Y.Should().BeApproximately(4f, Tol);
        scaled.Z.Should().BeApproximately(6f, Tol);
    }

    [Fact]
    public void DisplacementIn_ReturnsVector()
    {
        var v = new Velocity3<float>(10, 0, 0);
        var disp = v.DisplacementIn(5f);
        disp.X.Should().BeApproximately(50f, Tol);
    }

    [Fact]
    public void ImplicitConversion_FromVector3()
    {
        Velocity3<float> v = new Vector3<float>(1, 2, 3);
        v.X.Should().BeApproximately(1f, Tol);
    }

    [Fact]
    public void Zero_IsZero()
    {
        var v = Velocity3<float>.Zero;
        v.Speed.Value.Should().BeApproximately(0f, Tol);
    }
}
