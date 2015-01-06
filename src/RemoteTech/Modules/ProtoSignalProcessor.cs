using System;
using UnityEngine;

namespace RemoteTech
{
    public class ProtoSignalProcessor : ISignalProcessor
    {
        private readonly ProtoPartModuleSnapshot protoPart;
        private readonly Vessel vessel;

        public String Name { get { return String.Format("ProtoSignalProcessor({0})", VesselName); } }

        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(vessel); } }

        public CelestialBody Body { get { return vessel.mainBody; } }

        public Vector3 Position { get { return vessel.GetWorldPos3D(); } }

        public String VesselName { get { return vessel.vesselName; } set { vessel.vesselName = value; } }

        public bool VesselLoaded { get { return false; } }

        public bool Powered { get; private set; }

        public bool IsCommandStation { get; private set; }

        public Guid Guid { get { return vessel.id; } }

        public Vessel Vessel { get { return vessel; } }

        public FlightComputer FlightComputer { get { return null; } }

        public bool IsMaster { get { return true; } }


        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v)
        {
            protoPart = ppms;
            vessel = v;
            Initialize();
        }

        private void Initialize()
        {
            Powered = protoPart.GetBool("IsRTPowered");

            int commandMinCrew = RTSettings.Instance.DefaultMinimumCrew;

            try
            {
                commandMinCrew = protoPart.GetInt("RTCommandMinCrew");
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/{3})",
                    Powered, vessel.HasCommandStation(), vessel.GetVesselCrew().Count, commandMinCrew);
            }
            catch (ArgumentException)
            {
                // I'm assuming this would get thrown by ppms.GetInt()... do the other functions have an exception spec?
                RTLog.Notify("ProtoSignalProcessor(Powered: {0}, HasCommandStation: {1}, Crew: {2}/{3} [Default]",
                    Powered, vessel.HasCommandStation(), vessel.GetVesselCrew().Count, RTSettings.Instance.DefaultMinimumCrew);
            }
            var hasEnoughCrew = vessel.GetVesselCrew().Count >= Math.Max(commandMinCrew, 1);
            IsCommandStation = Powered && vessel.HasCommandStation() && hasEnoughCrew;
        }

        public override String ToString()
        {
            return Name;
        }
    }
}