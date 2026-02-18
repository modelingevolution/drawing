using FluentAssertions;
using ModelingEvolution.Drawing;

namespace ModelingEvolution.Drawing.Tests;

public class PosePath3Tests
{
    private const float Tol = 1e-3f;

    private static Pose3<float> P(float x, float y, float z) => new(x, y, z, 0, 0, 0);

    // ─────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultCtor_Empty()
    {
        var path = new PosePath3<float>();
        path.Count.Should().Be(0);
    }

    [Fact]
    public void ParamsCtor_StoresPoses()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(10, 0, 0));
        path.Count.Should().Be(2);
        path[0].Should().Be(P(0, 0, 0));
        path[1].Should().Be(P(10, 0, 0));
    }

    [Fact]
    public void Start_End()
    {
        var path = new PosePath3<float>(P(1, 2, 3), P(4, 5, 6));
        path.Start.Should().Be(P(1, 2, 3));
        path.End.Should().Be(P(4, 5, 6));
    }

    // ─────────────────────────────────────────────
    // Length
    // ─────────────────────────────────────────────

    [Fact]
    public void Length_StraightLine()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(10, 0, 0));
        path.Length().Should().BeApproximately(10f, Tol);
    }

    [Fact]
    public void Length_MultiSegment()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(10, 0, 0), P(10, 5, 0));
        path.Length().Should().BeApproximately(15f, Tol);
    }

    // ─────────────────────────────────────────────
    // Reverse
    // ─────────────────────────────────────────────

    [Fact]
    public void Reverse_FlipsOrder()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(10, 0, 0), P(20, 0, 0));
        var rev = path.Reverse();
        rev[0].Should().Be(P(20, 0, 0));
        rev[1].Should().Be(P(10, 0, 0));
        rev[2].Should().Be(P(0, 0, 0));
    }

    // ─────────────────────────────────────────────
    // ToPolyline3
    // ─────────────────────────────────────────────

    [Fact]
    public void ToPolyline3_ExtractsPositions()
    {
        var path = new PosePath3<float>(P(1, 2, 3), P(4, 5, 6));
        var polyline = path.ToPolyline3();
        polyline.Count.Should().Be(2);
        polyline[0].Should().Be(new Point3<float>(1, 2, 3));
        polyline[1].Should().Be(new Point3<float>(4, 5, 6));
    }

    // ─────────────────────────────────────────────
    // ToTrajectory
    // ─────────────────────────────────────────────

    [Fact]
    public void ToTrajectory_AssignsUniformTimestamps()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(5, 0, 0), P(10, 0, 0));
        var traj = path.ToTrajectory(4f);
        traj.Count.Should().Be(3);
        traj[0].Time.Should().BeApproximately(0f, Tol);
        traj[1].Time.Should().BeApproximately(2f, Tol);
        traj[2].Time.Should().BeApproximately(4f, Tol);
    }

    // ─────────────────────────────────────────────
    // Operators
    // ─────────────────────────────────────────────

    [Fact]
    public void PlusVector_TranslatesPositions()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(10, 0, 0));
        var moved = path + new Vector3<float>(1, 2, 3);
        moved[0].X.Should().BeApproximately(1f, Tol);
        moved[0].Y.Should().BeApproximately(2f, Tol);
        moved[0].Z.Should().BeApproximately(3f, Tol);
    }

    // ─────────────────────────────────────────────
    // Equality
    // ─────────────────────────────────────────────

    [Fact]
    public void Equality_SamePoses_AreEqual()
    {
        var a = new PosePath3<float>(P(1, 2, 3), P(4, 5, 6));
        var b = new PosePath3<float>(P(1, 2, 3), P(4, 5, 6));
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ─────────────────────────────────────────────
    // JSON round-trip
    // ─────────────────────────────────────────────

    [Fact]
    public void Json_RoundTrip()
    {
        var path = new PosePath3<float>(P(1, 2, 3), P(4, 5, 6));
        var json = System.Text.Json.JsonSerializer.Serialize(path);
        json.Should().Be("[1,2,3,0,0,0,4,5,6,0,0,0]");
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PosePath3<float>>(json);
        deserialized.Should().Be(path);
    }

    // ─────────────────────────────────────────────
    // ToString
    // ─────────────────────────────────────────────

    [Fact]
    public void ToString_ShowsCount()
    {
        var path = new PosePath3<float>(P(0, 0, 0), P(1, 1, 1));
        path.ToString().Should().Be("PosePath3(2 poses)");
    }
}
