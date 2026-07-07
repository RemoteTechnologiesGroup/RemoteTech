using System.Collections.Generic;
using KSP.Testing;
using RemoteTech.Collections;
using RemoteTech.Network;
using RemoteTech.SimpleTypes;
using Unity.Collections;

namespace RemoteTech.InGameTests.Network;

public class BuildAdjacencyJobTests : RTTestBase
{
    private static NetworkEdge Edge(int a, int b, double dist, bool valid = true)
        => new NetworkEdge { aIdx = a, bIdx = b, distance = dist, linkType = valid ? LinkType.Omni : LinkType.None };

    private static JobNode Node(NodeFlags flags) => new JobNode { flags = flags };

    private sealed class Csr
    {
        public NativeList<IntRange> Ranges;
        public NativeList<int> Adjacency;
        public NativeList<double> Distances;

        public List<(int target, double cost)> Row(int node)
        {
            var list = new List<(int, double)>();
            foreach (int e in Ranges[node])
                list.Add((Adjacency[e], Distances[e]));
            return list;
        }
    }

    private static Csr Build(JobNode[] nodes, NetworkEdge[] edges)
    {
        var nodeArr = new NativeArray<JobNode>(nodes, Allocator.Temp);
        var edgeArr = new NativeArray<NetworkEdge>(edges, Allocator.Temp);

        var ranges = new NativeList<IntRange>(nodes.Length, Allocator.Temp);
        ranges.ResizeUninitialized(nodes.Length);
        var adjacency = new NativeList<int>(0, Allocator.Temp);
        var distances = new NativeList<double>(0, Allocator.Temp);

        NetworkUpdate.ComputeAdjacencyLists(nodeArr, edgeArr, ranges, adjacency, distances);

        return new Csr { Ranges = ranges, Adjacency = adjacency, Distances = distances };
    }

    [TestInfo("BuildAdjacencyJobTests_UndirectedEdge_AppearsOnBothRows")]
    public void UndirectedEdge_AppearsOnBothRows()
    {
        var powered = NodeFlags.Powered | NodeFlags.CanRelay;
        var csr = Build(
            new[] { Node(powered), Node(powered), Node(powered) },
            new[] { Edge(0, 1, 10.0), Edge(1, 2, 20.0) });

        CollectionAssert.AreEqual(new[] { (1, 10.0) }, csr.Row(0));
        CollectionAssert.AreEqual(new[] { (0, 10.0), (2, 20.0) }, csr.Row(1));
        CollectionAssert.AreEqual(new[] { (1, 20.0) }, csr.Row(2));
        int total = csr.Ranges[0].Length + csr.Ranges[1].Length + csr.Ranges[2].Length;
        Assert.AreEqual(4, total); // two undirected edges -> four directed entries
    }

    [TestInfo("BuildAdjacencyJobTests_InvalidEdges_AreSkipped")]
    public void InvalidEdges_AreSkipped()
    {
        var powered = NodeFlags.Powered | NodeFlags.CanRelay;
        var csr = Build(
            new[] { Node(powered), Node(powered) },
            new[] { Edge(0, 1, 10.0, valid: false) });

        Assert.AreEqual(0, csr.Row(0).Count);
        Assert.AreEqual(0, csr.Row(1).Count);
    }

    [TestInfo("BuildAdjacencyJobTests_UnpoweredEndpoint_MakesEdgeNonTraversable")]
    public void UnpoweredEndpoint_MakesEdgeNonTraversable()
    {
        var csr = Build(
            new[] { Node(NodeFlags.Powered), Node(NodeFlags.None) },
            new[] { Edge(0, 1, 10.0) });

        Assert.AreEqual(0, csr.Row(0).Count);
        Assert.AreEqual(0, csr.Row(1).Count);
    }

    [TestInfo("BuildAdjacencyJobTests_RelayCapability_DoesNotGateAdjacency")]
    public void RelayCapability_DoesNotGateAdjacency()
    {
        // Node 1 is powered but not relay-capable. The edge still exists — relay
        // gates transit (in the Dijkstra), not whether the link is present.
        var csr = Build(
            new[] { Node(NodeFlags.Powered | NodeFlags.CanRelay), Node(NodeFlags.Powered) },
            new[] { Edge(0, 1, 10.0) });

        Assert.AreEqual(1, csr.Row(0).Count);
        Assert.AreEqual(1, csr.Row(1).Count);
    }

    [TestInfo("BuildAdjacencyJobTests_CanTransit_GatesOnRelayCapabilityWhenEnabled")]
    public void CanTransit_GatesOnRelayCapabilityWhenEnabled()
    {
        var nodes = new NativeArray<JobNode>(new[]
        {
            Node(NodeFlags.Powered | NodeFlags.CanRelay),
            Node(NodeFlags.Powered),
        }, Allocator.Temp);

        var off = new NativeBitArray(2, Allocator.Temp);
        NetworkUpdate.ComputeCanTransit(new JobConfig { signalRelayEnabled = false }, nodes, off);
        Assert.IsTrue(off.IsSet(0));
        Assert.IsTrue(off.IsSet(1)); // relay disabled -> everyone can transit

        var on = new NativeBitArray(2, Allocator.Temp);
        NetworkUpdate.ComputeCanTransit(new JobConfig { signalRelayEnabled = true }, nodes, on);
        Assert.IsTrue(on.IsSet(0));
        Assert.IsFalse(on.IsSet(1)); // relay enabled -> non-relay node cannot transit
    }

    [TestInfo("BuildAdjacencyJobTests_VesselConnected_FlagsOnlyNonEmptyRanges")]
    public void VesselConnected_FlagsOnlyNonEmptyRanges()
    {
        var ranges = new NativeArray<IntRange>(new[] { new IntRange(0, 1), new IntRange(1, 0), new IntRange(1, 2) }, Allocator.Temp);
        var connected = new NativeBitArray(3, Allocator.Temp);

        NetworkUpdate.ComputeVesselConnected(ranges, connected);

        Assert.IsTrue(connected.IsSet(0));
        Assert.IsFalse(connected.IsSet(1));
        Assert.IsTrue(connected.IsSet(2));
    }
}
