using System;
using UnityEngine;

namespace RemoteTech {
    internal class ProtoSignalProcessor : ISignalProcessor {
        public bool Active { get; private set; }

        public String Name { get { return Vessel.vesselName; } }
        public Guid Guid { get { return Vessel.id; } }
        public Vector3 Position { get { return Vessel.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime()); } }
        public CelestialBody Body { get { return Vessel.orbit.referenceBody; } }

        public int CrewCount { get { return Vessel.protoVessel.GetVesselCrew().Count; } }

        public Vessel Vessel { get; private set; }

        private ProtoPartModuleSnapshot mParent;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            mParent = ppms;
            Vessel = v;
            Active = ppms.GetBool("IsPowered");
        }
    }
}
