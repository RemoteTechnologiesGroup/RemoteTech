using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class ModuleSPU : PartModule, ISignalProcessor {
        public bool Powered {
            get { return mRegisteredId != Guid.Empty && IsRTPowered; }
        }

        public bool CommandStation {
            get { return Powered && IsRTCommandStation && Vessel.GetVesselCrew().Count >= 6; }
        }

        public Guid Guid {
            get { return Vessel == null ? Guid.Empty : Vessel.id; }
        }

        public Vessel Vessel {
            get { return vessel; }
        }

        public VesselSatellite Satellite {
            get {
                return mRegisteredId == Guid.Empty 
                    ? null 
                    : RTCore.Instance.Satellites[mRegisteredId];
            }
        }

        public FlightComputer FlightComputer { get; private set; }

        [KSPField(isPersistant = true)]
        public bool IsRTPowered = false;

        [KSPField(isPersistant = true)]
        public bool IsRTSignalProcessor = true;

        [KSPField(isPersistant = true)]
        public bool IsRTCommandStation = false;

        [KSPField]
        public int minimumCrew = 0;

        [KSPField]
        public bool
            ShowGUI_Status = true,
            ShowEditor_Type = true;

        [KSPField(guiName = "State", guiActive = true)]
        public String Status;

        [KSPEvent(guiName = "Control From Here", guiActive = true)]
        [IgnoreSignalDelayAttribute]
        public void MakeReference() {
            vessel.SetReferenceTransform(part);
        }

        [KSPEvent(name = "ToggleDebugDelay", active = true, guiActive = true, guiName = "Toggle debug delay")]
        [IgnoreSignalDelayAttribute]
        public void ToggleDebugDelay() {
            if (RTCore.Instance.Settings.DebugOffsetDelay == 0)
                RTCore.Instance.Settings.DebugOffsetDelay = 1;
            else
                RTCore.Instance.Settings.DebugOffsetDelay = 0;
        }

        [KSPEvent(name = "OpenFC", active = true, guiActive = true, guiName = "Flight Computer")]
        [IgnoreSignalDelayAttribute]
        public void OpenFC() {
            RTCore.Instance.Gui.OpenFlightComputer(this);
        }

        public bool Master {
            get {
                return (Satellite == null) ? false : (Satellite.Master == (ISignalProcessor) this);
            }
        }

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
            
            if (!ShowEditor_Type) return String.Empty;

            return IsRTCommandStation ? "Remote Command" : "Remote Control";
        }

        // This is required in order to pass control source test on flight start directly from launchpad/runway.
        public override void OnAwake() {
            part.isControlSource = true;
        }

        public override void OnStart(StartState state) {
            if (RTCore.Instance != null) {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = RTCore.Instance.Satellites.Register(Vessel, this);
                if (FlightComputer == null) {
                    FlightComputer = new FlightComputer(this);
                }
            }
            Fields["Status"].guiActive = ShowGUI_Status;
        }

        public void OnDestroy() {
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null) {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = Guid.Empty;
            }
            if (FlightComputer != null) {
                FlightComputer.Dispose();
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
            IsRTPowered = part.isControlSource = true;
            if (!RTCore.Instance) {
                return State.Operational;
            }
            if (part.protoModuleCrew.Count < minimumCrew) {
                IsRTPowered = part.isControlSource = false;
                return State.NoCrew;
            }
            foreach (ModuleResource rs in RequiredResources) {
                rs.currentRequest = rs.rate * TimeWarp.deltaTime;
                rs.currentAmount = part.RequestResource(rs.id, rs.currentRequest);
                if (rs.currentAmount < rs.currentRequest * 0.9) {
                    IsRTPowered = part.isControlSource = false;
                    return State.NoResources;
                }
            }
            if (Satellite == null || !Satellite.Connection.Exists) {
                return State.NoConnection;
            }
            return State.Operational;
        }

        public void FixedUpdate() {
            if (FlightComputer != null) {
                FlightComputer.OnFixedUpdate();
            }
            HookPartMenus();
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
            OnVesselModified(p.vessel);
        }

        public void OnVesselModified(Vessel v) {
            if ((mRegisteredId != Vessel.id)) {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                if (vessel != null) {
                    mRegisteredId = RTCore.Instance.Satellites.Register(Vessel, this);
                }
            }
        }

        public void HookPartMenus() {
            UIPartActionMenuPatcher.Wrap(vessel, (e) => {
                Vessel v = e.listParent.part.vessel;
                if (v != null && v.loaded) {
                    var vs = RTCore.Instance.Satellites[v];
                    if (vs != null) {
                        vs.Master.FlightComputer.Enqueue(EventCommand.Event(e));
                    }
                } else {
                    e.Invoke();
                }
            });
        }

        [KSPEvent]
        public void RTdata(BaseEventData data) {
            bool success = true;
            try {
                data.Set<float>("controlDelay", Satellite.Connection.Delay);
            } catch { data.Set<float>("controlDelay", 0); success = false; }

            try {
                data.Set<bool>("localControl", Satellite.LocalControl);
            } catch { data.Set<bool>("localControl", true); success = false; }

            try {
                data.Set<bool>("inRadioContact", Satellite.Connection.Exists);
            } catch { data.Set<bool>("inRadioContact", true); success = false; }

            try {
                data.Set<bool>("isCommandStation", Satellite.CommandStation);
            } catch { data.Set<bool>("isCommandStation", true); success = false; }

            data.Set<bool>("success", success);
        }
    }
}
