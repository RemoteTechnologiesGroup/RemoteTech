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
        /// <summary>Gets whether or not the satellite if visible in the Tracking station or the Flight Map view.</summary>
        public bool Visible => SignalProcessor.Visible;

        /// <summary>Gets or sets the name of the satellite.</summary>
        public string Name
        {
            get { return SignalProcessor.VesselName; }
            set { SignalProcessor.VesselName = value; }
        }

        /// <summary>Gets the satellite id.</summary>
        public Guid Guid => SignalProcessor.VesselId;

        /// <summary>Get a double precision vector for the vessel's world space position.</summary>
        public Vector3d Position => SignalProcessor.Position;

        /// <summary>Gets the celestial body around which the satellite is orbiting.</summary>
        public CelestialBody Body => SignalProcessor.Body;

        /// <summary>Gets the color of the ground station mark in Tracking station or Flight map view.</summary>
        public Color MarkColor => RTSettings.Instance.RemoteStationColorDot;

        /// <summary>Gets or sets the list of signal processor (<see cref="ISignalProcessor"/>) for the satellite.</summary>
        public List<ISignalProcessor> SignalProcessors { get; set; }

        /// <summary>Gets if the satellite is actually powered or not.</summary>
        public bool Powered
        {
            get { return SignalProcessors.Any(s => s.Powered); }
        }

        /// <summary>Gets if the satellite is a RemoteTech command station.</summary>
        public bool IsCommandStation
        {
            get { return SignalProcessors.Any(s => s.IsCommandStation); }
        }

        /// <summary>Gets a signal processor.</summary>
        public ISignalProcessor SignalProcessor
        {
            get
            {
                return SignalProcessors.FirstOrDefault(s => s.FlightComputer != null) ?? SignalProcessors[0];
            }
        }

        /// <summary>Gets whether the satellite has local control or not (that is, if it is locally controlled or not).</summary>
        public bool HasLocalControl
        {
            get
            {
                return RTUtil.CachePerFrame(ref _localControl, () => SignalProcessor.Vessel.HasLocalControl());
            }
        }

        /// <summary>Indicates whether the ISatellite corresponds to a vessel.</summary>
        /// <value><c>true</c> if satellite is vessel or asteroid; otherwise (e.g. a ground station), <c>false</c>.</value>
        /// <remarks>Implementation note: always return true for a <see cref="VesselSatellite"/>.</remarks>
        public bool isVessel => true;

        /// <summary>The vessel hosting the satellite.</summary>
        /// <value>The vessel corresponding to this ISatellite. Returns null if !isVessel.</value>
        public Vessel parentVessel => SignalProcessor.Vessel;

        /// <summary>Gets a list of antennas for this satellite.</summary>
        public IEnumerable<IAntenna> Antennas => RTCore.Instance.Antennas[this];

        /// <summary>Gets the flight computer for this satellite.</summary>
        public FlightComputer.FlightComputer FlightComputer => SignalProcessor.FlightComputer;

        /*
         * Helpers
         */

        /// <summary>List of network routes for the satellite.</summary>
        public List<NetworkRoute<ISatellite>> Connections => RTCore.Instance.Network[this];

        /// <summary>Called on connection refresh to update the connections.</summary>
        /// <param name="routes">List of network routes.</param>
        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            }
        }

        /// <summary>Local control cache variable.</summary>
        private CachedField<bool> _localControl;

        /*
         * Methods
         */

        /// <summary>Build a new instance of VesselSatellite.</summary>
        /// <param name="signalProcessors">List of signal processor for this satellites. Can't be null.</param>
        public VesselSatellite(List<ISignalProcessor> signalProcessors)
        {
            if (signalProcessors == null)
            {
                RTLog.Notify("VesselSatellite constructor: signalProcessor parameter is null", RTLogLevel.LVL4);
                throw new ArgumentNullException();
            }

            SignalProcessors = signalProcessors;
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