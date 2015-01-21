using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.RangeModel;
using RemoteTech.SimpleTypes;

namespace RemoteTech.API
{
    public static class API
    {
        public static bool HasFlightComputer(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;
            var hasFlightComputer = satellite.FlightComputer != null;
            RTLog.Verbose("Flight: {0} HasFlightComputer: {1}", id, hasFlightComputer);
            return hasFlightComputer;
        }

        public static void AddSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return;
            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null) continue;
                if (spu.FlightComputer.SanctionedPilots.Contains(autopilot)) continue;
                RTLog.Verbose("Flight: {0} Adding Sanctioned Pilot", id);
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
                RTLog.Verbose("Flight: {0} Removing Sanctioned Pilot", id);
                spu.FlightComputer.SanctionedPilots.Remove(autopilot);
            }
        }

        public static bool HasAnyConnection(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            var hasConnection = RTCore.Instance.Network[satellite].Any();
            RTLog.Verbose("Flight: {0} Has Connection: {1}", id, hasConnection);
            return hasConnection;
        }

        public static bool HasConnectionToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            var connectedToKerbin = RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid));
            RTLog.Verbose("Flight: {0} Has Connection to Kerbin: {1}", id, connectedToKerbin);
            return connectedToKerbin;
        }

        public static double GetShortestSignalDelay(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any()) return Double.PositiveInfinity;
            var shortestDelay = RTCore.Instance.Network[satellite].Min().Delay;
            RTLog.Verbose("Flight: Shortest signal delay from {0} to {1}", id, shortestDelay);
            return shortestDelay;
        }

        public static double GetSignalDelayToKSC(Guid id)
        {
            var satellite = RTCore.Instance.Satellites[id];
            if (!RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid))) return Double.PositiveInfinity;
            var signalDelaytoKerbin = RTCore.Instance.Network[satellite].Where(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid)).Min().Delay;
            RTLog.Verbose("Connection from {0} to Kerbin Delay: {1}", id, signalDelaytoKerbin);
            return signalDelaytoKerbin;
        }

        public static double GetSignalDelayToSatellite(Guid a, Guid b)
        {
            var satelliteA = RTCore.Instance.Satellites[a];
            var satelliteB = RTCore.Instance.Satellites[b];
            if (satelliteA == null || satelliteB == null) return Double.PositiveInfinity;

            Func<ISatellite, IEnumerable<NetworkLink<ISatellite>>> neighbors = RTCore.Instance.Network.FindNeighbors;
            Func<ISatellite, NetworkLink<ISatellite>, double> cost = RangeModelExtensions.DistanceTo;
            Func<ISatellite, ISatellite, double> heuristic = RangeModelExtensions.DistanceTo;

            var path = NetworkPathfinder.Solve(satelliteA, satelliteB, neighbors, cost, heuristic);
            var delayBetween = path.Delay;
            RTLog.Verbose("Connection from {0} to {1} Delay: {2}", a, b, delayBetween);
            return delayBetween;
        }
        public static void ReceiveData(ConfigNode ExternalData) //exposed method called by other mods, passing a ConfigNode to RemoteTech
        {

            RemoteTech.FlightComputer.Commands.ExternalAPICommand extCmd = new RemoteTech.FlightComputer.Commands.ExternalAPICommand() //make our command
            {
                externalData = ExternalData,
                TimeStamp = RTUtil.GameTime,
                description = ExternalData.GetValue("Description"),
                shortName = ExternalData.GetValue("ShortName"),
                reflectionGetType = ExternalData.GetValue("ReflectionGetType"),
                reflectionInvokeMember = ExternalData.GetValue("ReflectionInvokeMember"),
                vslGUIDstr = ExternalData.GetValue("GUIDString"),
            };
            foreach (Vessel vsl2 in FlightGlobals.Vessels) //can not find a Guid.Parse method, so do it this way
            {
                if (vsl2.id.ToString() == extCmd.vslGUIDstr)
                {
                    extCmd.vslGUID = vsl2.id;
                    extCmd.vsl = vsl2;
                }
            }
            RemoteTech.FlightComputer.FlightComputer fltComp = RTCore.Instance.Satellites[extCmd.vslGUID].FlightComputer;

            fltComp.Enqueue(extCmd);
        }
    }
}
