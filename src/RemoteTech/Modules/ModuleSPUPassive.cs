using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech.Modules
{
    /// <summary>
    /// This module allows any vessel with an antenna to participate in a RemoteTech network, even if it does not have a <see cref="ModuleSPU"/>.
    /// <para>It should be included in all RemoteTech antennas. Unlike ModuleSPU, it does not filter commands or provide a flight computer.</para>
    /// </summary>
    public class ModuleSPUPassive : PartModule, ISignalProcessor
    {
        public string Name => $"ModuleSPUPassive({VesselName})";
        public string VesselName
        {
            get { return vessel.vesselName; }
            set { vessel.vesselName = value; }
        }
        public bool VesselLoaded => vessel.loaded;
        public Guid VesselId { get; private set; }
        public Vector3 Position => vessel.GetWorldPos3D();
        public CelestialBody Body => vessel.mainBody;
        public bool Visible => MapViewFiltering.CheckAgainstFilter(vessel);
        public bool Powered => Vessel.IsControllable;
        public bool IsCommandStation => false;
        public FlightComputer.FlightComputer FlightComputer => null;
        public Vessel Vessel => vessel;
        public bool IsMaster => false;

        [KSPField(isPersistant = true)] public bool IsRTPowered;
        [KSPField(isPersistant = true)] public bool IsRTSignalProcessor = true;
        [KSPField(isPersistant = true)] public bool IsRTCommandStation = false;

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                VesselId = vessel.id;
                if(RTCore.Instance != null)
                {
                    RTCore.Instance.Satellites.Register(vessel, this);
                } 
            }
        }

        private void FixedUpdate()
        {
            if (Vessel != null)
            {
                IsRTPowered = Powered;
            }
        }

        public void OnDestroy()
        {
            RTLog.Notify("ModuleSPUPassive: OnDestroy");
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.Unregister(VesselId, this);
                VesselId = Guid.Empty;
            }
        }

        public void OnPartUndock(Part p)
        {
            OnVesselModified(p.vessel);
        }

        public void OnVesselModified(Vessel v)
        {
            if (RTCore.Instance != null && VesselId != vessel.id)
            {
                RTCore.Instance.Satellites.Unregister(VesselId, this);
                VesselId = vessel.id; 
                RTCore.Instance.Satellites.Register(vessel, this);
            }
        }

        public override string ToString()
        {
            return $"ModuleSPUPassive({(Vessel != null ? Vessel.vesselName : "null")}, {VesselId})";
        }
    }
}