using System;
using UnityEngine;

namespace RemoteTech.Modules
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
        public FlightComputer.FlightComputer FlightComputer { get { return null; } }
        public bool IsMaster { get { return true; } }

        private readonly Vessel mVessel;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v)
        {
            mVessel = v;
            Powered = ppms.GetBool("IsRTPowered");

            // get the crew count from the vessel
            int crewcount = v.GetVesselCrew().Count;
            // when the crewcount is eq 0 than look into the protoVessel
            if (crewcount == 0 && v.protoVessel.GetVesselCrew() != null)
                crewcount = v.protoVessel.GetVesselCrew().Count;

            RTLog.Notify("ProtoSignalProcessor crew count of {0} is {1}", v.vesselName, crewcount);

            try {
                IsCommandStation = Powered && v.HasCommandStation() && crewcount >= ppms.GetInt("RTCommandMinCrew");
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/{3})",
                    Powered, v.HasCommandStation(), crewcount, ppms.GetInt("RTCommandMinCrew"));
            } catch (ArgumentException argexeception) {
                // I'm assuming this would get thrown by ppms.GetInt()... do the other functions have an exception spec?
                IsCommandStation = false;
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2})",
                    Powered, v.HasCommandStation(), crewcount);
                RTLog.Notify("ProtoSignalProcessor ", argexeception);
            }
        }

        public override String ToString()
        {
            return Name;
        }
    }
}