using System;

namespace RemoteTech
{
    public class TrottleChange {
        public readonly float Throttle;
        public readonly long Duration;
        public readonly long Delay;

        public TrottleChange(float throttle, long duration, long delay) {
            this.Throttle = throttle;
            this.Duration = duration;
            this.Delay = delay;
        }

    }
}

