using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class SPUPartModule : PartModule {

        // To keep ownership of the Satellite while the PartModule is alive.
        Satellite mSatellite;

        public long Enqueue(AttitudeChange change) {
            return 0;
        }

        public long Enqueue(TrottleChange change) {
            return 0;
        }

        public override void OnStart(StartState state) {
            base.OnStart(state);
            if (state == StartState.Editor || state == StartState.None) {
                return;
            }
        }

        public override void OnFixedUpdate() {
            if(vessel != null) {
                mSatellite = Satellite.Instance(this);
            }
            
        }

        public void OnDestroy() {
            Satellite.Unclaim(this.vessel, this);
        }
    }
}



