using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class VesselSatellite : ISatellite
    {
        public bool Visible { 
            get { return SignalProcessor.Visible; }
        }

        public String Name { 
            get { return SignalProcessor.VesselName; } 
            set { SignalProcessor.VesselName = value; }
        }

        public Guid Guid { 
            get { return SignalProcessor.Guid; }
        }

        public Vector3d Position { 
            get { return SignalProcessor.Position; }
        }

        public CelestialBody Body { 
            get { return SignalProcessor.Body; }
        }

        public List<ISignalProcessor> SignalProcessors { get; set; }

        public bool Powered { 
            get { return SignalProcessors.Any(s => s.Powered); } 
        }

        public bool IsCommandStation { 
            get { return SignalProcessors.Any(s => s.IsCommandStation); } 
        }

        public ISignalProcessor SignalProcessor
        { 
            get {
                return SignalProcessors.FirstOrDefault(s => s.FlightComputer != null) ?? SignalProcessors[0];
            }
        }

        public bool HasLocalControl
        {
            get
            {
                return RTUtil.CachePerFrame(ref mLocalControl, () =>
                {
                    return SignalProcessor.Vessel.parts.Any(p => p.isControlSource && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
                });
            }
        }

        public bool isVessel { get { return true; } }

        public VesselType getType {
            get {
                return SignalProcessor.Vessel.vesselType;
            }
        }

        public IEnumerable<IAntenna> Antennas
        {
            get { return RTCore.Instance.Antennas[this]; }
        }

        public FlightComputer FlightComputer { 
            get { return SignalProcessor.FlightComputer; } 
        }

        // Helpers
        public List<NetworkRoute<ISatellite>> Connections { 
            get { return RTCore.Instance.Network[this]; } 
        }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            } 
        }

        private CachedField<ISignalProcessor> mSignalProcessor;
        private CachedField<bool> mLocalControl;

        public VesselSatellite(List<ISignalProcessor> parts)
        {
            if (parts == null) throw new ArgumentNullException();
            SignalProcessors = parts;
        }

        public override String ToString()
        {
            return String.Format("VesselSatellite({0}, {1})", Name, Guid);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}