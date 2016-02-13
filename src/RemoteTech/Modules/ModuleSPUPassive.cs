using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech.Modules
{
    public class ModuleSPUPassive : PartModule, ISignalProcessor
    {
        public String Name { get { return String.Format("ModuleSPUPassive({0})", VesselName); } }
        public String VesselName { get { return vessel.vesselName; } set { vessel.vesselName = value; } }
        public bool VesselLoaded { get { return vessel.loaded; } }
        public Guid Guid { get { return mRegisteredId; } }
        public Vector3 Position { get { return vessel.GetWorldPos3D(); } }
        public CelestialBody Body { get { return vessel.mainBody; } }
        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(vessel); } }
        public bool Powered { get { return Vessel.IsControllable; } }
        public bool IsCommandStation { get { return false; } }
        public FlightComputer.FlightComputer FlightComputer { get { return null; } }
        public Vessel Vessel { get { return vessel; } }
        public bool IsMaster { get { return false; } }

        private ISatellite Satellite { get { return RTCore.Instance.Satellites[mRegisteredId]; } }

        private Guid mRegisteredId;

        [KSPField(isPersistant = true)]
        public bool
            IsRTPowered = false,
            IsRTSignalProcessor = true,
            IsRTCommandStation = false;

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = vessel.id;
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
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = Guid.Empty;
            }
        }

        public void OnPartUndock(Part p)
        {
            OnVesselModified(p.vessel);
        }

        public void OnVesselModified(Vessel v)
        {
            if (RTCore.Instance != null && mRegisteredId != vessel.id)
            {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = vessel.id; 
                RTCore.Instance.Satellites.Register(vessel, this);
            }
        }

        public override string ToString()
        {
            return String.Format("ModuleSPUPassive({0}, {1})", Vessel != null ? Vessel.vesselName : "null", mRegisteredId);
        }
    }
}