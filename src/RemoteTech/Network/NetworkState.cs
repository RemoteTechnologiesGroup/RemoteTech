using System;
using System.Collections.Generic;
using RemoteTech.Collections;
using RemoteTech.SimpleTypes;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace RemoteTech.Network;

internal class NetworkState : IDisposable
{
    JobHandle handle;

    /// <summary>
    /// A handle for consumers of this state. It won't block <see cref="Complete" />
    /// but it will be completed before the arrays are disposed of.
    /// </summary>
    JobHandle dependentHandle;

    ISatellite[] satellites;

    // The settings snapshot this state was built from; reused by point-to-point
    // queries so they gate hops the same way the tick's routing did.
    JobConfig config;

    // This maps guid to satellite index
    NativeHashMap<Guid, int> satmap;
    NativeArray<JobNode> nodes;
    NativeArray<JobBody> bodies;
    NativeArray<NetworkEdge> edges;

    // Whether individual antennas are connected.
    NativeBitArray connected;

    // Network topology relative to the ground stations
    NativeArray<double> scoreGs;
    NativeArray<int> parentGs;
    NativeArray<int> originGs;

    // Network topology relative to all command stations
    NativeArray<double> scoreCs;
    NativeArray<int> parentCs;
    NativeArray<int> originCs;

    // This tick's per-node route fingerprint (used to diff against the previous
    // tick's state without a per-satellite dictionary walk).
    NativeList<Hash128> fingerprints;

    // Antenna candidates for rendering the cone mesh
    NativeList<ConeCandidate> coneCandidates;

    // Ground stations and command-station vessels eligible for a map-view mark
    NativeList<SatelliteMarkCandidate> markCandidates;

    // Node indices (this tick's indexing) whose fingerprint differs from the
    // previous tick's — see ScheduleFingerprintDiff / FireConnectionRefresh.
    NativeList<int> changedNodes;

    readonly Dictionary<long, NetworkLink<ISatellite>[]> routeCache = [];

    private NetworkState() { }

    static readonly ProfilerMarker CompleteMarker = new("NetworkState.Complete");
    public void Complete()
    {
        using var scope = CompleteMarker.Auto();
        handle.Complete();
    }

    public void Dispose()
    {
        Dispose(JobHandle.CombineDependencies(this.handle, dependentHandle));
    }

    public JobHandle Dispose(JobHandle deps)
    {
        var handle = new NetworkStateDisposeJob { state = new ObjectHandle<NetworkState>(this) }
            .Schedule(JobHandle.CombineDependencies(deps, this.handle, dependentHandle));
        return handle;
    }

    void DisposeInternal()
    {
        satmap.Dispose();
        nodes.Dispose();
        edges.Dispose();
        connected.Dispose();

        scoreGs.Dispose();
        parentGs.Dispose();
        originGs.Dispose();

        scoreCs.Dispose();
        parentCs.Dispose();
        originCs.Dispose();

        fingerprints.Dispose();
        changedNodes.Dispose();

        bodies.Dispose();
        coneCandidates.Dispose();
        markCandidates.Dispose();
    }

    void AddDependentHandle(JobHandle handle)
    {
        dependentHandle = JobHandle.CombineDependencies(dependentHandle, handle);
    }

    static readonly ProfilerMarker ScheduleNetworkUpdateMarker = new("NetworkState.ScheduleNetworkUpdate");

