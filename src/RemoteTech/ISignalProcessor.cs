using System;
using UnityEngine;

namespace RemoteTech
{
    public interface ISignalProcessor
    {
        String Name { get; }
        String VesselName { get; set; }
        bool VesselLoaded { get; }
        Guid VesselId { get; }
        Vector3 Position { get; }
        CelestialBody Body { get; }

        bool Visible { get; }
        bool Powered { get; }
        bool IsCommandStation { get; }
        bool IsMaster { get; }

        // Reserved for Flight Computer
        FlightComputer.FlightComputer FlightComputer { get; }
        Vessel Vessel { get; }
    }
}