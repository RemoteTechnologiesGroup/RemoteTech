using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RemoteTech.Collections;
using RemoteTech.SimpleTypes;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using VesselTypeFilter = MapViewFiltering.VesselTypeFilter ;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace RemoteTech.Network;

[BurstCompile]
internal static class NetworkUpdate
{
    #region Gather Celestial Bodies
    internal struct RawCelestialBodyInfo
    {
        public Guid guid;
        public Vector3d position;
        public double radius;
        public int instanceID;
        public int parentID;
    }

    internal struct RecordCelestialBodiesJob : IJob
    {
        public ObjectHandle<List<CelestialBody>> bodies;
        public NativeArray<RawCelestialBodyInfo> infos;

        public void Execute()
        {
            using var guard = this.bodies;
            var bodies = this.bodies.Target.AsReadOnlySpan();

            int i = 0;
            foreach (var body in bodies)
            {
                infos[i++] = new()
                {
                    guid = RTUtil.Guid(body),
                    position = body.position,
                    radius = body.Radius,
                    instanceID = body.GetInstanceID(),
                    parentID = body.referenceBody?.GetInstanceID() ?? 0
                };
            }
        }
    }
    
    struct BodyComputeState
    {
        public NativeArray<RawCelestialBodyInfo> input;
        public NativeArray<JobBody> output;
        public NativeHashMap<Guid, int> mapping;

        NativeMultiHashMap<int, int> children;

        public void Compute()
        {
            // If the tree is invalid we can end up with entries that never have
            // anything assigned. With uninitialized data that leads to an instant
            // crash, better to make things work normally.
            output.Clear();

            children = new(input.Length, Allocator.Temp);
            for (int i = 0; i < input.Length; ++i)
            {
                var parentID = input[i].parentID;
                if (parentID == input[i].instanceID)
                    parentID = 0;

                children.Add(parentID, i);
            }

            int index = 0;
            if (children.TryGetFirstValue(0, out int childIndex, out var it))
            {
                do
                {
                    Dfs(childIndex, -1, ref index);
                } while (children.TryGetNextValue(out childIndex, ref it));
            }

            if (index != input.Length)
                ThrowUnreachableBody(index, input.Length);
        }

        void Dfs(int inputIndex, int parent, ref int index)
        {
            var body = input[inputIndex];

            int slot = index++;
            ref var jb = ref output.GetElement(slot);

            mapping.Add(body.guid, slot);
            jb = new()
            {
                position = body.position.ToDouble3(),
                radius = body.radius,
                parent = parent,
            };

            if (children.TryGetFirstValue(body.instanceID, out int childIndex, out var it))
            {
                do
                {
                    Dfs(childIndex, slot, ref index);
                } while (children.TryGetNextValue(out childIndex, ref it));
            }

            jb.subtree = new(slot, index - slot);
        }

        [IgnoreWarning(1370)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        void ThrowUnreachableBody(int visited, int length) =>  
            throw new InvalidOperationException($"celestial body job only visited {visited}/{length} bodies");
    }

    internal static void ComputeBodyInfo(
        NativeArray<RawCelestialBodyInfo> input,
        NativeArray<JobBody> output,
        NativeHashMap<Guid, int> mapping)
    {
        var state = new BodyComputeState
        {
            input = input,
            output = output,
            mapping = mapping,
        };

        state.Compute();
    }
    #endregion

    #region Gather Vessel Info
    internal struct RawVesselInfo
    {
        public SatelliteState state;
        public int instanceID;
        public VesselType type;
        public NodeKind kind;
        public Color32 markColor;
        [MarshalAs(UnmanagedType.U1)]
        public bool landed;
    }

    internal struct RecordVesselInfoJob : IJobParallelForBatch
    {
        public ObjectHandle<ArrayMap<Guid, VesselSatellite>> satellites;
        public NativeArray<RawVesselInfo> infos;

