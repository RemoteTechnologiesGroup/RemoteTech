using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class Satellite : ISatellite {

        public bool Powered { get { return SignalProcessor.Powered; } }
        public String Name {
            get { return SignalProcessor.Name; }
        }
        public Guid Guid { get { return SignalProcessor.Guid; }  }
        public Vector3 Position { get { return SignalProcessor.Position; } }

        public ISignalProcessor SignalProcessor { get; set; }

        public IEnumerable<Pair<Guid, float>> DishRange {
            get {
                foreach (IAntenna a in RTCore.Instance.Antennas.For(this)) {
                    if(!a.Target.Equals(Guid.Empty)) {
                        yield return new Pair<Guid, float>(a.Target, a.DishRange);
                    }
                }
            }
        }

        public float OmniRange {
            get { return RTCore.Instance.Antennas.For(Guid).Max(a => a.OmniRange); }
        }

        public Satellite(ISignalProcessor parent) {
            SignalProcessor = parent;
        }

        public override string ToString() {
            return SignalProcessor.Vessel.vesselName;
        }
    }
}