    /// <summary>
    /// Kick off all jobs needed to compute the current network state.
    /// </summary>
    /// <param name="previous">
    /// The state from the previous tick, or null. Used to determine which
    /// satellite connections have been changed.
    /// </param>
    public static NetworkState ScheduleNetworkUpdate(NetworkManager manager, NetworkState previous)
    {
        using var scope = ScheduleNetworkUpdateMarker.Auto();
        var cbs = FlightGlobals.Bodies;

        var handles = new NativeList<JobHandle>(16, Allocator.Temp);

        var vesselCount = RTCore.Instance.Satellites.Count;
        var groundStationCount = manager.GroundStations.Count;
        var nodeCount = vesselCount + groundStationCount;
        int edgeCount = NetworkUpdateMath.PairCount(nodeCount);

        var rawVessels = new NativeArray<NetworkUpdate.RawVesselInfo>(
            vesselCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var satHandle = new ObjectHandle<ArrayMap<Guid, VesselSatellite>>(
            RTCore.Instance.Satellites.SatelliteCache);
        var recordVessels = new NetworkUpdate.RecordVesselInfoJob
        {
            satellites = satHandle,
            infos = rawVessels
        }.ScheduleBatch(vesselCount, 24);

        var rawStations = new NativeArray<NetworkUpdate.RawStationInfo>(
            groundStationCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var stationHandle = new ObjectHandle<ArrayMap<Guid, ISatellite>>(
            manager.GroundStations);
        var recordStations = new NetworkUpdate.RecordGroundStationsJob
        {
            stations = stationHandle,
            states = rawStations
        }.Schedule();

        var rawCbs = new NativeArray<NetworkUpdate.RawCelestialBodyInfo>(
            cbs.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var recordCbs = new NetworkUpdate.RecordCelestialBodiesJob
        {
            bodies = new(cbs),
            infos = rawCbs
        }.Schedule();

        var syncHandle = JobHandle.CombineDependencies(recordVessels, recordStations);
        JobHandle.ScheduleBatchedJobs();

        handles.Add(satHandle.Dispose(recordVessels));
        handles.Add(stationHandle.Dispose(recordStations));

        JobConfig config = new()
        {
            omniClamp = RTSettings.Instance.OmniRangeClampFactor,
            dishClamp = RTSettings.Instance.DishRangeClampFactor,
            multipleAntennaMultiplier = RTSettings.Instance.MultipleAntennaMultiplier,
            ignoreLineOfSight = RTSettings.Instance.IgnoreLineOfSight,
            signalRelayEnabled = RTSettings.Instance.SignalRelayEnabled,
        };
        NetworkUpdate.VisibilityState visibility = new()
        {
            ActiveVessel = FlightGlobals.ActiveVessel?.id ?? default,
            TargetVessel = FlightGlobals.fetch?.VesselTarget?.GetVessel().id ?? default,
            filter = MapViewFiltering.Instance.IsNotNullOrDestroyed()
                ? MapViewFiltering.vesselTypeFilter
                : null
        };

        var nodes = new NativeArray<JobNode>(
            nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var guids = new NativeArray<Guid>(
            nodeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var bodies = new NativeArray<JobBody>(
            cbs.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var ranges = new NativeArray<double>(
            nodeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var antennas = new NativeList<JobAntenna>(Allocator.TempJob);
        var directions = new NativeList<double3>(Allocator.TempJob);
        var coneCandidates = new NativeList<ConeCandidate>(0, Allocator.Persistent);
        var markCandidates = new NativeList<SatelliteMarkCandidate>(0, Allocator.Persistent);
        var satmap = new NativeHashMap<Guid, int>(vesselCount, Allocator.Persistent);
        var update1 = new NetworkUpdateJob
        {
            config = config,
            visibility = visibility,
            vessels = rawVessels,
            cbs = rawCbs,
            stations = rawStations,
            antennaData = StateManager.Antennas.GetStateArray(Allocator.TempJob),

            nodes = nodes,
            guids = guids,
            bodies = bodies,
            antennas = antennas,
            directions = directions,
            ranges = ranges,
            satmap = satmap,
            coneCandidates = coneCandidates,
            markCandidates = markCandidates,
        }.Schedule(JobHandle.CombineDependencies(syncHandle, recordCbs));
        StateManager.Antennas.AddUseHandle(update1);

        var edges = new NativeArray<NetworkEdge>(
            edgeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var buildEdges = new BuildEdgesJob
        {
            config = config,
            model = RTSettings.Instance.RangeModelType,
            nodes = nodes,
            bodies = bodies,
            antennas = antennas.AsDeferredJobArray(),
            vesselMaxRange = ranges,
            antennaDirections = directions.AsDeferredJobArray(),
            edges = edges
        }.Schedule(edgeCount, 256, update1);

        var adjranges = new NativeList<IntRange>(Allocator.TempJob);
        var adjacency = new NativeList<int>(Allocator.TempJob);
        var distances = new NativeList<double>(Allocator.TempJob);
        var connected = new NativeBitArray(nodeCount, Allocator.Persistent);

        var commandStations = new NativeList<int>(Allocator.TempJob);
        var groundStations = new NativeList<int>(Allocator.TempJob);
        var canTransit = new NativeBitArray(nodeCount, Allocator.TempJob);

        var update2 = new NetworkAdjacencyJob
        {
            config = config,
            nodes = nodes,
            edges = edges,

            ranges = adjranges,
            adjacency = adjacency,
            distances = distances,
            connected = connected,
            canTransit = canTransit,

            commandStations = commandStations,
            groundStations = groundStations,
        }.Schedule(buildEdges);

        var scoreGs = new NativeArray<double>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var scoreCs = new NativeArray<double>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var parentGs = new NativeArray<int>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var parentCs = new NativeArray<int>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var originGs = new NativeArray<int>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var originCs = new NativeArray<int>(nodeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        var dijkstraDeps = update2;

        var dijkstraCs = new MultiSourceDijkstraJob
        {
            ranges = adjranges.AsDeferredJobArray(),
            adjacency = adjacency.AsDeferredJobArray(),
            distances = distances.AsDeferredJobArray(),
            roots = commandStations.AsDeferredJobArray(),
            canTransit = canTransit,

            scores = scoreCs,
            parents = parentCs,
            origins = originCs,
        }.Schedule(dijkstraDeps);
        var dijkstraGs = new MultiSourceDijkstraJob
        {
            ranges = adjranges.AsDeferredJobArray(),
            adjacency = adjacency.AsDeferredJobArray(),
            distances = distances.AsDeferredJobArray(),
            roots = groundStations.AsDeferredJobArray(),
            canTransit = canTransit,

            scores = scoreGs,
            parents = parentGs,
            origins = originGs,
        }.Schedule(dijkstraDeps);
        var dijkstraAll = JobHandle.CombineDependencies(dijkstraCs, dijkstraGs);

        handles.Add(new DisposeTempContainersJob
        {
            antennas = antennas,
            directions = directions,
            ranges = ranges,
            adjranges = adjranges,
            adjacency = adjacency,
            distances = distances,
            commandStations = commandStations,
            groundStations = groundStations,
            canTransit = canTransit,
        }.Schedule(dijkstraAll));

        var fingerprints = new NativeList<Hash128>(0, Allocator.Persistent);
        var changedNodes = new NativeList<int>(0, Allocator.Persistent);

        NativeHashMap<Guid, int> prevSatmap;
        NativeArray<Hash128> prevFingerprints;
        var fingerprintDeps = dijkstraAll;
        if (previous is not null)
        {
            prevSatmap = previous.satmap;
            prevFingerprints = previous.fingerprints.AsDeferredJobArray();
            fingerprintDeps = JobHandle.CombineDependencies(fingerprintDeps, previous.handle);
        }
        else
        {
            prevSatmap = new NativeHashMap<Guid, int>(1, Allocator.Temp);
            prevFingerprints = default;
        }

        var fingerprintHandle = new NetworkFingerprintJob
        {
            guids = guids,
            originCs = originCs,
            parentCs = parentCs,
            originGs = originGs,
            parentGs = parentGs,
            prevSatmap = prevSatmap,
            prevFingerprints = prevFingerprints,
            fingerprints = fingerprints,
            changed = changedNodes,
        }.Schedule(fingerprintDeps);

        if (previous is not null)
            previous.AddDependentHandle(fingerprintHandle);
        else
            fingerprintHandle = prevSatmap.Dispose(fingerprintHandle);

        handles.Add(fingerprintHandle);

        JobHandle.ScheduleBatchedJobs();
        var satellites = GetSatellites(manager);
        syncHandle.Complete();

        return new()
        {
            handle = JobHandle.CombineDependencies(handles.AsArray()),
            satellites = satellites,
            config = config,

            satmap = satmap,
            nodes = nodes,
            edges = edges,
            connected = connected,

            scoreGs = scoreGs,
            parentGs = parentGs,
            originGs = originGs,

            scoreCs = scoreCs,
            parentCs = parentCs,
            originCs = originCs,

            fingerprints = fingerprints,
            changedNodes = changedNodes,

            bodies = bodies,
            coneCandidates = coneCandidates,
            markCandidates = markCandidates,
        };
    }

    /// <summary>Fires <c>ISatellite.OnConnectionRefresh</c> for every node whose route
    /// fingerprint changed since the previous tick. Call after <see cref="Complete"/>.</summary>
    internal void FireConnectionRefresh(NetworkManager manager)
    {
        for (int i = 0; i < changedNodes.Length; i++)
        {
            var sat = satellites[changedNodes[i]];
            if (sat != null)
                sat.OnConnectionRefresh(manager[sat]);
        }
    }

    private static ISatellite[] GetSatellites(NetworkManager manager)
    {
        var vessels = RTCore.Instance.Satellites.SatelliteCache.Values;
        var ground = manager.GroundStations.Values;

        var satellites = new ISatellite[vessels.Length + ground.Length];

        for (int i = 0; i < vessels.Length; ++i)
            satellites[i] = vessels[i];
        ground.CopyTo(satellites.AsSpan().Slice(vessels.Length));

        return satellites;
    }

    /// <summary>
    /// Indicates whether the vessel containing this antenna is connected.
    /// </summary>
    /// <param name="antenna"></param>
    /// <returns></returns>
    public bool IsAntennaConnected(IAntenna antenna)
    {
        if (antenna is null)
            return false;
        if (!satmap.TryGetValue(antenna.Guid, out var index))
            return false;
        if (!connected.IsSet(index))  
            return false;
        if (!antenna.Activated)
            return false;
        return antenna.Omni > 0 || antenna.Dish > 0;
    }

    internal bool TryGetNode(ISatellite sat, out int node)
    {
        if (sat != null) return satmap.TryGetValue(sat.Guid, out node);
        node = -1;
        return false;
    }

    internal ISatellite SatAt(int node) => satellites[node];

    internal int NodeCount => nodes.Length;
    internal int PairCount => edges.Length;

    internal NativeArray<JobNode> Nodes => nodes;

    /// <summary>
    /// Builds this tick's map-view connection-line mesh (edge selection/colouring
    /// through billboard-quad geometry); only <paramref name="p"/>/<paramref name="view"/>
    /// vary per frame.
    /// </summary>
    internal LineMeshData ScheduleLineMesh(in DrawEdgeParams p, in LineMeshViewParams view)
    {
        var drawEdges = new NativeList<DrawEdge>(0, Allocator.TempJob);
        var verts = new NativeList<LineVertex>(0, Allocator.TempJob);
        var indices = new NativeList<uint>(0, Allocator.TempJob);
        var bounds = new NativeArray<Bounds3>(1, Allocator.TempJob);

        JobHandle h1 = new BuildDrawEdgesJob
        {
            edges = edges,
            parent = parentCs,
            nodes = nodes,
            p = p,
            outEdges = drawEdges,
            verts = verts,
            indices = indices,
        }.Schedule(handle);

        JobHandle h2 = new LineMeshJob
        {
            edges = drawEdges.AsDeferredJobArray(),
            nodes = nodes,
            verts = verts.AsDeferredJobArray(),
            indices = indices.AsDeferredJobArray(),
            invScale = view.invScale,
            totalOffset = view.totalOffset,
            view = view.view,
            proj = view.proj,
            invView = view.invView,
            invProj = view.invProj,
            pixelWidth = view.pixelWidth,
            pixelHeight = view.pixelHeight,
            halfWidth = view.halfWidth,
            mode = view.mode,
        }.Schedule(drawEdges, 1024, h1);

        JobHandle h3 = new LineBoundsJob
        {
            verts = verts.AsDeferredJobArray(),
            boundsOut = bounds,
        }.Schedule(h2);

        AddDependentHandle(h3);

        return new LineMeshData
        {
            drawEdges = drawEdges,
            verts = verts,
            indices = indices,
            bounds = bounds,
            handle = h3,
            builtOffset = view.totalOffset,
        };
    }

    internal int ConeCandidateCount => coneCandidates.Length;

    /// <summary>
    /// Builds this tick's map-view dish-cone mesh from the candidates
    /// <see cref="NetworkUpdate.ComputeConeCandidates"/> gathered during the tick;
    /// like <see cref="ScheduleLineMesh"/> it billboards against
    /// <see cref="ConeMeshData.builtOffset"/>, so the draw must translate onto the
    /// current ScaledSpace origin.
    /// </summary>
    internal ConeMeshData ScheduleConeMesh(in ConeViewParams p)
    {
        int count = coneCandidates.Length;
        var verts = new NativeArray<LineVertex>(8 * count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var indices = new NativeArray<uint>(12 * count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var bounds = new NativeArray<Bounds3>(1, Allocator.TempJob);

        JobHandle h1 = new ConeMeshJob
        {
            candidates = coneCandidates.AsArray(),
            nodes = nodes,
            bodies = bodies,
            p = p,
            verts = verts,
            indices = indices,
        }.Schedule(count, 128, handle);

        JobHandle h2 = new LineBoundsJob
        {
            verts = verts,
            boundsOut = bounds,
        }.Schedule(h1);

        AddDependentHandle(h2);

        return new ConeMeshData { verts = verts, indices = indices, bounds = bounds, handle = h2, builtOffset = p.totalOffset };
    }

    internal int MarkCandidateCount => markCandidates.Length;

    /// <summary>
    /// Filters this tick's mark candidates to the ones visible in the map view and
    /// projects each to its GUI-space screen position; the renderer draws the
    /// survivors in OnGUI.
    /// </summary>
    internal SatelliteMarkData ScheduleSatelliteMarks(in SatelliteMarkViewParams p)
    {
        var marks = new NativeList<SatelliteMark>(markCandidates.Length, Allocator.TempJob);

        JobHandle h = new SatelliteMarkJob
        {
            candidates = markCandidates.AsArray(),
            nodes = nodes,
            bodies = bodies,
            p = p,
            marks = marks,
        }.Schedule(handle);

        AddDependentHandle(h);

        return new SatelliteMarkData { marks = marks, handle = h };
    }

    /// <summary>
    /// Get the distance between the requested satellite and its control point.
    /// </summary>
    /// <param name="sat"></param>
    /// <param name="groundOnly">Only consider ground stations, not vessels.</param>
    /// <returns>The length of network route, in meters.</returns>
    public double GetRouteLength(ISatellite sat, bool groundOnly)
    {
        if (!satmap.TryGetValue(sat.Guid, out int index))
            return double.PositiveInfinity;

        return RouteLength(index, groundOnly);
    }

    internal double RouteLength(int node, bool groundOnly)
    {
        var origin = GetOrigin(node, groundOnly);
        if (origin < 0)
            return double.PositiveInfinity;

        return groundOnly ? scoreGs[node] : scoreCs[node];
    }

    /// <summary>
    /// True even for a root's own zero-hop self-route; see <see cref="RouteExists"/> for the >=1-hop version.
    /// </summary>
    /// <param name="sat"></param>
    /// <param name="groundOnly">Only consider ground stations, not vessels.</param>
    public bool GetRouteExists(ISatellite sat, bool groundOnly)
    {
        if (!satmap.TryGetValue(sat.Guid, out int index))
            return false;

        return groundOnly
            ? originGs[index] >= 0
            : originCs[index] >= 0;
    }

    /// <summary>
    /// Unlike <see cref="GetRouteExists"/>, false for a root's own zero-hop self-route.
    /// </summary>
    internal bool RouteExists(int node, bool groundOnly)
    {
        var origin = GetOrigin(node, groundOnly);
        return origin >= 0 && origin != node;
    }

    /// <summary>
    /// Get the control station that is currently controlling this satellite.
    /// </summary>
    /// <param name="sat"></param>
    /// <param name="groundOnly">Only consider ground stations, not vessels.</param>
    public ISatellite GetRouteOrigin(ISatellite sat, bool groundOnly)
    {
        if (!satmap.TryGetValue(sat.Guid, out int index))
            return null;

        return RouteGoalSat(index, groundOnly);
    }

    internal ISatellite RouteGoalSat(int node, bool groundOnly)
    {
        var origin = GetOrigin(node, groundOnly);
        return origin >= 0 ? satellites[origin] : null;
    }

    private static long HopKey(int node, bool groundOnly) =>
        ((long)node << 1) | (groundOnly ? 1L : 0L);

    private static readonly NetworkLink<ISatellite>[] NoHops = [];

    /// <summary>
    /// Returns the network route from this satellite to its control station,
    /// or null if there is no route.
    /// </summary>
    /// <param name="sat"></param>
    /// <param name="groundOnly">Only consider ground stations, not vessels.</param>
    /// <returns></returns>
    public IReadOnlyList<NetworkLink<ISatellite>> GetRoute(ISatellite sat, bool groundOnly)
    {
        if (!satmap.TryGetValue(sat.Guid, out int index))
            return null;
        if (GetOrigin(index, groundOnly) < 0)
            return null;

        return BuildHops(index, groundOnly);
    }

    /// <summary>
    /// Never null, unlike <see cref="GetRoute"/> (empty when unreachable or a root's self-route).
    /// </summary>
    internal IReadOnlyList<NetworkLink<ISatellite>> RouteHops(int node, bool groundOnly) =>
        BuildHops(node, groundOnly);

    private IReadOnlyList<NetworkLink<ISatellite>> BuildHops(int index, bool groundOnly)
    {
        var cacheKey = HopKey(index, groundOnly);
        if (routeCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var origin = GetOrigin(index, groundOnly);
        if (origin < 0 || origin == index)
            return routeCache[cacheKey] = NoHops;

        int start = index;
        int count = 0;

        while (true)
        {
            var parent = GetParent(index, groundOnly);
            if (parent < 0)
                break;

            count += 1;
            index = parent;
        }

        var links = new NetworkLink<ISatellite>[count];
        index = start;
        for (int i = 0; i < count; ++i)
        {
            var parent = GetParent(index, groundOnly);
            links[i] = BuildLink(index, parent);
            index = parent;
        }

        return routeCache[cacheKey] = links;
    }

    private int GetOrigin(int index, bool groundOnly) =>
        groundOnly ? originGs[index] : originCs[index];

    private int GetParent(int index, bool groundOnly) =>
        groundOnly ? parentGs[index] : parentCs[index];

    private NetworkLink<ISatellite> BuildLink(int fromIdx, int toIdx)
    {
        var port = LinkType.None;
        if (TryGetEdge(fromIdx, toIdx, out var edge) && edge.valid)
            port = edge.linkType;

        return new NetworkLink<ISatellite>(satellites[toIdx], port);
    }

    /// <summary>
    /// Direct links out of <paramref name="sat"/> (its adjacency row).
    /// </summary>
    public List<NetworkLink<ISatellite>> GetLinks(ISatellite sat)
    {
        if (!TryGetNode(sat, out int node))
            return [];

        return GetLinksByNode(node);
    }

    private List<NetworkLink<ISatellite>> GetLinksByNode(int node)
    {
        var result = new List<NetworkLink<ISatellite>>();
        if (!connected.IsSet(node))
            return result;

        for (int j = 0; j < satellites.Length; j++)
        {
            if (j == node)
                continue;
            if (TryGetEdge(node, j, out var edge) && edge.valid)
                result.Add(new NetworkLink<ISatellite>(satellites[j], edge.linkType));
        }
        return result;
    }

    /// <summary>
    /// Enumerates the whole adjacency graph (inspection/debug seam).
    /// </summary>
    internal IEnumerable<KeyValuePair<Guid, List<NetworkLink<ISatellite>>>> EnumerateLinks()
    {
        for (int i = 0; i < satellites.Length; i++)
        {
            var sat = satellites[i];
            if (sat == null)
                continue;
            yield return new KeyValuePair<Guid, List<NetworkLink<ISatellite>>>(sat.Guid, GetLinksByNode(i));
        }
    }

    /// <summary>
    /// O(1) shortest signal delay for <paramref name="sat"/> (optionally to a
    /// ground station). +inf if unreachable, 0 if signal delay is disabled.
    /// </summary>
    public double ShortestDelay(ISatellite sat, bool groundOnly = false)
    {
        if (!TryGetNode(sat, out int index))
            return double.PositiveInfinity;

        var origin = GetOrigin(index, groundOnly);
        if (origin < 0)
            return double.PositiveInfinity;
        if (!RTSettings.Instance.EnableSignalDelay)
            return 0.0;

        var length = groundOnly ? scoreGs[index] : scoreCs[index];
        return length / RTSettings.Instance.SpeedOfLight;
    }

    /// <summary>
    /// Shortest signal delay between two specific satellites. Unlike
    /// <see cref="ShortestDelay"/> (which routes to the nearest station), this is
    /// a point-to-point query solved on demand over the tick's link graph. +inf
    /// if either is untracked or no route connects them, 0 if signal delay is
    /// disabled.
    /// </summary>
    public double ShortestDelayBetween(ISatellite a, ISatellite b)
    {
        if (a is null || b is null)
            return double.PositiveInfinity;
        if (!satmap.TryGetValue(a.Guid, out int src) || !satmap.TryGetValue(b.Guid, out int dst))
            return double.PositiveInfinity;

        var length = RouteLengthBetween(src, dst);
        if (double.IsPositiveInfinity(length))
            return double.PositiveInfinity;
        if (!RTSettings.Instance.EnableSignalDelay)
            return 0.0;

        return length / RTSettings.Instance.SpeedOfLight;
    }

    internal unsafe double RouteLengthBetween(int source, int target)
    {
        handle.Complete();

        double length = double.PositiveInfinity;
        new NetworkPathfindJob
        {
            config = config,
            source = source,
            target = target,
            nodes = nodes,
            edges = edges,
            length = &length,
        }.Run();

        return length;
    }

    /// <summary>
    /// The connection routes for <paramref name="sat"/> (best path to any
    /// command/ground station and, if different, to a ground station), shortest
    /// first.
    /// </summary>
    public List<NetworkRoute<ISatellite>> BuildConnections(ISatellite sat)
    {
        var list = new List<NetworkRoute<ISatellite>>(2);
        if (!TryGetNode(sat, out int node))
            return list;

        int rootAny = GetOrigin(node, false);
        int rootGs = GetOrigin(node, true);

        if (rootAny >= 0) list.Add(new NetworkRoute<ISatellite>(this, node, groundOnly: false));
        if (rootGs >= 0 && (rootAny < 0 || rootGs != rootAny))
            list.Add(new NetworkRoute<ISatellite>(this, node, groundOnly: true));
        list.Sort();
        return list;
    }

    /// <summary>Max range the two satellites' antennas could achieve, independent of their
    /// current separation. 0 if untracked, or if no antenna pairing currently reaches.</summary>
    public double GetMaxRangeDistance(ISatellite satA, ISatellite satB)
    {
        if (satA == null || satB == null)
            return 0.0;
        if (!satmap.TryGetValue(satA.Guid, out int a) || !satmap.TryGetValue(satB.Guid, out int b))
            return 0.0;
        if (a == b)
            return 0.0;

        return TryGetEdge(a, b, out var edge) && edge.valid ? edge.maxRange : 0.0;
    }

    // `edges` is a flat triangular-pair table (NetworkUpdateMath.DecodePairIndex scheme).
    private bool TryGetEdge(int a, int b, out NetworkEdge edge)
    {
        int lo = math.min(a, b);
        int hi = math.max(a, b);
        int pairIdx = hi * (hi - 1) / 2 + lo;

        if ((uint)pairIdx < (uint)edges.Length)
        {
            edge = edges[pairIdx];
            return true;
        }

        edge = default;
        return false;
    }



    struct NetworkStateDisposeJob : IJob
    {
        public ObjectHandle<NetworkState> state;

        public void Execute()
        {
            using var guard = state;
            state.Target.DisposeInternal();
        }
    }

    /// <summary>
    /// Disposes the per-tick scratch containers that don't outlive the update in
    /// a single job, sparing the scheduler one dispose job per container.
    /// </summary>
    [BurstCompile]
    struct DisposeTempContainersJob : IJob
    {
        public NativeList<JobAntenna> antennas;
        public NativeList<double3> directions;
        public NativeArray<double> ranges;
        public NativeList<IntRange> adjranges;
        public NativeList<int> adjacency;
        public NativeList<double> distances;
        public NativeList<int> commandStations;
        public NativeList<int> groundStations;
        public NativeBitArray canTransit;

        public void Execute()
        {
            antennas.Dispose();
            directions.Dispose();
            ranges.Dispose();
            adjranges.Dispose();
            adjacency.Dispose();
            distances.Dispose();
            commandStations.Dispose();
            groundStations.Dispose();
            canTransit.Dispose();
        }
    }
}