        public void Execute(int start, int count)
        {
            var satellites = this.satellites.Target.Values;
            int end = start + count;

            for (int i = start; i < end; ++i)
            {
                var sat = satellites[i];
                var vessel = sat.Vessel;

                infos[i] = new()
                {
                    state = sat.GetState(),
                    instanceID = vessel.GetInstanceID(),
                    type = vessel.DiscoveryInfo.HaveKnowledgeAbout(DiscoveryLevels.Appearance)
                        ? vessel.vesselType
                        : VesselType.Unknown,
                    kind = sat.isVessel
                        ? NodeKind.Vessel
                        : NodeKind.GroundStation,
                    markColor = sat.MarkColor,
                    landed = vessel.Landed
                };
            }
        }
    }

    internal static void ComputeVesselInfo(
        NativeArray<RawVesselInfo> raw,
        NativeArray<JobNode> nodes,
        NativeHashMap<Guid, int> satmap,
        NativeHashMap<Guid, int> bodymap)
    {
        for (int i = 0; i < raw.Length; ++i)
        {
            var info = raw[i];

            nodes[i] = new()
            {
                guid = info.state.Guid,
                position = info.state.Position.ToDouble3(),
                bodyIndex = bodymap.TryGetValue(info.state.Body, out var bodyIndex)
                    ? bodyIndex
                    : 0,
                kind = info.kind,
                flags = info.state.GetFlags()
            };
            satmap.Add(info.state.Guid, i);
        }
    }

    internal struct VisibilityState
    {
        public Guid ActiveVessel;
        public Guid TargetVessel;
        public VesselTypeFilter? filter;
    }

    internal static void ComputeVesselVisibility(
        VisibilityState context,
        NativeArray<RawVesselInfo> infos,
        NativeArray<JobNode> nodes)
    {
        if (context.filter is not VesselTypeFilter filter)
        {
            for (int i = 0; i < infos.Length; ++i)
                nodes.GetElement(i).flags |= NodeFlags.Visible;
            return;
        }

        for (int i = 0; i < infos.Length; ++i)
        {
            var info = infos[i];
            var guid = info.state.Guid;
            var type = info.type;

            if (EvaluateFilter(in context, guid, type, filter))
                nodes.GetElement(i).flags |= NodeFlags.Visible;
        }
    }

    static bool EvaluateFilter(
        in VisibilityState context,
        Guid guid,
        VesselType type,
        VesselTypeFilter filter)
    {
        if (guid == context.ActiveVessel)
            return true;
        if (guid == context.TargetVessel)
            return true;

        return type switch
        {
            VesselType.Debris => (filter & VesselTypeFilter.Debris) != 0,
            VesselType.SpaceObject => (filter & VesselTypeFilter.SpaceObjects) != 0,
            VesselType.Probe => (filter & VesselTypeFilter.Probes) != 0,
            VesselType.Relay => (filter & VesselTypeFilter.Relay) != 0,
            VesselType.Rover => (filter & VesselTypeFilter.Rovers) != 0,
            VesselType.Lander => (filter & VesselTypeFilter.Landers) != 0,
            VesselType.Ship => (filter & VesselTypeFilter.Ships) != 0,
            VesselType.Plane => (filter & VesselTypeFilter.Plane) != 0,
            VesselType.Station => (filter & VesselTypeFilter.Stations) != 0,
            VesselType.Base => (filter & VesselTypeFilter.Bases) != 0,
            VesselType.EVA => (filter & VesselTypeFilter.EVAs) != 0,
            VesselType.Flag => (filter & VesselTypeFilter.Flags) != 0,
            VesselType.DeployedScienceController => (filter & VesselTypeFilter.DeployedScienceController) != 0,
            VesselType.DeployedSciencePart => false,
            _ => (filter & VesselTypeFilter.Unknown) != 0,
        };
    }
    #endregion

    #region Gather Station Info
    internal struct RawStationInfo
    {
        public SatelliteState state;
        public NodeKind kind;
        public Color32 markColor;
    }

