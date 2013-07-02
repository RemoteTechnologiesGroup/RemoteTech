using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class ModuleSPU : PartModule, ISignalProcessor {
        public bool Powered { get { return IsPowered; } }

        public bool CommandStation {
            get { return IsPowered && IsRTCommandStation && 
                    Vessel.protoVessel.GetVesselCrew().Count >= 4;
            }
        }

        public String Name { get { return Vessel.vesselName; } }

        public Guid Guid { get { return Vessel.id; } }

        public Vector3 Position { get { return Vessel.GetWorldPos3D(); } }

        public CelestialBody Body { get { return Vessel.orbit.referenceBody; } }

        public bool LocalControl {
            get { return Vessel.protoVessel.GetVesselCrew().Count > 0; }
        }

        public Vessel Vessel { get { return vessel; } }

        [KSPField(isPersistant = true)]
        public bool IsPowered = false;

        [KSPField(isPersistant = true)]
        public bool IsRTSignalProcessor = true;

        [KSPField(isPersistant = true)]
        public bool IsRTCommandStation = true;

        [KSPField]
        public int minimumCrew = 0;

        [KSPField(guiName = "State", guiActive = true)]
        public String Status;

        private enum State {
            Operational,
            NoCrew,
            NoResources,
            NoConnection
        }

        private Guid mRegisteredId;

        // Unity requires this to be public for some fucking magical reason?!
        public List<ModuleResource> RequiredResources;

        public override string GetInfo() {
            return IsRTCommandStation ? "Remote Command" : "Remote Control";
        }

        public override void OnStart(StartState state) {
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            GameEvents.onPartUndock.Add(OnPartUndock);
            if (RTCore.Instance != null) {
                mRegisteredId = RTCore.Instance.Satellites.Register(Vessel, this);
            }
        }

        public void OnDestroy() {
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null) {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
            }
        }

        public override void OnLoad(ConfigNode node) {
            if (RequiredResources == null) {
                RequiredResources = new List<ModuleResource>();
            }
            foreach (ConfigNode cn in node.nodes) {
                if(!cn.name.Equals("RESOURCE")) continue;
                ModuleResource rs = new ModuleResource();
                rs.Load(cn);
                RequiredResources.Add(rs);
            }
        }

        private State UpdateControlState() {
            // Can't remove isControlSource or autopilot won't work.
            if (!RTCore.Instance) return State.NoConnection;
            if (part.protoModuleCrew.Count < minimumCrew) {
                IsPowered = part.isControlSource = false;
                return State.NoCrew;
            }
            foreach (ModuleResource rs in RequiredResources) {
                rs.currentRequest = rs.rate * TimeWarp.deltaTime;
                rs.currentAmount = part.RequestResource(rs.id, rs.currentRequest);
                if (rs.currentAmount < rs.currentRequest * 0.9) {
                    IsPowered = part.isControlSource = false;
                    return State.NoResources;
                }
            }
            IsPowered = part.isControlSource = true;
            if (mRegisteredId == Guid.Empty ||
                    !RTCore.Instance.Satellites.For(mRegisteredId).Connection.Exists) {
                return State.NoConnection;
            }
            return State.Operational;
        }

        public void FixedUpdate() {
            switch (UpdateControlState()) {
                case State.Operational:
                    Status = "Operational.";
                    break;
                case State.NoCrew:
                    Status = "Not enough crew.";
                    break;
                case State.NoConnection:
                    Status = "No connection.";
                    break;
                case State.NoResources:
                    Status = "Out of power";
                    break;
            }
        }

        public void OnPartUndock(Part p) {
            if (p.vessel == vessel) OnVesselModified(p.vessel);
        }

        public void OnVesselModified(Vessel v) {
            if (IsPowered) {
                if (vessel == null || (mRegisteredId != Vessel.id)) {
                    RTCore.Instance.Satellites.Unregister(Vessel.id, this);
                    if (vessel != null) {
                        mRegisteredId = RTCore.Instance.Satellites.Register(Vessel, this); 
                    }
                }
            }
        }
    }
}
