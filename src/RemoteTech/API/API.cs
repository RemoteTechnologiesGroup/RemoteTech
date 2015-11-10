using RemoteTech.RangeModel;
using RemoteTech.SimpleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using WrappedEvent = RemoteTech.FlightComputer.UIPartActionMenuPatcher.WrappedEvent;


namespace RemoteTech.API
{
    public static class API
    {
        public static bool HasLocalControl(Guid id)
        {
            var vessel = RTUtil.GetVesselById(id);            
            if (vessel == null) return false;

            RTLog.Verbose("Flight: {0} HasLocalControl: {1}", RTLogLevel.API, id, vessel.HasLocalControl());

            return vessel.HasLocalControl();
        }

        public static bool HasFlightComputer(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var hasFlightComputer = satellite.FlightComputer != null;
            RTLog.Verbose("Flight: {0} HasFlightComputer: {1}", RTLogLevel.API, id, hasFlightComputer);

            return hasFlightComputer;
        }

        public static void AddSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            if (RTCore.Instance == null) return;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null || satellite.SignalProcessor == null) return;

            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null || spu.FlightComputer.SanctionedPilots == null) continue;
                if (spu.FlightComputer.SanctionedPilots.Contains(autopilot)) continue;
                RTLog.Verbose("Flight: {0} Adding Sanctioned Pilot", RTLogLevel.API, id);
                spu.FlightComputer.SanctionedPilots.Add(autopilot);
            }
        }

        public static void RemoveSanctionedPilot(Guid id, Action<FlightCtrlState> autopilot)
        {
            if (RTCore.Instance == null) return;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null || satellite.SignalProcessor == null) return;

            foreach (var spu in satellite.SignalProcessors)
            {
                if (spu.FlightComputer == null || spu.FlightComputer.SanctionedPilots == null) continue;
                RTLog.Verbose("Flight: {0} Removing Sanctioned Pilot", RTLogLevel.API, id);
                spu.FlightComputer.SanctionedPilots.Remove(autopilot);
            }
        }

        public static bool HasAnyConnection(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var hasConnection = RTCore.Instance.Network[satellite].Any();
            RTLog.Verbose("Flight: {0} Has Connection: {1}", RTLogLevel.API, id, hasConnection);
            return hasConnection;
        }

        public static bool HasConnectionToKSC(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var connectedToKerbin = RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid));
            RTLog.Verbose("Flight: {0} Has Connection to Kerbin: {1}", RTLogLevel.API, id, connectedToKerbin);
            return connectedToKerbin;
        }

        public static double GetShortestSignalDelay(Guid id)
        {
            if (RTCore.Instance == null) return double.PositiveInfinity;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return double.PositiveInfinity;

            if (!RTCore.Instance.Network[satellite].Any()) return double.PositiveInfinity;
            var shortestDelay = RTCore.Instance.Network[satellite].Min().Delay;
            RTLog.Verbose("Flight: Shortest signal delay from {0} to {1}", RTLogLevel.API, id, shortestDelay);
            return shortestDelay;
        }

        public static double GetSignalDelayToKSC(Guid id)
        {
            if (RTCore.Instance == null) return double.PositiveInfinity;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return double.PositiveInfinity;

            if (!RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid))) return double.PositiveInfinity;
            var signalDelaytoKerbin = RTCore.Instance.Network[satellite].Where(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid)).Min().Delay;
            RTLog.Verbose("Connection from {0} to Kerbin Delay: {1}", RTLogLevel.API, id, signalDelaytoKerbin);
            return signalDelaytoKerbin;
        }

        public static double GetSignalDelayToSatellite(Guid a, Guid b)
        {
            if (RTCore.Instance == null) return double.PositiveInfinity;
            var satelliteA = RTCore.Instance.Satellites[a];
            var satelliteB = RTCore.Instance.Satellites[b];
            if (satelliteA == null || satelliteB == null) return double.PositiveInfinity;

            Func<ISatellite, IEnumerable<NetworkLink<ISatellite>>> neighbors = RTCore.Instance.Network.FindNeighbors;
            Func<ISatellite, NetworkLink<ISatellite>, double> cost = RangeModelExtensions.DistanceTo;
            Func<ISatellite, ISatellite, double> heuristic = RangeModelExtensions.DistanceTo;

            var path = NetworkPathfinder.Solve(satelliteA, satelliteB, neighbors, cost, heuristic);
            var delayBetween = path.Delay;
            RTLog.Verbose("Connection from {0} to {1} Delay: {2}", RTLogLevel.API, a, b, delayBetween);
            return delayBetween;
        }

        //exposed method called by other mods, passing a ConfigNode to RemoteTech
        public static bool QueueCommandToFlightComputer(ConfigNode externalData)
        {
            //check we were actually passed a config node
            if (externalData == null) return false;
            // check our min values
            if (!externalData.HasValue("GUIDString") && !externalData.HasValue("Executor") && !externalData.HasValue("ReflectionType"))
            {
                return false;
            }

            try
            {
                Guid externalVesselId = new Guid(externalData.GetValue("GUIDString"));
                // you can only push a new external command if the vessel guid is the current active vessel
                if (FlightGlobals.ActiveVessel.id != externalVesselId)
                {
                    RTLog.Verbose("Passed Guid is not the active Vessels guid", RTLogLevel.API);
                    return false;
                }

                // maybe we should look if this vessel hasLocal controll or not. If so, we can execute the command
                // immediately

                // get the flightcomputer
                FlightComputer.FlightComputer computer = RTCore.Instance.Satellites[externalVesselId].FlightComputer;

                var extCmd = FlightComputer.Commands.ExternalAPICommand.FromExternal(externalData);

                computer.Enqueue(extCmd);
                return true;
            }
            catch(Exception ex)
            {
                RTLog.Verbose(ex.Message, RTLogLevel.API);
            }

            return false;
        }

        // this method provides a workaround for issue #437, it may be possible to remove it in the future
        public static void InvokeOriginalEvent(BaseEvent e)
        {
            if (e is WrappedEvent)
            {
                WrappedEvent wrappedEvent = e as WrappedEvent;
                wrappedEvent.InvokeOriginalEvent();
            }
            else
            {
                e.Invoke();
            }
        }
    }
}