    internal struct RecordGroundStationsJob : IJob
    {
        public ObjectHandle<ArrayMap<Guid, ISatellite>> stations;
        public NativeArray<RawStationInfo> states;

        public void Execute()
        {
            using var guard = stations;
            var satellites = stations.Target.Values;

            int index = 0;
            foreach (var sat in satellites)
            {
                states[index++] = new()
                {
                    state = sat.GetState(),
                    kind = sat.isVessel
                        ? NodeKind.Vessel
                        : NodeKind.GroundStation,
                    markColor = sat.MarkColor
                };
            }
        }
    }

    internal static void ComputeGroundStationInfo(
        NativeArray<RawStationInfo> infos,
        NativeArray<JobNode> nodes,
        int offset,
        NativeHashMap<Guid, int> bodymap)
    {
        for (int i = 0; i < infos.Length; ++i)
        {
            var info = infos[i];
            var state = info.state;

            nodes[i + offset] = new()
            {
                guid = state.Guid,
                position = state.Position.ToDouble3(),
                bodyIndex = bodymap.TryGetValue(state.Body, out var bodyIndex)
                    ? bodyIndex
                    : 0,
                kind = info.kind,
                flags = state.GetFlags(),
            };
        }
    }
    #endregion

    #region Gather Antenna Info
    internal static unsafe void ComputeAntennaInfo(
        NativeArray<JobNode> nodes,
        NativeList<JobAntenna> antennas,
        StateManager.NativePointerArray<AntennaData> datas,
        NativeHashMap<Guid, int> mapping)
    {
        NativeMultiHashMap<Guid, JobAntenna> amap = new(datas.Length, Allocator.Temp);
        antennas.Capacity = datas.Length;

        for (int i = 0; i < datas.Length; ++i)
        {
            var data = datas[i];
            var ran = SatelliteRecordUtil.BuildAntenna(in *data);

            if (!mapping.TryGetValue(ran.target, out int target))
                target = 0;

            ran.antenna.target = target;

            amap.Add(data->Guid, ran.antenna);
        }

        for (int i = 0; i < nodes.Length; ++i)
        {
            int start = antennas.Length;
            ref var node = ref nodes.GetElement(i);

            if (amap.TryGetFirstValue(node.guid, out var ant, out var it))
            {
                do
                {
                    ant.nodeIndex = i;
                    antennas.Add(ant);
                } while (amap.TryGetNextValue(out ant, ref it));
            }

            node.antennas = new(start, antennas.Length - start);
        }
    }
    
    internal static void ComputeAntennaDirections(
        NativeArray<JobNode> nodes,
        NativeArray<JobBody> bodies,
        NativeArray<JobAntenna> antennas,
        NativeArray<double3> directions)
    {
        for (int i = 0; i < antennas.Length; ++i)
        {
            var antenna = antennas[i];
            var origin = nodes[antenna.nodeIndex].position;

            double3 target;
            if (antenna.target == 0)
                target = origin;
            else if (antenna.target > 0)
                target = nodes[antenna.target - 1].position;
            else
                target = bodies[-antenna.target - 1].position;

            double3 rel = target - origin;
            double magSq = math.lengthsq(rel);
            directions[i] = magSq > 0.0 ? rel * math.rsqrt(magSq) : double3.zero;
        }
    }
    #endregion

    #region Compute Cone Candidates
    internal static void ComputeConeCandidates(
        NativeArray<JobAntenna> antennas,
        NativeList<ConeCandidate> candidates)
    {
        foreach (var a in antennas)
        {
            if (!a.Powered || !a.CanTarget || a.target == 0)
                continue;

            candidates.Add(new ConeCandidate
            {
                nodeIndex = a.nodeIndex,
                target = a.target,
                cosAngle = a.cosAngle,
                dishRange = a.dish,
            });
        }
    }
    #endregion

