using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteTech;
using UnityEngine;

namespace kOS.RemoteTech2
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class RemoteTechIntegrator : MonoBehaviour, IRemoteTech
    {
        public void Start()
        {
            if (RTSettings.Instance == null) return;
            RTHook.Instance = this;
            RTLog.Notify("kOS integration loaded successfully");
        }

        public void AddSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
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

        public void RemoveSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return;
            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null) continue;
                spu.FlightComputer.SanctionedPilots.Remove(autopilot);
            }
        }

        public bool HasAnyConnection(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            return RTCore.Instance.Network[satellite].Any();
        }

        public bool HasConnectionToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            return RTCore.Instance.Network[satellite].Any(r => r.Goal.Guid == RTCore.Instance.Network.MissionControl.Guid);
        }

        public double GetShortestSignalDelay(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any()) return Double.PositiveInfinity;
            return RTCore.Instance.Network[satellite].Min().Delay;
        }

        public double GetSignalDelayToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any(r => r.Goal.Guid == RTCore.Instance.Network.MissionControl.Guid)) return Double.PositiveInfinity;
            return RTCore.Instance.Network[satellite].Where(r => r.Goal.Guid == RTCore.Instance.Network.MissionControl.Guid).Min().Delay;
        }

        public double GetSignalDelayToSatellite(Guid a, Guid b)
        {
            var sat_a = RTCore.Instance.Satellites[a];
            var sat_b = RTCore.Instance.Satellites[b];
            if (sat_a == null || sat_b == null) return Double.PositiveInfinity;

            Func<ISatellite, IEnumerable<NetworkLink<ISatellite>>> neighbors = RTCore.Instance.Network.FindNeighbors;
            Func<ISatellite, NetworkLink<ISatellite>, double> cost = NetworkManager.Distance;
            Func<ISatellite, ISatellite, double> heuristic = NetworkManager.Distance;

            var path = NetworkPathfinder.Solve(sat_a, sat_b, neighbors, cost, heuristic);
            return path.Delay;
        }
    }
}
