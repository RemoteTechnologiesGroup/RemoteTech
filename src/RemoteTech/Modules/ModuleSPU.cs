using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Interfaces.FlightComputer;
using RemoteTech.Common.Interfaces.SignalProcessor;
using RemoteTech.Settings;
using RemoteTech.UI;
using UnityEngine;

namespace RemoteTech.Modules
{
    /// <summary>
    /// Module Signal Processing Unit.
    /// <para>This module represents the “autopilot” living in a probe core. A vessel will only filter commands according to network availability and time delay if all parts with ModuleCommand also have ModuleSPU; otherwise, the vessel can be controlled in real time. Having at least one ModuleSPU on board is also required to use the flight computer.</para>
    /// <para>Signal Processors are any part that can receive commands over a working connection (this include all stock probe cores).</para>
    /// <para>Thus, controlling a vessel is made only through the ModuleSPU unit. Players are only able to control a signal processor 5SPU) as long as they have a working connection (which by default is subject to signal delay).</para>
    /// </summary>
    [KSPModule("Signal Processor")]
    public class ModuleSPU : PartModule, ISignalProcessor
    {
        public string Name => $"ModuleSPU({VesselName})";

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
        public bool Powered => IsRTPowered;

        public bool IsCommandStation => IsRTPowered && IsRTCommandStation && vessel.GetVesselCrew().Count >= RTCommandMinCrew;
        public IFlightComputer FlightComputer { get; private set; }
        public Vessel Vessel => vessel;
        public bool IsMaster => Satellite != null && ReferenceEquals(Satellite.SignalProcessor, this);

        /* KSP fields */
        [KSPField(isPersistant = true)] public bool IsRTPowered;
        [KSPField(isPersistant = true)] public bool IsRTSignalProcessor = true;
        [KSPField(isPersistant = true)] public bool IsRTCommandStation = false;
        [KSPField(isPersistant = true)] public int RTCommandMinCrew = 6;

        [KSPField] public bool ShowGUI_Status = true;
        [KSPField] public bool ShowEditor_Type = true;

        [KSPField(guiName = "SPU", guiActive = true)] public string GUI_Status;

        private enum State
        {
            Operational,
            ParentDefect,
            NoConnection
        }

        private VesselSatellite Satellite => RTCore.Instance.Satellites[VesselId];

        /// <summary>Contains the names of any events that should always be run, 
        /// regardless of connection status or signal delay
        /// </summary>
        private static readonly HashSet<string> EventWhiteList = new HashSet<string>
        {
            "RenameVessel", "RenameAsteroidEvent", //  allow renaming vessels and Asteroids.
            "SpawnTransferDialog", // allow Kerbals to transfer even if no connection
            "AimCamera", "ResetCamera" // advanced tweakables: camera events
        };

        /// <summary>Contains the names of any fields that should always be run, 
        /// regardless of connection status or signal delay.
        /// </summary>
        private static readonly HashSet<string> FieldWhiteList = new HashSet<string>{};

        /*
         * Private methods
         */

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
                if (moduleCommand != null)
                {
                    // it's a module command *and* a ModuleSPU, so in this case it's still RTPowered (controllable)!
                    // e.g. even if there's no crew in the pod, we should be able to control it because it's a SPU.
                    IsRTPowered = true;
                }
                else
                {
                    return State.ParentDefect;
                }
            }

            if (Satellite == null || !RTCore.Instance.Network[Satellite].Any())
            {
                return State.NoConnection;
            }

