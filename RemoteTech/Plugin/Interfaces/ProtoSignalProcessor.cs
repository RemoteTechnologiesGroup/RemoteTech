using System;
using UnityEngine;

namespace RemoteTech {
    internal class ProtoSignalProcessor : ISignalProcessor {
        public bool Powered { get; private set; }

        public bool CommandStation { get; private set; }

        public String Name { get { return Vessel.vesselName; } }

        public Guid Guid { get { return Vessel.id; } }

        public Vector3 Position { get { return Vessel.GetWorldPos3D(); } }

        public CelestialBody Body { get { return Vessel.orbit.referenceBody; } }

        public bool LocalControl { get { return false; } }

        public Vessel Vessel { get; private set; }

        private ProtoPartModuleSnapshot mParent;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            mParent = ppms;
            Vessel = v;
            Powered = ppms.GetBool("IsPowered");
            CommandStation = Powered && v.HasCommandStation() && 
                Vessel.protoVessel.GetVesselCrew().Count >= 4;
        }
    }
}
