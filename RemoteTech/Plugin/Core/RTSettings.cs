using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTSettings {

        public float SignalSpeed = 299792458.0f;

        RTCore mCore;

        public RTSettings(RTCore core) {
            mCore = core;
            Load();
        }

        public void Save() {

        }

        public void Load() {

        }
    }
}
