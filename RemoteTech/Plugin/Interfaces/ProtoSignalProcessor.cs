using System;
using UnityEngine;

namespace RemoteTech {
    internal class ProtoSignalProcessor : ISignalProcessor {
        public bool Powered { get; private set; }
        public bool CommandStation { get; private set; }
        public Guid Guid { get { return Vessel.id; } }
        public Vessel Vessel { get; private set; }

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            Vessel = v;
            Powered = ppms.GetBool("IsPowered");
            CommandStation = Powered && v.HasCommandStation() && Vessel.GetVesselCrew().Count >= 4;
        }
    }
}
