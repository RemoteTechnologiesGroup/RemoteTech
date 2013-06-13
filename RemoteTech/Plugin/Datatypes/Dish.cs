using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class Dish {
        public readonly Guid Target;
        public readonly double Factor;
        public readonly float Distance;

        public Dish(Guid target, double angleFactor, float distance) {
            this.Target = target;
            this.Factor = angleFactor;
            this.Distance = distance;
        }
    }
}
