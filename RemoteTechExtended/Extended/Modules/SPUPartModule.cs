using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class SPUPartModule : PartModule {

        Satellite mParent;

        public long Enqueue(AttitudeChange change) {
            return 0;
        }

        public long Enqueue(TrottleChange change) {
            return 0;
        }

        public override int GetHashCode() {
            return this.vessel.GetInstanceID();
        }
    }
}



