using System;
using UnityEngine;

namespace RemoteTech.Modules
{
    public class ProtoSignalProcessor : ISignalProcessor
    {
        public string Name { get { return $"ProtoSignalProcessor({VesselName})"; } }
        public bool Visible => MapViewFiltering.CheckAgainstFilter(Vessel);
        public CelestialBody Body => Vessel.mainBody;
        public Vector3 Position => Vessel.GetWorldPos3D();
        public string VesselName
        {
            get { return Vessel.vesselName; }
            set { Vessel.vesselName = value; }
        }
        public bool VesselLoaded => false;
        public bool Powered { get; }
        public bool IsCommandStation { get; }
        public Guid VesselId => Vessel.id;
        public Vessel Vessel { get; }
        public FlightComputer.FlightComputer FlightComputer => null;
        public bool IsMaster => true;

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v)
        {
            Vessel = v;
            Powered = ppms.GetBool("IsRTPowered");

            // get the crew count from the vessel
            var crewcount = v.GetVesselCrew().Count;

            // when the crew count is equal to 0 then look into the protoVessel
            if (crewcount == 0 && v.protoVessel.GetVesselCrew() != null)
                crewcount = v.protoVessel.GetVesselCrew().Count;

            RTLog.Notify("ProtoSignalProcessor crew count of {0} is {1}", v.vesselName, crewcount);

            int ppmsMinCrew;
            //try to get the RTCommandMinCrew value in the ProtoPartModuleSnapshot
            if (ppms.GetInt("RTCommandMinCrew", out ppmsMinCrew))
            {
                IsCommandStation = Powered && v.HasCommandStation() && crewcount >= ppmsMinCrew;
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/{3})",
                    Powered, v.HasCommandStation(), crewcount, ppmsMinCrew);
            }
            else
            {
                // there was no RTCommandMinCrew member in the ProtoPartModuleSnapshot
                IsCommandStation = false;

                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2})",
                    Powered, v.HasCommandStation(), crewcount);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}