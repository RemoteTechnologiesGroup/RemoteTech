using System;

namespace RemoteTech {
    public interface IAntenna {
        String Name { get; }
        bool Activated { get; }
        bool Powered { get; }

        bool CanTarget { get; }
        Guid DishTarget { get; set; }
        double DishFactor { get; }

        float CurrentDishRange { get; }
        float CurrentOmniRange { get; }
        float CurrentConsumption { get; }

        ISatellite Owner { get; }
    }
}
