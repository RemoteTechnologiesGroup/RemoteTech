using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using RemoteTech.SimpleTypes;

namespace RemoteTech.Modules
{
    /// <summary>
    /// This module represents an omni-directional antenna that is always on, but consumes no power. Intended as a secondary function on non-antenna parts, such as probe cores.
    /// </summary>
    [KSPModule("#RT_Editor_TechnologyPerk")]//Technology Perk
    public class ModuleRTAntennaPassive : PartModule, IAntenna
    {
        public String Name { get { return part.partInfo.title; } }
        public Guid Guid { get { return vessel.id; } }
        public bool Powered { get { return Activated; } }
        public bool Connected { get { return RTCore.Instance != null && RTCore.Instance.Network.IsAntennaConnected(this); } }
        public bool Activated { get { return Unlocked; } set { return; } }
        public bool Animating { get { return false; } }

        public bool CanTarget { get { return false; } }
        public Guid Target { get { return Guid.Empty; } set { return; } }

        public float Dish { get { return -1.0f; } }
        public double CosAngle { get { return 1.0f; } }
        public float Omni { get { return Activated ? OmniRange * RangeMultiplier : 0.0f; } }
        public float Consumption { get { return 0.0f; } }
        public Vector3d Position { get { return vessel.GetWorldPos3D(); } }

        private float RangeMultiplier { get { return RTSettings.Instance.RangeMultiplier; } }
        public bool Unlocked { get { return RTUtil.IsTechUnlocked(TechRequired); } }

        [KSPField]
        public bool
            ShowEditor_OmniRange = true,
            ShowGUI_OmniRange = true;

        [KSPField(guiName = "#RT_ModuleUI_Omnirange")]//Omni range
        public String GUI_OmniRange;

        [KSPField]
        public String
            TechRequired = "None";

        [KSPField]
        public float
            OmniRange;

        [KSPField(isPersistant = true)]
        public bool
            IsRTAntenna = true,
            IsRTBroken = false;

        public bool IsRTActive { get => mState.Activated; set => mState.Activated = value; }
        public bool IsRTPowered { get => mState.Powered; set => mState.Powered = value; }
        public double RTDishCosAngle { get => mState.CosAngle; set => mState.CosAngle = value; }

        [KSPField(isPersistant = true)]
        public float
            RTOmniRange = 0.0f,
            RTDishRange = -1.0f;

        [KSPField] // Persistence handled by Save()
        public Guid RTAntennaTarget = Guid.Empty;

        // workarround for ksp 1.0
        [KSPField]
        public float
            RTPacketInterval = 0.0f,
            RTPacketSize = 0.0f,
            RTPacketResourceCost = 0.0f;

        public int[] mDeployFxModuleIndices, mProgressFxModuleIndices;
        public ConfigNode mTransmitterConfig;
        private IScienceDataTransmitter mTransmitter;
        private readonly AntennaState mState = new() { Activated = true };

        private Guid mRegisteredId { get => mState.Guid; set => mState.Guid = value; }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            if (ShowEditor_OmniRange && Unlocked)
            {
                info.AppendFormat(Localizer.Format("#RT_Editor_TechnologyPerk_info1","{1}"), RTUtil.FormatSI(OmniRange, "m"), RTUtil.FormatSI(OmniRange, "m"));//"Integrated Omni: {1} always-on"
            }

            return info.ToString();
        }

