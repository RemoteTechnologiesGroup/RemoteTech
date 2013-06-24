using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class FlightHandler : IDisposable {
        private Vessel mVessel;

        private readonly RTCore mCore;

        public FlightHandler(RTCore core) {
            mCore = core;
            GameEvents.onVesselChange.Add(OnVesselChange);
            OnVesselChange(FlightGlobals.ActiveVessel);
        }

        public void OnFlyByWire(FlightCtrlState fcs) {
            VesselSatellite vs;
            if ((vs = mCore.Satellites.For(mVessel)) != null) {
                double delayedTime = RTUtil.GetGameTime() + vs.Connection.Delay;
                BufferFlightCtrlStates(vs, fcs, vs.LocalControl ? 0.0 : delayedTime);
                BufferActionGroups(vs, vs.LocalControl ? 0.0 : delayedTime);
            }
        }

        private void BufferFlightCtrlStates(VesselSatellite vs, FlightCtrlState fcs, double delayedTime) {
            vs.FlightComputer.Enqueue(new DelayedFlightCtrlState(fcs, delayedTime));
        }

        private void BufferActionGroups(VesselSatellite vs, double delayedTime) {
            GetLocks();
            KSPActionGroup group = GetActivatedGroup();
            if (group != default(KSPActionGroup)) {
                vs.FlightComputer.Enqueue(new DelayedActionGroup(group, delayedTime));
            }
        }

        private KSPActionGroup GetActivatedGroup() {
            KSPActionGroup action = default(KSPActionGroup);
            if (GameSettings.LAUNCH_STAGES.GetKeyDown())
                action = KSPActionGroup.Stage;
            else if (GameSettings.AbortActionGroup.GetKeyDown())
                action = KSPActionGroup.Abort;
            else if (GameSettings.RCS_TOGGLE.GetKeyDown())
                action = KSPActionGroup.RCS;
            else if (GameSettings.SAS_TOGGLE.GetKeyDown())
                action = KSPActionGroup.SAS;
            else if (GameSettings.SAS_HOLD.GetKeyDown())
                action = KSPActionGroup.SAS;
            else if (GameSettings.SAS_HOLD.GetKeyUp())
                action = KSPActionGroup.SAS;
            else if (GameSettings.BRAKES.GetKeyDown())
                action = KSPActionGroup.Brakes;
            else if (GameSettings.LANDING_GEAR.GetKeyDown())
                action = KSPActionGroup.Gear;
            else if (GameSettings.HEADLIGHT_TOGGLE.GetKeyDown())
                action = KSPActionGroup.Light;
            else if (GameSettings.CustomActionGroup1.GetKeyDown())
                action = KSPActionGroup.Custom01;
            else if (GameSettings.CustomActionGroup2.GetKeyDown())
                action = KSPActionGroup.Custom02;
            else if (GameSettings.CustomActionGroup3.GetKeyDown())
                action = KSPActionGroup.Custom03;
            else if (GameSettings.CustomActionGroup4.GetKeyDown())
                action = KSPActionGroup.Custom04;
            else if (GameSettings.CustomActionGroup5.GetKeyDown())
                action = KSPActionGroup.Custom05;
            else if (GameSettings.CustomActionGroup6.GetKeyDown())
                action = KSPActionGroup.Custom06;
            else if (GameSettings.CustomActionGroup7.GetKeyDown())
                action = KSPActionGroup.Custom07;
            else if (GameSettings.CustomActionGroup8.GetKeyDown())
                action = KSPActionGroup.Custom08;
            else if (GameSettings.CustomActionGroup9.GetKeyDown())
                action = KSPActionGroup.Custom09;
            else if (GameSettings.CustomActionGroup10.GetKeyDown())
                action = KSPActionGroup.Custom10;
            return action;
        }

        void OnVesselChange(Vessel v) {
            if (mVessel != null) {
                mVessel.OnFlyByWire -= this.OnFlyByWire;
            }
            if (v != null) {
                mVessel = v;
                mVessel.OnFlyByWire = this.OnFlyByWire + mVessel.OnFlyByWire;
            }
        }

        void GetLocks() {
            if (!InputLockManager.IsLocked(ControlTypes.STAGING)) {
                InputLockManager.SetControlLock(ControlTypes.STAGING, "LockStaging");
            }
            if (!InputLockManager.IsLocked(ControlTypes.SAS)) {
                InputLockManager.SetControlLock(ControlTypes.SAS, "LockSAS");
            }
            if (!InputLockManager.IsLocked(ControlTypes.RCS)) {
                InputLockManager.SetControlLock(ControlTypes.RCS, "LockRCS");
            }
            if (!InputLockManager.IsLocked(ControlTypes.GROUPS_ALL)) {
                InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "LockActions");
            }
        }

        public void Dispose() {
            InputLockManager.RemoveControlLock("LockStaging");
            InputLockManager.RemoveControlLock("LockSAS");
            InputLockManager.RemoveControlLock("LockRCS");
            InputLockManager.RemoveControlLock("LockActions");
            OnVesselChange(null);
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
    }
}
