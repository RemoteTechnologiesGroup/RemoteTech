using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class RTNetworkManager : IEnumerable<ISatellite> {
        static int REFRESH_TICKS = 50;
        public delegate void RTTypedEdgeHandler(TypedEdge<ISatellite> edge);
        public delegate void RTConnectionHandler(Path<ISatellite> connection);

        public event RTConnectionHandler ConnectionUpdated;
        public event RTTypedEdgeHandler EdgeUpdated;

        //public Path<ISatellite> Connection { get; private set; }
        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }
        public Dictionary<ISatellite, List<ISatellite>> Graph { get; private set; }
        
        RTCore mCore;
        int mTick;
        int mTickIndex;

        public RTNetworkManager(RTCore core) {
            mCore = core;
            MissionControl = new MissionControlSatellite();
            Graph = new Dictionary<ISatellite, List<ISatellite>>();
            
            Planets = GeneratePlanetGuidMap();
            mCore.Satellites.Registered += OnSatelliteRegister;
            mCore.Satellites.Unregistered += OnSatelliteUnregister;
        }

        Dictionary<Guid, CelestialBody> GeneratePlanetGuidMap() {
            var planetMap = new Dictionary<Guid, CelestialBody>();

            foreach(CelestialBody cb in FlightGlobals.Bodies) {
                planetMap[cb.Guid()] = cb;
            }

            return planetMap;
        }

        public static float Distance(ISatellite a, ISatellite b) {
            return Vector3.Distance(a.Position, b.Position);
        }

        public void FindPath(ISatellite start, ISatellite goal) {
            RTUtil.Log("SatelliteNetwork: FindCommandPath");
            Path<ISatellite> conn = Pathfinder.Solve(start, MissionControl, s => Graph[s], Distance, Distance);
            OnConnectionUpdate(conn);
        }

        void UpdateGraph(ISatellite a) {
            RTUtil.Log("ConnectionManager: UpdateGraph: " + a);
            List<TypedEdge<ISatellite>> result = new List<TypedEdge<ISatellite>>();

            foreach(ISatellite b in this) {
                EdgeType aToB = IsConnectedTo(a, b);
                if (aToB == EdgeType.None)
                    continue;
                EdgeType bToA = IsConnectedTo(b, a);
                if (bToA == EdgeType.None)
                    continue;
                RTUtil.Log("Result: aToB = {0}, bToA = {1}", aToB.ToString(), bToA.ToString());
                if (aToB == EdgeType.Dish || bToA == EdgeType.Dish) {
                    result.Add(new TypedEdge<ISatellite>(a, b, EdgeType.Dish));
                } else {
                    result.Add(new TypedEdge<ISatellite>(a, b, EdgeType.Omni));
                }
            } 

            // Process
            foreach (ISatellite b in Graph[a]) {
                var edge = new TypedEdge<ISatellite>(a, b, EdgeType.None);
                // If the edge no longer exists, send an event; note TypedEdge.Equals() ignores EdgeType.
                if(!result.Contains(edge)) {
                    OnEdgeUpdate(edge);
                }
            }
            Graph[a].Clear();
            foreach(TypedEdge<ISatellite> edge in result) {
                Graph[a].Add(edge.B);
                OnEdgeUpdate(edge);
            }

            RTUtil.Log("Edges: " + a.Name + ": " + Graph[a].ToDebugString());
        }

        EdgeType IsConnectedTo(ISatellite a, ISatellite b) {
            if (a == b)
                return EdgeType.None;

            float distance = Distance(a, b);
            if (distance < a.Omni)
                return EdgeType.Omni;

            foreach (Dish dish in a.Dishes) {
                if (distance > dish.Distance)
                    continue;
                if (dish.Target == b.Guid)
                    return EdgeType.Dish;
                if (Planets.ContainsKey(dish.Target) && Planets[dish.Target] == b.Body) { 
                    // Planet being targeted. Is b in the dish cone?
                    Vector3 dir_cb = (Planets[dish.Target].position - a.Position);
                    Vector3 dir_b = (b.Position - a.Position);
                    RTUtil.Log("Angle: " + Planets[dish.Target] + " " + b + " " + Vector3.Angle(dir_cb.normalized, dir_b.normalized));
                    if (Vector3.Dot(dir_cb.normalized, dir_b.normalized) >= dish.Factor) {
                        return EdgeType.Dish;
                    }
                }
            }

            return EdgeType.None;
        }

        public static bool LineOfSight(ISatellite a, ISatellite b) {
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
                Vector3d bodyFromA = referenceBody.position - a.Position;
                Vector3d bFromA = b.Position - a.Position;
                if (Vector3d.Dot(bodyFromA, bFromA) > 0) {
                    Vector3d bFromAnorm = bFromA.normalized;
                    if (Vector3d.Dot(bodyFromA, bFromAnorm) < bFromA.magnitude) {
                        Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                        if (lateralOffset.magnitude < (referenceBody.Radius - 5)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public IEnumerator Tick() {
            yield return 0; // Wait for end of physics frame.
            int takeCount = mCore.Satellites.Count / REFRESH_TICKS + (mCore.Satellites.Count % REFRESH_TICKS) < mTick ? 1 : 0;
            if (takeCount > 0) {
                foreach (ISatellite s in mCore.Satellites.Skip(mTickIndex).Take(takeCount)) {
                    UpdateGraph(s);
                    if (FlightGlobals.ActiveVessel && s == mCore.Satellites.For(FlightGlobals.ActiveVessel.id)) {
                        FindPath(s, MissionControl);
                    }
                }
                mTickIndex += takeCount;
            }

            mTick = (mTick + 1) % REFRESH_TICKS;
            if (mTick == 0) {
                mTickIndex = 0;
            }
        }

        public void OnSatelliteUnregister(ISatellite s) {
            Graph.Remove(s);
        }

        public void OnSatelliteRegister(ISatellite s) {
            Graph[s] = new List<ISatellite>();
        }

        public void OnEdgeUpdate(TypedEdge<ISatellite> edge) {
            if(EdgeUpdated != null) {
                EdgeUpdated.Invoke(edge);
            }
        }

        public void OnConnectionUpdate(Path<ISatellite> path) {
            if(ConnectionUpdated != null) {
                ConnectionUpdated.Invoke(path);
            }
        }

        public void Dispose() {
            mCore.Satellites.Registered -= OnSatelliteRegister;
            mCore.Satellites.Unregistered -= OnSatelliteUnregister;
        }

        public IEnumerator<ISatellite> GetEnumerator() {
            return mCore.Satellites.Cast<ISatellite>().Concat(new[] { MissionControl }).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}

