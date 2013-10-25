using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleSPU : PartModule, ISignalProcessor
    {
        public String Name { get { return String.Format("ModuleSPU({0})", VesselName); } }
        public String VesselName { get { return vessel.vesselName; } set { vessel.vesselName = value; } }
        public bool VesselLoaded { get { return vessel.loaded; } }
        public Guid Guid { get { return vessel.id; } }
        public Vector3 Position { get { return vessel.GetWorldPos3D(); } }
        public CelestialBody Body { get { return vessel.mainBody; } }
        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(vessel); } }
        public bool Powered { get { return IsRTPowered; } }
        public bool IsCommandStation { get { return IsRTPowered && IsRTCommandStation && vessel.GetVesselCrew().Count >= 6; } }
        public FlightComputer FlightComputer { get; private set; }
        public Vessel Vessel { get { return vessel; } }

        private ISatellite Satellite { get { return RTCore.Instance.Satellites[mRegisteredId]; } }

        [KSPField(isPersistant = true)]
        public bool 
            IsRTPowered = false,
            IsRTSignalProcessor = true,
            IsRTCommandStation = false;

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

        private Guid mRegisteredId;

        public override String GetInfo()
        {
            if (!ShowEditor_Type) return String.Empty;
            return IsRTCommandStation ? "Remote Command" : "Remote Control";
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                GameEvents.onVesselWasModified.Add(OnVesselModified);
                GameEvents.onPartUndock.Add(OnPartUndock);
                mRegisteredId = RTCore.Instance.Satellites.Register(vessel, this);
                FlightComputer = new FlightComputer(this);
            }
            Fields["GUI_Status"].guiActive = ShowGUI_Status;
        }

        public void OnDestroy()
        {
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
                mRegisteredId = RTCore.Instance.Satellites.Register(vessel, this);
            }
        }

        public void HookPartMenus()
        {
            UIPartActionMenuPatcher.Wrap(vessel, (e) =>
            {
                Vessel v = e.listParent.part.vessel;
                if (v != null && v.loaded)
                {
                    var vs = RTCore.Instance.Satellites[v];
                    if (vs != null)
                    {
                        if (vs.SignalProcessor.FlightComputer == null) return;
                        vs.SignalProcessor.FlightComputer.Enqueue(EventCommand.Event(e));
                    }
                }
                else
                {
                    e.Invoke();
                }
            });
        }
    }
}