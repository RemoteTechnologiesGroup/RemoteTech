using System;

namespace RemoteTech
{
    public class AntennaPartModule : PartModule, IAntenna {

        // Properties
        public String Name { get { return ClassName; } }
        public String DisplayName { get { return IsActive ? Mode1Name : Mode0Name; } }

        public String Target { get { return pointedAt; } }
        
        public bool IsActive { get { return modeState > 0; } }
        
        public float DishRange { get { return IsActive ? DishActiveRange : DishInactiveRange; } }
        public float DishInactiveRange { get { return dishRange0; } }
        public float DishActiveRange { get { return dishRange1; } }
            
        public float OmniRange { get { return IsActive ? OmniActiveRange : OmniInactiveRange; } }
        public float OmniInactiveRange { get { return antennaRange0; } }
        public float OmniActiveRange { get { return antennaRange1; } }
                
        public float ActiveConsumption { get { return Mode0EnergyCost; } }
        public float InactiveConsumption { get { return Mode1EnergyCost; } }

        // KSPFields
        [KSPField(isPersistant = true)]
        public bool
            Locked = false;

        [KSPField(isPersistant = true)]
        public String 
            pointedAt = "None";

        [KSPField(isPersistant = true)]
        public int
            modeState = 0;

        [KSPField]
        public String
            Mode1Name = "Mode1",
            Mode0Name = "Mode0",
            ToggleName = "Toggle";

        [KSPField]
        public float
            MinimumDrag = 0,
            MaximumDrag = 0,
            Dragmodifier = 0,
            MaxQ = -1,
            EnergyDrain0 = 0,
            EnergyDrain1 = 0,
            Mode0EnergyCost = 0,
            Mode1EnergyCost = 0,
            antennaRange0 = 0,
            antennaRange1 = 0,
            dishRange0 = 0,
            dishRange1 = 0;

        public AntennaPartModule() {
        }
    }
}

