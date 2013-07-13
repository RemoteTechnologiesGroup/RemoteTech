using System;

namespace RemoteTech {
    public interface IAntenna {
        String Name { get; }
        Guid DishTarget { get; set; }
        double DishFactor { get; }
        float DishRange { get; }
        float OmniRange { get; }
        bool CanTarget { get; }
    }
}
