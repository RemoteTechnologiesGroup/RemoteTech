using System;
using System.Text;
using System.Collections.Generic;
using KSP.IO;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteNetwork {

        Dictionary<String, ISatellite> mSatelliteCache;
        Pair<IList<ISatellite>, double> mPathCache;
        MissionControlSatellite mMissionControlSatellite;

        public Dictionary<String, ISatellite> Satellites { get { return mSatelliteCache; } }
        public IList<ISatellite> Path { get { return mPathCache.First; } }
        public double Cost { get { return mPathCache.Second; } }

        public SatelliteNetwork() {
            mSatelliteCache = new Dictionary<String, ISatellite>();
            mMissionControlSatellite = new MissionControlSatellite();
            mPathCache = new Pair<IList<ISatellite>, double>(new List<ISatellite>(), Double.NegativeInfinity);
        }

        public Pair<IList<ISatellite>, double> FindCommandPath(Vessel v) {
            return FindCommandPath(mSatelliteCache[v.vesselName]);
        }

        public Pair<IList<ISatellite>, double> FindCommandPath(ISatellite start) {
            RTUtil.Log("SatelliteNetwork.FindCommandPath");
            RTUtil.Log(mSatelliteCache.ToDebugString());
            return mPathCache = Pathfinder.Solve(start, mMissionControlSatellite,
                                   new Pathfinder.NeighbourDelegate<ISatellite>(FindConnections),
                                   new Pathfinder.CostDelegate<ISatellite>(Distance),
                                   new Pathfinder.HeuristicDelegate<ISatellite>(Distance)
                                   );
        }

        public static double Distance(ISatellite a, ISatellite b) {
            return Vector3d.Distance(a.Position, b.Position);
        }

        IList<ISatellite> FindConnections(ISatellite sat) {
            IList<ISatellite> satellites = new List<ISatellite>();

            // Dish connections
            foreach (IAntenna a in sat.Antennas) {
                if (!mSatelliteCache.ContainsKey(a.Target))
                    continue;
                if (mSatelliteCache[a.Target] == sat)
                    continue;
                double targetRange = mSatelliteCache[a.Target].IsPointingAt(sat);
                double distance = Distance(sat, mSatelliteCache[a.Target]);
                if (distance > targetRange || distance > a.DishRange)
                    continue;
                if (!IsLineOfSight(sat, mSatelliteCache[a.Target]))
                    continue;
                satellites.Add(mSatelliteCache[a.Target]);
            }

            // Omni connections
            foreach (ISatellite a in mSatelliteCache.Values) {
                if (a == sat)
                    continue;
                double distance = Distance(sat, a);
                RTUtil.Log("OmniDistanceCheck: " + a.FindMaxOmniRange() + ", " + sat.FindMaxOmniRange() + ", " + distance);
                if (distance > a.FindMaxOmniRange() || distance > sat.FindMaxOmniRange())
                    continue;
                satellites.Add(a);
                
            }
            RTUtil.Log("Neighbours: " + satellites.ToDebugString());
            return satellites;
        }

        bool IsLineOfSight(ISatellite a, ISatellite b) {
            return true;
        }

        public void Update() {
            RTUtil.Log("SatelliteNetwork.Update");
            mSatelliteCache.Clear();
            foreach(Satellite a in Satellite.FindAll()) {
                mSatelliteCache[a.Name] = (ISatellite) a;
            }
            mSatelliteCache[mMissionControlSatellite.Name] = mMissionControlSatellite;
            RTUtil.Log(mSatelliteCache.ToDebugString());
        }


    }

}

