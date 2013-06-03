using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    class ProtoSignalProcessor : ISignalProcessor {

        public Vessel Vessel { get; private set; }
        public bool Powered { get; private set; }

        public ProtoSignalProcessor(ProtoPartModuleSnapshot ppms, Vessel v) {
            Vessel = v;
            Powered = ppms.GetBool("IsPowered");
        }
    }
}