            return State.Operational;
        }

        /*
         * PartModule overridden methods
         */

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                if (vessel == null)
                {
                    RTLog.Notify("ModuleSPU: OnStart : No vessel!", RTLogLevel.LVL2);
                    return;
                }

                RTLog.Notify($"ModuleSPU: OnStart [{VesselName}]");

                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                GameEvents.onPartActionUICreate.Add(OnPartActionUiCreate);
                GameEvents.onPartActionUIDismiss.Add(OnPartActionUiDismiss);
                VesselId = vessel.id; 
                if(RTCore.Instance != null)
                {
                    RTCore.Instance.Satellites.Register(vessel, this);
                    if (FlightComputer == null)
                    {
                        FlightComputerLoader.GetTypes();
                        var computer = (IFlightComputer)FlightComputerLoader.Constructor.Invoke(new object[] {this});
                        FlightComputer = computer;
                    }
                }
            }

            Fields["GUI_Status"].guiActive = ShowGUI_Status;
        }

        public void Update()
        {
            FlightComputer?.OnUpdate();
        }

        public void FixedUpdate()
        {
            FlightComputer?.OnFixedUpdate();

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

        public override string ToString()
        {
            return $"ModuleSPU({(Vessel != null ? Vessel.vesselName : "null")}, {VesselId})";
        }

        public override string GetInfo()
        {
            if (!ShowEditor_Type)
                return string.Empty;

            return IsRTCommandStation
                ? $"Remote Command capable <color=#00FFFF>({RTCommandMinCrew}+ crew)</color>"
                : "Remote Control capable";
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightComputer == null)
                    {
                        FlightComputerLoader.GetTypes();
                        var computer = (IFlightComputer)FlightComputerLoader.Constructor.Invoke(new object[] { this });
                        FlightComputer = computer;
                    }
                    FlightComputer.Save(node);
                }

            }
            catch (Exception e)
            {
                RTLog.Notify("An exception occurred in ModuleSPU.OnSave(): ", RTLogLevel.LVL4, e);
                print(e);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    FlightComputerLoader.GetTypes();
                    var computer = (IFlightComputer)FlightComputerLoader.Constructor.Invoke(new object[] { this });
                    FlightComputer = computer;
                }
            }
            catch (Exception e)
            {
                RTLog.Notify("An exception occurred in ModuleSPU.OnLoad(): ", RTLogLevel.LVL4, e);
                print(e);
            };
        }

        /*
         * Unity engine overridden methods
         */

        public void OnDestroy()
        {
            if (vessel == null)
            {
                RTLog.Notify("ModuleSPU: OnDestroy: no vessel!", RTLogLevel.LVL2);
                FlightComputer?.Dispose();
                return;
            }

            RTLog.Notify($"ModuleSPU: OnDestroy [{VesselName}]");

            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            GameEvents.onPartActionUICreate.Remove(OnPartActionUiCreate);
            GameEvents.onPartActionUIDismiss.Remove(OnPartActionUiDismiss);

            if (RTCore.Instance == null)
                return;

            RTCore.Instance.Satellites.Unregister(VesselId, this);
            VesselId = Guid.Empty;

            FlightComputer?.Dispose();
        }

        /*
         * KSP game events callbacks
         */

        public void OnVesselModified(Vessel v)
        {
            if (RTCore.Instance == null || VesselId == vessel.id)
                return;

            RTCore.Instance.Satellites.Unregister(VesselId, this);
            VesselId = vessel.id;
            RTCore.Instance.Satellites.Register(vessel, this);
        }

        public void OnPartUndock(Part p)
        {
            OnVesselModified(p.vessel);
        }

        public void OnPartActionUiCreate(Part partForUi)
        {
            // check if the part is actually one from this vessel
            if (partForUi.vessel != vessel)
                return;

            // hook part menu
            UIPartActionMenuPatcher.WrapPartActionEventItem(partForUi, InvokeEvent);
            UIPartActionMenuPatcher.WrapPartActionFieldItem(partForUi, InvokePartAction);
        }

        public void OnPartActionUiDismiss(Part partForUi)
        {
            UIPartActionMenuPatcher.ParsedPartActions.Clear();
        }

        /*
         * UIPartActionMenuPatcher actions
         */

        private static void InvokeEvent(BaseEvent baseEvent, bool ignoreDelay)
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
            else if (EventWhiteList.Contains(baseEvent.name))
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
                    vs.SignalProcessor.FlightComputer.EnqueueEventCommand(baseEvent);
                }
            }
            else if (baseEvent.listParent.part.Modules.OfType<IAntenna>().Any() &&
                     !baseEvent.listParent.part.Modules.OfType<ModuleRTAntennaPassive>().Any() &&
                     CoreSettingsManager.Instance.ControlAntennaWithoutConnection)
            {
                baseEvent.Invoke();
            }
            else
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
            }
        }

        private static void InvokePartAction(BaseField baseField,  bool ignoreDelay)
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

            else if (FieldWhiteList.Contains(baseField.name))
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
                    vs.SignalProcessor.FlightComputer.EnqueuePartActionCommand(baseField, field.NewValue);
                }
            }            
            else if (field.host is PartModule && ((PartModule)field.host).part.Modules.OfType<IAntenna>().Any() &&
                     !((PartModule)field.host).part.Modules.OfType<ModuleRTAntennaPassive>().Any() &&
                     CoreSettingsManager.Instance.ControlAntennaWithoutConnection)
            {
                field.Invoke();
            }
            else
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
            }
        }
    }
}