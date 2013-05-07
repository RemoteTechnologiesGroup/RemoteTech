using System;
using System.Collections.Generic;

namespace RemoteTechExtended
{
    public class RemoteCommandModule : PartModule {

        SatelliteNetwork mNetwork;

        public RemoteCommandModule() {
            mNetwork = SatelliteNetwork.Instance;
        }

        /// <summary>
        /// Enqueue a change in Attitude 
        /// </summary>
        /// <param name="change">Change.</param>
        /// <returns>When the action will be applied, in game time.</returns>
        public long Enqueue(AttitudeChange change) {
            return 0;
        }

        public long Enqueue(TrottleChange change) {
            return 0;
        }

        List<AntennaPartModule> GetAntennas() {
            List<AntennaPartModule> antennas = new List<AntennaPartModule>();
            foreach (Part p in this.vessel.Parts) {
                if (p.Modules.Contains(typeof(AntennaPartModule).ToString())) {
                    antennas.Add(p);
                }
            }
            return antennas; 
        }

        void Hook() {
            // Override right-click properties of all other modules to add delays?
        }

    }
}

