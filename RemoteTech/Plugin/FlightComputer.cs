using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class FlightComputer : IDisposable, IEnumerable<String> {
        private FlightCommand mCommand = new FlightCommand(0.0f);
        private Vector3 mManeuver;
        private Quaternion mKillrot;
        private FlightCtrlState mPreviousCtrl = new FlightCtrlState();

        private readonly Legacy.FlightComputer mLegacyComputer;
        private readonly VesselSatellite mSatellite;
        private readonly List<FlightCommand> mFlightCommandBuffer = new List<FlightCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer = new PriorityQueue<DelayedFlightCtrlState>();
        private readonly List<DelayedActionGroup> mActionGroupBuffer = new List<DelayedActionGroup>();
        
        public double ExtraDelay { get; set; }

        public FlightComputer(VesselSatellite vs) {
            mSatellite = vs;
            mPreviousCtrl.CopyFrom(vs.Vessel.ctrlState);
            mLegacyComputer = new Legacy.FlightComputer(vs.Vessel);
            vs.Vessel.OnFlyByWire = this.OnFlyByWirePre + vs.Vessel.OnFlyByWire;
        }

        public void Dispose() {
            if (mSatellite.Vessel != null) {
                mSatellite.Vessel.OnFlyByWire -= OnFlyByWirePre;
                mSatellite.Vessel.OnFlyByWire -= OnFlyByWirePost;
            }
        }

        public void Enqueue(FlightCommand fc) {
            fc.ExtraDelay = ExtraDelay;
            mFlightCommandBuffer.Insert(~mFlightCommandBuffer.BinarySearch(fc), fc);
        }

        public void Enqueue(DelayedFlightCtrlState fcs) {
            fcs.ExtraDelay = ExtraDelay;
            mFlightCtrlBuffer.Enqueue(fcs);
        }

        public void Enqueue(DelayedActionGroup ag) {
            // For some reason the system sometimes catches the key twice?
            if (mActionGroupBuffer.Count > 0 &&
                ag.ActionGroup == mActionGroupBuffer[mActionGroupBuffer.Count - 1].ActionGroup &&
                ag.TimeStamp - mActionGroupBuffer[mActionGroupBuffer.Count - 1].TimeStamp
                    < 0.1)
                return;

            ag.ExtraDelay = ExtraDelay;
            mActionGroupBuffer.Insert(~mActionGroupBuffer.BinarySearch(ag), ag);
        }

        private void OnFlyByWirePre(FlightCtrlState fs) {
            // Ensure the post-onflybywire still has correct execution order
            mSatellite.Vessel.OnFlyByWire -= OnFlyByWirePost;
            mSatellite.Vessel.OnFlyByWire += OnFlyByWirePost;
            // Ensure all controls are still locked
            RTCore.Instance.GetLocks();
            // Take inputs if active vessel
            if (mSatellite.Vessel == FlightGlobals.ActiveVessel && mSatellite.Connection.Exists) {
                double delayedTime = mSatellite.LocalControl ? 0.0f : RTUtil.GetGameTime() +
                                                                      mSatellite.Connection.Delay;
                // Buffer action groups
                foreach (KSPActionGroup g in GetActivatedGroup()) {
                    Enqueue(new DelayedActionGroup(g, delayedTime));
                }
                // Buffer flight input
                Enqueue(new DelayedFlightCtrlState(fs, delayedTime));
            }
            // Process buffered inputs
            ProcessInputs(fs);
            // Process flightcomputer autopilot
            Autopilot(fs);
        }

        private void OnFlyByWirePost(FlightCtrlState fs) {
            mPreviousCtrl.CopyFrom(fs);
        }

        private IEnumerable<KSPActionGroup> GetActivatedGroup() {
            if (GameSettings.LAUNCH_STAGES.GetKeyDown())
                yield return KSPActionGroup.Stage;
            if (GameSettings.AbortActionGroup.GetKeyDown())
                yield return KSPActionGroup.Abort;
            if (GameSettings.RCS_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.RCS;
            if (GameSettings.SAS_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyDown())
                yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyUp())
                yield return KSPActionGroup.SAS;
            if (GameSettings.BRAKES.GetKeyDown())
                yield return KSPActionGroup.Brakes;
            if (GameSettings.LANDING_GEAR.GetKeyDown())
                yield return KSPActionGroup.Gear;
            if (GameSettings.HEADLIGHT_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.Light;
            if (GameSettings.CustomActionGroup1.GetKeyDown())
                yield return KSPActionGroup.Custom01;
            if (GameSettings.CustomActionGroup2.GetKeyDown())
                yield return KSPActionGroup.Custom02;
            if (GameSettings.CustomActionGroup3.GetKeyDown())
                yield return KSPActionGroup.Custom03;
            if (GameSettings.CustomActionGroup4.GetKeyDown())
                yield return KSPActionGroup.Custom04;
            if (GameSettings.CustomActionGroup5.GetKeyDown())
                yield return KSPActionGroup.Custom05;
            if (GameSettings.CustomActionGroup6.GetKeyDown())
                yield return KSPActionGroup.Custom06;
            if (GameSettings.CustomActionGroup7.GetKeyDown())
                yield return KSPActionGroup.Custom07;
            if (GameSettings.CustomActionGroup8.GetKeyDown())
                yield return KSPActionGroup.Custom08;
            if (GameSettings.CustomActionGroup9.GetKeyDown())
                yield return KSPActionGroup.Custom09;
            if (GameSettings.CustomActionGroup10.GetKeyDown())
                yield return KSPActionGroup.Custom10;
        }

        private void Autopilot(FlightCtrlState fs) {
            while (mFlightCommandBuffer.Count > 0 &&
                   mFlightCommandBuffer[0].TimeStamp < RTUtil.GetGameTime()) {
                if (mSatellite.Connection.Exists) {
                    mCommand = mFlightCommandBuffer[0];
                    if (mCommand.Mode == FlightMode.KillRot) {
                        mKillrot = mSatellite.Vessel.transform.rotation *
                                   Quaternion.AngleAxis(90, Vector3.left);
                    }
                }
                mFlightCommandBuffer.RemoveAt(0);
            }
            if (mCommand.ExtraDelay > 0) {
                mCommand.ExtraDelay -= TimeWarp.deltaTime;
            } else {
                switch (mCommand.Mode) {
                    case FlightMode.Off:
                        return;
                    case FlightMode.KillRot:
                        ProcessOrientation(fs, mKillrot);
                        break;
                    case FlightMode.AttitudeHold:
                        HoldAttitude(fs);
                        break;
                    case FlightMode.AltitudeHold:
                        HoldAltitude(fs);
                        break;
                }
                Burn(fs);
            }
        }

        private void ProcessInputs(FlightCtrlState fcs) {
            // FlightCtrlState
            FlightCtrlState delayed = mPreviousCtrl;
            // Pop any due states off the queue
            while (mFlightCtrlBuffer.Count > 0 &&
                   mFlightCtrlBuffer.Peek().TimeStamp < RTUtil.GetGameTime()) {
                delayed = mFlightCtrlBuffer.Dequeue().State;
            }
            // Decide to nullify it if no connection exists, keep throttle.
            if (!mSatellite.Connection.Exists) {
                float keepThrottle = delayed.mainThrottle;
                delayed.Neutralize();
                delayed.mainThrottle = keepThrottle;
            }
            // Apply FlightCtrlState
            fcs.CopyFrom(delayed);

            // Action groups, pop any due activations off the sorted list
            while (mActionGroupBuffer.Count > 0 &&
                   mActionGroupBuffer[0].TimeStamp < RTUtil.GetGameTime()) {
                // Apply if connected
                if (mSatellite.Connection.Exists) {
                    KSPActionGroup group = mActionGroupBuffer[0].ActionGroup;
                    mSatellite.Vessel.ActionGroups.ToggleGroup(group);
                    if (group == KSPActionGroup.Stage && !FlightInputHandler.fetch.stageLock) {
                        Staging.ActivateNextStage();
                        ResourceDisplay.Instance.Refresh();
                    }
                    if (group == KSPActionGroup.RCS) {
                        FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
                    }
                }
                // Remove from list
                mActionGroupBuffer.RemoveAt(0);
            }
        }

        private void ProcessOrientation(FlightCtrlState fs, Quaternion target) {
            mLegacyComputer.drive(fs, target);
        }

        private void HoldAttitude(FlightCtrlState fs) {
            Vessel v = mSatellite.Vessel;
            Vector3 forward = Vector3.zero;
            Vector3 up = Vector3.zero;
            Quaternion rotationReference;
            switch (mCommand.Frame) {
                case ReferenceFrame.Orbit:
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
                case ReferenceFrame.Surface:
                    forward = v.GetSrfVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
                case ReferenceFrame.Target: // TODO
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
                case ReferenceFrame.North:
                    up = (v.mainBody.position - v.CoM);
                    forward = Vector3.Exclude(
                        up, 
                        v.mainBody.position + v.mainBody.transform.up * (float) v.mainBody.Radius - v.CoM
                     );
                    break;
                case ReferenceFrame.Maneuver: // TODO
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
            }
            Vector3.OrthoNormalize(ref forward, ref up);
            rotationReference = Quaternion.LookRotation(forward, up);
            switch (mCommand.Attitude) {
                case FlightAttitude.Prograde:
                    break;
                case FlightAttitude.Retrograde:
                    rotationReference = rotationReference * Quaternion.AngleAxis(180, Vector3.up);
                    break;
                case FlightAttitude.NormalPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.up);
                    break;
                case FlightAttitude.NormalMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.down);
                    break;
                case FlightAttitude.RadialPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.right);
                    break;
                case FlightAttitude.RadialMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.left);
                    break;
                case FlightAttitude.Surface:
                    rotationReference = rotationReference * Quaternion.Euler(mCommand.Direction.x, 
                                                                            -mCommand.Direction.y,
                                                                            mCommand.Direction.z + 180);
                    break;
            }
            ProcessOrientation(fs, rotationReference);
            // Leave out the mapping of the vessel's forward to up because of legacy code.
        }

        private void HoldAltitude(FlightCtrlState fs) {

        }

        private void Burn(FlightCtrlState fs) {
            if (!Single.IsNaN(mCommand.Throttle)) {
                if (!Double.IsNaN(mCommand.Duration) && mCommand.Duration > 0) {
                    fs.mainThrottle = mCommand.Throttle;
                    mCommand.Duration -= Time.deltaTime;
                } else if (!Double.IsNaN(mCommand.DeltaV) && mCommand.DeltaV > 0) {
                    fs.mainThrottle = mCommand.Throttle;
                } else {
                    fs.mainThrottle = 0;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<String> GetEnumerator() {
            yield return mCommand.ToString();

            var e1 = mFlightCommandBuffer.GetEnumerator();
            var e2 = mActionGroupBuffer.GetEnumerator();

            bool remaining1 = e1.MoveNext();
            bool remaining2 = e2.MoveNext();

            while (remaining1 || remaining2) {
                if (remaining1 && remaining2) {
                    if (e1.Current.TimeStamp.CompareTo(e2.Current.TimeStamp) > 0) {
                        yield return e2.Current.ToString();
                        remaining2 = e2.MoveNext();
                    } else {
                        yield return e1.Current.ToString();
                        remaining1 = e1.MoveNext();
                    }
                } else if (remaining2) {
                    yield return e2.Current.ToString();
                    remaining2 = e2.MoveNext();
                } else {
                    yield return e1.Current.ToString();
                    remaining1 = e1.MoveNext();
                }
            }
        } 
    }
}