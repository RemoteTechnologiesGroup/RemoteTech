using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class Satellite : ISatellite {
        public bool Powered { get { return SignalProcessor.Powered; } }
        public String Name { get { return SignalProcessor.Name; } }
        public Guid Guid { get { return SignalProcessor.Guid; } }
        public Vector3 Position { get { return SignalProcessor.Position; } }
        public CelestialBody Body { get { return SignalProcessor.Body; } }
        public ISignalProcessor SignalProcessor { get; set; }
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

        public Satellite(ISignalProcessor parent) {
            SignalProcessor = parent;
        }

        public override string ToString() {
            return SignalProcessor.Vessel.vesselName;
        }
    }
}
