using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAntennaPassive : PartModule, IAntenna
    {
        public String Name { get { return part.partInfo.title; } }
        public Guid Guid { get { return vessel.id; } }
        public bool Powered { get { return part.isControllable; } }
        public bool Activated { get { return true; } set { return; } }
        public bool Animating { get { return false; } }

        public bool CanTarget { get { return false; } }
        public Guid Target { get { return Guid.Empty; } set { return; } }

        public float Dish { get { return 0.0f; } }
        public double Radians { get { return 1.0f; } }
        public float Omni { get { return OmniRange; } }
        public float Consumption { get { return 0.0f; } }

        [KSPField]
        public bool
            ShowEditor_OmniRange = true,
            ShowGUI_OmniRange = true;

        [KSPField(guiName = "Omni range")]
        public String GUI_OmniRange;

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
        public double RTDishRadians = 1.0f;

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

            if (ShowEditor_OmniRange && OmniRange > 0)
            {
                info.AppendFormat("Integrated Omni: {0} / {1}", RTUtil.FormatSI(OmniRange, "m"), RTUtil.FormatSI(OmniRange, "m")).AppendLine();
            }

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public virtual void SetState(bool state)
        {
            IsRTActive = state;
            var satellite = RTCore.Instance.Network[Guid];
            bool route_home = RTCore.Instance.Network[satellite].Any(r => r.Links[0].Interfaces.Contains(this) && r.Goal == RTCore.Instance.Network.MissionControl);
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
                RTUtil.Log("Found Transmitter");
                mTransmitterConfig = node.GetNode("TRANSMITTER");
                mTransmitterConfig.AddValue("name", "ModuleRTDataTransmitter");
            }
        }

        public override void OnStart(StartState state)
        {
            Fields["GUI_OmniRange"].guiActive = ShowGUI_OmniRange;

            if (RTCore.Instance != null)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = vessel.id;
                RTCore.Instance.Antennas.Register(vessel.id, this);
                SetState(true);
                RTOmniRange = OmniRange;
                GUI_OmniRange = RTUtil.FormatSI(Omni, "m");
            }
        }

        private void AddTransmitter()
        {
            if (mTransmitterConfig == null || !mTransmitterConfig.HasValue("name")) return;
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
                    RTUtil.Log("[TransmitterModule]: Part Module {0} doesn't implement IScalarModule", part.Modules[i].name);
                }
            }
            return modules;
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

        public override string ToString()
        {
            return String.Format("ModuleRTAntennaPassive({0}, {1})", Name, GetInstanceID());
        }
    }
}