using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public partial class FlightComputer : IDisposable, IEnumerable<DelayedCommand> {
        public BifrostUnit Bifrost { get; private set; }

        public double ExtraDelay { get; set; }

        public bool InputAllowed {
            get {
                return mSignalProcessor.Powered && 
                       mSignalProcessor.Satellite != null &&
                      (mSignalProcessor.Satellite.Connection.Exists ||
                       mSignalProcessor.Satellite.LocalControl);
            }
        }

        public double Delay {
            get {
                if (mSignalProcessor.Satellite.LocalControl) {
                    return 0.0f;
                } else {
                    return mSignalProcessor.Satellite.Connection.Delay;
                }
            }
        }

        private DelayedCommand mCommand = AttitudeCommand.Off();
        private Vector3 mManeuver;
        private Quaternion mKillrot;
        private double mLastSpeed;
        private FlightCtrlState mPreviousCtrl = new FlightCtrlState();

        private readonly Legacy.FlightComputer mLegacyComputer;
        private readonly List<DelayedCommand> mCommandBuffer
            = new List<DelayedCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer
            = new PriorityQueue<DelayedFlightCtrlState>();

        private readonly ISignalProcessor mSignalProcessor;
        private Vessel mAttachedVessel;

        public FlightComputer(ISignalProcessor s) {
            mSignalProcessor = s;
            mPreviousCtrl.CopyFrom(s.Vessel.ctrlState);
            mAttachedVessel = s.Vessel;

            mLegacyComputer = new Legacy.FlightComputer();
            Bifrost = new BifrostUnit(s);
        }

        public void Dispose() {
            if (mAttachedVessel != null) {
                mAttachedVessel.OnFlyByWire -= OnFlyByWirePre;
            }
        }

        public void Enqueue(DelayedCommand fc) {
            fc.TimeStamp += Delay;
            fc.ExtraDelay += ExtraDelay;
            int pos = mCommandBuffer.BinarySearch(fc);
            if (pos < 0) {
                mCommandBuffer.Insert(~pos, fc);
            }
        }

        private void Enqueue(FlightCtrlState fs) {
            DelayedFlightCtrlState dfs = new DelayedFlightCtrlState(fs);
            dfs.TimeStamp += Delay;
            mFlightCtrlBuffer.Enqueue(dfs);
        }

        private void OnFlyByWirePre(FlightCtrlState fs) {
            if (mSignalProcessor.Master) {
                if (mAttachedVessel == FlightGlobals.ActiveVessel && InputAllowed) {
                    Enqueue(fs);
                }

                PopFlightCtrlState(fs);

                if (mSignalProcessor.Powered) {
                    Autopilot(fs);
                }

                mPreviousCtrl.CopyFrom(fs); 
            }
        }

        public void OnFixedUpdate() {
            // Ensure the onflybywire is still on the correct vessel, in the correct order
            if (mSignalProcessor.Vessel != null) {
                mAttachedVessel.OnFlyByWire -= OnFlyByWirePre;
                mAttachedVessel = mSignalProcessor.Vessel;
                mAttachedVessel.OnFlyByWire = OnFlyByWirePre + mAttachedVessel.OnFlyByWire;
            }

            if (mSignalProcessor.Powered && mSignalProcessor.Master) {
                PopCommand();
                if (Bifrost != null) {
                    Bifrost.OnFixedUpdate();
                }
            }
        }

        private void Autopilot(FlightCtrlState fs) {
            switch (mCommand.AttitudeCommand.Mode) {
                case FlightMode.Off:
                    break;
                case FlightMode.KillRot:
                    HoldOrientation(fs, mKillrot);
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

        private void PopFlightCtrlState(FlightCtrlState fcs) {
            FlightCtrlState delayed = mPreviousCtrl;
            // Pop any due flightctrlstates off the queue
            while (mFlightCtrlBuffer.Count > 0 &&
                   mFlightCtrlBuffer.Peek().TimeStamp < RTUtil.GetGameTime()) {
                delayed = mFlightCtrlBuffer.Dequeue().State;
            }
            if (!InputAllowed) {
                float keepThrottle = mPreviousCtrl.mainThrottle;
                delayed.Neutralize();
                delayed.mainThrottle = keepThrottle;
            }
            fcs.CopyFrom(delayed);
        }

        private void PopCommand() {
            if (mCommandBuffer.Count > 0) {
                for (int i = 0; i < mCommandBuffer.Count &&
                                        mCommandBuffer[i].TimeStamp < RTUtil.GetGameTime(); i++) {
                    DelayedCommand dc = mCommandBuffer[i];
                    if (dc.ExtraDelay > 0) {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    } else {
                        if (dc.ActionGroupCommand != null) {
                            KSPActionGroup ag = dc.ActionGroupCommand.ActionGroup;
                            mAttachedVessel.ActionGroups.ToggleGroup(ag);
                            if (ag == KSPActionGroup.Stage && !FlightInputHandler.fetch.stageLock) {
                                Staging.ActivateNextStage();
                                ResourceDisplay.Instance.Refresh();
                            }
                            if (ag == KSPActionGroup.RCS) {
                                FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
                            }
                        }

                        if (dc.AttitudeCommand != null) {
                            mKillrot = mAttachedVessel.transform.rotation *
                                Quaternion.AngleAxis(90, Vector3.left);
                            mCommand = dc;
                        }

                        if (dc.BurnCommand != null) {
                            mLastSpeed = mAttachedVessel.obt_velocity.magnitude;
                            mCommand.BurnCommand = dc.BurnCommand;
                        }

                        if (dc.Event != null) {
                            dc.Event.BaseEvent.Invoke();
                        }

                        mCommandBuffer.RemoveAt(i);
                    }
                }
            }
        }

        private void HoldOrientation(FlightCtrlState fs, Quaternion target) {
            mLegacyComputer.HoldOrientation(fs, mAttachedVessel, target, true);
        }

        private void HoldAttitude(FlightCtrlState fs) {
            Vessel v = mAttachedVessel;
            Vector3 forward = Vector3.zero;
            Vector3 up = Vector3.zero;
            Quaternion rotationReference;
            switch (mCommand.AttitudeCommand.Frame) {
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
            switch (mCommand.AttitudeCommand.Attitude) {
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
                    rotationReference = rotationReference * mCommand.AttitudeCommand.Orientation;
                    break;
            }
            HoldOrientation(fs, rotationReference);
            // Leave out the mapping of the vessel's forward to up because of legacy code.
        }

        private void HoldAltitude(FlightCtrlState fs) {
            const double damping = 1000.0f;
            Vessel v = mAttachedVessel;
            double target_height = mCommand.AttitudeCommand.Altitude;
            float target_pitch = (float) (Math.Atan2(target_height - v.orbit.ApA, damping) / Math.PI * 180.0f);
            Vector3 up = (v.mainBody.position - v.CoM);
            Vector3 forward = Vector3.Exclude(up,
                v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius - v.CoM
            );
            Vector3.OrthoNormalize(ref forward, ref up);
            Vector3 direction = Vector3.Exclude(up, v.obt_velocity).normalized;
            float target_heading = Vector3.Angle(forward, direction);

            Quaternion rotationReference = Quaternion.LookRotation(forward, up);
            RTUtil.Log("Pitch: {0}, Heading: {1}", target_pitch, target_heading);
            HoldOrientation(fs, rotationReference * 
                Quaternion.Euler(new Vector3(target_pitch, target_heading, 0)));
        }

        private void Burn(FlightCtrlState fs) {
            if (mCommand.BurnCommand == null)
                return;
            if (!Single.IsNaN(mCommand.BurnCommand.Throttle)) {
                if (mCommand.BurnCommand.Duration > 0) {
                    fs.mainThrottle = mCommand.BurnCommand.Throttle;
                    mCommand.BurnCommand.Duration -= TimeWarp.deltaTime;
                } else if (mCommand.BurnCommand.DeltaV > 0) {
                    fs.mainThrottle = mCommand.BurnCommand.Throttle;
                    mCommand.BurnCommand.DeltaV -=
                        Math.Abs(mLastSpeed - mAttachedVessel.obt_velocity.magnitude);
                    mLastSpeed = mAttachedVessel.obt_velocity.magnitude;
                } else {
                    mCommand.BurnCommand = null;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<DelayedCommand> GetEnumerator() {
            yield return mCommand;
            foreach (DelayedCommand dc in mCommandBuffer) {
                yield return dc;
            }
        } 
    }
}