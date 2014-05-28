using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    using AdjacencyTable = Dictionary<ISatellite, AdjacencyMap>;
    using ConnectionTable = Dictionary<ISatellite, ConnectionMap>;
    public class NetworkManager : IEnumerable<ISatellite>
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(NetworkManager));
        /// <summary>
        /// Triggers when an edge is added to the network graph.
        /// </summary>
        public event Action<NetworkLink<ISatellite>> LinkAdded = delegate { };
        /// <summary>
        /// Triggers when an edge is removed from the network graph.
        /// </summary>
        public event Action<NetworkLink<ISatellite>> LinkRemoved = delegate { };

        /// <summary>
        /// Triggers when a connection is added.
        /// </summary>
        public event Action<ISatellite, ISatellite> ConnectionAdded = delegate { };
        /// <summary>
        /// Triggers when a connection is removed.
        /// </summary>
        public event Action<ISatellite, ISatellite> ConnectionRemoved = delegate { };

        /// <summary>
        /// A dictionary of the Ground Stations as defined in the configuration file.
        /// </summary>
        public Dictionary<Guid, ISatellite> GroundStations { get; private set; }
        /// <summary>
        /// The network graph as an adjacency list. Describes the satellite's neighbours by outgoing NetworkLink.
        public AdjacencyTable Graph { get; private set; }

        /// <summary>
        /// The total amount of satellites in the current network.
        /// </summary>
        public int Count { get { return RTCore.Instance.Satellites.Count + GroundStations.Count; } }

        public IRangeModel RangeModel { get; set; }
        public NetworkPathfinder<ISatellite> PathFinder { get; private set; }

        /// <summary>
        /// Gets the <see cref="ISatellite"/> with the specified Guid.
        /// </summary>
        /// <value>
        /// The <see cref="ISatellite"/>. Returns null if there is no satellite with the specified identifier.
        /// </value>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        public ISatellite this[Guid guid]
        {
            get
            {
                ISatellite gs;
                return RTCore.Instance.Satellites[guid] ??
                       (GroundStations.TryGetValue(guid, out gs) ? gs : null);
            }
        }

        /// <summary>
        /// Retrieves all cached connections (start -> finish) of the specified satellite.
        /// </summary>
        /// <value>
        /// Returns an empty dictionary if no connections exist.
        /// </value>
        /// <param name="sat">The satellite. May be null.</param>
        /// <returns>All cached connections as a dictionary.</returns>
        public ConnectionMap this[ISatellite sat]
        {
            get
            {
                ConnectionMap result;
                return (sat != null && connectionCache.TryGetValue(sat, out result)) 
                    ? result 
                    : new ConnectionMap();
            }
        }

        private const int TotalRefreshTicks = 100;

        private int currentTick;
        private int currentIndex;
        private readonly ConnectionTable connectionCache = new ConnectionTable();

        public NetworkManager()
        {
            Graph = new AdjacencyTable();
            PathFinder = new NetworkPathfinder<ISatellite>(FindNeighbors, RangeModelExtensions.DistanceTo);
            RangeModel = RangeModelBuilder.Create(RTSettings.Instance.RangeModelType);

            // Load all ground stations into a dictionary;
            GroundStations = new Dictionary<Guid, ISatellite>();
            foreach (ISatellite sat in RTSettings.Instance.GroundStations)
            {
                try
                {
                    GroundStations.Add(sat.Guid, sat);
                    OnSatelliteRegister(sat);
                }
                catch (ArgumentException e) // Already exists.
                {
                    Logger.Error("A ground station cannot be loaded: " + e.Message);
                }
            }

            // Events
            RTCore.Instance.Satellites.OnRegister += OnSatelliteRegister;
            RTCore.Instance.Satellites.OnUnregister += OnSatelliteUnregister;
        }

        public void Dispose()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.OnRegister -= OnSatelliteRegister;
                RTCore.Instance.Satellites.OnUnregister -= OnSatelliteUnregister;
            }
        }

        private void UpdateConnections(ISatellite commandStation)
        {
            var connections = PathFinder.GenerateConnections(commandStation);
            foreach (var satellite in this)
            {
                NetworkRoute<ISatellite> connection;
                if (connections.TryGetValue(satellite, out connection))
                {
                    bool contains = connectionCache[satellite].ContainsKey(commandStation);
                    connectionCache[satellite][commandStation] = connection;
                    if (!contains) ConnectionAdded.Invoke(satellite, commandStation);
                }
                else
                {
                    if (connectionCache[satellite].Remove(commandStation))
                        ConnectionRemoved.Invoke(satellite, commandStation);
                }
            }
        }

        private IEnumerable<NetworkLink<ISatellite>> FindNeighbors(ISatellite sat_a)
        {
            if (!sat_a.IsPowered) return Enumerable.Empty<NetworkLink<ISatellite>>();
            return Graph[sat_a].Values;
        }

        private NetworkLink<ISatellite> GetLink(ISatellite a, ISatellite b)
        {
            if (a == null || b == null || a == b) return null;
            if (!a.HasLineOfSightWith(b) && !CheatOptions.InfiniteEVAFuel) return null;

            return RangeModel.GetLink(a, b);
        }

        private void UpdateGraph(ISatellite a, ISatellite b)
        {
            var link = GetLink(a, b);
            bool contains = Graph[a].ContainsKey(b) || Graph[b].ContainsKey(a);
            if (link == null)
            {
                if (!contains) return;
                Graph[a].Remove(b);
                Graph[b].Remove(a);
                LinkRemoved(NetworkLink.Empty(a, b));
            }
            else
            {
                Graph[a][b] = link;
                Graph[b][a] = link.Invert();
                if (!contains) LinkAdded(link);
            }
        }

        private void UpdateGraph()
        {
            var source = RTCore.Instance.Network;
            var count = source.Count * source.Count - source.Count;
            if (count == 0) return;
            int baseline = (count / TotalRefreshTicks);
            int takeCount = baseline + (((currentTick++ % TotalRefreshTicks) < (count - baseline * TotalRefreshTicks)) ? 1 : 0);
            var commandStations = RTCore.Instance.Satellites.FindCommandStations().Concat(GroundStations.Values);
            foreach (var a in source.WrapAround().Skip(currentIndex).Take(takeCount))
            {
                // Update Graph
                foreach (var b in source)
                {
                    // This skips pairs of XX or wrong lexicographic ordering (process XY but not YX, so all non-repeating combinations).
                    if (b == a) break;
                    UpdateGraph(a, b);
                }

                // TODO: Make a lookup table for command stations.
                if (GroundStations.ContainsKey(a.Guid) || commandStations.Contains(a))
                {
                    UpdateConnections(a);
                }
            }

            currentIndex += takeCount;
            currentIndex = currentIndex % count;
        }

        public void OnFixedUpdate()
        {
            UpdateGraph();
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            Logger.Info("SatelliteUnregister({0})", s);
            Graph.Remove(s);
            foreach (var pair in Graph)
            {
                if (pair.Value.Remove(s)) LinkRemoved(NetworkLink.Empty(pair.Key, s));
            }
            connectionCache.Remove(s);
            foreach (var pair in connectionCache)
            {
                if (pair.Value.Remove(s)) ConnectionRemoved.Invoke(pair.Key, s);
            }
        }

        private void OnSatelliteRegister(ISatellite s)
        {
            Logger.Info("SatelliteRegister({0})", s);
            Graph[s] = new AdjacencyMap();
            connectionCache[s] = new ConnectionMap();
        }

        public IEnumerator<ISatellite> GetEnumerator()
        {
            return RTCore.Instance.Satellites.Cast<ISatellite>().Concat(GroundStations.Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class MissionControlSatellite : ISatellite, IPersistenceLoad
    {
        /* Config Node parameters */
        [Persistent] private String Guid = new Guid("5105f5a9d62841c6ad4b21154e8fc488").ToString();
        [Persistent] private String Name = "Mission Control";
        [Persistent] private double Latitude = -0.1313315f;
        [Persistent] private double Longitude = -74.59484f;
        [Persistent] private double Height = 75.0f;
        [Persistent] private int Body = 1;
        [Persistent(collectionIndex = "ANTENNA")] private MissionControlAntenna[] Antennas = new MissionControlAntenna[] { new MissionControlAntenna() };

        bool ISatellite.IsPowered { get { return true; } }
        bool ISatellite.IsVisible { get { return true; } }
        String ISatellite.Name { get { return Name; } }
        Guid ISatellite.Guid { get { return guid; } }
        Group ISatellite.Group { get { return Group.Empty; } set { } }
        Vector3d ISatellite.Position { get { return FlightGlobals.Bodies[Body].GetWorldSurfacePosition(Latitude, Longitude, Height); } }
        bool ISatellite.IsCommandStation { get { return true; } }
        bool ISatellite.HasLocalControl { get { return false; } }
        ICelestialBody ISatellite.Body { get { return (CelestialBodyProxy) FlightGlobals.Bodies[Body]; } } // FIXME TODO HACK WTF
        IEnumerable<IAntenna> ISatellite.Antennas { get { return Antennas; } }

        private Guid guid;
        private int hash;

        void IPersistenceLoad.PersistenceLoad()
        {
            foreach (var antenna in Antennas)
            {
                antenna.Parent = this;
            }
            this.guid = new Guid(Guid);
            this.hash = guid.GetHashCode();
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override String ToString()
        {
            return Name;
        }

    }
}