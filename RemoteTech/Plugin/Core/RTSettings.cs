using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTSettings {
        public float SIGNAL_SPEED = 299792458.0f;
        public float SIGNAL_DELAY_MODIFIER = 1.0f;

        public GraphMode GRAPH_MODE = GraphMode.MapView | GraphMode.TrackingStation;
        public EdgeType GRAPH_EDGE = EdgeType.Omni | EdgeType.Dish | EdgeType.Connection;

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
