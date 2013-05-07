using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class SPUPartModule : PartModule, ISatellite {

        // Properties
        public IAntenna[] Antennas { get { return FindAntennas(); } }
        public Vector3d Position { get { return this.WorldPosition; } }

        // Fields
        SatelliteNetwork mNetwork;

        // Constructor
        public SPUPartModule() {
            mNetwork = SatelliteNetwork.Instance;
        }

        // Interface Methods
        IAntenna[] FindAntennas() {
            List<IAntenna> antennas = new List<IAntenna>();
            foreach (Part p in vessel.parts) {
                if (p.Modules.Contains(typeof(AntennaPartModule).ToString())) {
                    antennas.Add(p.Modules[typeof(AntennaPartModule).ToString()] as IAntenna);
                }
            }
            return antennas.ToArray();
        }
        
        public float FindMaxOmniRange() {
            float maxOmniRange = 0.0f;
            foreach (IAntenna a in Antennas) {
                if (a.OmniRange > maxOmniRange) {
                    maxOmniRange = a.OmniRange;
                }
            }
            return maxOmniRange;
        }

        public long Enqueue(AttitudeChange change) {
            return 0;
        }

        public long Enqueue(TrottleChange change) {
            return 0;
        }

        // Misc
        public override int GetHashCode() {
            return this.vessel.GetInstanceID();
        }
    }

    public static class SPUExtensions {
        // Vessel extensions
        public static SPUPartModule FindSPU(this Vessel vessel) {
            foreach (Part p in vessel.parts) {
                if (p.Modules.Contains(typeof(SPUPartModule).ToString())) {
                    return p.Modules[typeof(SPUPartModule).ToString()] as SPUPartModule;
                }
            }
            return null;
        }
    }
}



