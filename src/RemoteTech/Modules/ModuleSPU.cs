using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
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
        public FlightComputer FlightComputer { get; private set; }
        public Vessel Vessel { get { return vessel; } }
        public bool IsMaster { get { return Satellite != null && Satellite.SignalProcessor == (ISignalProcessor) this; } }

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
        private static readonly HashSet<String> eventWhiteList = new HashSet<String>() {
            "RenameVessel", "RenameAsteroidEvent"
        };

        private Guid mRegisteredId;

        public override String GetInfo()
        {
            if (!ShowEditor_Type) return String.Empty;
            return IsRTCommandStation 
                ? String.Format("Remote Command capable ({0}+ crew)", RTCommandMinCrew) 
                : "Remote Control capable";
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = vessel.id; 
                RTCore.Instance.Satellites.Register(vessel, this);
                if (FlightComputer == null)
                    FlightComputer = new FlightComputer(this);
            }
            Fields["GUI_Status"].guiActive = ShowGUI_Status;
        }

        public void OnDestroy()
        {
            RTLog.Notify("ModuleSPU: OnDestroy");
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onPartUndock.Remove(OnPartUndock);
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = Guid.Empty;
            }
            if (FlightComputer != null) FlightComputer.Dispose();
        }

        private State UpdateControlState()
        {
            IsRTPowered = part.isControlSource;
            if (!RTCore.Instance)
            {
                return State.Operational;
            }

            if (!IsRTPowered)
            {
                return State.ParentDefect;
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
            HookPartMenus();
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
            if ((mRegisteredId != vessel.id))
            {
                RTCore.Instance.Satellites.Unregister(mRegisteredId, this);
                mRegisteredId = vessel.id;
                RTCore.Instance.Satellites.Register(vessel, this);
            }
        }

        public void HookPartMenus()
        {
            UIPartActionMenuPatcher.Wrap(vessel, (e, ignore_delay) =>
            {
                var v = FlightGlobals.ActiveVessel;
                if (v == null || v.isEVA || RTCore.Instance == null)
                {
                    e.Invoke();
                    return;
                }
                
                var vs = RTCore.Instance.Satellites[v];
                if (vs == null || vs.HasLocalControl)
                {
                    e.Invoke();
                }
                else if (eventWhiteList.Contains(e.name))
                {
                    e.Invoke();
                }
                else if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
                {
                    if (ignore_delay)
                    {
                        e.Invoke();
                    }
                    else
                    {
                        vs.SignalProcessor.FlightComputer.Enqueue(EventCommand.Event(e));
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
                }
            });
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
                        FlightComputer = new FlightComputer(this);
                    FlightComputer.Save(node);
                }

            }
            catch (Exception e) { print(e); };
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightComputer == null)
                        FlightComputer = new FlightComputer(this);
                    FlightComputer.load(node);
                }
            }
            catch (Exception e) { print(e); };
        }

    }
}