using System;
using System.Collections;
using System.Text;

namespace RemoteTech {
    public class ModuleRTAntenna : PartModule, IAntenna {
        public bool CanTarget { get { return Mode1DishRange != -1.0f; } }

        public String Name { get { return part.partInfo.title; } }

        public Guid DishTarget {
            get { return RTAntennaTargetGuid; }
            set {
                RTAntennaTargetGuid = value;
                RTAntennaTarget = value.ToString();
                StartCoroutine(UpdateContext());
            }
        }

        public float DishRange { get { return IsRTActive && IsPowered ? Mode1DishRange : Mode0DishRange; } }

        public double DishFactor { get { return RTDishFactor; } }

        public float OmniRange { get { return IsRTActive && IsPowered ? Mode1OmniRange : Mode0OmniRange; } }

        public float Consumption { get { return IsRTActive ? EnergyCost : 0.0f; } }

        public Vessel Vessel { get { return vessel; } }

        [KSPField(guiName = "Dish range")]
        public String GUI_DishRange;
        [KSPField(guiName = "Energy")]
        public String GUI_EnergyReq;
        [KSPField(guiName = "Omni range")]
        public String GUI_OmniRange;
        [KSPField(guiActive = true, guiName = "Status")]
        public String GUI_Status;
        [KSPField]
        public String Mode0Name = "Off";
        [KSPField]
        public String Mode1Name = "Operational";
        [KSPField]
        public String ActionMode0Name = "Deactivate";
        [KSPField]
        public String ActionMode1Name = "Activate";
        [KSPField]
        public String ActionToggleName = "Toggle";

        [KSPField]
        public float Mode0DishRange = -1.0f;
        [KSPField]
        public float Mode1DishRange = -1.0f;
        [KSPField]
        public float Mode0OmniRange = 0.0f;
        [KSPField]
        public float Mode1OmniRange = 0.0f;
        [KSPField]
        public float EnergyCost = 0.0f;
        [KSPField]
        public float DishAngle = 0.0f;

        [KSPField(isPersistant = true)]
        public bool IsPowered = true;
        [KSPField(isPersistant = true)]
        public bool IsRTActive = false;
        [KSPField(isPersistant = true)]
        public bool IsRTAntenna = true;
        [KSPField(isPersistant = true)]
        public double RTDishFactor = 1.0f;
        [KSPField(isPersistant = true)]
        public float RTOmniRange = 0.0f;
        [KSPField(isPersistant = true)]
        public float RTDishRange = 0.0f;

        public Guid RTAntennaTargetGuid = Guid.Empty;
        [KSPField(isPersistant = true)] public String RTAntennaTarget = Guid.Empty.ToString();

        private enum State {
            Off,
            Operational,
            NoResources,
        }

        private Guid mRegisteredId;

        public override string GetInfo() {
            var info = new StringBuilder();
            if (Mode1OmniRange > 0) {
                info.Append("Omni range: ");
                info.Append(RTUtil.FormatSI(Mode0OmniRange, "m"));
                info.Append(" / ");
                info.AppendLine(RTUtil.FormatSI(Mode1OmniRange, "m"));
            }
            if (Mode1DishRange > 0) {
                info.Append("Dish range: ");
                info.Append(RTUtil.FormatSI(Mode0DishRange, "m"));
                info.Append(" / ");
                info.AppendLine(RTUtil.FormatSI(Mode1DishRange, "m"));
            }
            if (EnergyCost > 0) {
                info.Append("Energy req.: ");
                info.Append(RTUtil.FormatConsumption(EnergyCost));
            }
            return info.ToString();
        }

        public virtual void SetState(bool state) {
            IsRTActive = state;
            RTDishRange = IsRTActive ? Mode1DishRange : Mode0DishRange;
            RTOmniRange = IsRTActive ? Mode1OmniRange : Mode0OmniRange;
            Events["EventOpen"].guiActive = !IsRTActive;
            Events["EventOpen"].active = Events["EventOpen"].guiActive;
            Events["EventClose"].guiActive = IsRTActive;
            Events["EventClose"].active = Events["EventClose"].guiActive;
            StartCoroutine(UpdateContext());
        }

