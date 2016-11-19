using RemoteTech.RangeModel;
﻿using RemoteTech.Modules;
using RemoteTech.SimpleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Extensions;
using RemoteTech.Common.Settings;

namespace RemoteTech.API
{
    public static class API
    {
        public static bool IsRemoteTechEnabled()
        {
            if (RTCore.Instance != null) return true;
            return false;
        }

        public static bool HasLocalControl(Guid id)
        {
            var vessel = VesselExtension.GetVesselById(id);            
            if (vessel == null) return false;

            RTLog.Verbose("Flight: {0} HasLocalControl: {1}", RTLogLevel.API, id, vessel.HasLocalControl());

            return vessel.HasLocalControl();
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

        public static Guid GetCelestialBodyGuid(CelestialBody celestialBody)
        {
            return celestialBody.Guid();
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

		public static Guid AddGroundStation(string name, double latitude, double longitude, double height, int body)
		{
			RTLog.Notify ("Trying to add groundstation {0}", RTLogLevel.API, name);
            Guid newStationId = RTSettings.Instance.AddGroundStation(name, latitude, longitude, height, body);

            return newStationId;
		}

		public static bool RemoveGroundStation(Guid stationid)
		{
			RTLog.Notify ("Trying to remove groundstation {0}", RTLogLevel.API, stationid);

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
            RTLog.Notify("Trying to change groundstation {0} Omni range to {1}", RTLogLevel.API, stationId.ToString(), newRange);

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
                        RTLog.Notify("Ground station Omni range successfuly chaned.", RTLogLevel.API);
                        return true;
                    }                    
                }
            }

            return false;
        }
    }
}
