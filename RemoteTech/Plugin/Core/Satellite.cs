using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class Satellite : ISatellite {

        public String Name {
            get { return SignalProcessor.Vessel.vesselName; }
            set { SignalProcessor.Vessel.vesselName = value; }
        }

        public Guid Guid {
            get {
                if (!SignalProcessor.Vessel.loaded) {
                    return SignalProcessor.Vessel.protoVessel.vesselID;
                } else {
                    return SignalProcessor.Vessel.id;
                }
            }
        }
        public Vector3 Position {
            get { return SignalProcessor.Vessel.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime()); }
        }

        public ISignalProcessor SignalProcessor { get; set; }

        public bool Powered { get { return SignalProcessor.Powered; } }

        public IEnumerable<Pair<Guid, float>> DishRange {
            get {
                foreach (IAntenna a in RTCore.Instance.Antennas.For(this)) {
                    yield return new Pair<Guid, float>(a.Target, a.DishRange);
                }
            }
        }

        public float OmniRange {
            get { return RTCore.Instance.Antennas.For(SignalProcessor.Vessel).Max(a => a.OmniRange); }
        }

        public Satellite(ISignalProcessor parent) {
            SignalProcessor = parent;
        }

        public override string ToString() {
            return SignalProcessor.Vessel.id.ToString();
        }
    }
}
