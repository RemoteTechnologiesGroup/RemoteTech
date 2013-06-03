using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class RTConnectionManager {
        public Path<ISatellite> Connection { get { return mPathCache; } }

        public Dictionary<Guid, CelestialBody> Planets { get; private set; }
        public MissionControlSatellite MissionControl { get; private set; }

        RTCore mCore;
        Path<ISatellite> mPathCache;
        

        public RTConnectionManager(RTCore core) {
            MissionControl = new MissionControlSatellite();
            mPathCache = Path.Empty<ISatellite>();
            mCore = core;

            Planets = GeneratePlanetGuidMap();
        }

        Dictionary<Guid, CelestialBody> GeneratePlanetGuidMap() {
            var planetMap = new Dictionary<Guid, CelestialBody>();

            foreach(CelestialBody cb in FlightGlobals.Bodies) {
                planetMap[cb.Guid()] = cb;
            }

            return planetMap;
        }

        public Path<ISatellite> FindConnection(ISatellite start, ISatellite goal) {
            RTUtil.Log("SatelliteNetwork: FindCommandPath");
            return mPathCache = Pathfinder.Solve(start, MissionControl, FindConnections, Distance, Distance);
        }

        public static float Distance(ISatellite a, ISatellite b) {
            return Vector3.Distance(a.Position, b.Position);
        }

        List<ISatellite> FindConnections(ISatellite sat) {
            List<ISatellite> result = new List<ISatellite>();
            
            // Dish connections
            foreach (Pair<Guid, float> antenna in sat.DishRange) {
                IEnumerable<ISatellite> toProcess = Enumerable.Empty<ISatellite>();

                // If the target is the GUID of a planet, but not the planet the sat is on.
                if (Planets.ContainsKey(antenna.First) && Planets[antenna.First] != sat.SignalProcessor.Vessel.mainBody) {
                    // Process all satellites in that planet's SoI. No hierarchies.
                    CelestialBody targetCB = Planets[antenna.First];
                    toProcess.Concat(mCore.Satellites.Where(s => s.SignalProcessor.Vessel.mainBody == targetCB));
                } else if (antenna.First.Equals(MissionControl.Guid)) {
                    toProcess.Concat(new[] { MissionControl });
                } else {
                    ISatellite targetSat = mCore.Satellites.WithGuid(antenna.First);
                    if(targetSat != null && targetSat != sat) {
                        toProcess.Concat(new [] { targetSat });
                    }
                }
                // Process.
                foreach(ISatellite sat2 in toProcess) {
                    foreach(Pair<Guid, float> antenna2 in sat2.DishRange) {
                        if (antenna2.First != sat.Guid || (Planets.ContainsKey(antenna2.First) && Planets[antenna2.First] != sat.SignalProcessor.Vessel.mainBody))
                            continue;
                        if (Distance(sat, sat2) > antenna2.Second || Distance(sat, sat2) > antenna.Second)
                            continue;
                        result.Add(sat2);
                        break;
                    }
                }
            }
            
            // Omni connections.
            foreach (ISatellite target in mCore.Satellites.Concat(new [] { MissionControl})) {
                if (target == sat)
                    continue;
                if (Distance(sat, target) > target.OmniRange || Distance(sat, target) > sat.OmniRange)
                    continue;
                result.Add(target);
            }
            RTUtil.Log("Neighbours: " + result.ToDebugString());
            return result;
        }

        bool IsLineOfSight(ISatellite a, ISatellite b) {
            return true;
        }

        public IEnumerator EstablishConnection() {
            yield return 0; // Wait for end of physics frame.
            ISatellite sat;
            if(FlightGlobals.ActiveVessel == null || (sat = mCore.Satellites.For(FlightGlobals.ActiveVessel)) == null) {
                mPathCache = Path.Empty<ISatellite>();
            } else {
                mPathCache = FindConnection(sat, MissionControl);
            }
        }
    }
}

