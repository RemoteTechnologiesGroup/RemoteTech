using System;

namespace RemoteTech {
    public class Dish {
        public readonly float Distance;
        public readonly double Factor;
        public readonly Guid Target;

        public Dish(Guid target, double angleFactor, float distance) {
            Target = target;
            Factor = angleFactor;
            Distance = distance;
        }
    }
}
