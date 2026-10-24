namespace ModelingEvolution.Drawing.Tests;

/// <summary>Random frames and comparisons shared by the Frame3 scenarios (seed 129, epic-129 test rules).</summary>
internal static class Frame3TestData
{
    /// <summary>A uniformly random unit quaternion (Shoemake).</summary>
    public static Quaternion<double> RandomRotation(Random rng)
    {
        double u1 = rng.NextDouble(), u2 = rng.NextDouble() * 2 * Math.PI, u3 = rng.NextDouble() * 2 * Math.PI;
        double a = Math.Sqrt(1 - u1), b = Math.Sqrt(u1);
        return new Quaternion<double>(b * Math.Cos(u3), a * Math.Sin(u2), a * Math.Cos(u2), b * Math.Sin(u3));
    }

    public static double Uniform(Random rng, double half) => (rng.NextDouble() * 2 - 1) * half;

    /// <summary>Position in ±<paramref name="half"/> per axis, random unit quaternion.</summary>
    public static Frame3<double> RandomFrame(Random rng, double half = 2.0) =>
        new(new Point3<double>(Uniform(rng, half), Uniform(rng, half), Uniform(rng, half)), RandomRotation(rng));

    /// <summary>The largest component difference of two quaternions, taking q and −q as the same rotation.</summary>
    public static double QuaternionError(Quaternion<double> a, Quaternion<double> b)
    {
        double plus = Math.Max(Math.Max(Math.Abs(a.W - b.W), Math.Abs(a.X - b.X)), Math.Max(Math.Abs(a.Y - b.Y), Math.Abs(a.Z - b.Z)));
        double minus = Math.Max(Math.Max(Math.Abs(a.W + b.W), Math.Abs(a.X + b.X)), Math.Max(Math.Abs(a.Y + b.Y), Math.Abs(a.Z + b.Z)));
        return Math.Min(plus, minus);
    }

    /// <summary>Rotation matrix (row-major) of a unit quaternion.</summary>
    public static double[,] ToMatrix(Quaternion<double> q)
    {
        double w = q.W, x = q.X, y = q.Y, z = q.Z;
        return new[,]
        {
            { 1 - 2 * (y * y + z * z), 2 * (x * y - w * z), 2 * (x * z + w * y) },
            { 2 * (x * y + w * z), 1 - 2 * (x * x + z * z), 2 * (y * z - w * x) },
            { 2 * (x * z - w * y), 2 * (y * z + w * x), 1 - 2 * (x * x + y * y) },
        };
    }
}
