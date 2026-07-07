using KSP.Testing;
using RemoteTech.Network;
using Unity.Collections;
using UnityEngine;

namespace RemoteTech.InGameTests.Network;

/// <summary>
/// Exercises NetworkUpdate.ComputeMarkCandidates's selection (command-station
/// vessels plus every ground station) and its alwaysShow / colour plumbing.
/// </summary>
public class GatherMarkCandidatesJobTests : RTTestBase
{
    private static NetworkUpdate.RawVesselInfo Vessel(bool commandStation, bool landed, Color32 color)
        => new NetworkUpdate.RawVesselInfo
        {
            state = new SatelliteState { IsCommandStation = commandStation },
            kind = NodeKind.Vessel,
            landed = landed,
            markColor = color,
        };

    private static NetworkUpdate.RawStationInfo Station(Color32 color)
        => new NetworkUpdate.RawStationInfo
        {
            state = new SatelliteState(),
            kind = NodeKind.GroundStation,
            markColor = color,
        };

    private static NativeList<SatelliteMarkCandidate> Gather(
        NetworkUpdate.RawVesselInfo[] vessels,
        NetworkUpdate.RawStationInfo[] stations)
    {
        var v = new NativeArray<NetworkUpdate.RawVesselInfo>(vessels, Allocator.Temp);
        var s = new NativeArray<NetworkUpdate.RawStationInfo>(stations, Allocator.Temp);
        var candidates = new NativeList<SatelliteMarkCandidate>(vessels.Length + stations.Length, Allocator.Temp);

        NetworkUpdate.ComputeMarkCandidates(v, s, candidates);

        return candidates;
    }

    [TestInfo("GatherMarkCandidatesJobTests_NonCommandVessel_IsExcluded")]
    public void NonCommandVessel_IsExcluded()
    {
        var candidates = Gather(
            new[] { Vessel(commandStation: false, landed: true, Color.white) },
            new NetworkUpdate.RawStationInfo[0]);

        Assert.AreEqual(0, candidates.Length);
    }

    [TestInfo("GatherMarkCandidatesJobTests_OrbitingCommandStation_AlwaysShown")]
    public void OrbitingCommandStation_AlwaysShown()
    {
        var candidates = Gather(
            new[] { Vessel(commandStation: true, landed: false, Color.green) },
            new NetworkUpdate.RawStationInfo[0]);

        Assert.AreEqual(1, candidates.Length);
        Assert.AreEqual(0, candidates[0].nodeIndex);
        Assert.IsTrue(candidates[0].alwaysShow);
    }

    [TestInfo("GatherMarkCandidatesJobTests_LandedCommandStation_NotAlwaysShown")]
    public void LandedCommandStation_NotAlwaysShown()
    {
        var candidates = Gather(
            new[] { Vessel(commandStation: true, landed: true, Color.green) },
            new NetworkUpdate.RawStationInfo[0]);

        Assert.AreEqual(1, candidates.Length);
        Assert.IsFalse(candidates[0].alwaysShow);
    }

    [TestInfo("GatherMarkCandidatesJobTests_GroundStationsOffsetPastVessels")]
    public void GroundStationsOffsetPastVessels()
    {
        // Two vessels (one non-command) precede the stations; a station's node
        // index must count every vessel slot, not just the emitted marks.
        Color32 stationColor = Color.red;
        var candidates = Gather(
            new[]
            {
                Vessel(commandStation: true, landed: true, Color.green),
                Vessel(commandStation: false, landed: true, Color.white),
            },
            new[] { Station(stationColor) });

        Assert.AreEqual(2, candidates.Length);
        Assert.AreEqual(0, candidates[0].nodeIndex);
        Assert.AreEqual(2, candidates[1].nodeIndex);
        Assert.IsFalse(candidates[1].alwaysShow);

        Color32 got = candidates[1].color;
        Assert.AreEqual(stationColor.r, got.r);
        Assert.AreEqual(stationColor.g, got.g);
        Assert.AreEqual(stationColor.b, got.b);
        Assert.AreEqual(stationColor.a, got.a);
    }
}
