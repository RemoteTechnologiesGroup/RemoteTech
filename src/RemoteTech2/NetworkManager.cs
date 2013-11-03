using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class NetworkManager : IEnumerable<ISatellite>, IConfigNode
    {
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkAdd = delegate { };
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkRemove = delegate { };

        public int Count { get { return RTCore.Instance.Satellites.Count + 1; } }

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }
        public Dictionary<Guid, List<NetworkLink<ISatellite>>> Graph { get; private set; }

        public Guid ActiveVesselGuid = new Guid("35b89a0d664c43c6bec8d0840afc97b2");

        public ISatellite this[Guid guid]
        {
            get
            {
                if (guid == MissionControl.Guid)
                    return MissionControl;
                if (guid == ActiveVesselGuid)
                    return RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                return RTCore.Instance.Satellites[guid];
            }
        }

        public List<NetworkRoute<ISatellite>> this[ISatellite sat]
        {
            get
            {
                if (sat == null) return new List<NetworkRoute<ISatellite>>();
                return mConnectionCache.ContainsKey(sat) ? mConnectionCache[sat] : new List<NetworkRoute<ISatellite>>();
            }
            set
            {
                mConnectionCache[sat] = value;
            }
        }

        public void Load(ConfigNode node)
        {

        }

        public void Save(ConfigNode node)
        {

        }

        private const int REFRESH_TICKS = 50;

        private int mTick;
        private int mTickIndex;
        private Dictionary<ISatellite, List<NetworkRoute<ISatellite>>> mConnectionCache = new Dictionary<ISatellite, List<NetworkRoute<ISatellite>>>();

        public NetworkManager()
        {
            MissionControl = new MissionControlSatellite();
            Graph = new Dictionary<Guid, List<NetworkLink<ISatellite>>>();
            Planets = new Dictionary<Guid, CelestialBody>();

            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                Planets[cb.Guid()] = cb;
            }

            OnSatelliteRegister(MissionControl);

            RTCore.Instance.Satellites.OnRegister += OnSatelliteRegister;
            RTCore.Instance.Satellites.OnUnregister += OnSatelliteUnregister;
            RTCore.Instance.OnPhysicsUpdate += OnPhysicsUpdate;
        }

        public void Dispose()
        {
            RTCore.Instance.OnPhysicsUpdate -= OnPhysicsUpdate;
            RTCore.Instance.Satellites.OnRegister -= OnSatelliteRegister;
            RTCore.Instance.Satellites.OnUnregister -= OnSatelliteUnregister;
        }

        public static float Distance(ISatellite a, NetworkLink<ISatellite> b)
        {
            return Vector3.Distance(a.Position, b.Target.Position);
        }

        public static float Distance(ISatellite a, ISatellite b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }

        public void FindPath(ISatellite start, IEnumerable<ISatellite> commandStations)
        {
            var paths = new List<NetworkRoute<ISatellite>>();
            foreach (ISatellite root in commandStations.Concat(new[] { MissionControl }).Where(r => r != start))
            {
                paths.Add(NetworkPathfinder.Solve(start, root, s => Graph[s.Guid].Where(l => l.Target.Powered), Distance, Distance));
            }
            this[start] = paths.Where(p => p.Exists).ToList();
            this[start].Sort((a,b) => a.Delay.CompareTo(b.Delay));
            start.OnConnectionRefresh(this[start]);
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

        private NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            bool los = LineOfSight(sat_a, sat_b) || CheatOptions.InfiniteEVAFuel;
            if (sat_a == sat_b || !los) return null;

            float distance = Distance(sat_a, sat_b);

            var omni_a = sat_a.Antennas.Where(a => a.Omni > distance);
            var omni_b = sat_b.Antennas.Where(b => b.Omni > distance);
            var dish_a = sat_a.Antennas.Where(a => (a.Target == sat_b.Guid || (a.Target == RTCore.Instance.Network.ActiveVesselGuid &&
                                                                               FlightGlobals.ActiveVessel != null &&
                                                                               sat_b.Guid == FlightGlobals.ActiveVessel.id) &&
                                                    a.Dish > distance));
            var dish_b = sat_b.Antennas.Where(b => (b.Target == sat_a.Guid || (b.Target == RTCore.Instance.Network.ActiveVesselGuid && 
                                                                               FlightGlobals.ActiveVessel != null && 
                                                                               sat_a.Guid == FlightGlobals.ActiveVessel.id) &&
                                                    b.Dish > distance));

            var planets = RTCore.Instance.Network.Planets;
            var planet_a = sat_a.Antennas.Where(a => 
            {
                if (!planets.ContainsKey(a.Target) || sat_b.Body != planets[a.Target]) return false;
                if (a.Dish < distance) return false;
                Vector3 dir_cb = (planets[a.Target].position - sat_a.Position);
                Vector3 dir_b = (sat_b.Position - sat_a.Position);
                if (Vector3.Dot(dir_cb.normalized, dir_b.normalized) >= a.Radians) return true;
                return false;
            });
            var planet_b = sat_b.Antennas.Where(b =>
            {
                if (!planets.ContainsKey(b.Target) || sat_a.Body != planets[b.Target]) return false;
                if (b.Dish < distance) return false;
                Vector3 dir_cb = (planets[b.Target].position - sat_b.Position);
                Vector3 dir_b = (sat_a.Position - sat_b.Position);
                if (Vector3.Dot(dir_cb.normalized, dir_b.normalized) >= b.Radians) return true;
                return false;
            });

            var conn_a = omni_a.Concat(dish_a).Concat(planet_a).FirstOrDefault();
            var conn_b = omni_b.Concat(dish_b).Concat(planet_b).FirstOrDefault();
            if (conn_a != null && conn_b != null)
            {
                var interfaces = omni_a.Concat(dish_a).Concat(planet_a).ToList();
                var type = LinkType.Omni;
                if (dish_a.Concat(planet_a).Contains(conn_a) || dish_b.Concat(planet_b).Contains(conn_b)) type = LinkType.Dish;
                return new NetworkLink<ISatellite>(sat_b, interfaces, type);
            }
            return null;
        }

        private static bool LineOfSight(ISatellite a, ISatellite b)
        {
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - a.Position;
                Vector3d bFromA = b.Position - a.Position;
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;
                Vector3d bFromAnorm = bFromA.normalized;
                if (Vector3d.Dot(bodyFromA, bFromAnorm) >= bFromA.magnitude) continue;
                Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                if (lateralOffset.magnitude < referenceBody.Radius - 5) return false;
            }
            return true;
        }

        public void OnPhysicsUpdate()
        {
            if (RTCore.Instance.Satellites.Count == 0) return;
            int takeCount = (RTCore.Instance.Satellites.Count / REFRESH_TICKS) + (((mTick++ % (REFRESH_TICKS / (RTCore.Instance.Satellites.Count + 1))) == 0) ? 1 : 0);
            IEnumerable<ISatellite> commandStations = RTCore.Instance.Satellites.FindCommandStations();
            foreach (VesselSatellite s in RTCore.Instance.Satellites.Concat(RTCore.Instance.Satellites).Skip(mTickIndex).Take(takeCount))
            {
                UpdateGraph(s);
                RTUtil.Log("{0} [ E: {1} ]", s.ToString(), Graph[s.Guid].ToDebugString());
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
            RTUtil.Log("NetworkManager: SatelliteUnregister({0}, {1})", s, s.Guid);
            Graph.Remove(s.Guid);
            foreach (var list in Graph.Values)
            {
                list.RemoveAll(l => l.Target == s);
            }
            mConnectionCache.Remove(s);
        }

        private void OnSatelliteRegister(ISatellite s)
        {
            RTUtil.Log("NetworkManager: SatelliteRegister({0}, {1})", s, s.Guid);
            Graph[s.Guid] = new List<NetworkLink<ISatellite>>();
        }

        public IEnumerator<ISatellite> GetEnumerator()
        {
            return RTCore.Instance.Satellites.Cast<ISatellite>().Concat(new[] { MissionControl }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class MissionControlSatellite : ISatellite
    {
        public bool Powered { get { return true; } }
        public bool Visible { get { return true; } }
        public String Name { get { return "Mission Control"; } set { return; } }
        public Guid Guid { get { return new Guid("5105f5a9d62841c6ad4b21154e8fc488"); } }
        public Vector3 Position { get { return FlightGlobals.Bodies[1].GetWorldSurfacePosition(-0.1313315, -74.59484, 75.0151197366649); } }
        public CelestialBody Body { get { return FlightGlobals.Bodies[1]; } }
        public IEnumerable<IAntenna> Antennas { get; private set; }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> route)
        {
            ;
        }

        public MissionControlSatellite()
        {
            var antennas = new List<IAntenna>();
            antennas.Add(new ProtoAntenna("Dummy Antenna", Guid, 75000000));
            Antennas = antennas;
        }


        public override String ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public bool IsCommandStation { get { return true; } }
        public bool HasLocalControl { get { return false; } }
    }
}