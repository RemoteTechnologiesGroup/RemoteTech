using System;
using UnityEngine;

namespace RemoteTech {
    public class ProtoSignalProcessor : ISignalProcessor {
        public bool Powered { get; private set; }
        public bool CommandStation { get; private set; }
        public Guid Guid { get { return Vessel.id; } }
        public Vessel Vessel { get; private set; }
        public VesselSatellite Satellite {
            get {
                return RTCore.Instance.Satellites[Vessel];
            }
        }

        public bool Master {
            get {
                return true;
            }
        }

        public FlightComputer FlightComputer {
            get { return null; }
        }

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            Vessel = v;
            Powered = ppms.GetBool("IsRTPowered");
            CommandStation = Powered && v.HasCommandStation() && v.GetVesselCrew().Count >= 6;
            RTUtil.Log("ProtoSignalProcessor: Powered: {0}, CommandStation: {1}, Crew: {2}",
                Powered, v.HasCommandStation(), v.GetVesselCrew().Count);
        }
    }
}