    #region Compute Mark Candidates
    internal static void ComputeMarkCandidates(
        NativeArray<RawVesselInfo> vessels,
        NativeArray<RawStationInfo> stations,
        NativeList<SatelliteMarkCandidate> candidates)
    {
        for (int i = 0; i < vessels.Length; ++i)
        {
            var v = vessels[i];
            if (!v.state.IsCommandStation)
                continue;

            candidates.Add(new SatelliteMarkCandidate
            {
                nodeIndex = i,
                color = v.markColor,
                alwaysShow = v.kind == NodeKind.Vessel && !v.landed,
            });
        }

        for (int i = 0; i < stations.Length; ++i)
        {
            candidates.Add(new SatelliteMarkCandidate
            {
                nodeIndex = vessels.Length + i,
                color = stations[i].markColor,
                alwaysShow = false,
            });
        }
    }
    #endregion

    #region Compute Max Vessel Range
    internal static void ComputeVesselMaxRanges(
        in JobConfig config,
        NativeArray<JobNode> nodes,
        NativeArray<JobAntenna> antennas,
        NativeArray<double> ranges)
    {
        for (int i = 0; i < nodes.Length; ++i)
        {
            var node = nodes[i];

            double maxOmni = 0.0;
            double maxDish = 0.0;
            double sumOmni = 0.0;
            foreach (int j in node.antennas)
            {
                var a = antennas[j];
                if (a.omni > maxOmni) maxOmni = a.omni;
                if (a.dish > maxDish) maxDish = a.dish;
                sumOmni += a.omni;
            }

            double bonus = config.multipleAntennaMultiplier > 0.0
                ? (sumOmni - maxOmni) * config.multipleAntennaMultiplier
                : 0.0;
            double effOmni = (maxOmni + bonus) * config.omniClamp;
            double effDish = maxDish * config.dishClamp;
            ranges[i] = math.max(effOmni, effDish);
        }
    }
    #endregion

    #region Compute Mapping
    internal static NativeHashMap<Guid, int> ComputeOverallMapping(
        NativeHashMap<Guid, int> cbmap,
        NativeHashMap<Guid, int> satmap)
    {
        var mapping = new NativeHashMap<Guid, int>(
            cbmap.Capacity + satmap.Capacity,
            Allocator.Temp);

        foreach (var (id, index) in cbmap.GetKeyValueArrays(Allocator.Temp))
            mapping.Add(id, -(index + 1));
        foreach (var (id, index) in satmap.GetKeyValueArrays(Allocator.Temp))
            mapping.Add(id, index + 1);

        return mapping;
    }
    #endregion

    #region Compute Adjacency
    public static void ComputeAdjacencyLists(
        in JobConfig config,
        NativeArray<JobNode> nodes,
        NativeArray<NetworkEdge> edges,
        NativeArray<IntRange> ranges,
        NativeList<int> adjacency,
        NativeList<double> distances)
    {
        var matrix = new NativeArray<int>(nodes.Length * nodes.Length, Allocator.Temp);
        var matrixDist = new NativeArray<double>(nodes.Length * nodes.Length, Allocator.Temp);
        var cursors = new NativeArray<int>(nodes.Length, Allocator.Temp);

        for (int i = 0; i < cursors.Length; ++i)
            cursors[i] = i * nodes.Length;

        int total = 0;
        for (int i = 0; i < edges.Length; ++i)
        {
            var edge = edges[i];
            if (!edge.valid)
                continue;
            if (!IsEdgeTraversable(in config, nodes, edge.aIdx, edge.bIdx))
                continue;

            var idxA = cursors[edge.aIdx]++;
            matrix[idxA] = edge.bIdx;
            matrixDist[idxA] = edge.distance;

            var idxB = cursors[edge.bIdx]++;
            matrix[idxB] = edge.aIdx;
            matrixDist[idxB] = edge.distance;

            total += 2;
        }

        adjacency.Capacity = total;
        distances.Capacity = total;

        int offset = 0;
        for (int i = 0; i < nodes.Length; ++i)
        {
            int count = cursors[i] - i * nodes.Length;
            ranges[i] = new(offset, count);
            offset += count;

            var indices = matrix.GetSubArray(i * nodes.Length, count);
            adjacency.AddRange(indices);

            var dists = matrixDist.GetSubArray(i * nodes.Length, count);
            distances.AddRange(dists);
        }
    }
    
