using System;
using UnityEngine;
using System.Text;
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
        public bool Powered { get { return mRegisteredId != Guid.Empty && IsRTPowered; } }
        public bool IsCommandStation { get { return IsRTPowered && IsRTCommandStation && vessel.GetVesselCrew().Count >= 6; } }

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
        }

        private State UpdateControlState()
        {
            IsRTPowered = part.isControlSource = true;
            if (!RTCore.Instance)
            {
                return State.Operational;
            }

            // Check if ModuleCommand is powered

            if (Satellite == null || RTCore.Instance.Network[Satellite].Count == 0)
            {
                return State.NoConnection;
            }
            return State.Operational;
        }

        public void FixedUpdate()
        {
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
                if (vessel != null)
                {
                    mRegisteredId = RTCore.Instance.Satellites.Register(vessel, this);
                }
            }
        }

        public void HookPartMenus()
        {
/*          UIPartActionMenuPatcher.Wrap(vessel, (e) =>
            {
                Vessel v = e.listParent.part.vessel;
                if (v != null && v.loaded)
                {
                    var vs = RTCore.Instance.Satellites[v];
                    if (vs != null)
                    {
                        vs.Master.FlightComputer.Enqueue(EventCommand.Event(e));
                    }
                }
                else
                {
                    e.Invoke();
                }
            });*/
        }
    }
}