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

        public ISignalProcessor SignalProcessor { get { return SignalProcessors[0]; } }
        public List<ISignalProcessor> SignalProcessors { get; set; }
        public bool Powered { get { return SignalProcessors.Any(s => s.Powered); } }
        public bool IsCommandStation { get { return SignalProcessors.Any(s => s.IsCommandStation); } }

        public IEnumerable<IAntenna> Antennas
        {
            get
            {
                return RTCore.Instance.Antennas[this];
            }
        }

        public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes)
        {
            foreach (IAntenna a in Antennas)
            {
                a.OnConnectionRefresh();
            } 
        }

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