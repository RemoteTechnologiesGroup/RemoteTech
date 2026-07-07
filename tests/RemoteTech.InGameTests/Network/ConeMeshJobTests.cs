using KSP.Testing;
using RemoteTech.Network;
using Unity.Collections;

namespace RemoteTech.InGameTests.Network;

/// <summary>
/// Exercises NetworkUpdate.ComputeConeCandidates's eligibility filter
/// (Powered, CanTarget, has a target).
/// </summary>
public class GatherConeCandidatesJobTests : RTTestBase
{
    private static JobAntenna Antenna(AntennaFlags flags, int target, int nodeIndex = 0, double cosAngle = 0.5, double dish = 1000.0)
        => new JobAntenna { nodeIndex = nodeIndex, target = target, cosAngle = cosAngle, dish = dish, flags = flags };

    private static NativeList<ConeCandidate> Gather(params JobAntenna[] antennas)
    {
        var arr = new NativeArray<JobAntenna>(antennas, Allocator.Temp);
        var candidates = new NativeList<ConeCandidate>(antennas.Length, Allocator.Temp);

        NetworkUpdate.ComputeConeCandidates(arr, candidates);

        return candidates;
    }

    [TestInfo("GatherConeCandidatesJobTests_PoweredCanTargetWithTarget_IsIncluded")]
    public void PoweredCanTargetWithTarget_IsIncluded()
    {
        var ant = Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: 5, nodeIndex: 3, cosAngle: 0.9, dish: 1234.0);
        var candidates = Gather(ant);

        Assert.AreEqual(1, candidates.Length);
        Assert.AreEqual(3, candidates[0].nodeIndex);
        Assert.AreEqual(5, candidates[0].target);
        Assert.AreEqual(0.9, candidates[0].cosAngle, 1e-9);
        Assert.AreEqual(1234.0, candidates[0].dishRange, 1e-9);
    }

    [TestInfo("GatherConeCandidatesJobTests_NotPowered_IsExcluded")]
    public void NotPowered_IsExcluded()
    {
        var ant = Antenna(AntennaFlags.CanTarget, target: 5);
        Assert.AreEqual(0, Gather(ant).Length);
    }

    [TestInfo("GatherConeCandidatesJobTests_CannotTarget_IsExcluded")]
    public void CannotTarget_IsExcluded()
    {
        var ant = Antenna(AntennaFlags.Powered, target: 5);
        Assert.AreEqual(0, Gather(ant).Length);
    }

    [TestInfo("GatherConeCandidatesJobTests_NoTarget_IsExcluded")]
    public void NoTarget_IsExcluded()
    {
        var ant = Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: 0);
        Assert.AreEqual(0, Gather(ant).Length);
    }

    [TestInfo("GatherConeCandidatesJobTests_TargetsABody_IsIncluded")]
    public void TargetsABody_IsIncluded()
    {
        // Negative target encodes a celestial body rather than a node.
        var ant = Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: -2);
        var candidates = Gather(ant);

        Assert.AreEqual(1, candidates.Length);
        Assert.AreEqual(-2, candidates[0].target);
    }

    [TestInfo("GatherConeCandidatesJobTests_MixedAntennas_OnlyQualifyingSurvive")]
    public void MixedAntennas_OnlyQualifyingSurvive()
    {
        var candidates = Gather(
            Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: 1, nodeIndex: 0), // qualifies
            Antenna(AntennaFlags.CanTarget, target: 1, nodeIndex: 1),                        // not powered
            Antenna(AntennaFlags.Powered, target: 1, nodeIndex: 2),                          // can't target
            Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: 0, nodeIndex: 3),  // no target
            Antenna(AntennaFlags.Powered | AntennaFlags.CanTarget, target: 2, nodeIndex: 4)); // qualifies

        Assert.AreEqual(2, candidates.Length);
        Assert.AreEqual(0, candidates[0].nodeIndex);
        Assert.AreEqual(4, candidates[1].nodeIndex);
    }
}
