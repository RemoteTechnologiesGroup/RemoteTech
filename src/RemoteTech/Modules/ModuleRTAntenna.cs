using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RemoteTech
{
    [KSPModule("Antenna")]
    public class ModuleRTAntenna : PartModule, IAntenna
    {
        public String Name { get { return part.partInfo.title; } }
        public Guid Guid { get { return mRegisteredId; } }
        public bool Powered { get { return IsRTPowered; } }
        public bool Activated { get { return IsRTActive; } set { SetState(value); } }
        public bool CanAnimate { get { return mDeployFxModules.Count > 0; } }
        public bool AnimClosed { get { return mDeployFxModules.Any(fx => fx.GetScalar <= 0.1f                        ); } }
        public bool Animating  { get { return mDeployFxModules.Any(fx => fx.GetScalar >  0.1f && fx.GetScalar <  0.9f); } }
        public bool AnimOpen   { get { return mDeployFxModules.Any(fx =>                         fx.GetScalar >= 0.9f); } }

        public bool CanTarget { get { return Mode1DishRange != -1.0f; } }
        public Guid Target
        {
            get { return RTAntennaTarget; }
            set
            {
                RTAntennaTarget = value;
                Events["EventTarget"].guiName = RTUtil.TargetName(Target);
                foreach (UIPartActionWindow w in GameObject.FindObjectsOfType(typeof(UIPartActionWindow)).Where(w => ((UIPartActionWindow) w).part == part))
                {
                    w.displayDirty = true;
                }
            }
        }

        public float Dish { get { return IsRTBroken ? 0.0f : ((IsRTActive && IsRTPowered) ? Mode1DishRange : Mode0DishRange) * RangeMultiplier; } }
        public double CosAngle { get { return RTDishCosAngle; } }
        public float Omni { get { return IsRTBroken ? 0.0f : ((IsRTActive && IsRTPowered) ? Mode1OmniRange : Mode0OmniRange) * RangeMultiplier; } }
        public float Consumption { get { return IsRTBroken ? 0.0f : IsRTActive ? EnergyCost * ConsumptionMultiplier : 0.0f; } }
        public Vector3d Position { get { return vessel.GetWorldPos3D(); } }

        private float RangeMultiplier { get { return RTSettings.Instance.RangeMultiplier; } }
        private float ConsumptionMultiplier { get { return RTSettings.Instance.ConsumptionMultiplier; } }

        [KSPField]
        public bool
            ShowGUI_DishRange = true,
            ShowGUI_OmniRange = true,
            ShowGUI_EnergyReq = true,
            ShowGUI_Status = true,
            ShowEditor_OmniRange = true,
            ShowEditor_DishRange = true,
            ShowEditor_EnergyReq = true,
            ShowEditor_DishAngle = true;

        [KSPField(guiName = "Dish range")]
        public String GUI_DishRange;
        [KSPField(guiName = "Energy")]
        public String GUI_EnergyReq;
        [KSPField(guiName = "Omni range")]
        public String GUI_OmniRange;
        [KSPField(guiName = "Status")]
        public String GUI_Status;

        [KSPField]
        public String
            Mode0Name = "Off",
            Mode1Name = "Operational",
            ActionMode0Name = "Deactivate",
            ActionMode1Name = "Activate",
            ActionToggleName = "Toggle";

        [KSPField]
        public float
            Mode0DishRange = -1.0f,
            Mode1DishRange = -1.0f,
            Mode0OmniRange = 0.0f,
            Mode1OmniRange = 0.0f,
            EnergyCost = 0.0f,
            DishAngle = 0.0f,
            MaxQ = -1;

        [KSPField(isPersistant = true)]
        public bool
            IsRTAntenna = true,
            IsRTActive = false,
            IsRTPowered = false,
            IsRTBroken = false;

        [KSPField(isPersistant = true)]
        public double RTDishCosAngle = 1.0f;

        [KSPField(isPersistant = true)]
        public float
            RTOmniRange = 0.0f,
            RTDishRange = 0.0f;

        [KSPField] // Persistence handled by Save()
        public Guid RTAntennaTarget = Guid.Empty;

        public int[] mDeployFxModuleIndices, mProgressFxModuleIndices;
        private List<IScalarModule> mDeployFxModules = new List<IScalarModule>();
        private List<IScalarModule> mProgressFxModules = new List<IScalarModule>();
        public ConfigNode mTransmitterConfig;
        private IScienceDataTransmitter mTransmitter;

        private enum State
        {
            Off,
            Operational,
            NoResources,
            Malfunction,
        }

        private Guid mRegisteredId;

        public override string GetInfo()
        {
            var info = new StringBuilder();

            if (ShowEditor_OmniRange && Mode1OmniRange > 0)
            {
                info.AppendFormat("Omni range: {0} / {1}", RTUtil.FormatSI(Mode0OmniRange * RangeMultiplier, "m"), RTUtil.FormatSI(Mode1OmniRange * RangeMultiplier, "m")).AppendLine();
            }
            if (ShowEditor_DishRange && Mode1DishRange > 0)
            {
                info.AppendFormat("Dish range: {0} / {1}", RTUtil.FormatSI(Mode0DishRange * RangeMultiplier, "m"), RTUtil.FormatSI(Mode1DishRange * RangeMultiplier, "m")).AppendLine();
            }
            if (ShowEditor_EnergyReq && EnergyCost > 0)
            {
                info.AppendFormat("Energy req.: {0}", RTUtil.FormatConsumption(EnergyCost * ConsumptionMultiplier)).AppendLine();
            }

            if (ShowEditor_DishAngle && CanTarget)
            {
                info.AppendFormat("Cone angle: {0} degrees", DishAngle.ToString("F2")).AppendLine();
            }

            if (IsRTActive)
            {
                info.AppendLine("Activated by default");
            }

            if (MaxQ > 0)
            {
                info.AppendLine("Snaps under high dynamic pressure");
            }

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public virtual void SetState(bool state)
        {
            IsRTActive = state && !IsRTBroken;
            Events["EventOpen"].guiActive = Events["EventOpen"].active = 
            Events["EventEditorOpen"].guiActiveEditor = 
            Events["OverrideOpen"].guiActiveUnfocused = !IsRTActive && !IsRTBroken;

            Events["EventClose"].guiActive = Events["EventClose"].active = 
            Events["EventEditorClose"].guiActiveEditor =
            Events["OverrideClose"].guiActiveUnfocused = IsRTActive && !IsRTBroken;

            UpdateContext();
            StartCoroutine(SetFXModules_Coroutine(mDeployFxModules, IsRTActive ? 1.0f : 0.0f));

            if (RTCore.Instance != null)
            {
                var satellite = RTCore.Instance.Network[Guid];
                bool route_home = RTCore.Instance.Network[satellite].Any(r => r.Links[0].Interfaces.Contains(this) && RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid));
                if (mTransmitter == null && route_home)
                {
                    AddTransmitter();
                }
                else if (!route_home && mTransmitter != null)
                {
                    RemoveTransmitter();
                }
            }
        }

        [KSPEvent(name = "EventToggle", guiActive = false)]
        public void EventToggle() { if (Animating) return; if (IsRTActive) { EventClose(); } else { EventOpen(); } }

        [KSPEvent(name = "EventTarget", guiActive = false, guiName = "Target", category = "skip_delay")]
        public void EventTarget() { (new AntennaWindow(this)).Show(); }

        [KSPEvent(name = "EventEditorOpen", guiActive = false, guiName = "Start deployed")]
        public void EventEditorOpen() { SetState(true); }

        [KSPEvent(name = "EventEditorClose", guiActive = false, guiName = "Start retracted")]
        public void EventEditorClose() { SetState(false); }

        [KSPEvent(name = "EventOpen", guiActive = false)]
        public void EventOpen() { if (!Animating) { SetState(true); } }

        [KSPEvent(name = "EventClose", guiActive = false)]
        public void EventClose() { if (!Animating) { SetState(false); } }

        [KSPAction("ActionToggle", KSPActionGroup.None)]
        public void ActionToggle(KSPActionParam param) { EventToggle(); }

        [KSPAction("ActionOpen", KSPActionGroup.None)]
        public void ActionOpen(KSPActionParam param) { EventOpen(); }

        [KSPAction("ActionClose", KSPActionGroup.None)]
        public void ActionClose(KSPActionParam param) { EventClose(); }

        [KSPEvent(name = "OverrideTarget", active = true, guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true, guiName = "[EVA] Set Target", category = "skip_delay;skip_control")]
        public void OverrideTarget() { (new AntennaWindow(this)).Show(); }

        [KSPEvent(name = "OverrideOpen", active = true, guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true, guiName = "[EVA] Force Open", category = "skip_delay;skip_control")]
        public void OverrideOpen() { EventOpen(); }

        [KSPEvent(name = "OverrideClose", active = true, guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true, guiName = "[EVA] Force Close", category = "skip_delay;skip_control")]
        public void OverrideClose() { EventClose(); }

        public void OnConnectionRefresh()
        {
            SetState(IsRTActive);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("RTAntennaTarget"))
            {
                try
                {
                    Target = new Guid(node.GetValue("RTAntennaTarget"));
                }
                catch (FormatException)
                {
                    Target = Guid.Empty;
                }
            }
            // Have RTDishRadians as a fallback to avoid corrupting save games
            if (node.HasValue("RTDishRadians"))
            {
                double temp_double;
                RTDishCosAngle = Double.TryParse(node.GetValue("RTDishRadians"), out temp_double) ? temp_double : 1.0;
            }
            if (node.HasValue("DishAngle"))
            {
                RTDishCosAngle = Math.Cos(DishAngle / 2 * Math.PI / 180);
            }
            if (node.HasValue("DeployFxModules"))
            {
                mDeployFxModuleIndices = KSPUtil.ParseArray<Int32>(node.GetValue("DeployFxModules"), new ParserMethod<Int32>(Int32.Parse));
            }
            if (node.HasValue("ProgressFxModules"))
            {
                mProgressFxModuleIndices = KSPUtil.ParseArray<Int32>(node.GetValue("ProgressFxModules"), new ParserMethod<Int32>(Int32.Parse));
            }
            if (node.HasNode("TRANSMITTER"))
            {
                RTLog.Notify("ModuleRTAntenna: Found TRANSMITTER block.");
                mTransmitterConfig = node.GetNode("TRANSMITTER");
                mTransmitterConfig.AddValue("name", "ModuleRTDataTransmitter");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (node.HasValue("RTAntennaTarget"))
            {
                node.SetValue("RTAntennaTarget", RTAntennaTarget.ToString());
            }
            else
            {
                node.AddValue("RTAntennaTarget", RTAntennaTarget.ToString());
            }
        }

        public override void OnStart(StartState state)
        {
            Actions["ActionOpen"].guiName = ActionMode1Name;
            Actions["ActionOpen"].active = !IsRTBroken;
            Actions["ActionClose"].guiName = ActionMode0Name;
            Actions["ActionClose"].active = !IsRTBroken;
            Actions["ActionToggle"].guiName = ActionToggleName;
            Actions["ActionToggle"].active = !IsRTBroken;

            Events["EventOpen"].guiName = ActionMode1Name;
            Events["EventClose"].guiName = ActionMode0Name;
            Events["EventToggle"].guiName = ActionToggleName;
            Events["EventTarget"].guiActive = (Mode1DishRange > 0);
            Events["EventTarget"].active = Events["EventTarget"].guiActive;

            Fields["GUI_OmniRange"].guiActive = (Mode1OmniRange > 0) && ShowGUI_OmniRange;
            Fields["GUI_DishRange"].guiActive = (Mode1DishRange > 0) && ShowGUI_DishRange;
            Fields["GUI_EnergyReq"].guiActive = (EnergyCost > 0) && ShowGUI_EnergyReq;
            Fields["GUI_Status"].guiActive = ShowGUI_Status;

            if (RTCore.Instance != null)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = vessel.id;
                RTCore.Instance.Antennas.Register(vessel.id, this);
            }

            LoadAnimations();
            SetState(IsRTActive);
        }

        private void LoadAnimations()
        {
            mDeployFxModules = FindFxModules(this.mDeployFxModuleIndices, true);
            mProgressFxModules = FindFxModules(this.mProgressFxModuleIndices, false);
            mDeployFxModules.ForEach(fx => { fx.SetUIRead(false); fx.SetUIWrite(false); });
            mProgressFxModules.ForEach(fx => { fx.SetUIRead(false); fx.SetUIWrite(false); });
        }

        private void AddTransmitter()
        {
            if (mTransmitterConfig == null || !mTransmitterConfig.HasValue("name")) return;
            var transmitters = part.FindModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0)
            {
                RTLog.Notify("ModuleRTAntenna: Find TRANSMITTER success.");
                mTransmitter = transmitters.First();
            }
            else
            {
                var copy = new ConfigNode();
                mTransmitterConfig.CopyTo(copy);
                part.AddModule(copy);
                AddTransmitter();
                RTLog.Notify("ModuleRTAntenna: Add TRANSMITTER success.");
            }
        }

        private void RemoveTransmitter()
        {
            RTLog.Notify("ModuleRTAntenna: Remove TRANSMITTER success.");
            if (mTransmitter == null) return;
            part.RemoveModule((PartModule) mTransmitter);
            mTransmitter = null;
        }

        private State UpdateControlState()
        {
            if (RTCore.Instance == null) return State.Operational;

            if (IsRTBroken) return State.Malfunction;

            if (!IsRTActive) return State.Off;

            float resourceRequest = Consumption * TimeWarp.fixedDeltaTime;
            float resourceAmount = part.RequestResource("ElectricCharge", resourceRequest);
            if (resourceAmount < resourceRequest * 0.9) return State.NoResources;
            
            return State.Operational;
        }

        private void FixedUpdate()
        {
            switch (UpdateControlState())
            {
                case State.Off:
                    GUI_Status = Mode0Name;
                    IsRTPowered = false;
                    break;
                case State.Operational:
                    GUI_Status = Mode1Name;
                    IsRTPowered = true;
                    break;
                case State.NoResources:
                    GUI_Status = "Out of power";
                    IsRTPowered = false;
                    break;
                case State.Malfunction:
                    GUI_Status = "Malfunction";
                    IsRTPowered = false;
                    break;
            }
            RTDishRange = Dish;
            RTOmniRange = Omni;
            HandleDynamicPressure();
            UpdateContext();
        }

        private void UpdateContext()
        {
            GUI_OmniRange = RTUtil.FormatSI(Omni, "m");
            GUI_DishRange = RTUtil.FormatSI(Dish, "m");
            GUI_EnergyReq = RTUtil.FormatConsumption(Consumption);
            Events["EventTarget"].guiName = RTUtil.TargetName(Target);
        }

        /// <summary>
        /// Returns the FAR module managing aerodynamics for this part, if one exists
        /// </summary>
        /// 
        /// <returns>
        /// If FAR is installed and the antenna has a module of type <c>ferram4.FARBaseAerodynamics</c>, returns a 
        /// reference to that module. Otherwise, returns null. Behavior is undefined if the antenna has more than 
        /// one FARBaseAerodynamics module.
        /// </returns>
        ///
        /// <precondition><c>this.part</c> is not null</precondition>
        ///
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        private PartModule GetFARModule()
        {
            if (part.Modules.Contains("FARBasicDragModel")) {
                return part.Modules["FARBasicDragModel"];
            } else if (part.Modules.Contains ("FARWingAerodynamicModel")) {
                return part.Modules["FARWingAerodynamicModel"];
            } else if (part.Modules.Contains ("FARControlSys")) {
                return part.Modules["FARControlSys"];
            } else {
                return null;
            }
        }

        /// <summary>
        /// Determines whether or not the antenna is shielded from aerodynamic forces
        /// </summary>
        /// <returns><c>true</c>, if the antenna is shielded by a supported mod, <c>false</c> otherwise.</returns>
        ///
        /// <precondition><c>this.part</c> is not null</precondition>
        ///
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        private bool GetShieldedState()
        {
            PartModule FARPartModule = GetFARModule();

            if (FARPartModule != null) {
                try {
                    FieldInfo fi = FARPartModule.GetType ().GetField ("isShielded");
                    return (bool)(fi.GetValue (FARPartModule));
                } catch (Exception e) {
                    RTLog.Notify ("GetShieldedState: {0}", e);
                    return false;
                }
            } else {
                return false;
            }
        }

        /// <summary>
        /// Gets the ram pressure experienced by the antenna.
        /// </summary>
        /// 
        /// <returns>The pressure, in N/m^2.</returns>
        /// 
        /// <precondition><c>this.vessel</c> is not null</precondition>
        ///
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        private double GetDynamicPressure() {
            return 0.5 * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude;
        }
        
        private void HandleDynamicPressure()
        {
            if (vessel == null) return;
            if (!vessel.HoldPhysics && vessel.atmDensity > 0 && MaxQ > 0 && (!this.CanAnimate || this.AnimOpen)) {
                if (GetDynamicPressure() > MaxQ && GetShieldedState() == false) {
                    // Express flight clock in stockalike formatting
                    string timestamp = RTUtil.FormatTimestamp (FlightLogger.met_years, FlightLogger.met_days, 
                                           FlightLogger.met_hours, FlightLogger.met_mins, FlightLogger.met_secs);
                    FlightLogger.eventLog.Add(String.Format("[{0}]: {1} was ripped off by strong airflow.", 
                        timestamp, part.partInfo.title));
                    MaxQ = -1.0f;
                    part.decouple(0.0f);
                }
            }
        }

        private List<IScalarModule> FindFxModules(int[] indices, bool showUI)
        {
            var modules = new List<IScalarModule>();
            if (indices == null) return modules;
            foreach (int i in indices)
            {
                var item = base.part.Modules[i] as IScalarModule;
                if (item != null)
                {
                    item.SetUIWrite(showUI);
                    item.SetUIRead(showUI);
                    modules.Add(item);
                }
                else
                {
                    RTLog.Notify("ModuleRTAntenna: Part Module {0} doesn't implement IScalarModule", part.Modules[i].name);
                }
            }
            return modules;
        }

        private IEnumerator SetFXModules_Coroutine(List<IScalarModule> modules, float tgtValue)
        {
            bool done = false;
            while (!done)
            {
                done = true;
                foreach (var module in modules)
                {
                    if (Mathf.Abs(module.GetScalar - tgtValue) > 0.01f)
                    {
                        module.SetScalar(tgtValue);
                        done = false;
                    }
                }
                yield return true;
            }
        }

        private void OnDestroy()
        {
            RTLog.Notify("ModuleRTAntenna: OnDestroy");
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null && mRegisteredId != Guid.Empty)
            {
                RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
                mRegisteredId = Guid.Empty;
            }
        }

        private void OnPartUndock(Part p)
        {
            if (p.vessel == vessel)
            {
                OnVesselModified(p.vessel);
            } 
        }

        private void OnVesselModified(Vessel v)
        {
            if ((mRegisteredId != vessel.id))
            {
                RTCore.Instance.Antennas.Unregister(mRegisteredId, this);
                mRegisteredId = vessel.id;
                RTCore.Instance.Antennas.Register(vessel.id, this);
            }
        }

        public int CompareTo(IAntenna antenna)
        {
            return Consumption.CompareTo(antenna.Consumption);
        }

        public override string ToString()
        {
            return String.Format("ModuleRTAntenna(Name: {0}, Guid: {1}, Dish: {2}, Omni: {3}, Target: {4}, CosAngle: {5})", Name, mRegisteredId, Dish, Omni, Target, CosAngle);
        }
    }
}
