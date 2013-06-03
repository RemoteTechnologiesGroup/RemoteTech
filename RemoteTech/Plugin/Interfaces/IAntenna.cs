using System;

namespace RemoteTech
{
    public interface IAntenna {

        String Name { get; }
        Guid Target { get; set; }
        float DishRange { get; }
        float OmniRange { get; }
        float Consumption { get; }
        Vessel Vessel { get; }
        bool CanTarget { get; }
    }
}

