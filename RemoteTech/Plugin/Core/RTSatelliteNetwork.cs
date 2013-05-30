using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using KSP.IO;
using UnityEngine;

namespace RemoteTech {
    public class RTSatelliteNetwork {
        public List<ISatellite> Path { get { return mPathCache.First; } }
        public float Cost { get { return mPathCache.Second; } }

        Pair<List<ISatellite>, float> mPathCache;
        MissionControlSatellite mMissionControlSatellite;
        RTCore mCore;

        public RTSatelliteNetwork(RTCore core) {
            mMissionControlSatellite = new MissionControlSatellite();
            mPathCache = new Pair<List<ISatellite>, float>(new List<ISatellite>(), Single.NegativeInfinity);
            mCore = core;
        }
        
        public Pair<List<ISatellite>, float> FindCommandPath(ISatellite start) {
            RTUtil.Log("SatelliteNetwork.FindCommandPath");
            return mPathCache = Pathfinder.Solve(start, mMissionControlSatellite, FindConnections,
                                   Distance, Distance);
        }

        public static float Distance(ISatellite a, ISatellite b) {
            return Vector3.Distance(a.Position, b.Position);
        }

        List<ISatellite> FindConnections(ISatellite sat) {
            List<ISatellite> satellites = new List<ISatellite>();

            // Dish connections
            /*foreach (Pair<String, float> a in sat.DishRange) {
                foreach (ISatellite target in RTCore.Instance.Satellites.WithName(a.First)) {
                    if (target == sat)
                        continue;
                    float targetRange = target.IsPointingAt(sat);
                    if (targetRange == 0)
                        continue;
                    float distance = Distance(sat, target);
                    if (distance > targetRange || distance > a.Second)
                        continue;
                    if (!IsLineOfSight(sat, target))
                        continue;
                    satellites.Add(target);
                    break;
                }
            }*/

            // Omni connections
            foreach (ISatellite target in mCore.Satellites.Concat(new [] { mMissionControlSatellite })) {
                if (target == sat)
                    continue;
                float distance = Distance(sat, target);
                RTUtil.Log("OmniDistanceCheck: " + target.OmniRange + ", " + sat.OmniRange + ", " + distance);
                if (distance > target.OmniRange || distance > sat.OmniRange)
                    continue;
                satellites.Add(target);
            }
            RTUtil.Log("Neighbours: " + satellites.ToDebugString());
            return satellites;
        }

        bool IsLineOfSight(ISatellite a, ISatellite b) {
            return true;
        }
    }
}

