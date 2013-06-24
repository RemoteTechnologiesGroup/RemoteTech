namespace RemoteTech {
    public class Settings {
        public EdgeType GraphEdge = EdgeType.Omni | EdgeType.Dish | EdgeType.Connection;
        public GraphMode GraphMode = GraphMode.MapView | GraphMode.TrackingStation;
        public float SignalDelayModifier = 1.0f;
        public float SignalSpeed = 299792458.0f;

        public bool DebugAlwaysConnected = false;
        public float DebugOffsetDelay = 5.0f;

        private RTCore mCore;

        public Settings(RTCore core) {
            mCore = core;
        }
    }
}
