using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class Satellite : ISatellite {

        public String Name { get { return mParent.vesselName; } }
        public Guid Guid {
            get {
                if (!mParent.loaded) {
                    return mParent.protoVessel.vesselID;
                } else {
                    return mParent.id;
                }
            }
        }
        public Vector3 Position {
            get { return mParent.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime()); }
        }
        public Vessel Vessel { get { return mParent; } }

        public IEnumerable<Pair<Guid, float>> DishRange {
            get {
                foreach (IAntenna a in RTCore.Instance.Antennas.For(mParent)) {
                    yield return new Pair<Guid, float>(a.Target, a.DishRange);
                }
            }
        }

        public float OmniRange {
            get { return RTCore.Instance.Antennas.For(mParent).Max(a => a.OmniRange); }
        }

        Vessel mParent;

        public Satellite(Vessel parent) {
            mParent = parent;
        }

        public override int GetHashCode() {
            return mParent.GetInstanceID();
        }

        public override string ToString() {
            return mParent.id.ToString();
        }
    }
}
