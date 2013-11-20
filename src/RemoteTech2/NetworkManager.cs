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
    }

    public class NetworkManager : IEnumerable<ISatellite>, IConfigNode
    {
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkAdd = delegate { };
        public event Action<ISatellite, NetworkLink<ISatellite>> OnLinkRemove = delegate { };

        public int Count { get { return RTCore.Instance.Satellites.Count + 1; } }

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }
        public Dictionary<Guid, List<NetworkLink<ISatellite>>> Graph { get; private set; }

        public static Guid ActiveVesselGuid = new Guid(RTSettings.Instance.ActiveVesselGuid);

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

        public static double Distance(ISatellite a, ISatellite b)
        {
            return Vector3d.Distance(a.Position, b.Position);
        }

        public static double Distance(ISatellite a, NetworkLink<ISatellite> b)
        {
            return Vector3d.Distance(a.Position, b.Target.Position);
        }

        public void FindPath(ISatellite start, IEnumerable<ISatellite> commandStations)
        {
            var paths = new List<NetworkRoute<ISatellite>>();
            foreach (ISatellite root in commandStations.Concat(new[] { MissionControl }).Where(r => r != start))
            {
                paths.Add(NetworkPathfinder.Solve(start, root, FindNeighbors, Distance, Distance));
            }
            this[start] = paths.Where(p => p.Exists).ToList();
            this[start].Sort((a,b) => a.Delay.CompareTo(b.Delay));
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

        // NK
        private static double MaxDistance(double min, double max, double clamp)
        {
            if (min > max)
            {
                double tmp = max;
                max = min;
                min = tmp;
            }
            return Math.Min(clamp*min, min + Math.Sqrt(min*max));
        }

        // NK consts
        public const double OMNICLAMP = 100.0;
        public const double DISHCLAMP = 1000.0;
        public const double MULTIPLEANTFRACTION = 0.25;
        // NK end
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            bool los = LineOfSight(sat_a, sat_b) || CheatOptions.InfiniteEVAFuel;
            if (sat_a == sat_b || !los) return null;

            double distance = Distance(sat_a, sat_b);

            var active_vessel = FlightGlobals.ActiveVessel;
            if (active_vessel == null && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                active_vessel = MapView.MapCamera.target.vessel;
            }

            switch (RTSettings.Instance.RangeModelType)
            {
                case RangeModel.Standard: // Stock range model
                    {
                        var omni_a = sat_a.Antennas.Where(a => a.Omni > distance);
                        var omni_b = sat_b.Antennas.Where(b => b.Omni > distance);
                        var dish_a = sat_a.Antennas.Where(a => a.Dish > distance && (a.Target == sat_b.Guid || (a.Target == ActiveVesselGuid && active_vessel != null &&
                                                                                                                sat_b.Guid == active_vessel.id)));
                        var dish_b = sat_b.Antennas.Where(b => b.Dish > distance && (b.Target == sat_a.Guid || (b.Target == ActiveVesselGuid && active_vessel != null &&
                                                                                                                sat_a.Guid == active_vessel.id)));

                        var planets = RTCore.Instance.Network.Planets;
                        var planet_a = sat_a.Antennas.Where(a => 
                        {
                            if (!planets.ContainsKey(a.Target) || sat_b.Body != planets[a.Target]) return false;
                            if (a.Dish < distance) return false;
                            var dir_cb = (planets[a.Target].position - sat_a.Position);
                            var dir_b = (sat_b.Position - sat_a.Position);
                            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= a.Radians) return true;
                            return false;
                        });
                        var planet_b = sat_b.Antennas.Where(b =>
                        {
                            if (!planets.ContainsKey(b.Target) || sat_a.Body != planets[b.Target]) return false;
                            if (b.Dish < distance) return false;
                            var dir_cb = (planets[b.Target].position - sat_b.Position);
                            var dir_b = (sat_a.Position - sat_b.Position);
                            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= b.Radians) return true;
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
                    }
                    break;
                case RangeModel.Additive: // NathanKell
                    { 
                        var omni_a = sat_a.Antennas.Where(a => a.Omni > 0);
                        var omni_b = sat_b.Antennas.Where(b => b.Omni > 0);
                        var dish_a = sat_a.Antennas.Where(a => a.Dish > 0 && (a.Target == sat_b.Guid || (a.Target == ActiveVesselGuid && active_vessel != null &&
                                                                                                            sat_b.Guid == active_vessel.id)));
                        var dish_b = sat_b.Antennas.Where(b => b.Dish > 0 && (b.Target == sat_a.Guid || (b.Target == ActiveVesselGuid && active_vessel != null &&
                                                                                                            sat_a.Guid == active_vessel.id)));


                        var planets = RTCore.Instance.Network.Planets;
                        var planet_a = sat_a.Antennas.Where(a =>
                        {
                            if (!planets.ContainsKey(a.Target) || sat_b.Body != planets[a.Target]) return false;
                            if (!(a.Dish > 0)) return false;
                            var dir_cb = (planets[a.Target].position - sat_a.Position);
                            var dir_b = (sat_b.Position - sat_a.Position);
                            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= a.Radians) return true;
                            return false;
                        });
                        var planet_b = sat_b.Antennas.Where(b =>
                        {
                            if (!planets.ContainsKey(b.Target) || sat_a.Body != planets[b.Target]) return false;
                            if (!(b.Dish > 0)) return false;
                            var dir_cb = (planets[b.Target].position - sat_b.Position);
                            var dir_b = (sat_a.Position - sat_b.Position);
                            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= b.Radians) return true;
                            return false;
                        });

                        // get all links

                        double max_omni_a = 0;
                        double sum_omni_a = 0;
                        double max_omni_b = 0;
                        double sum_omni_b = 0;
                        bool inRange = false;
                        if (RTSettings.Instance.NathanKell_MultipleAntennaSupport)
                        {
                            foreach (IAntenna a in omni_a)
                            {
                                if (a.Omni > max_omni_a)
                                    max_omni_a = a.Omni;
                                sum_omni_a += a.Omni;
                            }

                            foreach (IAntenna b in omni_b)
                            {
                                if (b.Omni > max_omni_b)
                                    max_omni_b = b.Omni;
                                sum_omni_b += b.Omni;
                            }
                            max_omni_a += (sum_omni_a - max_omni_a) * MULTIPLEANTFRACTION;
                            max_omni_b += (sum_omni_b - max_omni_b) * MULTIPLEANTFRACTION;

                            inRange = MaxDistance(max_omni_a, max_omni_b, OMNICLAMP) >= distance;
                            //MonoBehaviour.print("*RT2* For " + sat_a.Name + "(" + max_omni_a + ") <-> " + sat_b.Name + "(" + max_omni_b + "), inrange = " + inRange);
                        }
                        var omnis = omni_a.Where(a => // omni to omni
                            {
                                if (omni_b == null)
                                    return false;
                                if (inRange)
                                    return true;
                                foreach (IAntenna b in omni_b)
                                    if (MaxDistance(a.Omni, b.Omni, OMNICLAMP) >= distance)
                                        return true;
                                return false;
                            });
                        var alldishes_b = dish_b.Concat(planet_b);
                        var dishes = omni_a.Where(a => // omni to dish
                            {
                                if (dish_b.Concat(planet_b) == null)
                                    return false;
                                foreach (IAntenna b in alldishes_b)
                                    if (MaxDistance(RTSettings.Instance.NathanKell_MultipleAntennaSupport ? max_omni_a : a.Omni, b.Dish, OMNICLAMP) >= distance)
                                        return true;
                                return false;
                            });
                        dishes = dishes.Concat((dish_a.Concat(planet_a)).Where(a => // dish to omni or dish
                            {
                                if (omni_b == null)
                                    return false;
                                foreach (IAntenna b in omni_b)
                                    if (MaxDistance(a.Dish, RTSettings.Instance.NathanKell_MultipleAntennaSupport ? max_omni_b : b.Omni, OMNICLAMP) >= distance)
                                        return true;
                                foreach (IAntenna b in alldishes_b)
                                    if (MaxDistance(a.Dish, b.Dish, DISHCLAMP) >= distance)
                                        return true;
                                return false;
                            }));

                        if (omnis.Concat(dishes).Any())
                        {
                            var interfaces = omnis.Concat(dishes).ToList();
                            var type = LinkType.Omni;
                            if (dishes.Any())
                                type = LinkType.Dish;
                            return new NetworkLink<ISatellite>(sat_b, interfaces, type);
                        }
                    }
                    break;
         
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
            var count = RTCore.Instance.Satellites.Count;
            if (count == 0) return;
            int baseline = (count / REFRESH_TICKS);
            int takeCount = baseline + (((mTick++ % REFRESH_TICKS) < (count - baseline * REFRESH_TICKS)) ? 1 : 0);
            IEnumerable<ISatellite> commandStations = RTCore.Instance.Satellites.FindCommandStations();
            foreach (VesselSatellite s in RTCore.Instance.Satellites.Concat(RTCore.Instance.Satellites).Skip(mTickIndex).Take(takeCount))
            {
                UpdateGraph(s);
                //RTLog.Debug("{0} [ E: {1} ]", s.ToString(), Graph[s.Guid].ToDebugString());
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
        public Guid Guid { get { return new Guid(RTSettings.Instance.MissionControlGuid); } }
        public Vector3d Position { get { return FlightGlobals.Bodies[RTSettings.Instance.MissionControlBody].GetWorldSurfacePosition(RTSettings.Instance.MissionControlPosition.x, 
                                                                                                                                         RTSettings.Instance.MissionControlPosition.y,
                                                                                                                                         RTSettings.Instance.MissionControlPosition.z); } }
        public CelestialBody Body { get { return FlightGlobals.Bodies[RTSettings.Instance.MissionControlBody]; } }
        public IEnumerable<IAntenna> Antennas { get; private set; }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> route)
        {
            ;
        }

        public MissionControlSatellite()
        {
            var antennas = new List<IAntenna>();
            antennas.Add(new ProtoAntenna("Dummy Antenna", Guid, RTSettings.Instance.MissionControlRange));
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