    static bool IsEdgeTraversable(
        in JobConfig config,
        NativeArray<JobNode> nodes,
        int a,
        int b)
    {
        JobNode na = nodes[a];
        JobNode nb = nodes[b];

        if (!na.Powered || !nb.Powered)
            return false;
        if (config.signalRelayEnabled && (!na.CanRelay || !nb.CanRelay))
            return false;
        return true;
    }
    #endregion

    #region Compute Vessel Connection Info
    public static void ComputeVesselConnected(
        NativeArray<IntRange> ranges,
        NativeBitArray connected)
    {
        for (int i = 0; i < ranges.Length; ++i)
        {
            if (!ranges[i].IsEmpty)
                connected.Set(i, true);
        }
    }
    #endregion

    #region Compute Roots
    public static void ComputeNetworkRoots(
        NativeArray<JobNode> nodes,
        NativeList<int> commandStations,
        NativeList<int> groundStations)
    {
        commandStations.Capacity = nodes.Length;
        groundStations.Capacity = nodes.Length;

        for (int i = 0; i < nodes.Length; ++i)
        {
            if (nodes[i].IsCommandStation || nodes[i].kind == NodeKind.GroundStation)
                commandStations.Add(i);
            if (nodes[i].kind == NodeKind.GroundStation)
                groundStations.Add(i);
        }
    }
    #endregion

    #region Compute Route Fingerprints
    internal static void ComputeRouteFingerprints(
        NativeArray<Guid> guids,
        NativeArray<int> originCs,
        NativeArray<int> parentCs,
        NativeArray<int> originGs,
        NativeArray<int> parentGs,
        NativeHashMap<Guid, int> prevSatmap,
        NativeArray<Hash128> prevFingerprints,
        NativeArray<Hash128> fingerprints,
        NativeList<int> changed)
    {
        var path = new NativeList<Guid>(16, Allocator.Temp);

        for (int index = 0; index < guids.Length; ++index)
        {
            Hash128 hash = default;
            HashPath(guids, originCs, parentCs, index, path, ref hash);
            HashPath(guids, originGs, parentGs, index, path, ref hash);

            fingerprints[index] = hash;

            if (!prevSatmap.TryGetValue(guids[index], out int prevIndex) || prevFingerprints[prevIndex] != hash)
                changed.Add(index);
        }
    }

    static unsafe void HashPath(
        NativeArray<Guid> guids,
        NativeArray<int> origins,
        NativeArray<int> parents,
        int node,
        NativeList<Guid> path,
        ref Hash128 hash)
    {
        int origin = origins[node];
        int state = origin < 0 ? 0 : (origin == node ? 1 : 2);
        SpookyHash.Hash128(&state, sizeof(int), ref hash);

        path.Clear();
        int cur = node;
        int guard = parents.Length + 1;
        while (guard-- > 0)
        {
            int next = parents[cur];
            if (next < 0)
                break;

            path.Add(guids[next]);
            cur = next;
        }

        if (path.Length > 0)
        {
            var size = path.Length * UnsafeUtility.SizeOf<Guid>();
            SpookyHash.Hash128(NativeListUnsafeUtility.GetUnsafePtr(path), size, ref hash);
        }
    }
    #endregion

    internal static NodeFlags GetFlags(in this SatelliteState state)
    {
        var flags = NodeFlags.None;
        if (state.Powered) flags |= NodeFlags.Powered;
        if (state.CanRelaySignal) flags |= NodeFlags.CanRelay;
        if (state.IsInRadioBlackout) flags |= NodeFlags.InBlackout;
        if (state.IsCommandStation) flags |= NodeFlags.IsCommandStation;
        return flags;
    }
}

[BurstCompile]
struct NetworkUpdateJob : IJob
{
    public JobConfig config;
    public NetworkUpdate.VisibilityState visibility;

    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<NetworkUpdate.RawVesselInfo> vessels;
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<NetworkUpdate.RawCelestialBodyInfo> cbs;
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<NetworkUpdate.RawStationInfo> stations;
    public StateManager.NativePointerArray<AntennaData> antennaData;

