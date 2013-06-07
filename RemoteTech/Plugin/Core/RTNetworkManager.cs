using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class RTNetworkManager {
        public delegate void RTTypedEdgeHandler(TypedEdge<ISatellite> edge);
        public delegate void RTConnectionHandler(Path<ISatellite> connection);

        public event RTTypedEdgeHandler EdgeUpdated;

        public Path<ISatellite> Connection { get { return mPathCache; } }
        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }
        public Dictionary<ISatellite, List<ISatellite>> Graph { get; private set; }
        
        RTCore mCore;
        Path<ISatellite> mPathCache;
        int mTick;
        int mTickIndex;
        
        public RTNetworkManager(RTCore core) {
            mCore = core;
            MissionControl = new MissionControlSatellite();
            Graph = new Dictionary<ISatellite, List<ISatellite>>();
            
            Planets = GeneratePlanetGuidMap();
            mPathCache = Path.Empty<ISatellite>();
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

        public static EdgeType ConnectedTo(ISatellite a, ISatellite b) {
            float distance = Distance(a, b);
            bool los = LineOfSight(a, b);
            if (!los)
                return EdgeType.None;

            bool omni_A = a.OmniRange > distance;
            bool omni_B = b.OmniRange > distance;
            if (omni_A && omni_B)
                return EdgeType.Omni;

            bool dish_A = a.DishRange.Any(x => (x.First == b.Guid) && (x.Second > distance));
            bool dish_B = b.DishRange.Any(x => (x.First == a.Guid) && (x.Second > distance));
            if((omni_A || dish_A) && (omni_B || dish_B))
                return EdgeType.Dish;

            return EdgeType.None;
        }

        public Path<ISatellite> FindPath(ISatellite start, ISatellite goal) {
            RTUtil.Log("SatelliteNetwork: FindCommandPath");
            return mPathCache = Pathfinder.Solve(start, MissionControl, 
                s => Graph[s], 
                Distance,
                Distance);
        }

        void UpdateGraph(ISatellite a) {
            RTUtil.Log("ConnectionManager: UpdateGraph: " + a);
            Graph[a].Clear();
            foreach(ISatellite b in mCore.Satellites.Concat(new [] { MissionControl })) {
                if (a == b)
                    continue;
                EdgeType edge = ConnectedTo(a, b);
                if(edge != EdgeType.None) {
                    Graph[a].Add(b);
                }
                OnEdgeUpdate(new TypedEdge<ISatellite>(a, b, edge));
            }
            RTUtil.Log("Edges: " + a.Name + ": " + Graph[a].ToDebugString());
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

        public void Dispose() {
            mCore.Satellites.Registered -= OnSatelliteRegister;
            mCore.Satellites.Unregistered -= OnSatelliteUnregister;
        }

        public void OnSatelliteUnregister(ISatellite s) {
            Graph.Remove(s);
        }

        public void OnSatelliteRegister(ISatellite s) {
            Graph[s] = new List<ISatellite>();
        }

        public IEnumerator Tick() {
            yield return 0; // Wait for end of physics frame.
            int takeCount =  Math.Min(mCore.Satellites.Count / 50 + 1, mCore.Satellites.Count - mTickIndex);
            if(takeCount > 0) {
                foreach(ISatellite s in mCore.Satellites.Skip(mTickIndex).Take(takeCount)) {
                    UpdateGraph(s);
                    if (FlightGlobals.ActiveVessel && s == mCore.Satellites.For(FlightGlobals.ActiveVessel.id)) {
                        FindPath(s, MissionControl);
                        if(Connection.Exists) {
                            RTUtil.Log("Path established");
                        } else {
                            RTUtil.Log("No path");
                        }
                    }
                }
                mTickIndex += takeCount;
            }

            mTick = (mTick + 1) % 50;
            if(mTick == 0) {
                mTickIndex = 0;
            }
        }

        public void OnEdgeUpdate(TypedEdge<ISatellite> edge) {
            if(EdgeUpdated != null) {
                EdgeUpdated.Invoke(edge);
            }
        }
    }
}

