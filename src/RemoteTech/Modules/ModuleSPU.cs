using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.FlightComputer;
using UnityEngine;

namespace RemoteTech.Modules
{
    [KSPModule("Signal Processor")]
    public class ModuleSPU : PartModule, ISignalProcessor
    {
        public String Name { get { return String.Format("ModuleSPU({0})", VesselName); } }
        public String VesselName { get { return vessel.vesselName; } set { vessel.vesselName = value; } }
        public bool VesselLoaded { get { return vessel.loaded; } }
        public Guid Guid { get { return mRegisteredId; } }
        public Vector3 Position { get { return vessel.GetWorldPos3D(); } }
        public CelestialBody Body { get { return vessel.mainBody; } }
        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(vessel); } }
        public bool Powered { get { return IsRTPowered; } }
        public bool IsCommandStation 
        { 
            get
            { 
                return IsRTPowered && IsRTCommandStation && vessel.GetVesselCrew().Count >= RTCommandMinCrew;
            }
        }
        public FlightComputer.FlightComputer FlightComputer { get; private set; }
        public Vessel Vessel { get { return vessel; } }
        public bool IsMaster { get { return Satellite != null && (object)Satellite.SignalProcessor == this; } }

        private VesselSatellite Satellite { get { return RTCore.Instance.Satellites[mRegisteredId]; } }

        [KSPField(isPersistant = true)]
        public bool 
            IsRTPowered = false,
            IsRTSignalProcessor = true,
            IsRTCommandStation = false;
        [KSPField(isPersistant = true)]
        public int RTCommandMinCrew = 6;

        [KSPField]
        public bool
            ShowGUI_Status = true,
            ShowEditor_Type = true;

        [KSPField(guiName = "SPU", guiActive = true)]
        public String GUI_Status;

        private enum State
        {
            Operational,
            ParentDefect,
            NoConnection
        }

        /// <summary>
        /// Contains the names of any events that should always be run, 
        /// regardless of connection status or signal delay
        /// </summary>
        private static readonly HashSet<String> eventWhiteList = new HashSet<String>
        {
            "RenameVessel", "RenameAsteroidEvent", //  allow renaming vessels and Asteroids.
            "SpawnTransferDialog", // allow kerbal to transfer even if no connection
            "AimCamera", "ResetCamera" // advanced tweakables: camera events
        };

        /// <summary>
        /// Contains the names of any fields that should always be run, 
        /// regardless of connection status or signal delay.
        /// </summary>
        private static readonly HashSet<String> fieldWhiteList = new HashSet<String>
        {

        };

        private Guid mRegisteredId;

        public override String GetInfo()
        {
            if (!ShowEditor_Type) return String.Empty;
            return IsRTCommandStation 
                ? String.Format("Remote Command capable <color=#00FFFF>({0}+ crew)</color>", RTCommandMinCrew) : "Remote Control capable";
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                GameEvents.onPartActionUICreate.Add(onPartActionUICreate);
                GameEvents.onPartActionUIDismiss.Add(onPartActionUIDismiss);
                mRegisteredId = vessel.id; 
                if(RTCore.Instance != null)
                {
                    RTCore.Instance.Satellites.Register(vessel, this);
                    if (FlightComputer == null)
                        FlightComputer = new FlightComputer.FlightComputer(this);
                }
            }
            Fields["GUI_Status"].guiActive = ShowGUI_Status;
        }

        public void onPartActionUICreate(Part p)
        {
            HookPartMenus();
        }

        public void onPartActionUIDismiss(Part part)
        {
            UIPartActionMenuPatcher.parsedPartActions.Clear();
        }

        public void OnDestroy()
        {
            RTLog.Notify("ModuleSPU: OnDestroy");
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            GameEvents.onPartActionUICreate.Remove(onPartActionUICreate);
            GameEvents.onPartActionUIDismiss.Remove(onPartActionUIDismiss);
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = Guid.Empty;
                if (FlightComputer != null) FlightComputer.Dispose();
            }
        }

        private State UpdateControlState()
        {
            IsRTPowered = part.isControlSource > Vessel.ControlLevel.NONE;
            if (RTCore.Instance == null)
            {
                return State.Operational;
            }

            if (!IsRTPowered)
            {
                // check if the part is itself a ModuleCommand
                var moduleCommand = part.Modules.GetModule<ModuleCommand>();
                if (moduleCommand != null) {
                    // it's a module command *and* a ModuleSPU, so in this case it's still RTPowered (controllable)!
                    // e.g. even if there's no crew in the pod, we should be able to control it because it's a SPU.
                    IsRTPowered = true;
                }
                else  {
                    return State.ParentDefect;
                }
            }

            if (Satellite == null || !RTCore.Instance.Network[Satellite].Any())
            {
                return State.NoConnection;
            }
            return State.Operational;
        }

        public void Update()
        {
            if (FlightComputer != null) FlightComputer.OnUpdate();
        }

        public void FixedUpdate()
        {
            if (FlightComputer != null) FlightComputer.OnFixedUpdate();
            
            switch (UpdateControlState())
            {
                case State.Operational:
                    GUI_Status = "Operational.";
                    break;
                case State.ParentDefect:
                case State.NoConnection:
                    GUI_Status = "No connection.";
                    break;
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

        public void HookPartMenus()
        {
            UIPartActionMenuPatcher.WrapEvent(vessel, (e, ignoreDelay) => InvokeEvent(e, ignoreDelay));
            UIPartActionMenuPatcher.WrapPartAction(vessel, (f, ignoreDelay) => InvokePartAction(f, ignoreDelay));
        }

        private void InvokeEvent(BaseEvent baseEvent, bool ignoreDelay)
        {
            // note: this gets called when the event is invoked through:
            // RemoteTech.FlightComputer.UIPartActionMenuPatcher.Wrapper.Invoke()

            var v = FlightGlobals.ActiveVessel;
            if (v == null || v.isEVA || RTCore.Instance == null)
            {
                baseEvent.Invoke();
                return;
            }

            VesselSatellite vs = null;
            if (RTCore.Instance != null)
            {
                vs = RTCore.Instance.Satellites[v];
            }

            if (vs == null || vs.HasLocalControl)
            {
                baseEvent.Invoke();
            }
            else if (eventWhiteList.Contains(baseEvent.name))
            {
                baseEvent.Invoke();
            }
            else if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
            {
                if (ignoreDelay)
                {
                    baseEvent.Invoke();
                }
                else
                {
                    vs.SignalProcessor.FlightComputer.Enqueue(EventCommand.Event(baseEvent));
                }
            }
            else if (baseEvent.listParent.part.Modules.OfType<IAntenna>().Any() &&
                     !baseEvent.listParent.part.Modules.OfType<ModuleRTAntennaPassive>().Any() &&
                     RTSettings.Instance.ControlAntennaWithoutConnection)
            {
                baseEvent.Invoke();
            }
            else
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
            }
        }


        private void InvokePartAction(BaseField baseField,  bool ignoreDelay)
        {
            var field = (baseField as UIPartActionMenuPatcher.WrappedField);
            if (field == null)
                return;

            var v = FlightGlobals.ActiveVessel;
            if (v == null || v.isEVA || RTCore.Instance == null)
            {
                field.Invoke();
                return;
            }

            VesselSatellite vs = null;
            if (RTCore.Instance != null)
            {
                vs = RTCore.Instance.Satellites[v];
            }

            if (vs == null || vs.HasLocalControl)
            {
                field.Invoke();
            }

            else if (fieldWhiteList.Contains(baseField.name))
            {
                field.Invoke();
            }
            else if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
            {
                if (ignoreDelay)
                {
                    field.Invoke();
                }
                else
                {
                    // queue command into FC
                    vs.SignalProcessor.FlightComputer.Enqueue(PartActionCommand.Field(baseField, field.NewValue));
                }
            }
            /*
            else if (baseEvent.listParent.part.Modules.OfType<IAntenna>().Any() &&
                     !baseEvent.listParent.part.Modules.OfType<ModuleRTAntennaPassive>().Any() &&
                     RTSettings.Instance.ControlAntennaWithoutConnection)
            {
                baseEvent.Invoke();
            }*/
            else
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
            }

        }

        public override string ToString()
        {
            return String.Format("ModuleSPU({0}, {1})", Vessel != null ? Vessel.vesselName : "null", mRegisteredId);
        }

        
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightComputer == null)
                        FlightComputer = new FlightComputer.FlightComputer(this);
                    FlightComputer.Save(node);
                }

            }
            catch (Exception e) { print(e); }
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightComputer == null)
                        FlightComputer = new FlightComputer.FlightComputer(this);
                    FlightComputer.load(node);
                }
            }
            catch (Exception e) { print(e); };
        }

    }
}