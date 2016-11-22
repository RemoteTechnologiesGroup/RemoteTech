using System;
using RemoteTech.Common;
using RemoteTech.Common.Interfaces.FlightComputer;
using RemoteTech.UI;

namespace RemoteTech.FlightComputer.API
{

    public static class API
    {

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

        //exposed method called by other mods, passing a ConfigNode to RemoteTech
        public static bool QueueCommandToFlightComputer(ConfigNode externalData)
        {
            if (RTCore.Instance == null) return false;
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
                var computer = RTCore.Instance.Satellites[externalVesselId].FlightComputer;

                var extCmd = Commands.ExternalAPICommand.FromExternal(externalData);

                computer.Enqueue(extCmd);
                return true;
            }
            catch (Exception ex)
            {
                RTLog.Verbose(ex.Message, RTLogLevel.API);
            }

            return false;
        }

        // this method provides a workaround for issue #437, it may be possible to remove it in the future
        public static void InvokeOriginalEvent(BaseEvent e)
        {
            var wrappedEvent = e as UIPartActionMenuPatcher.WrappedEvent;
            if (wrappedEvent != null)
            {
                wrappedEvent.InvokeOriginalEvent();
            }
            else
            {
                e.Invoke();
            }
        }

    }

}