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
        private readonly List<FlightCommand> mCommandBuffer =
            new List<FlightCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer =
            new PriorityQueue<DelayedFlightCtrlState>();
        private readonly List<DelayedActionGroup> mActionGroupBuffer =
            new List<DelayedActionGroup>();
        
        public double ExtraDelay { get; set; }

        public FlightComputer(VesselSatellite vs) {
            mSatellite = vs;
            mPreviousCtrl.CopyFrom(vs.Vessel.ctrlState);
            mLegacyComputer = new Legacy.FlightComputer(vs.Vessel);
            vs.Vessel.OnFlyByWire += OnFlyByWire;
        }

        public void Dispose() {
            if (mSatellite.Vessel != null) {
                mSatellite.Vessel.OnFlyByWire -= OnFlyByWire;
            }
        }

        public void Enqueue(FlightCommand fc) {
            fc.EffectiveFrom += ExtraDelay;
            mCommandBuffer.Insert(~mCommandBuffer.BinarySearch(fc), fc);
        }

        public void Enqueue(DelayedFlightCtrlState fcs) {
            fcs.EffectiveFrom += ExtraDelay;
            mFlightCtrlBuffer.Enqueue(fcs);
        }

        public void Enqueue(DelayedActionGroup ag) {
            ag.EffectiveFrom += ExtraDelay;

            // For some reason the system sometimes catches the key twice?
            if (mActionGroupBuffer.Count > 0 &&
                ag.ActionGroup == mActionGroupBuffer[mActionGroupBuffer.Count - 1].ActionGroup &&
                ag.EffectiveFrom - mActionGroupBuffer[mActionGroupBuffer.Count - 1].EffectiveFrom
                    < 0.1)
                return;

            mActionGroupBuffer.Insert(~mActionGroupBuffer.BinarySearch(ag), ag);
        }

        private void OnFlyByWire(FlightCtrlState fs) {
            ProcessInputBuffer(fs);
            ProcessFlightCommand(fs);
        }

        private void ProcessFlightCommand(FlightCtrlState fs) {
            while (mCommandBuffer.Count > 0 &&
                mCommandBuffer[0].EffectiveFrom < RTUtil.GetGameTime()) {
                if (mSatellite.Connection.Exists) {
                    mCommand = mCommandBuffer[0];
                    if (mCommand.Mode == FlightMode.KillRot) {
                        mKillrot = mSatellite.Vessel.transform.rotation;
                    }
                }
                mCommandBuffer.RemoveAt(0);
            }
            if (mCommand.Mode == FlightMode.Off)
                return;
            Quaternion direction = Quaternion.identity;
            switch (mCommand.Mode) {
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

        private void ProcessInputBuffer(FlightCtrlState fcs) {
            // FlightCtrlState
            FlightCtrlState delayed = mPreviousCtrl;
            while (mFlightCtrlBuffer.Count > 0 &&
                    mFlightCtrlBuffer.Peek().EffectiveFrom < RTUtil.GetGameTime()) {
                delayed = mFlightCtrlBuffer.Dequeue().State;
            }
            if (!mSatellite.Connection.Exists) {
                float keepThrottle = delayed.mainThrottle;
                delayed.Neutralize();
                delayed.mainThrottle = keepThrottle;
            } else {
                mPreviousCtrl.CopyFrom(delayed);
            }
            fcs.CopyFrom(delayed);
            // Action groups
            while (mActionGroupBuffer.Count > 0 && 
                    mActionGroupBuffer[0].EffectiveFrom < RTUtil.GetGameTime()) {
                if (mSatellite.Connection.Exists) {
                    KSPActionGroup delayedGroup = mActionGroupBuffer[0].ActionGroup;
                    mSatellite.Vessel.ActionGroups.ToggleGroup(delayedGroup);
                    if (delayedGroup == KSPActionGroup.Stage &&
                            !FlightInputHandler.fetch.stageLock) {
                        Staging.ActivateNextStage();
                        ResourceDisplay.Instance.Refresh();
                    }
                    if (delayedGroup == KSPActionGroup.RCS) {
                        FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
                    }
                }
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
                if (!Double.IsNaN(mCommand.Duration) && mCommand.Duration > Double.Epsilon) {
                    fs.mainThrottle = mCommand.Throttle;
                    mCommand.Duration -= Time.deltaTime;
                } else if (!Double.IsNaN(mCommand.DeltaV) && mCommand.DeltaV > Double.Epsilon) {
                    fs.mainThrottle = mCommand.Throttle;
                } else {
                    fs.mainThrottle = 0;
                    mCommand.Throttle = Single.NaN;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<String> GetEnumerator() {
            yield return mCommand.ToString();

            var e1 = mCommandBuffer.GetEnumerator();
            var e2 = mActionGroupBuffer.GetEnumerator();

            bool remaining1 = e1.MoveNext();
            bool remaining2 = e2.MoveNext();

            while (remaining1 || remaining2) {
                if (remaining1 && remaining2) {
                    if (e1.Current.EffectiveFrom.CompareTo(e2.Current.EffectiveFrom) > 0) {
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