        [KSPEvent(name = "EventToggle", guiActive = false)]
        public void EventToggle() {
            if (IsRTActive) {
                EventClose();
            }
            else {
                EventOpen();
            }
        }

        [KSPEvent(name = "EventTarget", guiActive = false, guiName = "Target")]
        public void EventTarget() {
            RTCore.Instance.Gui.OpenAntennaConfig(this, vessel);
        }

        [KSPEvent(name = "EventOpen", guiActive = false)]
        public void EventOpen() {
            SetState(true);
        }

        [KSPEvent(name = "EventClose", guiActive = false)]
        public void EventClose() {
            SetState(false);
        }

        [KSPAction("ActionToggle", KSPActionGroup.None)]
        public void ActionToggle(KSPActionParam param) {
            EventToggle();
        }

        [KSPAction("ActionOpen", KSPActionGroup.None)]
        public void ActionOpen(KSPActionParam param) {
            EventOpen();
        }

        [KSPAction("ActionClose", KSPActionGroup.None)]
        public void ActionClose(KSPActionParam param) {
            EventClose();
        }

        public override void OnStart(StartState state) {
            Actions["ActionOpen"].guiName = ActionMode1Name;
            Actions["ActionOpen"].active = true;
            Actions["ActionClose"].guiName = ActionMode0Name;
            Actions["ActionClose"].active = true;
            Actions["ActionToggle"].guiName = ActionToggleName;
            Actions["ActionToggle"].active = true;

            Events["EventOpen"].guiName = ActionMode1Name;
            Events["EventClose"].guiName = ActionMode0Name;
            Events["EventToggle"].guiName = ActionToggleName;
            Events["EventTarget"].guiActive = Mode1DishRange > 0;
            Events["EventTarget"].active = Events["EventTarget"].guiActive;

            Fields["GUI_OmniRange"].guiActive = Mode1OmniRange > 0;
            Fields["GUI_DishRange"].guiActive = Mode1DishRange > 0;
            Fields["GUI_EnergyReq"].guiActive = EnergyCost > 0;


            if (RTCore.Instance != null) {
                try {
                    DishTarget = new Guid(RTAntennaTarget);
                } catch (FormatException) {
                    DishTarget = Guid.Empty;
                }
                RTDishFactor = Math.Cos(DishAngle * Math.PI / 180);

                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = RTCore.Instance.Antennas.Register(vessel.id, this);
                SetState(IsRTActive);
            }
        }

        private IEnumerator UpdateContext() {
            GUI_OmniRange = RTUtil.FormatSI(OmniRange, "m");
            GUI_DishRange = RTUtil.FormatSI(DishRange, "m");
            GUI_EnergyReq = RTUtil.FormatConsumption(Consumption);
            Events["EventTarget"].guiName = RTUtil.TargetName(DishTarget);
            Events["EventTarget"].active = false;
            yield return 1;
            Events["EventTarget"].active = true;
        }

        private State UpdateControlState() {
            if (!IsRTActive) return State.Off;
            ModuleResource request = new ModuleResource();
            float resourceRequest = Consumption * TimeWarp.deltaTime;
            float resourceAmount = part.RequestResource("ElectricCharge", resourceRequest);
            if (resourceAmount < resourceRequest * 0.9) {
                IsPowered = false;
                return State.NoResources;
            }
            IsPowered = true;
            return State.Operational;
        }

        public void FixedUpdate() {
            switch (UpdateControlState()) {
                case State.Off:
                    GUI_Status = Mode0Name;
                    break;
                case State.Operational:
                    GUI_Status = Mode1Name;
                    break;
                case State.NoResources:
                    GUI_Status = "Out of power";
                    break;
            }
        }

        public void OnDestroy() {
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null) {
                RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
            }
        }

        public void OnPartUndock(Part p) {
            if (p.vessel == vessel) {
                OnVesselModified(p.vessel);
            }
        }

        public void OnVesselModified(Vessel v) {
            if (vessel == null || (mRegisteredId != vessel.id)) {
                RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
                if (vessel != null) {
                    mRegisteredId = RTCore.Instance.Antennas.Register(vessel.id, this);
                }
            }
        }

        public override String ToString() {
            return "ModuleRTAntenna {" + DishTarget + ", " + DishRange + ", " + OmniRange + ", " +
                   Vessel.vesselName + "}";
        }
    }
}
