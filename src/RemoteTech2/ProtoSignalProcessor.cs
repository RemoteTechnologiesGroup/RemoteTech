using System;
using UnityEngine;

namespace RemoteTech
{
    public class ProtoSignalProcessor : ISignalProcessor
    {
        public String Name { get { return String.Format("ProtoSignalProcessor({0})", VesselName); } }
        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(mVessel); } }
        public CelestialBody Body { get { return mVessel.mainBody; } }
        public Vector3 Position { get { return mVessel.GetWorldPos3D(); } }
        public String VesselName { get { return mVessel.vesselName; } set { mVessel.vesselName = value; } }
        public bool VesselLoaded { get { return false; } }
        public bool Powered { get; private set; }
        public bool IsCommandStation { get; private set; }
        public Guid Guid { get { return mVessel.id; } }
        public Vessel Vessel { get { return mVessel; } }
        public FlightComputer FlightComputer { get { return null; } }

        private readonly Vessel mVessel;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v)
        {
            mVessel = v;
            Powered = ppms.GetBool("IsRTPowered");
            IsCommandStation = Powered && v.HasCommandStation() && v.GetVesselCrew().Count >= 6;
            RTUtil.Log("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2})",
                Powered, v.HasCommandStation(), v.GetVesselCrew().Count);
        }

        public override String ToString()
        {
            return Name;
        }
    }
}