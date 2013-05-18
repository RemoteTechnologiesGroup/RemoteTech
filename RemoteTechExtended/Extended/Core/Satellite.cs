using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class Satellite {
        Dictionary<int, WeakReference<Satellite>> mSingletons = new Dictionary<int, WeakReference<Satellite>>();

        public Satellite Instance(Vessel vessel) {
            if (!mSingletons.ContainsKey(vessel.GetInstanceID()) || mSingletons[vessel.GetInstanceID()] == null) {
                mSingletons[vessel.GetInstanceID()] = new WeakReference<Satellite>(new Satellite(vessel));
            }
            return mSingletons[vessel.GetInstanceID()].Target;
        }

        public String Name { get { return mParent.vesselName; } }
        public IAntenna[] Antennas { get { return FindAntennas(); } }
        public Vector3d Position { get { return mParent.GetWorldPos3D(); } }

        Vessel mParent;

        Satellite(Vessel parent) {
            mParent = parent;
        }

        public void Unload(Vessel vessel) {
            mSingletons.Remove(vessel.GetInstanceID());
        }

        IAntenna[] FindAntennas() {
            List<IAntenna> antennas = new List<IAntenna>();
            foreach (Part p in mParent.parts) {
                if (p.Modules.Contains(typeof(AntennaPartModule).ToString())) {
                    antennas.Add(p.Modules[typeof(AntennaPartModule).ToString()] as IAntenna);
                }
            }
            return antennas.ToArray();
        }

        public float FindMaxOmniRange() {
            float maxOmniRange = 0.0f;
            foreach (IAntenna a in Antennas) {
                if (a.OmniRange > maxOmniRange) {
                    maxOmniRange = a.OmniRange;
                }
            }
            return maxOmniRange;
        }

    }
}
