using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public partial class FlightComputer : IDisposable, IEnumerable<DelayedCommand> {
#if PROGCOM
        public ProgcomUnit Progcom { get; private set; }
#endif
        public double ExtraDelay { get; set; }

        public bool InputAllowed {
            get {
                return mSatellite.Connection.Exists || mSatellite.LocalControl;
            }
        }

        public double Delay {
            get {
                if (mSatellite.LocalControl) {
                    return 0.0f;
                } else {
                    return mSatellite.Connection.Delay;
                }
            }
        }

        private DelayedCommand mAttitude = AttitudeCommand.Off();
        private DelayedCommand mBurn = BurnCommand.Off();
        private Vector3 mManeuver;
        private Quaternion mKillrot;
        private double mLastSpeed;
        private FlightCtrlState mPreviousCtrl = new FlightCtrlState();

        private readonly Legacy.FlightComputer mLegacyComputer;
        private readonly VesselSatellite mSatellite;
        private Vessel mAttachedVessel;
        private readonly List<DelayedCommand> mCommandBuffer
            = new List<DelayedCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer
            = new PriorityQueue<DelayedFlightCtrlState>();

        public FlightComputer(VesselSatellite vs) {
            mSatellite = vs;
            mPreviousCtrl.CopyFrom(vs.Vessel.ctrlState);
            mLegacyComputer = new Legacy.FlightComputer(vs.Vessel);
            mAttachedVessel = vs.Vessel;
            mAttachedVessel.OnFlyByWire = this.OnFlyByWirePre + mAttachedVessel.OnFlyByWire;
#if PROGCOM
            Progcom = new ProgcomUnit(vs);
#endif
        }

        public void Dispose() {
            if (mAttachedVessel != null) {
                mAttachedVessel.OnFlyByWire -= OnFlyByWirePre;
                mAttachedVessel.OnFlyByWire -= OnFlyByWirePost;
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
            // Ensure the onflybywire is still on the correct vessel, in the correct order
            mAttachedVessel.OnFlyByWire -= OnFlyByWirePost;
            mAttachedVessel.OnFlyByWire -= OnFlyByWirePre;
            mAttachedVessel = mSatellite.Vessel;
            mAttachedVessel.OnFlyByWire = OnFlyByWirePre + mAttachedVessel.OnFlyByWire;
            mAttachedVessel.OnFlyByWire += OnFlyByWirePost;

            if (mSatellite.Vessel == FlightGlobals.ActiveVessel && InputAllowed) {
                Enqueue(fs);
            }

            PopBuffers(fs);
            Autopilot(fs);

#if PROGCOM
            Progcom.OnFlyByWire(fs);
#endif
        }

        private void OnFlyByWirePost(FlightCtrlState fs) {
            mPreviousCtrl.CopyFrom(fs);
        }

        private void Autopilot(FlightCtrlState fs) {
            switch (mAttitude.AttitudeCommand.Mode) {
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

        private void PopBuffers(FlightCtrlState fcs) {
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

            // Pop any due commands
            if (mCommandBuffer.Count > 0) {
                for (int i = 0; i < mCommandBuffer.Count && 
                                        mCommandBuffer[i].TimeStamp < RTUtil.GetGameTime(); i++) {
                    DelayedCommand dc = mCommandBuffer[i];
                    if (dc.ExtraDelay > 0) {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    } else {
                        if (dc.ActionGroupCommand != null) {
                            KSPActionGroup ag = dc.ActionGroupCommand.ActionGroup;
                            mSatellite.Vessel.ActionGroups.ToggleGroup(ag);
                            if (ag == KSPActionGroup.Stage && !FlightInputHandler.fetch.stageLock) {
                                Staging.ActivateNextStage();
                                ResourceDisplay.Instance.Refresh();
                            }
                            if (ag == KSPActionGroup.RCS) {
                                FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
                            } 
                        }

                        if (dc.AttitudeCommand != null) {
                            mKillrot = mSatellite.Vessel.transform.rotation * 
                                Quaternion.AngleAxis(90, Vector3.left);
                            mAttitude = dc;
                        }

                        if (dc.BurnCommand != null) {
                            mLastSpeed = mSatellite.Vessel.obt_velocity.magnitude;
                            mBurn = dc; 
                        }

                        mCommandBuffer.RemoveAt(i);
                    }
                }
            }
        }

        private void HoldOrientation(FlightCtrlState fs, Quaternion target) {
            mLegacyComputer.drive(fs, target);
        }

        private void HoldAttitude(FlightCtrlState fs) {
            Vessel v = mSatellite.Vessel;
            Vector3 forward = Vector3.zero;
            Vector3 up = Vector3.zero;
            Quaternion rotationReference;
            switch (mAttitude.AttitudeCommand.Frame) {
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
            switch (mAttitude.AttitudeCommand.Attitude) {
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
                    rotationReference = rotationReference * mAttitude.AttitudeCommand.Orientation;
                    break;
            }
            HoldOrientation(fs, rotationReference);
            // Leave out the mapping of the vessel's forward to up because of legacy code.
        }

        private void HoldAltitude(FlightCtrlState fs) {

        }

        private void Burn(FlightCtrlState fs) {
            if (!Single.IsNaN(mBurn.BurnCommand.Throttle)) {
                if (mBurn.BurnCommand.Duration > 0) {
                    fs.mainThrottle = mBurn.BurnCommand.Throttle;
                    mBurn.BurnCommand.Duration -= TimeWarp.deltaTime;
                } else if (mBurn.BurnCommand.DeltaV > 0) {
                    fs.mainThrottle = mBurn.BurnCommand.Throttle;
                    mBurn.BurnCommand.DeltaV -= 
                        Math.Abs(mLastSpeed - mSatellite.Vessel.obt_velocity.magnitude);
                    mLastSpeed = mSatellite.Vessel.obt_velocity.magnitude;
                } else {
                    mBurn.BurnCommand.Throttle = Single.NaN;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<DelayedCommand> GetEnumerator() {
            yield return mAttitude;
            yield return mBurn;
            foreach (DelayedCommand dc in mCommandBuffer) {
                yield return dc;
            }
        } 
    }
}