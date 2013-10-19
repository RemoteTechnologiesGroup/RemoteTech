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

        public ISatellite this[Guid guid]
        {
            get
            {
                if (guid == MissionControl.Guid)
                    return MissionControl;
                return RTCore.Instance.Satellites[guid];
            }
        }

        public List<NetworkRoute<ISatellite>> this[ISatellite sat]
        {
            get
            {
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
            var omni_b = sat_b.Antennas.Where(a => a.Omni > distance);
            var dish_a = sat_a.Antennas.Where(a => a.Target == sat_b.Guid && a.Dish > distance);
            var dish_b = sat_a.Antennas.Where(a => a.Target == sat_a.Guid && a.Dish > distance);

            if (omni_a.Concat(dish_a).Any() && omni_b.Concat(dish_b).Any())
            {
                var optimal = omni_a.Concat(dish_a).Min();
                var type = dish_a.Contains(optimal) ? LinkType.Dish : LinkType.Omni;
                return new NetworkLink<ISatellite>(sat_b, optimal, type);
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
                RTUtil.Log("{0} [ CS: {1}, E: {2} ]", s.ToString(), commandStations.Contains(s), Graph[s.Guid].ToDebugString());
                if (s.SignalProcessor.VesselLoaded)
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
            antennas.Add(new ProtoAntenna("Dummy Antenna", Guid, 150000));
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
    }
}