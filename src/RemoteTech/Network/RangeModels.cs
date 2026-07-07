using Unity.Mathematics;

namespace RemoteTech.Network;

/// <summary>
/// Interface used for range models in jobs.
/// </summary>
internal interface IRangeModel
{
    double MaxDistance(double r1, double r2);
}

internal readonly struct StandardRangeModel : IRangeModel
{
    public double MaxDistance(double r1, double r2) => math.min(r1, r2);
}

internal readonly struct AdditiveRangeModel : IRangeModel
{
    public double MaxDistance(double r1, double r2) => math.min(r1, r2) + math.sqrt(r1 * r2);
}
