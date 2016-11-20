using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Extensions;
using RemoteTech.Modules;
using RemoteTech.RangeModel;
using RemoteTech.Settings;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech
{
    /// <summary>
    /// Class managing the satellites network.
    /// Acts as a list of vessels in one or more networks.
    /// </summary>
    public partial class NetworkManager : IEnumerable<ISatellite>
    {
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkAdd = delegate { };
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkRemove = delegate { };

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public Dictionary<Guid, ISatellite> GroundStations { get; private set; }
        public Dictionary<Guid, List<NetworkLink<ISatellite>>> Graph { get; private set; }

        public int Count { get { return RTCore.Instance.Satellites.Count + GroundStations.Count; } }

        public static Guid ActiveVesselGuid = new Guid(CoreSettingsManager.Instance.ActiveVesselGuid);

        public ISatellite this[Guid guid]
        {
            get
            {
                Vessel activeVessel = (FlightGlobals.ActiveVessel == null && HighLogic.LoadedScene == GameScenes.TRACKSTATION 
                    ? MapView.MapCamera.target.vessel : FlightGlobals.ActiveVessel);

                ISatellite vesselSatellite = RTCore.Instance.Satellites[guid];
                ISatellite activeSatellite = (guid == ActiveVesselGuid ? RTCore.Instance.Satellites[activeVessel] : null);
                ISatellite groundSatellite = (GroundStations.ContainsKey(guid) ? GroundStations[guid] : null);

                return vesselSatellite ?? activeSatellite ?? groundSatellite;
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
            foreach (ISatellite sat in CoreSettingsManager.Instance.GroundStations)
            {
                try
                {
                    GroundStations.Add(sat.Guid, sat);
                    OnSatelliteRegister(sat);
                }
                catch (Exception e) // Already exists.
                {
					RTLog.Notify("A ground station cannot be loaded: " + e.Message, RTLogLevel.LVL1);
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
            mConnectionCache[start].Sort((a, b) => a.Length.CompareTo(b.Length));
            start.OnConnectionRefresh(this[start]);
        }

        public IEnumerable<NetworkLink<ISatellite>> FindNeighbors(ISatellite s)
        {
            if (!s.Powered) return Enumerable.Empty<NetworkLink<ISatellite>>();
            return Graph[s.Guid].Where(l => l.Target.Powered);
        }

        private void UpdateGraph(ISatellite a)
        {
            var result = this.Select(b => GetLink(a, b)).Where(link => link != null).ToList();

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
            bool los = sat_a.HasLineOfSightWith(sat_b) || CheatOptions.InfinitePropellant;
            if (!los) return null;

            switch (CoreSettingsManager.Instance.RangeModelType)
            {
                case RangeModel.RangeModel.Additive: // NathanKell
                    return RangeModelRoot.GetLink(sat_a, sat_b);
                default: // Stock range model
                    return RangeModelStandard.GetLink(sat_a, sat_b);
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

        /// <summary>Gets the position of a RemoteTech target from its id</summary>
        /// <returns>The absolute position or null if <paramref name="targetable"/> is neither 
        /// a satellite nor a celestial body.</returns>
        /// <param name="targetable">The id of the satellite or celestial body whose position is 
        ///     desired. May be the active vessel Guid.</param>
        /// 
        /// <exceptsafe>The program state is unchanged in the event of an exception.</exceptsafe>
        internal Vector3d? GetPositionFromGuid(Guid targetable)
        {
            ISatellite targetSat = this[targetable];
            if (targetSat != null) {
                return targetSat.Position;
            }

            if (Planets.ContainsKey(targetable)) {
                return Planets[targetable].position;
            }

            return null;
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
        [Persistent] private Color MarkColor = new Color(0.996078f, 0, 0, 1);
        [Persistent(collectionIndex = "ANTENNA")] private MissionControlAntenna[] Antennas = { new MissionControlAntenna() };

        private bool AntennaActivated = true;

        bool ISatellite.Powered { get { return this.AntennaActivated; } }
        bool ISatellite.Visible { get { return true; } }
        String ISatellite.Name { get { return Name; } set { Name = value; } }
        Guid ISatellite.Guid { get { return mGuid; } }
        Vector3d ISatellite.Position { get { return FlightGlobals.Bodies[Body].GetWorldSurfacePosition(Latitude, Longitude, Height); } }
        bool ISatellite.IsCommandStation { get { return true; } }
        bool ISatellite.HasLocalControl { get { return false; } }
        bool ISatellite.isVessel { get { return false; } }
        Vessel ISatellite.parentVessel { get { return null; } }
        CelestialBody ISatellite.Body { get { return FlightGlobals.Bodies[Body]; } }
        Color ISatellite.MarkColor { get { return MarkColor; } }
        IEnumerable<IAntenna> ISatellite.Antennas { get { return Antennas; } }

        public Guid mGuid { get; private set; }
        public IEnumerable<IAntenna> MissionControlAntennas { get { return Antennas; } }


        void ISatellite.OnConnectionRefresh(List<NetworkRoute<ISatellite>> route) { }

        public MissionControlSatellite()
        {
            this.mGuid = new Guid(Guid);
        }

        public void reloadUpgradeableAntennas(int techlvl = 0)
        {
            foreach (var antenna in this.Antennas)
            {
                antenna.reloadUpgradeableAntennas(techlvl);
            }
        }
		/*
		 * Simple getter + setter. 
		 * For being able to add groundstations.
		 */
		public void SetDetails(String name, double lat, double longi, double height, int body)
		{
			this.Name = name;
			this.Latitude = lat;
			this.Longitude = longi;
			this.Height = height;
			this.Body = body;
			this.mGuid = System.Guid.NewGuid ();
			this.Guid = this.mGuid.ToString ();
		}

		public String GetDetails()
		{
			return String.Format ("name:{0}, lat={1}, long={2}, height={3}, body={4}", this.Name, this.Latitude, this.Longitude, this.Height, this.Body);
		}
        
        public String GetName()
        {
            return this.Name;
        }

        void IPersistenceLoad.PersistenceLoad()
        {
            foreach (var antenna in Antennas)
            {
                antenna.Parent = this;
            }
            mGuid = new Guid(Guid);
        }

        public override String ToString()
        {
            return Name;
        }

        /// <summary>
        /// Used currently for debug purposes only. This method can be used to shut down the mission control
        /// </summary>
        /// <param name="powerswitch">true=Missioncontrol on, false=MissionControl off</param>
        public void togglePower(bool powerswitch)
        {
            this.AntennaActivated = powerswitch;
        }

        /// <summary>
        /// Adds a new ground station to the list. 
        /// </summary>
        /// <param name="name">Name of the ground station</param>
        /// <param name="latitude">Latitude position</param>
        /// <param name="longitude">Longitude position</param>
        /// <param name="height">Height above sea level</param>
        /// <param name="body">Reference body 1=Kerbin etc...</param>
        /// <returns>A new <see cref="System.Guid"/> if a new station was successfully added otherwise a Guid.Empty.</returns>
        public static Guid AddGroundStation(string name, double latitude, double longitude, double height, int body)
        {
            RTLog.Notify("Trying to add ground station({0})", RTLogLevel.LVL1, name);

            var newGroundStation = new MissionControlSatellite();
            newGroundStation.SetDetails(name, latitude, longitude, height, body);

            //
            var groundStations = CoreSettingsManager.Instance.GroundStations;


            // Already on the list?
            var foundGroundStation = groundStations.FirstOrDefault(ms => ms.GetDetails().Equals(newGroundStation.GetDetails()));
            if (foundGroundStation != null)
            {
                RTLog.Notify("Ground station already exists!");
                return System.Guid.Empty;
            }

            groundStations.Add(newGroundStation);
            CoreSettingsManager.Instance.Save();

            return newGroundStation.mGuid;
        }

        /// <summary>Removes a ground station from the list by its unique <paramref name="stationid"/>.</summary>
        /// <param name="stationid">Unique ground station id</param>
        /// <returns>Returns true for a successful removed station, otherwise false.</returns>
        public static bool RemoveGroundStation(Guid stationid)
        {
            RTLog.Notify("Trying to remove ground station {0}", RTLogLevel.LVL1, stationid);

            var groundStations = CoreSettingsManager.Instance.GroundStations;
            for (var i = 0; i < groundStations.Count; i++)
            {
                if (!groundStations[i].mGuid.Equals(stationid))
                    continue;

                RTLog.Notify("Removing {0} ", RTLogLevel.LVL1, groundStations[i].GetName());
                groundStations.RemoveAt(i);
                CoreSettingsManager.Instance.Save();
                return true;
            }

            RTLog.Notify("Cannot find station {0}", RTLogLevel.LVL1, stationid);
            return false;
        }
    }
}