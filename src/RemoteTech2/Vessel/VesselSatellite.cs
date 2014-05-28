using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class VesselSatellite : ISatellite
    {
        public String Name { 
            get { return SignalProcessor.Vessel.Name; } 
        }
        public Guid Guid { get; private set; }

        public bool IsVisible { get { return SignalProcessor.Vessel.IsVisible; } }
        public Group Group
        {
            get { return group; }
            set { group = RTCore.Instance.Groups.Register(this, value); }
        }
        public Vector3d Position { 
            get { return SignalProcessor.Vessel.Position; }
        }
        public ICelestialBody Body { 
            get { return SignalProcessor.Vessel.Body; }
        }
        public List<ISignalProcessor> SignalProcessors { get; set; }
        public bool IsPowered { 
            get { return SignalProcessors.Any(s => s.Powered); } 
        }
        public bool IsCommandStation { 
            get { return SignalProcessors.Any(s => s.IsCommandStation == true); } 
        }
        public ISignalProcessor SignalProcessor
        { 
            get
            { 
                return signalProcessor.Cache(() => {
                    return SignalProcessors.FirstOrDefault(s => s.FlightComputer != null) ?? SignalProcessors[0];
                });
            }
        }
        public bool HasLocalControl
        {
            get
            {
                return localControl.Cache(() => {
                    return SignalProcessor.Vessel.Parts.Any(p => p.isControlSource && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
                });
            }
        }
        public IEnumerable<IAntenna> Antennas
        {
            get { return RTCore.Instance.Antennas[this].Cast<IAntenna>(); }
        }
        public FlightComputer FlightComputer { 
            get { return SignalProcessor.FlightComputer; } 
        }

        private CachedField<ISignalProcessor> signalProcessor = new CachedField<ISignalProcessor>();
        private CachedField<bool> localControl = new CachedField<bool>();
        private Group group;
        private readonly int hash;
        public VesselSatellite(Guid key, List<ISignalProcessor> parts)
        {
            if (parts == null) throw new ArgumentNullException();
            SignalProcessors = parts;
            Guid = key;
            hash = Guid.GetHashCode();
        }

        public override String ToString()
        {
            return String.Format("VesselSatellite({0})", Guid);
        }

        public override int GetHashCode()
        {
            return hash;
        }
    }
}