using System;
using System.Runtime.InteropServices;
using RemoteTech.Collections;
using Unity.Burst;
using Unity.Collections;
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
