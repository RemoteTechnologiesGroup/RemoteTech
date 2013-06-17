namespace RemoteTech {
    public class TrottleChange {
        public readonly long Delay;
        public readonly long Duration;
        public readonly float Throttle;

        public TrottleChange(float throttle, long duration, long delay) {
            Throttle = throttle;
            Duration = duration;
            Delay = delay;
        }
    }
}