    public NativeArray<JobNode> nodes;
    [WriteOnly] public NativeArray<Guid> guids;
    public NativeArray<JobBody> bodies;
    public NativeList<JobAntenna> antennas;
    public NativeList<double3> directions;
    public NativeArray<double> ranges;
    public NativeHashMap<Guid, int> satmap;
    public NativeList<ConeCandidate> coneCandidates;
    public NativeList<SatelliteMarkCandidate> markCandidates;

    public void Execute()
    {
        using var _guard1 = antennaData;

        var cbmap = new NativeHashMap<Guid, int>(bodies.Length, Allocator.Temp);

        NetworkUpdate.ComputeBodyInfo(cbs, bodies, cbmap);
        NetworkUpdate.ComputeVesselInfo(vessels, nodes, satmap, cbmap);
        NetworkUpdate.ComputeVesselVisibility(visibility, vessels, nodes);
        NetworkUpdate.ComputeGroundStationInfo(stations, nodes, vessels.Length, cbmap);

        var mapping = NetworkUpdate.ComputeOverallMapping(cbmap, satmap);
        NetworkUpdate.ComputeAntennaInfo(nodes, antennas, antennaData, mapping);

        directions.ResizeUninitialized(antennas.Length);
        NetworkUpdate.ComputeAntennaDirections(nodes, bodies, antennas, directions);
        NetworkUpdate.ComputeVesselMaxRanges(in config, nodes, antennas, ranges);
        NetworkUpdate.ComputeConeCandidates(antennas.AsArray(), coneCandidates);
        NetworkUpdate.ComputeMarkCandidates(vessels, stations, markCandidates);

        for (int i = 0; i < nodes.Length; ++i)
            guids[i] = nodes[i].guid;
    }
}

[BurstCompile]
struct NetworkAdjacencyJob : IJob
{
    public JobConfig config;

    public NativeArray<JobNode> nodes;
    public NativeArray<NetworkEdge> edges;

    public NativeList<IntRange> ranges;
    public NativeList<int> adjacency;
    public NativeList<double> distances;
    public NativeBitArray connected;

    public NativeList<int> commandStations;
    public NativeList<int> groundStations;

    public void Execute()
    {
        ranges.ResizeUninitialized(nodes.Length);

        NetworkUpdate.ComputeAdjacencyLists(
            in config,
            nodes,
            edges,
            ranges,
            adjacency,
            distances);
        NetworkUpdate.ComputeVesselConnected(ranges, connected);
        NetworkUpdate.ComputeNetworkRoots(nodes, commandStations, groundStations);
    }
}

[BurstCompile]
struct NetworkFingerprintJob : IJob
{
    [ReadOnly]
    [DeallocateOnJobCompletion]
    public NativeArray<Guid> guids;
    [ReadOnly] public NativeArray<int> originCs;
    [ReadOnly] public NativeArray<int> parentCs;
    [ReadOnly] public NativeArray<int> originGs;
    [ReadOnly] public NativeArray<int> parentGs;

    [ReadOnly] public NativeHashMap<Guid, int> prevSatmap;
    [ReadOnly] public NativeArray<Hash128> prevFingerprints;

    public NativeList<Hash128> fingerprints;
    public NativeList<int> changed;

    public void Execute()
    {
        fingerprints.ResizeUninitialized(guids.Length);

        NetworkUpdate.ComputeRouteFingerprints(
            guids,
            originCs,
            parentCs,
            originGs,
            parentGs,
            prevSatmap,
            prevFingerprints,
            fingerprints.AsArray(),
            changed);
    }
}
