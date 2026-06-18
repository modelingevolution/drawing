using FluentAssertions;
using ModelingEvolution.Drawing.Units;
using Xunit;

namespace ModelingEvolution.Drawing.Tests;

public class UnitKinematicsTests
{
    private const double Tol = 1e-6;

    // --- Length basics (mirrors Speed) ---

    [Fact]
    public void Millimetre_ToSi_isMetres()
        => new Length<double, Millimetre<double>>(1000).ToSi().Should().BeApproximately(1.0, 1e-12);

    [Fact]
    public void Length_To_convertsUnits()
        => new Length<double, Metre<double>>(2).To<Millimetre<double>>().Value.Should().BeApproximately(2000, 1e-9);

    [Fact]
    public void Length_operators()
    {
        var a = new Length<double, Millimetre<double>>(100);
        var b = new Length<double, Millimetre<double>>(40);
        (a + b).Value.Should().Be(140);
        (a / b).Should().BeApproximately(2.5, 1e-12);
        (a * new Percentage(50f)).Value.Should().BeApproximately(50, 1e-4);
    }

    // --- Kinematics:  s = v·t,  v = s/t,  Δv = a·t,  a = Δv/t  ---

    [Fact]
    public void Speed_times_time_isDistance()   // s = v · t
    {
        var v = new Speed<double, MillimetrePerSecond<double>>(80);    // 80 mm/s
        Length<double, Metre<double>> s = v * TimeSpan.FromSeconds(2); // 160 mm = 0.16 m
        s.To<Millimetre<double>>().Value.Should().BeApproximately(160, Tol);
        (TimeSpan.FromSeconds(2) * v).To<Millimetre<double>>().Value.Should().BeApproximately(160, Tol);
    }

    [Fact]
    public void Distance_over_time_isSpeed()     // v = s / t
    {
        var s = new Length<double, Millimetre<double>>(160);
        Speed<double, MetrePerSecond<double>> v = s / TimeSpan.FromSeconds(2);  // 80 mm/s
        v.To<MillimetrePerSecond<double>>().Value.Should().BeApproximately(80, Tol);
    }

    [Fact]
    public void Acceleration_times_time_isSpeedChange()   // Δv = a · t
    {
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(100);  // 100 mm/s²
        Speed<double, MetrePerSecond<double>> dv = a * TimeSpan.FromSeconds(0.8);   // 80 mm/s
        dv.To<MillimetrePerSecond<double>>().Value.Should().BeApproximately(80, Tol);
        (TimeSpan.FromSeconds(0.8) * a).To<MillimetrePerSecond<double>>().Value.Should().BeApproximately(80, Tol);
    }

    [Fact]
    public void Speed_over_time_isAcceleration()  // a = Δv / t
    {
        var v = new Speed<double, MillimetrePerSecond<double>>(80);
        Acceleration<double, MetrePerSecondSquared<double>> a = v / TimeSpan.FromSeconds(0.8);  // 100 mm/s²
        a.To<MillimetrePerSecondSquared<double>>().Value.Should().BeApproximately(100, Tol);
    }

    [Fact]
    public void TimeToTravelAt_isDistanceOverSpeed()   // t = s / v
    {
        var s = new Length<double, Millimetre<double>>(160);
        var v = new Speed<double, MillimetrePerSecond<double>>(80);
        s.TimeToTravelAt(v).TotalSeconds.Should().BeApproximately(2.0, Tol);   // 160 mm / 80 mm/s
    }

    [Fact]
    public void TimeToReach_isSpeedOverAcceleration()   // t = Δv / a
    {
        var v = new Speed<double, MillimetrePerSecond<double>>(80);
        var a = new Acceleration<double, MillimetrePerSecondSquared<double>>(100);
        v.TimeToReach(a).TotalSeconds.Should().BeApproximately(0.8, Tol);   // 80 / 100
    }

    [Fact]
    public void RoundTrip_distance_back_to_speed()
    {
        var v = new Speed<double, MillimetrePerSecond<double>>(123);
        var dt = TimeSpan.FromSeconds(1.5);
        ((v * dt) / dt).To<MillimetrePerSecond<double>>().Value.Should().BeApproximately(123, Tol);
    }
}
