using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class VesselSatellite : ISatellite
    {
        public bool Visible { get { return SignalProcessor.Visible; } }
        public String Name { get { return SignalProcessor.VesselName; } set { SignalProcessor.VesselName = value; } }
        public Guid Guid { get { return SignalProcessor.Guid; } }
        public Vector3 Position { get { return SignalProcessor.Position; } }
        public CelestialBody Body { get { return SignalProcessor.Body; } }
        public List<ISignalProcessor> SignalProcessors { get; set; }
        public ISignalProcessor SignalProcessor { get { return SignalProcessors.FirstOrDefault(s => s.FlightComputer != null) ?? SignalProcessors[0]; } }

        public bool Powered { get { return SignalProcessors.Any(s => s.Powered); } }
        public bool IsCommandStation { get { return SignalProcessors.Any(s => s.IsCommandStation); } }

        public bool HasLocalControl
        {
            get
            {
                if (mLastFrame != Time.frameCount)
                {
                    mLastFrame = Time.frameCount;
                    return mLastLocalControl = SignalProcessor.Vessel.parts.Any(p => p.isControlSource && !p.FindModulesImplementing<ISignalProcessor>().Any());
                }
                else
                {
                    return mLastLocalControl;
                }
            }
        }


        public IEnumerable<IAntenna> Antennas
        {
            get
            {
                return RTCore.Instance.Antennas[this];
            }
        }

        public FlightComputer FlightComputer { 
            get 
            {
                if (SignalProcessor.FlightComputer != null)
                {
                    return SignalProcessor.FlightComputer;
                }
                else
                {
                    return null;
                }
            } 
        }

        // Helpers
        public List<NetworkRoute<ISatellite>> Connections { get { return RTCore.Instance.Network[this]; } }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            } 
        }

        private int mLastFrame;
        private bool mLastLocalControl;
        public VesselSatellite(List<ISignalProcessor> parts)
        {
            if (parts == null) throw new ArgumentNullException();
            SignalProcessors = parts;
        }

        public override String ToString()
        {
            return String.Format("VesselSatellite({0})", Name);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}