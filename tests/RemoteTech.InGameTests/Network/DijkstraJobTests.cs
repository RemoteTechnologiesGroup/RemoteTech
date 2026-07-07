using KSP.Testing;
using RemoteTech.Collections;
using RemoteTech.Network;
using Unity.Collections;
using Unity.Jobs;

namespace RemoteTech.InGameTests.Network;

public class DijkstraJobTests : RTTestBase
{
    private static (NativeArray<IntRange> ranges, NativeArray<int> adjacency, NativeArray<double> distances)
        BuildCsr(int nodeCount, params (int a, int b, double w)[] edges)
    {
        var degree = new int[nodeCount];
        foreach (var (a, b, _) in edges) { degree[a]++; degree[b]++; }

        var ranges = new NativeArray<IntRange>(nodeCount, Allocator.Temp);
        int offset = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            ranges[i] = new IntRange(offset, degree[i]);
            offset += degree[i];
        }

        var adjacency = new NativeArray<int>(offset, Allocator.Temp);
        var distances = new NativeArray<double>(offset, Allocator.Temp);

        var cursor = new int[nodeCount];
        for (int i = 0; i < nodeCount; i++) cursor[i] = ranges[i].Start;
        foreach (var (a, b, w) in edges)
        {
            int ia = cursor[a]++; adjacency[ia] = b; distances[ia] = w;
            int ib = cursor[b]++; adjacency[ib] = a; distances[ib] = w;
        }

        return (ranges, adjacency, distances);
    }

    private struct Result
    {
        public NativeList<double> Scores;
        public NativeList<int> Parents;
        public NativeList<int> Origins;
    }

    private static Result Run(int nodeCount, (int a, int b, double w)[] edges, int[] roots, bool[] canTransit = null)
    {
        var (ranges, adjacency, distances) = BuildCsr(nodeCount, edges);
        var rootArr = new NativeArray<int>(roots, Allocator.Temp);

        var transit = new NativeBitArray(nodeCount, Allocator.Temp);
        for (int i = 0; i < nodeCount; i++)
            transit.Set(i, canTransit == null || canTransit[i]);

        // The job writes one entry per node, so the outputs must already be sized to
        // nodeCount (as NetworkState's nodeCount-length NativeArrays are).
        var scores = new NativeList<double>(nodeCount, Allocator.Temp);
        var parents = new NativeList<int>(nodeCount, Allocator.Temp);
        var origins = new NativeList<int>(nodeCount, Allocator.Temp);
        scores.ResizeUninitialized(nodeCount);
        parents.ResizeUninitialized(nodeCount);
        origins.ResizeUninitialized(nodeCount);

        new MultiSourceDijkstraJob
        {
            ranges = ranges,
            adjacency = adjacency,
            distances = distances,
            roots = rootArr,
            canTransit = transit,
            scores = scores,
            parents = parents,
            origins = origins,
        }.Run();

        return new Result { Scores = scores, Parents = parents, Origins = origins };
    }

    [TestInfo("DijkstraJobTests_Line_SingleSource_AccumulatesDistance")]
    public void Line_SingleSource_AccumulatesDistance()
    {
        var r = Run(4, new[] { (0, 1, 1.0), (1, 2, 1.0), (2, 3, 1.0) }, new[] { 0 });

        Assert.AreEqual(0.0, r.Scores[0], 1e-9);
        Assert.AreEqual(1.0, r.Scores[1], 1e-9);
        Assert.AreEqual(2.0, r.Scores[2], 1e-9);
        Assert.AreEqual(3.0, r.Scores[3], 1e-9);
        Assert.AreEqual(-1, r.Parents[0]);
        Assert.AreEqual(0, r.Parents[1]);
        Assert.AreEqual(1, r.Parents[2]);
        Assert.AreEqual(2, r.Parents[3]);
    }

    [TestInfo("DijkstraJobTests_Diamond_PicksShorterPath")]
    public void Diamond_PicksShorterPath()
    {
        // 0->1 (1), 0->2 (5), 1->3 (1), 2->3 (1): node 3 should route via 1 (cost 2).
        var r = Run(4, new[] { (0, 1, 1.0), (0, 2, 5.0), (1, 3, 1.0), (2, 3, 1.0) }, new[] { 0 });

        Assert.AreEqual(2.0, r.Scores[3], 1e-9);
        Assert.AreEqual(1, r.Parents[3]);
    }

    [TestInfo("DijkstraJobTests_MultiSource_EachNodeBindsToNearestRoot")]
    public void MultiSource_EachNodeBindsToNearestRoot()
    {
        // Line 0-1-2-3, roots at both ends. Midpoints split to their nearer root.
        var r = Run(4, new[] { (0, 1, 1.0), (1, 2, 1.0), (2, 3, 1.0) }, new[] { 0, 3 });

        Assert.AreEqual(0.0, r.Scores[0], 1e-9);
        Assert.AreEqual(1.0, r.Scores[1], 1e-9);
        Assert.AreEqual(1.0, r.Scores[2], 1e-9);
        Assert.AreEqual(0.0, r.Scores[3], 1e-9);
        Assert.AreEqual(0, r.Origins[1]);
        Assert.AreEqual(3, r.Origins[2]);
        Assert.AreEqual(0, r.Parents[1]);
        Assert.AreEqual(3, r.Parents[2]);
    }

    [TestInfo("DijkstraJobTests_UnreachableNode_StaysAtInfinity")]
    public void UnreachableNode_StaysAtInfinity()
    {
        // Node 2 is isolated.
        var r = Run(3, new[] { (0, 1, 1.0) }, new[] { 0 });

        Assert.IsTrue(double.IsPositiveInfinity(r.Scores[2]));
        Assert.AreEqual(-1, r.Parents[2]);
        Assert.AreEqual(-1, r.Origins[2]);
    }

    [TestInfo("DijkstraJobTests_NoRoots_LeavesEverythingUnreachable")]
    public void NoRoots_LeavesEverythingUnreachable()
    {
        var r = Run(3, new[] { (0, 1, 1.0), (1, 2, 1.0) }, System.Array.Empty<int>());

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(double.IsPositiveInfinity(r.Scores[i]));
            Assert.AreEqual(-1, r.Origins[i]);
        }
    }

    [TestInfo("DijkstraJobTests_NonTransitNode_ReachableButBlocksDownstream")]
    public void NonTransitNode_ReachableButBlocksDownstream()
    {
        // Line 0-1-2-3 rooted at 0; node 2 cannot transit -> it is still reached as
        // an endpoint, but node 3 (only reachable through 2) is not.
        var r = Run(4, new[] { (0, 1, 1.0), (1, 2, 1.0), (2, 3, 1.0) }, new[] { 0 },
            canTransit: new[] { true, true, false, true });

        Assert.AreEqual(2.0, r.Scores[2], 1e-9);
        Assert.AreEqual(1, r.Parents[2]);
        Assert.IsTrue(double.IsPositiveInfinity(r.Scores[3]));
        Assert.AreEqual(-1, r.Parents[3]);
    }
}
