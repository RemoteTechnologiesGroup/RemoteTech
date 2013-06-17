namespace RemoteTech {
    public class Settings {
        public EdgeType GRAPH_EDGE = EdgeType.Omni | EdgeType.Dish | EdgeType.Connection;
        public GraphMode GRAPH_MODE = GraphMode.MapView | GraphMode.TrackingStation;
        public float SIGNAL_DELAY_MODIFIER = 1.0f;
        public float SIGNAL_SPEED = 299792458.0f;

        private RTCore mCore;

        public Settings(RTCore core) {
            mCore = core;
        }
    }
}
