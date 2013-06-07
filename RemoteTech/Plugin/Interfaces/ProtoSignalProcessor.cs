using System;
using UnityEngine;

namespace RemoteTech {
    class ProtoSignalProcessor : ISignalProcessor {

        public String Name {
            get { return Vessel.vesselName; }
        }
        public Guid Guid { get { return Vessel.id; } }
        public Vector3 Position {
            get {
                return Vessel.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime());
            }
        }
        public Vessel Vessel { get; private set; }
        public bool Powered { get; private set; }

        ProtoPartModuleSnapshot mParent;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            mParent = ppms;
            Vessel = v;
            Powered = ppms.GetBool("IsPowered");
        }
    }
}
