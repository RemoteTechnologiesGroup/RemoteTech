using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class SatelliteNetwork {

        public static SatelliteNetwork Instance {
            get {
                if (mSingleton == null) {
                    mSingleton = new SatelliteNetwork();
                }
                return mSingleton;
            }
        }

        static SatelliteNetwork mSingleton;

        IList<ISatellite> mSatelliteCache;

        public static float Euclidean(ISatellite a, ISatellite b) {
            return Vector3d.Distance(a.Position, b.Position);
        }

        public IList<ISatellite> FindNeighbours(ISatellite a) {

        }

        public static float FindEdgeCost(ISatellite a, ISatellite b) {
            // Euclidean + Line of sight
        }


        public void FindShortestCommandPath(ISatellite goal) {

            List<ISatellite> sats = new List<ISatellite>();
            List<CommandSPUPartModule> commands = new List<CommandSPUPartModule>();


            foreach (Vessel v in FlightGlobals.vessels) {
                ISatellite sat = v.FindSPU();
                CommandSPUPartModule command = v.FindCommandSPU();
                if (command != null) {
                    commands.Add(command);
                    sat = command;
                }
                if (sat != null) {
                    sats.Add(sat);
                }
            }
            mSatelliteCache = sats;

            foreach (CommandSPUPartModule c in commands) {
                if (c.Crew > 0) {
                    List<ISatellite> path = Pathfinder.Solve(c, goal, FindNeighbours, FindEdgeCost, Euclidean);
                }
            }

        }

//        Dictionary<String,List<List<SPUPartModule>>> ConstructCelestialBins() {
//            Dictionary<String,List<List<SPUPartModule>>> celestialBins = 
//                new Dictionary<String,List<List<SPUPartModule>>>();
//            foreach (CelestialBody c in FlightGlobals.Bodies) {
//                if (!c.HasChild) {
//                    List<SPUPartModule> newBin = new List<SPUPartModule>();
//                    celestialBins[c.GetName()] = new List<List<SPUPartModule>>();
//                    celestialBins[c.GetName()].Add(newBin);
//                    CelestialBody parent = c;
//                    while ((parent = parent.referenceBody) != null) {
//                        if (!celestialBins.ContainsKey(parent.GetName())) {
//                            celestialBins[parent.GetName()] = new List<List<SPUPartModule>>();
//                        }
//                        celestialBins[parent.GetName()].Add(newBin);
//                    }
//                }
//            }
//        }

//        void Generate() {
//            // FIXME: Optimization, re-use old state. 
//
//            // Reset state
//            mVertices.Clear();
//            mCommandSPUModules.Clear();
//
//            // Gather part module lists. Command SPU modules have priority. One PartModule per Vessel.
//            foreach (Vessel v in FlightGlobals.Vessels) {
//                SPUPartModule spuModule = v.GetSPU();
//                CommandSPUPartModule commandSPUModule = v.GetCommandSPU();
//                if (commandSPUModule != null) {
//                    mCommandSPUModules.Add(commandSPUModule);
//                    spuModule = commandSPUModule;
//                }
//                if (spuModule != null) {
//                    mVertices.Add(new Vertex<SPUPartModule>(spuModule));
//                }
//            }
//
//            // Determine edges : Omnidirectional. O(n*n), sorry D:. FIXME: Use CelestialBody tree for optimization.
//            foreach (Vertex<SPUPartModule> p in mVertices) {
//                foreach (Vertex<SPUPartModule> q in mVertices) {
//                    Vessel a = p.Item.vessel;
//                    Vessel b = q.Item.vessel;
//                    if (a != b) {
//                        double distance = Vector3d.Distance(a.GetWorldPos3D, b.GetWorldPos3D);
//                        if (distance < p.Item.GetOmniRange() && distance < q.Item.GetOmniRange()) {
//                            p.AddEdge(q);
//                        }
//                    }
//                }
//            }
//
//            // Create a dictionary of Vessel Names to SPUPartModule reference
//            Dictionary<String, Vertex<SPUPartModule>> vesselNameMap = new Dictionary<String, Vertex<SPUPartModule>>();
//            foreach (Vertex<SPUPartModule> p in mVertices) {
//                vesselNameMap.Add(p.Item.vessel.GetName(), p)
//            }
//
//            // Determine edges : Dish. FIXME: Optimization
//            foreach (Vertex<SPUPartModule> vertex in mVertices) {
//                // For all antenans on this module
//                foreach (IAntenna antenna in vertex.Item.GetAntennas()) {
//                    // If Target matches an existing module
//                    if(vesselNameMap.ContainsKey(antenna.Target)) {
//                        // If any of the Target's antennas is pointing back
//                        Vertex<SPUPartModule> targetVertex = vesselNameMap[antenna.Target];
//                        foreach (IAntenna targetAntenna in targetVertex.Item.GetAntennas()) {
//                            if(targetAntenna.Target.Equals(antenna.Target)) {
//                                // If both are within range of each other
//                                Vessel a = vertex.Item.vessel;
//                                Vessel b = targetVertex.Item.vessel;
//                                double distance = Vector3d.Distance(a.GetWorldPos3D, b.GetWorldPos3D);
//                                if (distance < antenna.DishRange && distance < targetAntenna.DishRange) {
//                                    // Add edges to just this vertex; the other will be visited again in time
//                                    // Possibly allow for directed connections in the future (when adjusted)
//                                    vertex.AddEdge(targetVertex);
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }
    }
}

