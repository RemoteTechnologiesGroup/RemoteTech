using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.Modules;
using RemoteTech.Network;
using RemoteTech.RangeModel;
using RemoteTech.SimpleTypes;
using UnityEngine;
using KSP.Localization;
using RemoteTech.Collections;

namespace RemoteTech
{
    /// <summary>
    /// Class managing the satellites network.
    /// Acts as a list of vessels in one or more networks.
    /// </summary>
    public partial class NetworkManager : IEnumerable<ISatellite>
    {
        public ArrayMap<Guid, CelestialBody> Planets { get; private set; } = new();
        public ArrayMap<Guid, ISatellite> GroundStations { get; private set; } = new();

        public static Guid ActiveVesselGuid => RTSettings.Instance.ActiveVesselGuidParsed;

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
                if (sat == null || current == null) return new List<NetworkRoute<ISatellite>>();
                // Cache is cleared whenever _current changes (see OnPhysicsUpdate).
                if (mConnectionCache.TryGetValue(sat, out var cached)) return cached;
                var built = current.BuildConnections(sat);
                mConnectionCache[sat] = built;
                return built;
            }
        }

        private readonly Dictionary<ISatellite, List<NetworkRoute<ISatellite>>> mConnectionCache = new Dictionary<ISatellite, List<NetworkRoute<ISatellite>>>();

        private NetworkState current;
        private NetworkState next;

        private static readonly List<NetworkLink<ISatellite>> EmptyLinks = [];

        // --- Read seam ------------------------------------------------------
        // All consumers go through these accessors rather than touching Graph /
        // the connection cache directly, so the backing representation can be
        // swapped (toward native/burst-array storage) without touching callers.

        /// <summary>
        /// Direct links out of <paramref name="sat"/> (its adjacency row).
        /// </summary>
        public IReadOnlyList<NetworkLink<ISatellite>> GetLinks(ISatellite sat) => current?.GetLinks(sat) ?? EmptyLinks;

        /// <summary>
        /// Whether <paramref name="antenna"/> is an interface on any current link
        /// of its owning satellite — i.e. the antenna's "connected" state.
        /// </summary>
        public bool IsAntennaConnected(IAntenna antenna) => current?.IsAntennaConnected(antenna) ?? false;

        /// <summary>
        /// O(1) "does <paramref name="sat"/> have a working connection" check
        /// (optionally restricted to ground-station routes), served from the
        /// current state without materializing any route. Equivalent to
        /// <c>this[sat].Any()</c>.
        /// </summary>
        public bool IsConnected(ISatellite sat, bool groundOnly = false) => current?.GetRouteExists(sat, groundOnly) ?? false;

        /// <summary>
        /// O(1) shortest signal delay for <paramref name="sat"/> (optionally to a
        /// ground station). +inf if unreachable, 0 if signal delay is disabled.
        /// Equivalent to <c>this[sat].Min().Delay</c>.
        /// </summary>
        public double ShortestDelay(ISatellite sat, bool groundOnly = false) => current?.ShortestDelay(sat, groundOnly) ?? double.PositiveInfinity;

        /// <summary>
        /// This tick's route from <paramref name="sat"/> to its controlling station
        /// (optionally restricted to ground stations), ordered from the satellite
        /// outward. Null if there is no such route.
        /// </summary>
        public IReadOnlyList<NetworkLink<ISatellite>> GetRoute(ISatellite sat, bool groundOnly = false) => current?.GetRoute(sat, groundOnly);

        /// <summary>
        /// Shortest signal delay between two specific satellites (a point-to-point
        /// query), as opposed to <see cref="ShortestDelay"/> which routes to the
        /// nearest station. +inf if unreachable, 0 if signal delay is disabled.
        /// </summary>
        public double ShortestDelayBetween(ISatellite a, ISatellite b) => current?.ShortestDelayBetween(a, b) ?? double.PositiveInfinity;

        /// <summary>
        /// Enumerates the whole adjacency graph (inspection/debug seam).
        /// </summary>
        internal IEnumerable<KeyValuePair<Guid, List<NetworkLink<ISatellite>>>> EnumerateLinks() =>
            current?.EnumerateLinks() ?? Enumerable.Empty<KeyValuePair<Guid, List<NetworkLink<ISatellite>>>>();

        /// <summary>
        /// This tick's state — freshest positions, but not yet completed; callers pay for <see cref="NetworkState.Complete"/> if it's still running.
        /// </summary>
        internal NetworkState Next => next;

        public NetworkManager()
        {
            // Load all planets into a dictionary;
            foreach (CelestialBody cb in FlightGlobals.Bodies)
                Planets.Add(cb.Guid(), cb);

            // Load all ground stations into a dictionary;
            foreach (MissionControlSatellite station in RTSettings.Instance.GroundStations)
            {
                try
                {
                    ISatellite sat = station;
                    GroundStations.Add(sat.Guid, sat);
                    OnSatelliteRegister(sat);
                    station.RegisterAntennaStates();
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
            foreach (ISatellite sat in GroundStations.Values)
            {
                if (sat is MissionControlSatellite station)
                    station.UnregisterAntennaStates();
            }
            current?.Dispose();
            next?.Dispose();
        }

        public static NetworkLink<ISatellite>? GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            if (sat_a == null || sat_b == null || sat_a == sat_b) return null;
            if (sat_a.IsInRadioBlackout || sat_b.IsInRadioBlackout) return null;
            bool los = sat_a.HasLineOfSightWith(sat_b) || RTSettings.Instance.IgnoreLineOfSight;
            if (!los) return null;

            switch (RTSettings.Instance.RangeModelType)
            {
                case RangeModel.RangeModel.Additive: // NathanKell
                    return RangeModelRoot.GetLink(sat_a, sat_b);
                default: // Stock range model
                    return RangeModelStandard.GetLink(sat_a, sat_b);
            }
        }

        public void OnPhysicsUpdate()
        {
            if (RTCore.Instance.Satellites.Count == 0) return;
            if (HighLogic.LoadedScene != GameScenes.TRACKSTATION &&
                HighLogic.LoadedScene != GameScenes.FLIGHT &&
                !(HighLogic.LoadedScene == GameScenes.SPACECENTER && API.API.enabledInSPC))
                return;

            foreach (ISatellite sat in GroundStations.Values)
            {
                if (sat is MissionControlSatellite station)
                    station.UpdateAntennaStates();
            }

            // Complete() runs last so _current's job has had a full tick to run in the background.
            current?.Dispose();
            current = next;
            mConnectionCache.Clear();
            next = NetworkState.ScheduleNetworkUpdate(this, current);
            current?.Complete();
            current?.FireConnectionRefresh(this);
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            RTLog.Notify("NetworkManager: SatelliteUnregister({0})", s);
            // The graph/routes are rebuilt wholesale each tick from the store, so
            // we only need to drop the satellite's lazily-cached route list. The
            // renderer clears its own edges via its OnSatelliteUnregister handler.
            mConnectionCache.Remove(s);
        }

        private void OnSatelliteRegister(ISatellite s)
        {
            RTLog.Notify("NetworkManager: SatelliteRegister({0})", s);
        }

        public IEnumerator<ISatellite> GetEnumerator()
        {
            return RTCore.Instance.Satellites.Cast<ISatellite>()
                .Concat(GroundStations.Values.ToArray())
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the position of a RemoteTech target from its id
        /// </summary>
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

    public sealed class MissionControlSatellite : ISatellite, IConfigNode
    {
        /* Config Node parameters */
        private String Guid = new Guid("5105f5a9d62841c6ad4b21154e8fc488").ToString();
        private String Name = Localizer.Format("#RT_MissionControl");//"Mission Control"
        private double Latitude = -0.1313315f;
        private double Longitude = -74.59484f;
        private double Height = 75.0f;
        private int Body = 1;
        private Color MarkColor = new Color(0.996078f, 0, 0, 1);
        private MissionControlAntenna[] Antennas = { new MissionControlAntenna() };

        private bool AntennaActivated = true;

        bool ISatellite.Powered { get { return PowerShutdownFlag ? false : this.AntennaActivated; } }
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
        IReadOnlyList<IAntenna> ISatellite.Antennas { get { return Antennas; } }
        bool ISatellite.CanRelaySignal { get { return true; } } //not sure if should relay signal. Mission Control can "do" everything isnt it?
        
        public Guid mGuid { get; private set; }
        public IEnumerable<IAntenna> MissionControlAntennas { get { return Antennas; } }
        public bool IsInRadioBlackout { get; set; } // could be EMP
        public bool PowerShutdownFlag { get; set; } // flag for third-party realism mods

        void ISatellite.OnConnectionRefresh(List<NetworkRoute<ISatellite>> route) { }

        SatelliteState ISatellite.GetState()
        {
            var self = (ISatellite)this;
            return new SatelliteState
            {
                Guid = mGuid,
                Body = RTUtil.Guid(self.Body),
                Position = self.Position,
                Powered = self.Powered,
                IsCommandStation = true,
                CanRelaySignal = true,
                IsInRadioBlackout = IsInRadioBlackout,
            };
        }

        public MissionControlSatellite()
        {
            this.mGuid = new Guid(Guid);
            foreach (var antenna in Antennas)
            {
                antenna.Parent = this;
            }
        }

        internal void RegisterAntennaStates()
        {
            foreach (var antenna in Antennas)
            {
                antenna.RegisterState();
            }
        }

        internal void UnregisterAntennaStates()
        {
            foreach (var antenna in Antennas)
            {
                antenna.UnregisterState();
            }
        }

        internal void UpdateAntennaStates()
        {
            foreach (var antenna in Antennas)
            {
                antenna.UpdateState();
            }
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

        public CelestialBody GetBody()
        {
            return FlightGlobals.Bodies[this.Body];
        }

        public void SetBodyIndex(int index)
        {
            this.Body = index;
        }

        public void Load(ConfigNode node)
        {
            node.TryGetValue("Guid", ref Guid);
            node.TryGetValue("Name", ref Name);
            node.TryGetValue("Latitude", ref Latitude);
            node.TryGetValue("Longitude", ref Longitude);
            node.TryGetValue("Height", ref Height);
            node.TryGetValue("Body", ref Body);
            node.TryGetValue("MarkColor", ref MarkColor);

            var antennas = node.GetNode("Antennas");
            if (antennas != null)
            {
                var antennaNodes = antennas.GetNodes("ANTENNA");
                Antennas = new MissionControlAntenna[antennaNodes.Length];
                for (int i = 0; i < antennaNodes.Length; i++)
                {
                    Antennas[i] = new MissionControlAntenna { Parent = this };
                    Antennas[i].Load(antennaNodes[i]);
                }
            }

            mGuid = new Guid(Guid);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("Guid", Guid);
            node.AddValue("Name", Name);
            node.AddValue("Latitude", Latitude);
            node.AddValue("Longitude", Longitude);
            node.AddValue("Height", Height);
            node.AddValue("Body", Body);
            node.AddValue("MarkColor", MarkColor);

            var antennas = node.AddNode("Antennas");
            foreach (var antenna in Antennas)
            {
                antenna.Save(antennas.AddNode("ANTENNA"));
            }
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
    }
}