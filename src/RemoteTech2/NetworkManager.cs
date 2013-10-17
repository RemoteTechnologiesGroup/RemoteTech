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
        public event Action<BidirectionalEdge<ISatellite>> OnEdgeRefresh;

        public int Count { get { return RTCore.Instance.Satellites.Count + 1; } }

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }
        public Dictionary<Guid, List<ISatellite>> Graph { get; private set; }

        public ISatellite this[Guid guid]
        {
            get
            {
                if (guid == MissionControl.Guid)
                    return MissionControl;
                return RTCore.Instance.Satellites[guid];
            }
        }

        public List<Path<ISatellite>> this[ISatellite sat]
        {
            get
            {
                return mConnectionCache.ContainsKey(sat) ? mConnectionCache[sat] : null;
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
        private Dictionary<ISatellite, List<Path<ISatellite>>> mConnectionCache = new Dictionary<ISatellite, List<Path<ISatellite>>>();

        public NetworkManager()
        {
            MissionControl = new MissionControlSatellite();
            Graph = new Dictionary<Guid, List<ISatellite>>();
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

        public static float Distance(ISatellite a, ISatellite b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }

        public void FindPath(ISatellite start, IEnumerable<ISatellite> commandStations)
        {
            var paths = new List<Path<ISatellite>>();
            foreach (ISatellite root in commandStations.Concat(new[] { MissionControl }).Where(r => r != start))
            {
                paths.Add(Pathfinder.Solve(start, root, s => Graph[s.Guid].Where(x => x.Powered), Distance, Distance));
            }
            this[start] = paths.Where(p => p.Exists).ToList();
        }

        private void UpdateGraph(ISatellite a)
        {
            var result = new List<BidirectionalEdge<ISatellite>>();

            foreach (ISatellite b in this)
            {
                var edge = GetEdge(a, b);
                if (edge.Type == LinkType.None) continue;
                result.Add(edge);
            }

            // Send events for removed edges
            foreach (ISatellite b in Graph[a.Guid])
            {
                var edge = new BidirectionalEdge<ISatellite>(a, b, LinkType.None);
                if (!result.Contains(edge))
                {
                    OnEdgeRefresh(edge);
                }
            }
            Graph[a.Guid].Clear();

            // Input new edges
            foreach (var edge in result)
            {
                Graph[a.Guid].Add(edge.B);
                OnEdgeRefresh(edge);
            }
        }

        private BidirectionalEdge<ISatellite> GetEdge(ISatellite a, ISatellite b)
        {
            bool los = LineOfSight(a, b) || CheatOptions.InfiniteEVAFuel;
            if (a == b || !los) return new BidirectionalEdge<ISatellite>(a, b, LinkType.None);

            float distance = Distance(a, b);
            if (distance < (a.OmniRange + b.OmniRange) && a.OmniRange > 0 && b.OmniRange > 0)
            {
                return new BidirectionalEdge<ISatellite>(a, b, LinkType.Omni);
            }

            float dishRange_A = a.Dishes.Where(d => d.Target == b.Guid).Max(d => (float?) d.Range) ?? 0.0f;
            float dishRange_B = b.Dishes.Where(d => d.Target == a.Guid).Max(d => (float?) d.Range) ?? 0.0f;

            foreach (Dish d in a.Dishes.Where(d => Planets.ContainsKey(d.Target) && Planets[d.Target] == b.Body))
            {
                Vector3 dir_cb = (Planets[d.Target].position - a.Position);
                Vector3 dir_b = (b.Position - a.Position);
                if (Vector3.Dot(dir_cb.normalized, dir_b.normalized) >= d.Radians)
                {
                    dishRange_A = Math.Max(dishRange_A, d.Range);
                }
            }

            foreach (Dish d in b.Dishes.Where(d => Planets.ContainsKey(d.Target) && Planets[d.Target] == a.Body))
            {
                Vector3 dir_cb = (Planets[d.Target].position - b.Position);
                Vector3 dir_a = (a.Position - b.Position);
                if (Vector3.Dot(dir_cb.normalized, dir_a.normalized) >= d.Radians)
                {
                    dishRange_B = Math.Max(dishRange_B, d.Range);
                }
            }

            if ((distance < (dishRange_A + dishRange_B) && dishRange_A > 0 && dishRange_B > 0) ||
                (distance < (a.OmniRange + dishRange_B) && a.OmniRange > 0 && dishRange_B > 0) ||
                (distance < (dishRange_A + b.OmniRange) && dishRange_A > 0 && b.OmniRange > 0))
            {
                return new BidirectionalEdge<ISatellite>(a, b, LinkType.Dish);
            }

            return new BidirectionalEdge<ISatellite>(a, b, LinkType.None);
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
            int takeCount = (RTCore.Instance.Satellites.Count / REFRESH_TICKS) + ((mTick++ % (RTCore.Instance.Satellites.Count % REFRESH_TICKS) == 0) ? 1 : 0);
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
            RTUtil.Log("{0} satellites were processed this frame.", takeCount);
            mTickIndex += takeCount % RTCore.Instance.Satellites.Count;
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            RTUtil.Log("NetworkManager: SatelliteUnregister({0}, {1})", s, s.Guid);
            Graph.Remove(s.Guid);
            foreach (var list in Graph.Values)
            {
                list.Remove(s);
            }
            mConnectionCache.Remove(s);
        }

        private void OnSatelliteRegister(ISatellite s)
        {
            RTUtil.Log("NetworkManager: SatelliteRegister({0}, {1})", s, s.Guid);
            Graph[s.Guid] = new List<ISatellite>();
        }

        private void OnEdgeUpdate(BidirectionalEdge<ISatellite> edge)
        {
            if (OnEdgeRefresh != null)
            {
                OnEdgeRefresh.Invoke(edge);
                RTUtil.Log(edge.ToString());
            }
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
        public float OmniRange { get { return 8000000; } }
        public IEnumerable<Dish> Dishes { get { return Enumerable.Empty<Dish>(); } }

        public void OnConnectionRefresh(Path<ISatellite> path)
        {
            return;
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