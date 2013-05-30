using System;
using System.Text;

namespace RemoteTech
{
    public class AntennaPartModule : PartModule, IAntenna {

        // Properties
        public String Name { get { return ClassName; } }
        public Guid Target { get { return RTAntennaTarget; } set { RTAntennaTarget = value; } }
        public float DishRange { get { return RTMode ? Mode1DishRange : Mode0DishRange; } }
        public float OmniRange { get { return RTMode ? Mode1OmniRange : Mode0OmniRange; } }     
        public float Consumption { get { return RTMode ? Mode1EnergyCost : Mode0EnergyCost; } }
        public Vessel Vessel { get { return vessel; } }

        // KSPFields
        [KSPField(isPersistant = true)]
        public bool
            IsRTAntenna = true,
            RTLocked = false,
            RTMode = false;

        [KSPField(isPersistant = true)]
        public float
            RTDishRange,
            RTOmniRange;

        [KSPField(isPersistant = true)]
        public Guid
            RTAntennaTarget = Guid.Empty;

        [KSPField]
        public float
            Mode0EnergyCost = 0,
            Mode1EnergyCost = 0,
            Mode0OmniRange = 0,
            Mode1OmniRange = 900000,
            Mode0DishRange = 0,
            Mode1DishRange = 900000;

        [KSPField(guiActive = true, guiName="Antenna status")] public String GUI_Status;
        [KSPField(guiName="Omni range")] public String GUI_OmniRange;
        [KSPField(guiName="Dish range")] public String GUI_DishRange;
        [KSPField(guiName="Energy")] public String GUI_EnergyReq;
        [KSPField(guiName="Target")] public String GUI_Target;

        Guid mRegisteredId;

        public override string GetInfo() {
            StringBuilder info = new StringBuilder();
            if(Mode1OmniRange > 0) {
                info.Append("Omni range: ");
                info.AppendLine(RTUtil.FormatDistance(Mode1OmniRange));
            }
            if (Mode1DishRange > 0) {
                info.Append("Dish range: ");
                info.AppendLine(RTUtil.FormatDistance(Mode1DishRange));
            }
            if (Mode1EnergyCost > 0) {
                info.Append("Energy req.: ");
                info.AppendLine((Mode1EnergyCost*60).ToString("0.00")+"/min.");
            }
            return info.ToString();
        }

        public void SetState(bool state) {
            RTMode = !RTMode;
            RTDishRange = RTMode ? Mode1DishRange : Mode0DishRange;
            RTOmniRange = RTMode ? Mode1OmniRange : Mode0OmniRange;  
            UpdateGUI();
        }

        [KSPEvent(guiName = "Toggle", guiActive = true)]
        public void ToggleEvent() {
            if (RTLocked)
                return;
            SetState(!RTMode);
        }

        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ToggleAction(KSPActionParam param) {
            ToggleEvent();
        }

        public void Start() {
            mRegisteredId = RTCore.Instance.Antennas.Register(vessel, this);
            GameEvents.onVesselWasModified.Add(OnVesselModified);

            Fields["GUI_OmniRange"].guiActive = Mode1OmniRange > 0;
            Fields["GUI_DishRange"].guiActive = Mode1DishRange > 0;
            Fields["GUI_EnergyReq"].guiActive = Mode1EnergyCost > 0;
            Fields["GUI_Target"].guiActive = Mode1DishRange > 0;

            UpdateGUI();
        }

        void UpdateGUI() {
            ISatellite target = RTCore.Instance.Satellites.WithGuid(RTAntennaTarget);
            GUI_Status = RTMode ? "Active" : "Inactive";
            GUI_OmniRange = RTUtil.FormatDistance(OmniRange);
            GUI_DishRange = RTUtil.FormatDistance(DishRange);
            GUI_EnergyReq = (Consumption * 60).ToString("0.00") + "/min";
            GUI_Target = (target != null) ? target.Name : "None";
        }

        public void Destroy() {
            RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
        }

        public void OnVesselModified(Vessel v) {
            if(v == vessel && mRegisteredId != vessel.id) {
                RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
                Start();
            }
        }
    }
}

