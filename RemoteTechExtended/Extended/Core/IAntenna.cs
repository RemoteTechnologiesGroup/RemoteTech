using System;

namespace RemoteTech
{
    public interface IAntenna {

        String Name { get; }

        String Target { get; }
    
        bool Active { get; }
        
        float DishRange { get; }
        float DishInactiveRange { get; }
        float DishActiveRange { get; }

        float OmniRange { get; }
        float OmniInactiveRange { get; }
        float OmniActiveRange { get; }
        
        float ActiveConsumption { get; }
        float InactiveConsumption { get; }
        
    }
}

