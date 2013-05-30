using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class MissionControlSatellite : ISatellite {

        public String Name {
            get { return "RTCore_MissionControlSatellite"; }
        }
        public Guid Guid { get; private set; }
        public Vector3 Position {
            get { 
                return FlightGlobals.Bodies[1].position + 600094 * 
                       FlightGlobals.Bodies[1].GetSurfaceNVector(-0.11641926192966, -74.606391806057);
            }
        }

        public Vessel Vessel {
            get {
                return null;
            }
        }

        public IEnumerable<Pair<Guid, float>> DishRange {
            get { return null; }
        }

        public float OmniRange {
            get { return 9000000; }
        }

        public MissionControlSatellite() {
            Guid = new Guid("5105f5a9d62841c6ad4b21154e8fc488");
        }

        public float IsPointingAt(ISatellite a) {
            throw new NotImplementedException();
        }

        public override String ToString() {
            return Name;
        }

    }
    
}
