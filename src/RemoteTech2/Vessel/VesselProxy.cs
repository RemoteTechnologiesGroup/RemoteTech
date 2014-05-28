using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class VesselProxy : IVessel, IEquatable<VesselProxy>
    {
        String IVessel.Name             { get { return vessel.vesselName; } }
        Guid IVessel.Guid               { get { return vessel.id; } }
        Vector3d IVessel.Position       { get { return vessel.GetWorldPos3D(); } }
        ICelestialBody IVessel.Body     { get { return (CelestialBodyProxy) vessel.mainBody; } }
        int IVessel.CrewCount           { get { return vessel.GetVesselCrew().Count; } }
        IEnumerable<Part> IVessel.Parts { get { return vessel.Parts; } }
        ProtoVessel IVessel.Proto       { get { return vessel.protoVessel; } }
        bool IVessel.IsLoaded           { get { return vessel.loaded; } }
        bool IVessel.IsPacked           { get { return vessel.packed; } }
        bool IVessel.IsEVA              { get { return vessel.isEVA; } }
        bool IVessel.IsControllable     { get { return vessel.IsControllable; } }
        bool IVessel.IsVisible          { get { return MapViewFiltering.CheckAgainstFilter(vessel); } }

        event FlightInputCallback IVessel.FlyByWire
        {
            add { vessel.OnFlyByWire += value; }
            remove { vessel.OnFlyByWire -= value; }
        }

        private readonly Vessel vessel;

        private VesselProxy(Vessel vessel) {
            this.vessel = vessel;
        }

        public override int GetHashCode()
        {
            return vessel.GetHashCode();
        }

        public bool Equals(VesselProxy other)
        {
            return Object.Equals(vessel, other.vessel);

        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var v = obj as VesselProxy;
            if (v == null)
                return false;
            else
                return Equals(v);
        }

        public static bool operator ==(VesselProxy a, VesselProxy b)
        {
            return Object.ReferenceEquals(a.vessel, b.vessel);
        }

        public static bool operator !=(VesselProxy a, VesselProxy b)
        {
            return !(a == b);
        }

        public static explicit operator VesselProxy(Vessel vessel)
        {
            return new VesselProxy(vessel);
        }
    }
}
