using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class MissionControlSatellite : ISatellite {

        public string Name {
            get { return "RTCore_MissionControlSatellite"; }
        }

        public IAntenna[] Antennas {
            get { return null; }
        }

        public Vector3d Position {
            get { 
                return FlightGlobals.Bodies[1].position + 600094 * 
                       FlightGlobals.Bodies[1].GetSurfaceNVector(-0.11641926192966, -74.606391806057);
            }
        }

        public double FindMaxOmniRange() {
            return 9000000;
        }

        public double IsPointingAt(ISatellite a) {
            throw new NotImplementedException();
        }

    }
    
}
