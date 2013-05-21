using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class Satellite : ISatellite {
        static Dictionary<int, Satellite> mSingletons = new Dictionary<int, Satellite>();

        public static Satellite Instance(PartModule part) {
            int id = part.vessel.GetInstanceID();
            if (!mSingletons.ContainsKey(id) || mSingletons[id] == null) {
                mSingletons[id] = new Satellite(part);
            }
            return mSingletons[id];
        }

        public static void Unclaim(Vessel v, PartModule p) {
            if(mSingletons[v.GetInstanceID()].Owner == p){
                mSingletons.Remove(v.GetInstanceID());
            }
        }

        public static IEnumerable<Satellite> FindAll() {
            return mSingletons.Values;
        }

        public String Name { get { return Owner.vessel.vesselName; } }
        public IAntenna[] Antennas { get { return FindAntennas(); } }
        public Vector3d Position { 
            get {
                return Owner.vessel.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime());
            }
        }

        public PartModule Owner { get; private set; }

        Satellite(PartModule parent) {
            Owner = parent;
        }

        IAntenna[] FindAntennas() {
            List<IAntenna> antennas = new List<IAntenna>();
            foreach (Part p in Owner.vessel.parts) {
                if (p.Modules.Contains(typeof(AntennaPartModule).Name)) {
                    antennas.Add(p.Modules[typeof(AntennaPartModule).Name] as IAntenna);
                }
            }
            return antennas.ToArray();
        }

        public double FindMaxOmniRange() {
            double maxOmniRange = 0.0;
            foreach (IAntenna a in Antennas) {
                if (a.OmniRange > maxOmniRange) {
                    maxOmniRange = a.OmniRange;
                }
            }
            return maxOmniRange;
        }

        public double IsPointingAt(ISatellite a) {
            double range = 0.0;
            foreach (IAntenna antenna in Antennas) {
                if(antenna.Target.Equals(a.Name)) {
                    range = antenna.DishRange;
                }
            }
            return range;
        }

        public override int GetHashCode() {
            return Owner.GetInstanceID();
        }

        public override string ToString() {
            return "{ " + Name + " }";
        }
    }
}
