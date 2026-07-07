using System;
using KSP.Testing;
using RemoteTech.Network;
using RemoteTech.SimpleTypes;
using Unity.Collections;
using Unity.Jobs;

namespace RemoteTech.InGameTests.Network;

/// <summary>
/// Drives NetworkPathfindJob over a hand-built node/edge graph — the on-demand
/// A-to-B query behind API.GetSignalDelayToSatellite. Verifies shortest-path
/// selection and the powered/relay hop gating.
/// </summary>
public class NetworkPathfindJobTests : RTTestBase
{
    private static unsafe double Solve(
        int nodeCount,
        NodeFlags[] flags,
        bool signalRelay,
        int source,
        int target,
        params (int a, int b, double dist)[] edges)
    {
        var nodes = new NativeArray<JobNode>(nodeCount, Allocator.Temp);
        for (int i = 0; i < nodeCount; i++)
            nodes[i] = new JobNode { flags = flags[i] };

        var table = new NativeArray<NetworkEdge>(NetworkUpdateMath.PairCount(nodeCount), Allocator.Temp);
        foreach (var (a, b, dist) in edges)
        {
            table[NetworkUpdateMath.EncodePairIndex(a, b)] = new NetworkEdge
            {
                aIdx = Math.Min(a, b),
                bIdx = Math.Max(a, b),
                distance = dist,
                maxRange = dist,
                linkType = LinkType.Omni,
            };
        }

        double length = double.PositiveInfinity;
        new NetworkPathfindJob
        {
            config = new JobConfig { signalRelayEnabled = signalRelay },
            source = source,
            target = target,
            nodes = nodes,
            edges = table,
            length = &length,
        }.Run();

        return length;
    }

    private static NodeFlags[] AllRelay(int n)
    {
        var flags = new NodeFlags[n];
        for (int i = 0; i < n; i++)
            flags[i] = NodeFlags.Powered | NodeFlags.CanRelay;
        return flags;
    }

    [TestInfo("NetworkPathfindJobTests_Diamond_PicksShorterPath")]
    public void Diamond_PicksShorterPath()
    {
        // 0-1 (1), 0-2 (5), 1-3 (1), 2-3 (1): 0->3 routes via 1 at cost 2.
        double d = Solve(4, AllRelay(4), signalRelay: false, source: 0, target: 3,
            (0, 1, 1.0), (0, 2, 5.0), (1, 3, 1.0), (2, 3, 1.0));

        Assert.AreEqual(2.0, d, 1e-9);
    }

    [TestInfo("NetworkPathfindJobTests_SameNode_IsZero")]
    public void SameNode_IsZero()
    {
        double d = Solve(3, AllRelay(3), signalRelay: false, source: 1, target: 1,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.AreEqual(0.0, d, 1e-9);
    }

    [TestInfo("NetworkPathfindJobTests_Disconnected_IsInfinite")]
    public void Disconnected_IsInfinite()
    {
        // 0-1 linked, node 2 isolated.
        double d = Solve(3, AllRelay(3), signalRelay: false, source: 0, target: 2,
            (0, 1, 1.0));

        Assert.IsTrue(double.IsPositiveInfinity(d));
    }

    [TestInfo("NetworkPathfindJobTests_UnpoweredIntermediate_BlocksPath")]
    public void UnpoweredIntermediate_BlocksPath()
    {
        // Only route is 0-1-2, but node 1 is unpowered -> its edges are untraversable.
        var flags = AllRelay(3);
        flags[1] = NodeFlags.CanRelay; // powered bit cleared
        double d = Solve(3, flags, signalRelay: false, source: 0, target: 2,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.IsTrue(double.IsPositiveInfinity(d));
    }

    [TestInfo("NetworkPathfindJobTests_RelayOff_RoutesThroughNonRelay")]
    public void RelayOff_RoutesThroughNonRelay()
    {
        // Intermediate 1 cannot relay, but with signal relay disabled that is ignored.
        var flags = AllRelay(3);
        flags[1] = NodeFlags.Powered; // relay bit cleared
        double d = Solve(3, flags, signalRelay: false, source: 0, target: 2,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.AreEqual(2.0, d, 1e-9);
    }

    [TestInfo("NetworkPathfindJobTests_RelayOn_NonRelayIntermediateBlocks")]
    public void RelayOn_NonRelayIntermediateBlocks()
    {
        // Relay enabled: the non-relay intermediate cannot forward, so no route survives.
        var flags = AllRelay(3);
        flags[1] = NodeFlags.Powered; // relay bit cleared
        double d = Solve(3, flags, signalRelay: true, source: 0, target: 2,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.IsTrue(double.IsPositiveInfinity(d));
    }

    [TestInfo("NetworkPathfindJobTests_RelayOn_NonRelaySource_StillConnects")]
    public void RelayOn_NonRelaySource_StillConnects()
    {
        // Source is a route endpoint: it never needs to relay, even with relay on.
        var flags = AllRelay(3);
        flags[0] = NodeFlags.Powered; // relay bit cleared on the source
        double d = Solve(3, flags, signalRelay: true, source: 0, target: 2,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.AreEqual(2.0, d, 1e-9);
    }

    [TestInfo("NetworkPathfindJobTests_RelayOn_NonRelayTarget_StillConnects")]
    public void RelayOn_NonRelayTarget_StillConnects()
    {
        // Target is a route endpoint: it never needs to relay, even with relay on.
        var flags = AllRelay(3);
        flags[2] = NodeFlags.Powered; // relay bit cleared on the target
        double d = Solve(3, flags, signalRelay: true, source: 0, target: 2,
            (0, 1, 1.0), (1, 2, 1.0));

        Assert.AreEqual(2.0, d, 1e-9);
    }
}
