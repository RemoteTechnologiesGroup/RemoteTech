using RemoteTech.RangeModel;
﻿using RemoteTech.Modules;
using RemoteTech.SimpleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using WrappedEvent = RemoteTech.FlightComputer.UIPartActionMenuPatcher.WrappedEvent;

namespace RemoteTech.API
{
    public static class API
    {
        /// <summary>If true then RTCore will be available in the Space Center</summary>
        internal static bool enabledInSPC = false;

        public static bool IsRemoteTechEnabled()
        {
            if (RTCore.Instance != null) return true;
            return false;
        }

        public static void EnableInSPC(bool state)  // its advised that modders who need RTCore active in the SPC should set this from the MainMenu Scene
        {
            enabledInSPC = state;  // setting to true will only take effect after a scene change
            RTLog.Verbose("Flag for RemoteTech running in Space Center scene is set: {0}", RTLogLevel.API, enabledInSPC);
            if (!enabledInSPC && RTCore.Instance != null && HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                RTLog.Verbose("RemoteTech is terminated in Space Center scene", RTLogLevel.API);
                RTCore.Instance.OnDestroy();
            }
        }

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

        /// <summary> Determines if a satellite directly targets a ground station.</summary>
        /// <param name="id">The satellite id.</param>
        /// <returns>true if the satellite has an antenna with a ground station as its first link, false otherwise.</returns>
        public static bool HasDirectGroundStation(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var targetsGroundStation = RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Links.FirstOrDefault().Target.Guid));
            RTLog.Verbose("Flight: {0} Directly targets a ground station: {1}", RTLogLevel.API, id, targetsGroundStation);
            return targetsGroundStation;
        }

        /// <summary> Gets the name of the ground station directly targeted with the shortest link to the satellite.</summary>
        /// <param name="id">The satellite id.</param>
        /// <returns>name of the ground station if one is found, null otherwise.</returns>
        public static string GetClosestDirectGroundStation(Guid id)
        {
            if (RTCore.Instance == null) return null;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return null;

            var namedGroundStation = RTCore.Instance.Network[satellite].Where
                (r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Links.FirstOrDefault().Target.Guid)).Min().Goal.Name;
            RTLog.Verbose("Flight: {0} Directly targets the closest ground station: {1}", RTLogLevel.API, id, namedGroundStation);
            return namedGroundStation;
        }

        /// <summary> Gets the name of the first hop satellite with the shortest link to KSC by the specified satellite.</summary>
        /// <param name="id">The satellite id.</param>
        /// <returns>name of the satellite if one is found, null otherwise.</returns>
        public static string GetFirstHopToKSC(Guid id)
        {
            if (RTCore.Instance == null) return null;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return null;

            var namedSatellite = RTCore.Instance.Network[satellite].Where
                (r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid)).Min().Links.FirstOrDefault().Target.Name;
            RTLog.Verbose("Flight: {0} Has first hop satellite with shortest link to KSC: {1}", RTLogLevel.API, id, namedSatellite);
            return namedSatellite;
        }

        public static bool AntennaHasConnection(Part part)
        {
            if (RTCore.Instance == null) return false;
            var antennaModules = part.Modules.OfType<IAntenna>();

            return antennaModules.Any(m => m.Connected);
        }

        public static Guid GetAntennaTarget(Part part) {
            ModuleRTAntenna module = part.Modules.OfType<ModuleRTAntenna>().First();

            if (module == null)
            {
                throw new ArgumentException();
            }

            return module.Target;
        }

        /// <summary> Gets Guids of all satellites in the control route</summary>
        /// <param name="id">The satellite id </param>
        /// <returns> Guid array of all satellite in ground station router</returns>
        public static Guid[] GetControlPath(Guid id)
        {
            if (RTCore.Instance == null) return new Guid[]{ };
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return new Guid[] { };
            if (!RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid))) return new Guid[] { };

            List<NetworkLink<ISatellite>> bestRouter = RTCore.Instance.Network[satellite].Where(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid)).Min().Links;
            Guid[] guids = new Guid[bestRouter.Count - 1];

            // Get all satellites, not the last jump that is a station ground
            for (int i = 0; i < bestRouter.Count - 1; i++)
            {
                guids[i] = bestRouter[i].Target.Guid;
            }

            return guids;
        }
        
        public static void SetAntennaTarget(Part part, Guid id) {
            ModuleRTAntenna module = part.Modules.OfType<ModuleRTAntenna>().First();

            if (module == null)
            {
                throw new ArgumentException();
            }

            module.Target = id;
        }

        public static IEnumerable<string> GetGroundStations()
        {
            return RTSettings.Instance.GroundStations.Select(s => ((ISatellite)s).Name);
        }

        public static Guid GetGroundStationGuid(String name)
        {
            MissionControlSatellite groundStation = RTSettings.Instance.GroundStations.Where(station => station.GetName().Equals(name)).FirstOrDefault();

            if (groundStation == null)
            {
                return Guid.Empty;
            }

            return groundStation.mGuid;
        }

        /// <summary> Gets the name of a satellite.</summary>
        /// <param name="id">The satellite id.</param>
        /// <returns>name of the satellite with matching id if found, otherwise null</returns>
        public static string GetName(Guid id)
        {
            if (RTCore.Instance == null) return null;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return null;

            string satellitename = satellite.Name;
            RTLog.Verbose("Flight: {0} is: {1}", RTLogLevel.API, id, satellitename);
            return satellitename;
        }

        public static Guid GetCelestialBodyGuid(CelestialBody celestialBody)
        {
            return RTUtil.Guid(celestialBody);
        }

        public static Guid GetNoTargetGuid() {
            return new Guid(RTSettings.Instance.NoTargetGuid);
        }

        public static Guid GetActiveVesselGuid() {
            return new Guid(RTSettings.Instance.ActiveVesselGuid);
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

                // maybe we should look if this vessel hasLocal control or not. If so, we can execute the command
                // immediately

                // get the flight computer
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

		public static Guid AddGroundStation(string name, double latitude, double longitude, double height, int body)
		{
			RTLog.Notify ("Trying to add ground station {0}", RTLogLevel.API, name);
            Guid newStationId = RTSettings.Instance.AddGroundStation(name, latitude, longitude, height, body);

            return newStationId;
		}

		public static bool RemoveGroundStation(Guid stationid)
		{
			RTLog.Notify ("Trying to remove ground station {0}", RTLogLevel.API, stationid);

            // do not allow to remove the default mission control
			if (stationid.ToString ("N").Equals ("5105f5a9d62841c6ad4b21154e8fc488"))
			{
				RTLog.Notify ("Cannot remove KSC Mission Control!", RTLogLevel.API);
				return false;
			}

            return RTSettings.Instance.RemoveGroundStation(stationid);
        }

        /// <summary>
        /// Change the Omni range of a ground station.
        /// Note that this change is temporary. For example it is overridden to the value written in the settings file if the tracking station is upgraded.
        /// </summary>
        /// <param name="stationId">The station ID for which to change the antenna range.</param>
        /// <param name="newRange">The new range in meters.</param>
        /// <returns>true if the ground station antenna range was changed, false otherwise.</returns>
        public static bool ChangeGroundStationRange(Guid stationId, float newRange)
        {
            RTLog.Notify("Trying to change ground station {0} Omni range to {1}", RTLogLevel.API, stationId.ToString(), newRange);

            if (RTSettings.Instance == null)
                return false;

            if(RTSettings.Instance.GroundStations.Count > 0)
            {
                MissionControlSatellite groundStation = RTSettings.Instance.GroundStations.First(gs => gs.mGuid == stationId);
                if (groundStation == null)
                    return false;

                IEnumerable<IAntenna> antennas = groundStation.MissionControlAntennas.ToArray();
                if (antennas.Count() > 0)
                {
                    // first antenna
                    IAntenna antenna = antennas.ToArray()[0];
                    if (antenna is MissionControlAntenna)
                    {
                        ((MissionControlAntenna)antenna).SetOmniAntennaRange(newRange);
                        RTLog.Notify("Ground station Omni range successfully changed.", RTLogLevel.API);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Enforce or remove the radio blackout on target vessel (e.g. coronal mass ejection)
        /// </summary>
        /// <returns>Indicator on whether this request is executed successfully</returns>
        public static bool SetRadioBlackoutGuid(Guid id, bool flag, string reason = "")
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            satellite.IsInRadioBlackout = flag;
            RTLog.Verbose("Flight: {0} has radio blackout flag updated due to reason '{2}': {1}", RTLogLevel.API, id, satellite.IsInRadioBlackout, reason);
            return true;
        }

        /// <summary>
        /// Check if target vessel is currently in radio blackout
        /// </summary>
        /// <returns>Indicator on whether target vessel is in radio blackout</returns>
        public static bool GetRadioBlackoutGuid(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var blackoutFlag = satellite.IsInRadioBlackout;
            RTLog.Verbose("Flight: {0} is in radio blackout: {1}", RTLogLevel.API, id, blackoutFlag);
            return blackoutFlag;
        }

        /// <summary>
        /// Enforce or remove the power down on target vessel
        /// </summary>
        /// <returns>Indicator on whether this request is executed successfully</returns>
        public static bool SetPowerDownGuid(Guid id, bool flag, string reason = "")
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            satellite.PowerShutdownFlag = flag;
            RTLog.Verbose("Flight: {0} has power down flag updated due to reason '{2}': {1}", RTLogLevel.API, id, satellite.PowerShutdownFlag, reason);
            return true;
        }

        /// <summary>
        /// Check if target vessel is currently in power down state
        /// </summary>
        /// <returns>Indicator on whether target vessel is in power down state</returns>
        public static bool GetPowerDownGuid(Guid id)
        {
            if (RTCore.Instance == null) return false;
            var satellite = RTCore.Instance.Satellites[id];
            if (satellite == null) return false;

            var flag = satellite.PowerShutdownFlag;
            RTLog.Verbose("Flight: {0} is in power down: {1}", RTLogLevel.API, id, flag);
            return flag;
        }

        /// <summary>
        /// Get the maximum range distance between the satellites A and B based on current Range Model
        /// and no restrictions (such as LoS) applied
        /// </summary>
        /// <param name="sat_a">The satellite id.</param>
        /// <param name="sat_b">The satellite id.</param>
        /// <returns>Positive number</returns>
        public static double GetMaxRangeDistance(Guid sat_a, Guid sat_b)
        {
            if (RTCore.Instance == null) return 0.0;

            //sanity check
            var satelliteA = RTCore.Instance.Satellites[sat_a];
            var satelliteB = RTCore.Instance.Satellites[sat_b];
            if (satelliteA == null || satelliteB == null) return 0.0;

            //get link object
            NetworkLink<ISatellite> link = null;
            switch (RTSettings.Instance.RangeModelType)
            {
                case RangeModel.RangeModel.Additive:
                    link = RangeModelRoot.GetLink(satelliteA, satelliteB);
                    break;
                default:
                    link = RangeModelStandard.GetLink(satelliteA, satelliteB);
                    break;
            }
            if (link == null) return 0.0; //no connection possible

            //get max distance out of multiple antenna connections
            var distance = 0.0;
            var maxDistance = 0.0;
            for(int i=0; i < link.Interfaces.Count; i++)
            {
                switch (RTSettings.Instance.RangeModelType)
                {
                    case RangeModel.RangeModel.Additive:
                        distance = RangeModelRoot.GetRangeInContext(link.Interfaces[i], satelliteB, satelliteA);
                        break;
                    default:
                        distance = RangeModelStandard.GetRangeInContext(link.Interfaces[i], satelliteB, satelliteA);
                        break;
                }
                maxDistance = Math.Max(maxDistance, Double.IsNaN(distance) ? 0.0 : distance);
            }

            return maxDistance;
        }
    }
}
