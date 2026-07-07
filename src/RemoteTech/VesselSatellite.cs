using RemoteTech.Modules;
using RemoteTech.SimpleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    /// <summary>
    /// Represents a satellite. The concept of satellite is similar to a vessel.
    /// </summary>
    public class VesselSatellite : ISatellite
    {
        public Vessel Vessel { get; private set; }

        /// <summary>
        /// Gets whether or not the satellite is visible in the Tracking station or the Flight Map view.
        /// </summary>
        public bool Visible => MapViewFiltering.CheckAgainstFilter(Vessel);

        /// <summary>
        /// Gets or sets the name of the satellite.
        /// </summary>
        public string Name
        {
            get => Vessel.vesselName;
            set => Vessel.vesselName = value;
        }

        /// <summary>
        /// Gets the satellite id.
        /// </summary>
        public Guid Guid => Vessel.id;

        /// <summary>
        /// Get a double precision vector for the vessel's world space position.
        /// </summary>
        public Vector3d Position => Vessel.GetWorldPos3D();

        /// <summary>
        /// Gets the celestial body around which the satellite is orbiting.
        /// </summary>
        public CelestialBody Body => Vessel.mainBody;

        /// <summary>
        /// Gets the color of the ground station mark in Tracking station or Flight map view.
        /// </summary>
        public Color MarkColor => RTSettings.Instance.RemoteStationColorDot;

        /// <summary>
        /// Gets or sets the list of signal processor (<see cref="ISignalProcessor"/>) for the satellite.
        /// </summary>
        public List<ISignalProcessor> SignalProcessors { get; set; }

        /// <summary>
        /// Gets if the satellite is actually powered or not.
        /// </summary>
        public bool Powered
        {
            get
            {
                if (PowerShutdownFlag)
                    return false;

                foreach (var s in SignalProcessors)
                {
                    if (s.Powered)
                        return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Gets if the satellite is capable to forward other signals.
        /// </summary>
        public bool CanRelaySignal
        {
            get
            {
                if (!RTSettings.Instance.SignalRelayEnabled)
                    return true;

                foreach (var s in SignalProcessors)
                {
                    if (s is ModuleSPUPassive)
                        continue;

                    if (s.CanRelaySignal)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates whether the satellite is in radio blackout.
        /// </summary>
        public bool IsInRadioBlackout { get; set; }

        /// <summary>
        /// Indicates whether the manual power override is engaged.
        /// </summary>
        public bool PowerShutdownFlag { get; set; }

        /// <summary>
        /// Gets if the satellite is a RemoteTech command station.
        /// </summary>
        public bool IsCommandStation
        {
            get
            {
                foreach (var s in SignalProcessors)
                {
                    if (s.IsCommandStation)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a signal processor.
        /// </summary>
        public ISignalProcessor SignalProcessor
        {
            get
            {
                foreach (var s in SignalProcessors)
                {
                    if (s.FlightComputer is not null)
                        return s;
                }

                if (SignalProcessors.Count != 0)
                    return SignalProcessors[0];

                return null;
            }
        }

        /// <summary>
        /// Local control cache variable.
        /// </summary>
        private CachedField<bool> _localControl;

        /// <summary>
        /// Gets whether the satellite has local control or not (that is, if it is locally controlled or not).
        /// </summary>
        public bool HasLocalControl
        {
            get
            {
                if (RTUtil.ShouldUpdateCache(ref _localControl))
                    _localControl.Field = Vessel.HasLocalControl();

                return _localControl.Field;
            }
        }

        /// <summary>
        /// Indicates whether the ISatellite corresponds to a vessel.
        /// </summary>
        /// <value><c>true</c> if satellite is vessel or asteroid; otherwise (e.g. a ground station), <c>false</c>.</value>
        /// <remarks>Implementation note: always return true for a <see cref="VesselSatellite"/>.</remarks>
        public bool isVessel => true;

        /// <summary>
        /// The vessel hosting the satellite.
        /// </summary>
        /// <value>The vessel corresponding to this ISatellite. Returns null if !isVessel.</value>
        public Vessel parentVessel => Vessel;

        /// <summary>
        /// Gets a list of antennas for this satellite.
        /// </summary>
        public IReadOnlyList<IAntenna> Antennas => RTCore.Instance.Antennas[this];

        /// <summary>
        /// Gets the flight computer for this satellite.
        /// </summary>
        public FlightComputer.FlightComputer FlightComputer => SignalProcessor.FlightComputer;

        /*
         * Helpers
         */

        /// <summary>
        /// Called on connection refresh to update the connections.
        /// </summary>
        /// <param name="routes">List of network routes.</param>
        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            }
        }

        /*
         * Methods
         */

        /// <summary>
        /// Build a new instance of VesselSatellite.
        /// </summary>
        /// <param name="signalProcessors">List of signal processor for this satellites. Can't be null.</param>
        public VesselSatellite(Vessel vessel, List<ISignalProcessor> signalProcessors)
        {
            if (signalProcessors == null)
            {
                RTLog.Notify("VesselSatellite constructor: signalProcessor parameter is null", RTLogLevel.LVL4);
                throw new ArgumentNullException();
            }

            Vessel = vessel;
            SignalProcessors = signalProcessors;
        }

        public SatelliteState GetState()
        {
            return new SatelliteState
            {
                Guid = Guid,
                Body = RTUtil.Guid(Body),
                Position = Vessel.CoMD,
                Powered = Powered,
                IsCommandStation = IsCommandStation,
                CanRelaySignal = CanRelaySignal,
                IsInRadioBlackout = IsInRadioBlackout,
            };
        }

        public override string ToString()
        {
            return $"VesselSatellite({Name}, {Guid})";
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}