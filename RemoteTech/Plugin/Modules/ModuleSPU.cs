using System;
using System.Collections;
using UnityEngine;

namespace RemoteTech {
    public class ModuleSPU : PartModule, ISignalProcessor {
        public String Name { get { return Vessel.vesselName; } }
        public Guid Guid { get { return Vessel.id; } }
        public bool Powered { get { return IsPowered; } }
        public Vector3 Position { get { return Vessel.orbit.getTruePositionAtUT(Planetarium.GetUniversalTime()); } }
        public CelestialBody Body { get { return Vessel.orbit.referenceBody; } }
        public Vessel Vessel { get { return vessel; } }

        [KSPField(isPersistant = true)]
        public bool
            IsRTSignalProcessor = true,
            IsPowered = false;

        Guid mRegisteredId;

        public override string GetInfo() {
            return "Remote Control";
        }

        [KSPEvent(guiName = "Settings", guiActive = true)]
        public void Settings() {
            (new SatelliteGUIWindow(RTCore.Instance.Satellites.For(Vessel.id))).Show();
        }

        public override void OnStart(StartState state) {
            if (RTCore.Instance != null) {
                mRegisteredId = RTCore.Instance.Satellites.Register(Vessel.id, this);
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
            }
        }

        public void OnDestroy() {
            if (RTCore.Instance != null) {
                RTCore.Instance.Satellites.Register(Vessel.id, this);
                GameEvents.onVesselWasModified.Remove(OnVesselModified);
                GameEvents.onPartUndock.Remove(OnPartUndock);
            }
        }

        public void OnPartUndock(Part p) {
            if (p.vessel == vessel) {
                OnVesselModified(p.vessel);
            }
        }

        public void OnVesselModified(Vessel v) {
            if (vessel == null || (mRegisteredId != Vessel.id)) {
                RTCore.Instance.Satellites.Unregister(Vessel.id, this);
                if (vessel != null) {
                    mRegisteredId = RTCore.Instance.Satellites.Register(Vessel.id, this);
                }
            }
        }
    }
}



