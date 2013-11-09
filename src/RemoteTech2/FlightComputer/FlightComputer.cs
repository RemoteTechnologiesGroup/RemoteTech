using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class FlightComputer : IEnumerable<DelayedCommand>, IDisposable
    {
        public bool InputAllowed
        {
            get
            {
                var satellite = RTCore.Instance.Network[mParent.Guid];
                var connection = RTCore.Instance.Network[satellite];
                return (satellite != null && satellite.HasLocalControl) || (mParent.Powered && connection.Any());
            }
        }

        public double Delay
        {
            get
            {
                var satellite = RTCore.Instance.Network[mParent.Guid];
                if (satellite != null && satellite.HasLocalControl) return 0.0;
                var connection = RTCore.Instance.Network[satellite];
                if (!connection.Any()) return Double.PositiveInfinity;
                return connection.Min().Delay;
            }
        }

        public double TotalDelay { get; set; }

        public List<Action<FlightCtrlState>> SanctionedPilots { get; private set; }

        private ISignalProcessor mParent;
        private Vessel mVessel;

        private DelayedCommand mCurrentCommand = AttitudeCommand.Off();
        private FlightCtrlState mPreviousFcs = new FlightCtrlState();
        private readonly List<DelayedCommand> mCommandBuffer = new List<DelayedCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer = new PriorityQueue<DelayedFlightCtrlState>();

        private double mLastSpeed;
        private Quaternion mKillRot;
        private FlightComputerWindow mWindow;
        public FlightComputerWindow Window { get { if (mWindow != null) mWindow.Hide(); return mWindow = new FlightComputerWindow(this); } }

        public FlightComputer(ISignalProcessor s)
        {
            mParent = s;
            mVessel = s.Vessel;
            mPreviousFcs.CopyFrom(mVessel.ctrlState);
            SanctionedPilots = new List<Action<FlightCtrlState>>();
        }

        public void Dispose()
        {
            RTLog.Notify("FlightComputer: Dispose");
            if (mVessel != null)
            {
                mVessel.OnFlyByWire -= OnFlyByWirePre;
                mVessel.OnFlyByWire -= OnFlyByWirePost;
            }
            if (mWindow != null)
            {
                mWindow.Hide();
            }
        }

        public void Enqueue(DelayedCommand fc)
        {
            if (!InputAllowed) return;
            if (mVessel.packed) return;

            fc.TimeStamp += Delay;
            if (fc.CancelCommand == null && fc.TargetCommand == null && fc.ManeuverCommand == null)
            {
                fc.ExtraDelay += Math.Max(0, TotalDelay - Delay);
            }

            int pos = mCommandBuffer.BinarySearch(fc);
            if (pos < 0)
            {
                mCommandBuffer.Insert(~pos, fc);
            }
        }

        public void OnUpdate()
        {
            if (!mParent.IsMaster) return;
            if (!mParent.Powered) return;
            PopCommand();
        }

        public void OnFixedUpdate()
        {
            // Send updates for Target / Maneuver
            if (FlightGlobals.fetch.VesselTarget != null && (mCurrentCommand.TargetCommand == null || mCurrentCommand.TargetCommand.Target != FlightGlobals.fetch.VesselTarget))
            {
                if (!mCommandBuffer.Any(dc => dc.TargetCommand != null && dc.TargetCommand.Target == FlightGlobals.fetch.VesselTarget))
                {
                    Enqueue(TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget));
                }
            }
            if (mVessel.patchedConicSolver != null)
            {
                if (mVessel.patchedConicSolver.maneuverNodes.Count > 0 && (mCurrentCommand.ManeuverCommand == null || mCurrentCommand.ManeuverCommand.Node != mVessel.patchedConicSolver.maneuverNodes[0]))
                {
                    if (!mCommandBuffer.Any(dc => dc.TargetCommand != null && dc.ManeuverCommand.Node == mVessel.patchedConicSolver.maneuverNodes[0]))
                    {
                        Enqueue(ManeuverCommand.WithNode(mVessel.patchedConicSolver.maneuverNodes[0]));
                    }
                }
            }


            if (mVessel != mParent.Vessel)
            {
                mVessel.VesselSAS.LockHeading(mVessel.transform.rotation, false);
                mCurrentCommand.ManeuverCommand = null;
                mCommandBuffer.RemoveAll(dc => dc.ManeuverCommand != null);
                SanctionedPilots.Clear();
            }

            // Re-attach periodically
            mVessel.OnFlyByWire -= OnFlyByWirePre;
            mVessel.OnFlyByWire -= OnFlyByWirePost;
            mVessel = mParent.Vessel;
            mVessel.OnFlyByWire = OnFlyByWirePre + mVessel.OnFlyByWire + OnFlyByWirePost;
        }

        private void Enqueue(FlightCtrlState fs)
        {
            DelayedFlightCtrlState dfs = new DelayedFlightCtrlState(fs);
            dfs.TimeStamp += Delay;
            mFlightCtrlBuffer.Enqueue(dfs);
        }

        private void PopFlightCtrlState(FlightCtrlState fcs)
        {
            FlightCtrlState delayed = mPreviousFcs;
            mPreviousFcs.Neutralize();
            while (mFlightCtrlBuffer.Count > 0 && mFlightCtrlBuffer.Peek().TimeStamp <= RTUtil.GameTime)
            {
                delayed = mFlightCtrlBuffer.Dequeue().State;
            }

            fcs.CopyFrom(delayed);
        }

        private void PopCommand()
        {
            if (mCommandBuffer.Count > 0)
            {
                for (int i = 0; i < mCommandBuffer.Count && mCommandBuffer[i].TimeStamp <= RTUtil.GameTime; i++)
                {
                    DelayedCommand dc = mCommandBuffer[i];
                    if (dc.ExtraDelay > 0)
                    {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    }
                    else
                    {
                        bool do_not_delete = false;
                        if (dc.ActionGroupCommand != null)
                        {
                            if (mVessel.packed) { do_not_delete = true; return; }
                            KSPActionGroup ag = dc.ActionGroupCommand.ActionGroup;
                            mVessel.ActionGroups.ToggleGroup(ag);
                            if (ag == KSPActionGroup.Stage && !FlightInputHandler.fetch.stageLock)
                            {
                                Staging.ActivateNextStage();
                                ResourceDisplay.Instance.Refresh();
                            }
                            if (ag == KSPActionGroup.RCS)
                            {
                                FlightInputHandler.fetch.rcslock = !FlightInputHandler.RCSLock;
                            }
                        }

                        if (dc.AttitudeCommand != null)
                        {
                            mKillRot = mVessel.transform.rotation;
                            mCurrentCommand.AttitudeCommand = dc.AttitudeCommand;
                            if (dc.AttitudeCommand.Mode == FlightMode.Off)
                            {
                                FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                            }
                        }

                        if (dc.BurnCommand != null)
                        {
                            mLastSpeed = mVessel.obt_velocity.magnitude;
                            mCurrentCommand.BurnCommand = dc.BurnCommand;
                        }

                        if (dc.EventCommand != null)
                        {
                            dc.EventCommand.BaseEvent.Invoke();
                        }

                        if (dc.TargetCommand != null)
                        {
                            mCurrentCommand.TargetCommand = dc.TargetCommand;
                        }

                        if (dc.ManeuverCommand != null)
                        {
                            mCurrentCommand.ManeuverCommand = dc.ManeuverCommand;
                        }

                        if (dc.CancelCommand != null)
                        {
                            mCommandBuffer.Remove(dc.CancelCommand);
                            if (mCurrentCommand == dc.CancelCommand)
                            {
                                mCurrentCommand = AttitudeCommand.Off();
                                FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                            }
                            mCommandBuffer.Remove(dc);
                        }
                        else if (!do_not_delete)
                        {
                            mCommandBuffer.RemoveAt(i);
                        }

                    }
                }
            }
        }

        private void Autopilot(FlightCtrlState fs)
        {
            if (mCurrentCommand.AttitudeCommand != null)
                switch (mCurrentCommand.AttitudeCommand.Mode)
                {
                    case FlightMode.Off:
                        break;
                    case FlightMode.KillRot:
                        HoldOrientation(fs, mKillRot * Quaternion.AngleAxis(90, Vector3.left));
                        break;
                    case FlightMode.AttitudeHold:
                        HoldAttitude(fs);
                        break;
                    case FlightMode.AltitudeHold:
                        break;
                }

            Burn(fs);
        }

        private void Burn(FlightCtrlState fs)
        {
            if (mCurrentCommand.BurnCommand != null)
            {
                if (mCurrentCommand.BurnCommand.Duration > 0)
                {
                    fs.mainThrottle = mCurrentCommand.BurnCommand.Throttle;
                    mCurrentCommand.BurnCommand.Duration -= TimeWarp.deltaTime;
                }
                else if (mCurrentCommand.BurnCommand.DeltaV > 0)
                {
                    fs.mainThrottle = mCurrentCommand.BurnCommand.Throttle;
                    mCurrentCommand.BurnCommand.DeltaV -= Math.Abs(mLastSpeed - mVessel.obt_velocity.magnitude);
                    mLastSpeed = mVessel.obt_velocity.magnitude;
                }
                else
                {
                    fs.mainThrottle = 0.0f;
                    mCurrentCommand.BurnCommand = null;
                }
            }
            else if (!InputAllowed)
            {
                fs.mainThrottle = 0.0f;
            }
        }

        private void HoldOrientation(FlightCtrlState fs, Quaternion target)
        {
            //mVessel.VesselSAS.LockHeading(target * Quaternion.AngleAxis(90, Vector3.right), true);
            kOS.SteeringHelper.SteerShipToward(target, fs, mVessel);
            //FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
        }

        private void HoldAttitude(FlightCtrlState fs)
        {
            Vessel v = mVessel;
            Vector3 forward = Vector3.zero;
            Vector3 up = Vector3.zero;
            Quaternion rotationReference;
            switch (mCurrentCommand.AttitudeCommand.Frame)
            {
                case ReferenceFrame.Orbit:
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
                case ReferenceFrame.Surface:
                    forward = v.GetSrfVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;
                case ReferenceFrame.North:
                    up = (v.mainBody.position - v.CoM);
                    forward = Vector3.Exclude(up,
                        v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius - v.CoM
                     );
                    break;
                case ReferenceFrame.Maneuver:
                    up = mVessel.transform.up;
                    if (mCurrentCommand.ManeuverCommand != null)
                    {
                        forward = mCurrentCommand.ManeuverCommand.Node.GetBurnVector(mVessel.orbit);
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        forward = v.GetObtVelocity();
                        up = (v.mainBody.position - v.CoM);
                    }
                    break;
                case ReferenceFrame.TargetVelocity:
                    if (mCurrentCommand.TargetCommand != null && mCurrentCommand.TargetCommand.Target is Vessel)
                    {
                        forward = v.GetObtVelocity() - mCurrentCommand.TargetCommand.Target.GetObtVelocity();
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        up = (v.mainBody.position - v.CoM);
                        forward = v.GetObtVelocity();
                    }
                    break;
                case ReferenceFrame.TargetParallel:
                    if (mCurrentCommand.TargetCommand != null)
                    {
                        forward = mCurrentCommand.TargetCommand.Target.GetTransform().position - v.CoM;
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        up = (v.mainBody.position - v.CoM);
                        forward = v.GetObtVelocity();
                    }
                    break;
            }
            Vector3.OrthoNormalize(ref forward, ref up);
            rotationReference = Quaternion.LookRotation(forward, up);
            switch (mCurrentCommand.AttitudeCommand.Attitude)
            {
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
                    rotationReference = rotationReference * mCurrentCommand.AttitudeCommand.Orientation;
                    break;
            }
            HoldOrientation(fs, rotationReference);
        }

        private void OnFlyByWirePre(FlightCtrlState fcs)
        {
            var satellite = RTCore.Instance.Satellites[mParent.Guid];
            if (!mParent.IsMaster) return;

            if (mVessel == FlightGlobals.ActiveVessel && InputAllowed && !satellite.HasLocalControl)
            {
                Enqueue(fcs);
            }

            if (!satellite.HasLocalControl)
            {
                PopFlightCtrlState(fcs);
            }

        }

        private void OnFlyByWirePost(FlightCtrlState fcs)
        {
            if (!mParent.IsMaster) return;

            if (!InputAllowed)
            {
                fcs.Neutralize();
            }

            mPreviousFcs.CopyFrom(fcs);

            Autopilot(fcs);

            foreach (var pilot in SanctionedPilots)
            {
                pilot.Invoke(fcs);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<DelayedCommand> GetEnumerator()
        {
            yield return mCurrentCommand;
            foreach (DelayedCommand dc in mCommandBuffer)
            {
                yield return dc;
            }
        }
    }
}