        public virtual void SetState(bool state)
        {
            IsRTActive = state;
            if(RTCore.Instance != null)
            {
                var satellite = RTCore.Instance.Network[Guid];
                bool route_home = (Omni > 0 || Dish > 0)
                    && RTCore.Instance.Network[satellite].Any(r => RTCore.Instance.Network.GroundStations.ContainsKey(r.Goal.Guid));
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

        public void OnConnectionRefresh()
        {
            SetState(IsRTActive);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            bool isRTActive = IsRTActive;
            node.TryGetValue("IsRTActive", ref isRTActive);
            IsRTActive = isRTActive;

            bool isRTPowered = IsRTPowered;
            node.TryGetValue("IsRTPowered", ref isRTPowered);
            IsRTPowered = isRTPowered;

            double rtDishCosAngle = RTDishCosAngle;
            node.TryGetValue("RTDishCosAngle", ref rtDishCosAngle);
            RTDishCosAngle = rtDishCosAngle;

            if (node.HasNode("TRANSMITTER"))
            {
                RTLog.Notify("ModuleRTAntennaPassive: Found TRANSMITTER block.");
                mTransmitterConfig = node.GetNode("TRANSMITTER");
                mTransmitterConfig.AddValue("name", "ModuleRTDataTransmitter");

                // workarround for ksp 1.0
                if (mTransmitterConfig.HasValue("PacketInterval"))
                    RTPacketInterval = float.Parse(mTransmitterConfig.GetValue("PacketInterval"));

                if (mTransmitterConfig.HasValue("PacketSize"))
                    RTPacketSize = float.Parse(mTransmitterConfig.GetValue("PacketSize"));

                if (mTransmitterConfig.HasValue("PacketResourceCost"))
                    RTPacketResourceCost = float.Parse(mTransmitterConfig.GetValue("PacketResourceCost"));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.SetValue("IsRTActive", IsRTActive, createIfNotFound: true);
            node.SetValue("IsRTPowered", IsRTPowered, createIfNotFound: true);
            node.SetValue("RTDishCosAngle", RTDishCosAngle, createIfNotFound: true);
        }

        public override void OnStart(StartState state)
        {
            // Registered here rather than OnAwake: OnAwake also fires on part
            // prefabs during compilation, whose state is invalid to register.
            mState.Register();

            // workarround for ksp 1.0
            if (mTransmitterConfig == null)
            {
                mTransmitterConfig = new ConfigNode("TRANSMITTER");
                mTransmitterConfig.AddValue("PacketInterval", RTPacketInterval);
                mTransmitterConfig.AddValue("PacketSize", RTPacketSize);
                mTransmitterConfig.AddValue("PacketResourceCost", RTPacketResourceCost);
                mTransmitterConfig.AddValue("name", "ModuleRTDataTransmitter");
            }

            if (RTCore.Instance != null)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = vessel.id;
                RTCore.Instance.Antennas.Register(vessel.id, this);
                SetState(true);
                GUI_OmniRange = RTUtil.FormatSI(Omni, "m");
            }
        }

        private void FixedUpdate()
        {
            RTOmniRange = Omni;
            RTDishRange = Dish;
            IsRTPowered = Powered;
            Fields["GUI_OmniRange"].guiActive = Activated && ShowGUI_OmniRange;

            mState.Dish = Dish;
            mState.Omni = Omni;
            mState.Consumption = Consumption;
            mState.CanTarget = CanTarget;
            mState.Connected = Connected;
        }

        private void AddTransmitter()
        {
            if (mTransmitterConfig == null || !mTransmitterConfig.HasValue("name")) return;
            var transmitters = part.FindModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0)
            {
                RTLog.Notify("ModuleRTAntennaPassive: Find TRANSMITTER success.");
                mTransmitter = transmitters.First();
            }
            else
            {
                var copy = new ConfigNode();
                mTransmitterConfig.CopyTo(copy);
                part.AddModule(copy);
                AddTransmitter();
                RTLog.Notify("ModuleRTAntennaPassive: Add TRANSMITTER success.");
            }
        }

        private void RemoveTransmitter()
        {
            RTLog.Notify("ModuleRTAntennaPassive: Remove TRANSMITTER success.");
            if (mTransmitter == null) return;
            part.RemoveModule((PartModule) mTransmitter);
            mTransmitter = null;
        }

        /// <summary>
        /// Deprecated?
        /// </summary>
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
                    RTLog.Notify("ModuleRTAntennaPassive: Part Module {0} doesn't implement IScalarModule", part.Modules[i].name);
                }
            }
            return modules;
        }

        private void OnDestroy()
        {
            RTLog.Notify("ModuleRTAntennaPassive: OnDestroy");
            mState.Dispose();
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
            if (RTCore.Instance != null && mRegisteredId != vessel.id)
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
            return String.Format("ModuleRTAntennaPassive(Name: {0}, Guid: {1}, Omni: {2})", Name, mRegisteredId, Omni);
        }
    }
}
