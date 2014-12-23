using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    [KSPModule("Technology Perk")]
    public class ModuleRTAntennaPassive : PartModule, IAntenna
    {
        public String Name { get { return part.partInfo.title; } }
        public Guid Guid { get { return vessel.id; } }
        public bool Powered { get { return Activated; } }
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
        private bool Unlocked { get { return ResearchAndDevelopment.GetTechnologyState(TechRequired) == RDTech.State.Available || TechRequired.Equals("None"); } }

        [KSPField]
        public bool
            ShowEditor_OmniRange = true,
            ShowGUI_OmniRange = true;

        [KSPField(guiName = "Omni range")]
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
            IsRTActive = true,
            IsRTPowered = false,
            IsRTBroken = false;

        [KSPField(isPersistant = true)]
        public double RTDishCosAngle = 1.0f;

        [KSPField(isPersistant = true)]
        public float
            RTOmniRange = 0.0f,
            RTDishRange = -1.0f;

        [KSPField] // Persistence handled by Save()
        public Guid RTAntennaTarget = Guid.Empty;

        public int[] mDeployFxModuleIndices, mProgressFxModuleIndices;
//        private List<IScalarModule> mDeployFxModules = new List<IScalarModule>();
//        private List<IScalarModule> mProgressFxModules = new List<IScalarModule>();
        public ConfigNode mTransmitterConfig;
        private IScienceDataTransmitter mTransmitter;

        private Guid mRegisteredId;

        public override string GetInfo()
        {
            var info = new StringBuilder();
            if (ShowEditor_OmniRange && Unlocked)
            {
                info.AppendFormat("Integrated Omni: {1} always-on", RTUtil.FormatSI(OmniRange, "m"), RTUtil.FormatSI(OmniRange, "m"));
            }

            return info.ToString();
        }

        public virtual void SetState(bool state)
        {
            IsRTActive = state;
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

        public void OnConnectionRefresh()
        {
            SetState(IsRTActive);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("TRANSMITTER"))
            {
                RTLog.Notify("ModuleRTAntennaPassive: Found TRANSMITTER block.");
                mTransmitterConfig = node.GetNode("TRANSMITTER");
                mTransmitterConfig.AddValue("name", "ModuleRTDataTransmitter");
            }
        }

        public override void OnStart(StartState state)
        {
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
            return String.Format("ModuleRTAntennaPassive(Name: {0}, Guid: {1}, Omni: {2})", Name, mRegisteredId, Omni);
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ModuleRTAntennaPassive_ReloadPartInfo : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine(RefreshPartInfo());
        }

        private IEnumerator RefreshPartInfo()
        {
            yield return null;
            foreach (var ap in PartLoader.LoadedPartsList.Where(ap => ap.partPrefab.Modules != null && ap.partPrefab.Modules.Contains("ModuleRTAntennaPassive")))
            {
                var new_info = new StringBuilder();
                foreach (PartModule pm in ap.partPrefab.Modules)
                {
                    var info = pm.GetInfo();
                    new_info.Append(info);
                    if (info != String.Empty) new_info.AppendLine();
                }
                ap.moduleInfo = new_info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            }
        }
    }
}