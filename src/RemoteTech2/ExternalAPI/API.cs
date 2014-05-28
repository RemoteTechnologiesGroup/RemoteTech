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
            return RTCore.Instance.Network[satellite].ConnectedToKSC();
        }

        public static double GetShortestSignalDelay(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            var connection = RTCore.Instance.Network[satellite];
            if (!connection.Any()) return Double.PositiveInfinity;
            return connection.ShortestDelay().SignalDelay;
        }

        public static double GetSignalDelayToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            return RTCore.Instance.Network[satellite].DelayToKSC();
        }

        public static double GetSignalDelayToSatellite(Guid a, Guid b)
        {
            throw new NotImplementedException("Sorry");
        }
    }
}
