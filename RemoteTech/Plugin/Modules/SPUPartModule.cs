using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class SPUPartModule : PartModule, ISignalProcessor {
        [KSPField(isPersistant=true)]
        public bool
            IsRTSignalProcessor = true;
    }
}



