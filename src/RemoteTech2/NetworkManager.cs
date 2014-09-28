using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    [Flags]
    public enum RangeModel
    {
        Standard,
        Additive,
        Root = Additive,
    }

    public partial class NetworkManager : IEnumerable<ISatellite>
    {
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkAdd = delegate { };
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkRemove = delegate { };

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public Dictionary<Guid, ISatellite> GroundStations { get; private set; }
        public Dictionary<Guid, List<NetworkLink<ISatellite>>> Graph { get; private set; }

        public int Count { get { return RTCore.Instance.Satellites.Count + GroundStations.Count; } }

        public static Guid ActiveVesselGuid = new Guid(RTSettings.Instance.ActiveVesselGuid);

        public ISatellite this[Guid guid]
        {
            get
            {
                return RTCore.Instance.Satellites[guid] ??
                       ((guid == ActiveVesselGuid) ? RTCore.Instance.Satellites[FlightGlobals.ActiveVessel] : null) ??
                       (GroundStations.ContainsKey(guid) ? GroundStations[guid] : null);
            }
        }

        public List<NetworkRoute<ISatellite>> this[ISatellite sat]
        {
            get
            {
                if (sat == null) return new List<NetworkRoute<ISatellite>>();
                return mConnectionCache.ContainsKey(sat) ? mConnectionCache[sat] : new List<NetworkRoute<ISatellite>>();
            }
        }

        private const int REFRESH_TICKS = 50;

        private int mTick;
        private int mTickIndex;
        private Dictionary<ISatellite, List<NetworkRoute<ISatellite>>> mConnectionCache = new Dictionary<ISatellite, List<NetworkRoute<ISatellite>>>();

        public NetworkManager()
        {
            Graph = new Dictionary<Guid, List<NetworkLink<ISatellite>>>();

            // Load all planets into a dictionary;
            Planets = new Dictionary<Guid, CelestialBody>();
            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                Planets[cb.Guid()] = cb;
            }

            // Load all ground stations into a dictionary;
            GroundStations = new Dictionary<Guid, ISatellite>();
            foreach (ISatellite sat in RTSettings.Instance.GroundStations)
            {
                try
                {
                    GroundStations.Add(sat.Guid, sat);
                    OnSatelliteRegister(sat);
                }
                catch (Exception e) // Already exists.
                {
                    RTLog.Notify("A ground station cannot be loaded: " + e.Message);
                }
            }

            RTCore.Instance.Satellites.OnRegister += OnSatelliteRegister;
            RTCore.Instance.Satellites.OnUnregister += OnSatelliteUnregister;
            RTCore.Instance.OnPhysicsUpdate += OnPhysicsUpdate;
        }

        public void Dispose()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.OnPhysicsUpdate -= OnPhysicsUpdate;
                RTCore.Instance.Satellites.OnRegister -= OnSatelliteRegister;
                RTCore.Instance.Satellites.OnUnregister -= OnSatelliteUnregister;
            }
        }

        public void FindPath(ISatellite start, IEnumerable<ISatellite> commandStations)
        {
            var paths = new List<NetworkRoute<ISatellite>>();
            foreach (ISatellite root in commandStations.Concat(GroundStations.Values).Where(r => r != start))
            {
                paths.Add(NetworkPathfinder.Solve(start, root, FindNeighbors, RangeModelExtensions.DistanceTo, RangeModelExtensions.DistanceTo));
            }
            mConnectionCache[start] = paths.Where(p => p.Exists).ToList();
            mConnectionCache[start].Sort((a, b) => a.Delay.CompareTo(b.Delay));
            start.OnConnectionRefresh(this[start]);
        }

        public IEnumerable<NetworkLink<ISatellite>> FindNeighbors(ISatellite s)
        {
            if (!s.Powered) return Enumerable.Empty<NetworkLink<ISatellite>>();
            return Graph[s.Guid].Where(l => l.Target.Powered);
        }

        private void UpdateGraph(ISatellite a)
        {
            var result = new List<NetworkLink<ISatellite>>();

            foreach (ISatellite b in this)
            {
                var link = GetLink(a, b);
                if (link == null) continue;
                result.Add(link);
            }

            // Send events for removed edges
            foreach (var link in Graph[a.Guid].Except(result))
            {
                OnLinkRemove(a, link);
            }

            Graph[a.Guid].Clear();

            // Input new edges
            foreach (var link in result)
            {
                Graph[a.Guid].Add(link);
                OnLinkAdd(a, link);
            }
        }

        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            if (sat_a == null || sat_b == null || sat_a == sat_b) return null;
            bool los = sat_a.HasLineOfSightWith(sat_b) || CheatOptions.InfiniteEVAFuel;
            if (!los) return null;

            switch (RTSettings.Instance.RangeModelType)
            {
                default:
                case RangeModel.Standard: // Stock range model
                    return RangeModelStandard.GetLink(sat_a, sat_b);
                case RangeModel.Additive: // NathanKell
                    return RangeModelRoot.GetLink(sat_a, sat_b);
            }
        }

        public void OnPhysicsUpdate()
        {
            var count = RTCore.Instance.Satellites.Count;
            if (count == 0) return;
            int baseline = (count / REFRESH_TICKS);
            int takeCount = baseline + (((mTick++ % REFRESH_TICKS) < (count - baseline * REFRESH_TICKS)) ? 1 : 0);
            IEnumerable<ISatellite> commandStations = RTCore.Instance.Satellites.FindCommandStations();
            foreach (VesselSatellite s in RTCore.Instance.Satellites.Concat(RTCore.Instance.Satellites).Skip(mTickIndex).Take(takeCount))
            {
                UpdateGraph(s);
                //("{0} [ E: {1} ]", s.ToString(), Graph[s.Guid].ToDebugString());
                if (s.SignalProcessor.VesselLoaded || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    FindPath(s, commandStations);
                }
            }
            mTickIndex += takeCount;
            mTickIndex = mTickIndex % RTCore.Instance.Satellites.Count;
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            RTLog.Notify("NetworkManager: SatelliteUnregister({0})", s);
            Graph.Remove(s.Guid);
            foreach (var list in Graph.Values)
            {
                list.RemoveAll(l => l.Target == s);
            }
            mConnectionCache.Remove(s);
        }

        private void OnSatelliteRegister(ISatellite s)
        {
            RTLog.Notify("NetworkManager: SatelliteRegister({0})", s);
            Graph[s.Guid] = new List<NetworkLink<ISatellite>>();
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

        bool ISatellite.Powered { get { return true; } }
        bool ISatellite.Visible { get { return true; } }
        String ISatellite.Name { get { return Name; } set { Name = value; } }
        Guid ISatellite.Guid { get { return this.mGuid; } }
        Vector3d ISatellite.Position { get { return FlightGlobals.Bodies[Body].GetWorldSurfacePosition(Latitude, Longitude, Height); } }
        bool ISatellite.IsCommandStation { get { return true; } }
        bool ISatellite.HasLocalControl { get { return false; } }
        CelestialBody ISatellite.Body { get { return FlightGlobals.Bodies[Body]; } }
        IEnumerable<IAntenna> ISatellite.Antennas { get { return Antennas; } }
        private Guid mGuid;

        void ISatellite.OnConnectionRefresh(List<NetworkRoute<ISatellite>> route) { }

        public MissionControlSatellite()
        {
            this.mGuid = new Guid(this.Guid);
        }

        void IPersistenceLoad.PersistenceLoad()
        {
            foreach (var antenna in Antennas)
            {
                antenna.Parent = this;
            }
            this.mGuid = new Guid(this.Guid);
        }

        public override String ToString()
        {
            return Name;
        }

    }
}