using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAntenna : PartModule, IAntenna
    {
        public String Name { get { return part.partInfo.title; } }
        public Guid Guid { get { return vessel.id; } }
        public bool Powered { get { return IsRTPowered; } }
        public bool Activated { get { return IsRTActive; } set { SetState(value); } }
        public bool Animating { get { return mDeployFxModules.Any(fx => fx.GetScalar > 0.1f && fx.GetScalar < 0.9f); } }

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

        public float Dish { get { return IsRTBroken ? 0.0f : (IsRTActive && IsRTPowered) ? Mode1DishRange : Mode0DishRange; } }
        public double Radians { get { return RTDishRadians; } }
        public float Omni { get { return IsRTBroken ? 0.0f : (IsRTActive && IsRTPowered) ? Mode1OmniRange : Mode0OmniRange; } }
        public float Consumption { get { return IsRTBroken ? 0.0f : IsRTActive ? EnergyCost : 0.0f; } }

        [KSPField]
        public bool
            ShowGUI_DishRange = true,
            ShowGUI_OmniRange = true,
            ShowGUI_EnergyReq = true,
            ShowGUI_Status = true,
            ShowEditor_OmniRange = true,
            ShowEditor_DishRange = true,
            ShowEditor_EnergyReq = true;

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
            DishAngle = 0.0f;

        [KSPField(isPersistant = true)]
        public bool
            IsRTAntenna = true,
            IsRTActive = false,
            IsRTPowered = false,
            IsRTBroken = false;

        [KSPField(isPersistant = true)]
        public double RTDishRadians = 1.0f;

        [KSPField(isPersistant = true)]
        public float
            RTOmniRange = 0.0f,
            RTDishRange = 0.0f;

        [KSPField] // Persistence handled by Save()
        public Guid RTAntennaTarget = Guid.Empty;

        public int[] mDeployFxModuleIndices, mProgressFxModuleIndices;
        private List<IScalarModule> mDeployFxModules;
        private List<IScalarModule> mProgressFxModules;
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
                info.AppendFormat("Omni range: {0} / {1}", RTUtil.FormatSI(Mode0OmniRange, "m"), RTUtil.FormatSI(Mode1OmniRange, "m")).AppendLine();
            }
            if (ShowEditor_DishRange && Mode1DishRange > 0)
            {
                info.AppendFormat("Dish range: {0} / {1}", RTUtil.FormatSI(Mode0DishRange, "m"), RTUtil.FormatSI(Mode1DishRange, "m")).AppendLine();
            }
            if (ShowEditor_EnergyReq && EnergyCost > 0)
            {
                info.AppendFormat("Energy req.: {0}", RTUtil.FormatConsumption(EnergyCost)).AppendLine();
            }

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public virtual void SetState(bool state)
        {
            bool prev_state = IsRTActive;
            IsRTActive = state && !IsRTBroken;
            Events["EventOpen"].guiActive = !IsRTActive && !IsRTBroken;
            Events["EventOpen"].active = Events["EventOpen"].guiActive;
            Events["EventClose"].guiActive = IsRTActive && !IsRTBroken;
            Events["EventClose"].active = Events["EventClose"].guiActive;
            UpdateContext();
            if(IsRTActive != prev_state) StartCoroutine(SetFXModules_Coroutine(mDeployFxModules, IsRTActive ? 1.0f : 0.0f));
            var satellite = RTCore.Instance.Network[Guid];
            bool route_home = satellite != null ? RTCore.Instance.Network[satellite].Any(r => r.Links[0].Interface == (IAntenna)this && 
                                                                                              r.Goal == RTCore.Instance.Network.MissionControl) : false;
            RTUtil.Log("route_home: {0} on {1}", route_home, this);
            if (mTransmitter == null && route_home && IsRTActive)
            {
                AddTransmitter();
            }
            else if (!route_home && mTransmitter != null)
            {
                RemoveTransmitter();
            }
        }

        [KSPEvent(name = "EventToggle", guiActive = false)]
        public void EventToggle()
        {
            if (IsRTActive)
            {
                EventClose();
            }
            else
            {
                EventOpen();
            }
        }

        [KSPEvent(name = "EventTarget", guiActive = false, guiName = "Target")]
        [IgnoreSignalDelayAttribute]
        public void EventTarget()
        {
            //RTCore.Instance.Gui.OpenAntennaConfig(this, vessel);
        }

        [KSPEvent(name = "EventOpen", guiActive = false)]
        public void EventOpen()
        {
            SetState(true);
        }

        [KSPEvent(name = "EventClose", guiActive = false)]
        public void EventClose()
        {
            SetState(false);
        }

        [KSPAction("ActionToggle", KSPActionGroup.None)]
        public void ActionToggle(KSPActionParam param)
        {
            EventToggle();
        }

        [KSPAction("ActionOpen", KSPActionGroup.None)]
        public void ActionOpen(KSPActionParam param)
        {
            EventOpen();
        }

        [KSPAction("ActionClose", KSPActionGroup.None)]
        public void ActionClose(KSPActionParam param)
        {
            EventClose();
        }

        [KSPEvent(name = "OverrideTarget", active = true, guiActiveUnfocused = true,
            unfocusedRange = 5, externalToEVAOnly = true, guiName = "[EVA] Jack-in!")]

        [IgnoreSignalDelayAttribute]
        public void OverrideTarget()
        {
            //RTCore.Instance.Gui.OpenAntennaConfig(this, vessel);
        }

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
            if (node.HasValue("DishAngle"))
            {
                RTDishRadians = Math.Cos(DishAngle / 2 * Math.PI / 180);
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
                RTUtil.Log("Found Transmitter");
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
                LoadAnimations();
                SetState(IsRTActive);
            }
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
            RTUtil.Log("AddTransmitter: null = {0}", mTransmitterConfig == null);
            if (mTransmitterConfig == null) return;
            var transmitters = part.FindModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0)
            {
                mTransmitter = transmitters.First();
            }
            else
            {
                var copy = new ConfigNode();
                mTransmitterConfig.CopyTo(copy);
                part.AddModule(copy);
                AddTransmitter();
                RTUtil.Log("AddTransmitter Success");
            }
        }

        private void RemoveTransmitter()
        {
            RTUtil.Log("RemoveTransmitter");
            if (mTransmitter == null) return;
            part.RemoveModule((PartModule) mTransmitter);
            mTransmitter = null;
        }

        private State UpdateControlState()
        {
            if (RTCore.Instance == null) return State.Operational;

            if (IsRTBroken) return State.Malfunction;

            if (!IsRTActive) return State.Off;

            ModuleResource request = new ModuleResource();
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
            UpdateContext();
        }

        private void UpdateContext()
        {
            GUI_OmniRange = RTUtil.FormatSI(Omni, "m");
            GUI_DishRange = RTUtil.FormatSI(Dish, "m");
            GUI_EnergyReq = RTUtil.FormatConsumption(Consumption);
        }

        private List<IScalarModule> FindFxModules(int[] indices, bool showUI)
        {
            if (indices == null) return null;
            var modules = new List<IScalarModule>();
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
                    RTUtil.Log("[TransmitterModule]: Part Module {0} doesn't implement IScalarModule", part.Modules[i].name);
                }
            }
            return modules;
        }

        private IEnumerator SetFXModules_Coroutine(List<IScalarModule> modules, float tgtValue)
        {
            if (modules == null) yield break;
            bool done = false;
            while (!done)
            {
                done = true;
                foreach (var module in modules)
                {
                    if (Mathf.Abs(module.GetScalar - tgtValue) >= 0.01f)
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
    }
}