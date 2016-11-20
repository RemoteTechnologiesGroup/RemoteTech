using System;
using RemoteTech.Common.Interfaces.FlightComputer;
using UnityEngine;

namespace RemoteTech.Common.Interfaces.SignalProcessor
{
    public interface ISignalProcessor
    {
        string Name { get; }
        string VesselName { get; set; }
        bool VesselLoaded { get; }
        Guid VesselId { get; }
        Vector3 Position { get; }
        CelestialBody Body { get; }

        bool Visible { get; }
        bool Powered { get; }
        bool IsCommandStation { get; }
        bool IsMaster { get; }

        // Reserved for Flight Computer
        IFlightComputer FlightComputer { get; }
        Vessel Vessel { get; }
    }
}