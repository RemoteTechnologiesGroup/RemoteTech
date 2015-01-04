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
        public bool IsMaster { get { return true; } }

		public const int DEFAULT_COMMAND_MINIMUM_CREW = 6;
        private readonly Vessel mVessel;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v)
        {
            mVessel = v;
            Powered = ppms.GetBool("IsRTPowered");

			int commandMinCrew = DEFAULT_COMMAND_MINIMUM_CREW;
			
            try {
                commandMinCrew = ppms.GetInt("RTCommandMinCrew");
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/{3})", 
                    Powered, v.HasCommandStation(), v.GetVesselCrew().Count, commandMinCrew));
            } catch (ArgumentException) {
                // I'm assuming this would get thrown by ppms.GetInt()... do the other functions have an exception spec?
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/6 [Default]", 
                    Powered, v.HasCommandStation(), v.GetVesselCrew().Count);
            }
			IsCommandStation = Powered && v.HasCommandStation() && v.GetVesselCrew().Count >= commandMinCrew;
        }

        public override String ToString()
        {
            return Name;
        }
    }
}