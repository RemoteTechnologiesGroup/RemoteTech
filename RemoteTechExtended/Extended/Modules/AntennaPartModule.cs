using System;

namespace RemoteTech
{
    public class AntennaPartModule : PartModule, IAntenna {

        // Properties
        public String Name { get { return ClassName; } }
        public String DisplayName { get { return Active ? Mode1Name : Mode0Name; } }

        public String Target { get { return PointedAt; } }
        
        public bool Active { get { return Mode > 0; } }
        
        public float DishRange { get { return Active ? DishActiveRange : DishInactiveRange; } }
        public float DishInactiveRange { get { return Mode0DishRange; } }
        public float DishActiveRange { get { return Mode1DishRange; } }
            
        public float OmniRange { get { return Active ? OmniActiveRange : OmniInactiveRange; } }
        public float OmniInactiveRange { get { return Mode0OmniRange; } }
        public float OmniActiveRange { get { return Mode1OmniRange; } }
                
        public float ActiveConsumption { get { return Mode0EnergyCost; } }
        public float InactiveConsumption { get { return Mode1EnergyCost; } }

        // KSPFields
        [KSPField(isPersistant = true)]
        public bool
            Locked = false;

        [KSPField(isPersistant = true)]
        public String 
            PointedAt = "None";

        [KSPField(isPersistant = true)]
        public int
            Mode = 0;

        [KSPField]
        public String
            Mode1Name = "Mode1",
            Mode0Name = "Mode0",
            ToggleName = "Toggle";

        [KSPField]
        public float
            Mode0EnergyCost = 0,
            Mode1EnergyCost = 0,
            Mode0OmniRange = 0,
            Mode1OmniRange = 0,
            Mode0DishRange = 0,
            Mode1DishRange = 0;

        public AntennaPartModule() {
        }
    }
}

