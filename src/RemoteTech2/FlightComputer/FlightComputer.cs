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
                return (satellite != null &&satellite.HasLocalControl) || (mParent.Powered && connection.Any());
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

        public double ExtraDelay { get; set; }

        private ISignalProcessor mParent;
        private Vessel mVessel;

        private DelayedCommand mCurrentCommand;
        private FlightCtrlState mPreviousFcs = new FlightCtrlState();
        private readonly List<DelayedCommand> mCommandBuffer = new List<DelayedCommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlBuffer = new PriorityQueue<DelayedFlightCtrlState>();

        public FlightComputer(ISignalProcessor s)
        {
            mParent = s;
            mVessel = s.Vessel;
            mPreviousFcs.CopyFrom(mVessel.ctrlState);
        }

        public void Dispose()
        {
            if (mVessel != null)
            {
                mVessel.OnFlyByWire -= OnFlyByWirePre;
            }
        }

        public void Enqueue(DelayedCommand fc)
        {
            if (!InputAllowed) return;
            if (mVessel.packed) return;
            fc.TimeStamp += Delay;
            if (fc.CancelCommand == null)
            {
                fc.ExtraDelay += ExtraDelay;
            }

            int pos = mCommandBuffer.BinarySearch(fc);
            if (pos < 0)
            {
                mCommandBuffer.Insert(~pos, fc);
            }
        }

        public void OnUpdate()
        {

        }

        public void OnFixedUpdate()
        {
            mVessel.OnFlyByWire -= OnFlyByWirePre;
            mVessel.OnFlyByWire -= OnFlyByWirePost;
            mVessel = mParent.Vessel;
            mVessel.OnFlyByWire = OnFlyByWirePre + mVessel.OnFlyByWire + OnFlyByWirePost;

            var satellite = RTCore.Instance.Satellites[mParent.Guid];
            if (satellite == null || satellite.SignalProcessor != mParent) return;
            if (!mParent.Powered) return;
            if (mVessel.packed) return;
            PopCommand();
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
            while (mFlightCtrlBuffer.Count > 0 && mFlightCtrlBuffer.Peek().TimeStamp < RTUtil.GameTime)
            {
                delayed = mFlightCtrlBuffer.Dequeue().State;
            }

            fcs.CopyFrom(delayed);
        }

        private void PopCommand()
        {
            if (mCommandBuffer.Count > 0)
            {
                for (int i = 0; i < mCommandBuffer.Count && mCommandBuffer[i].TimeStamp < RTUtil.GameTime; i++)
                {
                    DelayedCommand dc = mCommandBuffer[i];
                    if (dc.ExtraDelay > 0)
                    {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    }
                    else
                    {
                        if (dc.ActionGroupCommand != null)
                        {
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

                        if (dc.EventCommand != null)
                        {
                            dc.EventCommand.BaseEvent.Invoke();
                        }

                        if (dc.CancelCommand != null)
                        {
                            mCommandBuffer.Remove(dc.CancelCommand);
                            if (mCurrentCommand == dc.CancelCommand)
                            {
                                mCurrentCommand = null;
                            }
                            mCommandBuffer.Remove(dc);
                        }
                        else
                        {
                            mCommandBuffer.RemoveAt(i);
                        }

                    }
                }
            }
        }

        private void OnFlyByWirePre(FlightCtrlState fcs)
        {
            var satellite = RTCore.Instance.Satellites[mParent.Guid];
            if (satellite == null || satellite.SignalProcessor != mParent) return;

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
            var satellite = RTCore.Instance.Satellites[mParent.Guid];
            if (satellite == null || satellite.SignalProcessor != mParent) return;

            if (!InputAllowed)
            {
                fcs.Neutralize();
            }

            mPreviousFcs.CopyFrom(fcs);
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
