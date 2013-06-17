using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class VesselSatellite : ISatellite {

        public ISignalProcessor SignalProcessor { get; set; }
        public Vessel Vessel { get { return SignalProcessor.Vessel; } }
        public bool Active { get { return SignalProcessor.Active; } }
        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(SignalProcessor.Vessel); } }

        public String Name { get { return SignalProcessor.Name; } }
        public Guid Guid { get { return SignalProcessor.Guid; } }
        public Vector3 Position { get { return SignalProcessor.Position; } }
        public CelestialBody Body { get { return SignalProcessor.Body; } }

        public bool LocalControl { get { return SignalProcessor.CrewCount > 0; } }

        public float Omni { get { return RTCore.Instance.Antennas.For(Guid).Max(a => a.OmniRange); } }
        public IEnumerable<Dish> Dishes {
            get {
                foreach (IAntenna a in RTCore.Instance.Antennas.For(this)) {
                    if (a.CanTarget && !a.DishTarget.Equals(Guid.Empty)) {
                        yield return new Dish(a.DishTarget, a.DishFactor, a.DishRange);
                    }
                }
            }
        }

        public VesselSatellite(ISignalProcessor parent) {
            SignalProcessor = parent;
        }

        public override string ToString() {
            return SignalProcessor.Vessel.vesselName;
        }
    }
}
