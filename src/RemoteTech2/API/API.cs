using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteTech;
using UnityEngine;

namespace RemoteTech
{
    public static class API
    {
        public static bool HasFlightComputer(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;
            return satellite.FlightComputer != null;
        }

        public static void AddSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return;
            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null) continue;
                if (spu.FlightComputer.SanctionedPilots.Contains(autopilot)) continue;
                spu.FlightComputer.SanctionedPilots.Add(autopilot);
            }
        }

        public static void RemoveSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return;
            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null) continue;
                spu.FlightComputer.SanctionedPilots.Remove(autopilot);
            }
        }

        public static bool HasAnyConnection(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            return RTCore.Instance.Network[satellite].Any();
        }

        public static bool HasConnectionToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            return RTCore.Instance.Network[satellite].Any(r => r.Goal.Guid == MissionControlSatellite.Guid);
        }

        public static double GetShortestSignalDelay(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any()) return Double.PositiveInfinity;
            return RTCore.Instance.Network[satellite].Min().Delay;
        }

        public static double GetSignalDelayToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any(r => r.Goal.Guid == MissionControlSatellite.Guid)) return Double.PositiveInfinity;
            return RTCore.Instance.Network[satellite].Where(r => r.Goal.Guid == MissionControlSatellite.Guid).Min().Delay;
        }

        public static double GetSignalDelayToSatellite(Guid a, Guid b)
        {
            var sat_a = RTCore.Instance.Satellites[a];
            var sat_b = RTCore.Instance.Satellites[b];
            if (sat_a == null || sat_b == null) return Double.PositiveInfinity;

            Func<ISatellite, IEnumerable<NetworkLink<ISatellite>>> neighbors = RTCore.Instance.Network.FindNeighbors;
            Func<ISatellite, NetworkLink<ISatellite>, double> cost = RangeModelExtensions.DistanceTo;
            Func<ISatellite, ISatellite, double> heuristic = RangeModelExtensions.DistanceTo;

            var path = NetworkPathfinder.Solve(sat_a, sat_b, neighbors, cost, heuristic);
            return path.Delay;
        }
    }
}
