using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface IVessel
    {
        String Name             { get; }
        Guid Guid               { get; }
        ICelestialBody Body     { get; }
        Vector3d Position       { get; }
        int CrewCount           { get; }
        IEnumerable<Part> Parts { get; }
        ProtoVessel Proto       { get; }
        bool IsPacked           { get; }
        bool IsLoaded           { get; }
        bool IsEVA              { get; }
        bool IsControllable     { get; }
        bool IsVisible          { get; }

        event FlightInputCallback FlyByWire;
    }
}
