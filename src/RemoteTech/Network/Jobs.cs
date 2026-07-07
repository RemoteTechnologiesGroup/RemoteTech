using System;
using System.Runtime.InteropServices;
using RemoteTech.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace RemoteTech.Network;

internal struct JobConfig
{
    public double omniClamp;
    public double dishClamp;
    public double multipleAntennaMultiplier;
    [MarshalAs(UnmanagedType.U1)]
    public bool ignoreLineOfSight;
    [MarshalAs(UnmanagedType.U1)]
    public bool signalRelayEnabled;
}

/// <summary>
/// Given a set of origins this job finds the distance of each node from the
/// nearest origin.
/// </summary>
[BurstCompile]
internal struct MultiSourceDijkstraJob : IJob
{
    [ReadOnly] public NativeArray<IntRange> ranges;
    [ReadOnly] public NativeArray<int> adjacency;
    [ReadOnly] public NativeArray<double> distances;
    [ReadOnly] public NativeArray<int> roots;

    // Per-node "may forward a signal onward". Roots and destinations don't need it;
    // only intermediate hops do (see NetworkUpdate.CanTransit).
    [ReadOnly] public NativeBitArray canTransit;

    public NativeArray<double> scores;
    public NativeArray<int> parents;
    public NativeArray<int> origins;

    public void Execute()
    {
        for (int i = 0; i < ranges.Length; ++i)
        {
            scores[i] = double.PositiveInfinity;
            parents[i] = -1;
            origins[i] = -1;
        }

        var heap = new ArrayMinHeap<HeapNode>(ranges.Length, Allocator.Temp);
        for (int i = 0; i < roots.Length; ++i)
        {
            var index = roots[i];
            if (index < 0 || index >= ranges.Length)
                continue;

            heap.Push(new()
            {
                cost = 0.0,
                node = index,
                parent = -1
            });
        }

        while (heap.TryPop(out var current))
        {
            if (current.cost >= scores[current.node])
                continue;

            scores[current.node] = current.cost;
            parents[current.node] = current.parent;
            if (current.parent == -1)
                origins[current.node] = current.node;
            else
                origins[current.node] = origins[current.parent];

            // A root (parent == -1) is a route endpoint and always forwards; any
            // other node may only be relayed through if it can transit.
            if (current.parent != -1 && !canTransit.IsSet(current.node))
                continue;

            foreach (int edge in ranges[current.node])
            {
                int node = adjacency[edge];
                var cost = current.cost + distances[edge];
                if (cost >= scores[node])
                    continue;

                heap.Push(new()
                {
                    cost = cost,
                    node = node,
                    parent = current.node
                });
            }
        }
    }

    struct HeapNode : IComparable<HeapNode>
    {
        public double cost;
        public int node;
        public int parent;

        public int CompareTo(HeapNode other) => cost.CompareTo(other.cost);
    }
}

/// <summary>
/// Single-pair shortest path over the persisted edge table: the minimum total
/// link distance from <see cref="source"/> to <see cref="target"/>. Links exist
/// between powered nodes; the relay constraint gates transit only, so the two
/// endpoints need not be relay-capable (see <see cref="NetworkUpdate.CanTransit"/>).
/// Writes +inf through <see cref="length"/> if the target is unreachable.
/// </summary>
[BurstCompile]
internal unsafe struct NetworkPathfindJob : IJob
{
    public JobConfig config;
    public int source;
    public int target;

    [ReadOnly] public NativeArray<JobNode> nodes;
    [ReadOnly] public NativeArray<NetworkEdge> edges;

    [NativeDisableUnsafePtrRestriction] public double* length;

    public void Execute()
    {
        *length = double.PositiveInfinity;

        int n = nodes.Length;
        if ((uint)source >= (uint)n || (uint)target >= (uint)n)
            return;
        if (source == target)
        {
            *length = 0.0;
            return;
        }

        var dist = new NativeArray<double>(n, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < n; ++i)
            dist[i] = double.PositiveInfinity;

        var heap = new ArrayMinHeap<HeapNode>(n, Allocator.Temp);
        dist[source] = 0.0;
        heap.Push(new HeapNode { cost = 0.0, node = source });

        while (heap.TryPop(out var current))
        {
            if (current.cost > dist[current.node])
                continue;
            if (current.node == target)
            {
                *length = current.cost;
                return;
            }

            int u = current.node;
            if (!nodes[u].Powered)
                continue;

            // The source is a route endpoint; every other node may only be relayed
            // through if it can transit. The target is exempt too — it returns above
            // before ever reaching this expansion.
            if (u != source && !NetworkUpdate.CanTransit(in config, nodes[u]))
                continue;

            for (int v = 0; v < n; ++v)
            {
                if (v == u)
                    continue;

                var edge = edges[NetworkUpdateMath.EncodePairIndex(u, v)];
                if (!edge.valid || !nodes[v].Powered)
                    continue;

                double next = current.cost + edge.distance;
                if (next < dist[v])
                {
                    dist[v] = next;
                    heap.Push(new HeapNode { cost = next, node = v });
                }
            }
        }
    }

    struct HeapNode : IComparable<HeapNode>
    {
        public double cost;
        public int node;

        public int CompareTo(HeapNode other) => cost.CompareTo(other.cost);
    }
}
