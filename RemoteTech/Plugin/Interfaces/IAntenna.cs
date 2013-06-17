using System;

namespace RemoteTech {
    public interface IAntenna {
        String Name { get; }
        Guid DishTarget { get; set; }
        double DishFactor { get; }
        float DishRange { get; }
        float OmniRange { get; }
        float Consumption { get; }
        Vessel Vessel { get; }
        bool CanTarget { get; }
    }
}
