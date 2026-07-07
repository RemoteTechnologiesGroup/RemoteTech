using RemoteTech.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace RemoteTech.Network;

internal static class NetworkUpdateMath
{
    public const double MIN_HEIGHT = 5.0;

    /// <summary>
    /// Do the satellites at <paramref name="posA"/> and <paramref name="posB"/>
    /// have a line of sight between each other? This checks against the bodies
    /// specified in <paramref name="range"/>.
    /// </summary>
    public static bool HasLineOfSight(
        double3 posA,
        double3 posB,
        NativeArray<JobBody> bodies,
        IntRange range)
    {
        double3 bFromA = posB - posA;
        double bFromALenSq = math.lengthsq(bFromA);
        if (bFromALenSq <= 0.0)
            return true;

        double bFromALen = math.sqrt(bFromALenSq);
        double3 bFromANorm = bFromA / bFromALen;

        foreach (int i in range)
        {
            JobBody body = bodies[i];
            double3 bodyFromA = body.position - posA;

            // Is body at least roughly between A and B?
            double dot = math.dot(bodyFromA, bFromA);
            if (dot <= 0.0)
                continue;

            double projLen = math.dot(bodyFromA, bFromANorm);
            if (projLen >= bFromALen)
                continue;

            double3 lateral = bodyFromA - projLen * bFromANorm;
            double threshold = body.radius - MIN_HEIGHT;
            if (math.lengthsq(lateral) < threshold * threshold)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Reproduces <c>AbstractRangeModel.CheckRange</c>: the smallest of
    /// (range-model joint range, r1 boosted by clamp1, r2 boosted by clamp2).
    /// </summary>
    public static double CheckRange<TRange>(
        in TRange model,
        double r1,
        double clamp1,
        double r2,
        double clamp2)
        where TRange : struct, IRangeModel
    {
        return math.min(math.min(model.MaxDistance(r1, r2), r1 * clamp1), r2 * clamp2);
    }

    /// <summary>
    /// Multiple-antenna omni bonus: when more than one omni is mounted, the
    /// non-best ones each contribute <c>multiplier</c> times their own omni.
    /// Mirrors <c>AbstractRangeModel.GetMultipleAntennaBonus</c>.
    /// </summary>
    public static double GetMultipleAntennaBonus(
        NativeArray<JobAntenna> antennas,
        IntRange range,
        double maxOmni,
        double multiplier)
    {
        if (multiplier <= 0.0)
            return 0.0;
        double total = 0.0;
        foreach (int i in range)
            total += antennas[i].omni;
        return (total - maxOmni) * multiplier;
    }

    /// <summary>
    /// Cheap upper-bound reachability test. If the sum of each vessel's
    /// effective range cap is less than the distance between them, no link
    /// is possible under either range model. Lets BuildEdgesJob skip the
    /// LOS sweep for the long tail of far-apart pairs.
    /// </summary>
    public static bool CouldPossiblyLink(double maxRangeA, double maxRangeB, double distance)
    {
        return maxRangeA + maxRangeB >= distance;
    }

    /// <summary>
    /// Decodes a flat triangular-pair index into <c>(i, j)</c> with
    /// <c>0 &lt;= i &lt; j</c>. Pair index k corresponds to <c>(i, j)</c> via
    /// <c>k = j*(j-1)/2 + i</c>.
    /// </summary>
    public static void DecodePairIndex(int pairIdx, out int i, out int j)
    {
        // Closed-form starting point; finish with a tiny correction loop to
        // guard against floating-point drift on edge values.
        j = (int)((1.0 + math.sqrt(1.0 + 8.0 * pairIdx)) * 0.5);
        while (j * (j - 1) / 2 > pairIdx) j--;
        while ((j + 1) * j / 2 <= pairIdx) j++;
        i = pairIdx - j * (j - 1) / 2;
    }

    /// <summary>
    /// Inverse of <see cref="DecodePairIndex"/>: the flat pair index for the
    /// unordered pair <c>(a, b)</c>.
    /// </summary>
    public static int EncodePairIndex(int a, int b)
    {
        int lo = math.min(a, b);
        int hi = math.max(a, b);
        return hi * (hi - 1) / 2 + lo;
    }

    public static int PairCount(int nodeCount) => nodeCount * (nodeCount - 1) / 2;
}
