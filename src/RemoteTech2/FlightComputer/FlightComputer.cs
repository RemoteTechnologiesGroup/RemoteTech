using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class FlightComputer : IDisposable
    {
        public enum State
        {
            Normal = 0,
            Packed = 2,
            OutOfPower = 4,
            NoConnection = 8,
            NotMaster = 16,
        }

        public bool InputAllowed
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.Guid];
                var connection = RTCore.Instance.Network[satellite];
                return (satellite != null && satellite.HasLocalControl) || (SignalProcessor.Powered && connection.Any());
            }
        }

        public double Delay
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.Guid];
                if (satellite != null && satellite.HasLocalControl) return 0.0;
                var connections = RTCore.Instance.Network[satellite];
                if (!connections.Any()) return Double.PositiveInfinity;
                return connections.ShortestDelay().SignalDelay;
            }
        }

        public State Status
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.Guid];
                var connection = RTCore.Instance.Network[satellite];
                var status = State.Normal;
                if (!SignalProcessor.Powered) status |= State.OutOfPower;
                if (!IsMaster) status |= State.NotMaster;
                if (!connection.Any()) status |= State.NoConnection;
                if (Vessel.packed) status |= State.Packed;
                return status;
            }
        }

        public double TotalDelay { get; set; }
        public ManeuverNode DelayedManeuver { get; set; }
        public ITargetable DelayedTarget { get; set; }
        public Vessel Vessel { get; private set; }
        public ISignalProcessor SignalProcessor { get; private set; }
        public List<Action<FlightCtrlState>> SanctionedPilots { get; private set; }
        public IEnumerable<ICommand> ActiveCommands { get { return mActiveCommands.Values; } }
        public IEnumerable<ICommand> QueuedCommands { get { return mCommandQueue; } }

        private readonly SortedDictionary<int, ICommand> mActiveCommands = new SortedDictionary<int, ICommand>();
        private readonly List<ICommand> mCommandQueue = new List<ICommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlQueue = new PriorityQueue<DelayedFlightCtrlState>();

        // Oh .NET, why don't you have deque's?
        private readonly LinkedList<DelayedManeuver> mManeuverQueue = new LinkedList<DelayedManeuver>();

        private FlightComputerWindow mWindow;
        public FlightComputerWindow Window { get { if (mWindow != null) mWindow.Hide(); return mWindow = new FlightComputerWindow(this); } }

        private bool IsMaster { get { return SignalProcessor == RTCore.Instance.Satellites[SignalProcessor.Guid].SignalProcessor; } }

        public FlightComputer(ISignalProcessor s)
        {
            SignalProcessor = s;
            Vessel = s.Vessel;
            SanctionedPilots = new List<Action<FlightCtrlState>>();

            var target = TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget);
            mActiveCommands[target.Priority] = target;
            var attitude = AttitudeCommand.Off();
            mActiveCommands[attitude.Priority] = attitude;
        }

        public void Dispose()
        {
            RTLog.Notify("FlightComputer: Dispose");
            if (Vessel != null)
            {
                Vessel.OnFlyByWire -= OnFlyByWirePre;
                Vessel.OnFlyByWire -= OnFlyByWirePost;
            }
            if (mWindow != null)
            {
                mWindow.Hide();
            }
        }

        public void Reset()
        {
            foreach (var cmd in mActiveCommands.Values)
            {
                cmd.Abort();
            }
        }

        public void Enqueue(ICommand cmd, bool ignore_control = false, bool ignore_delay = false, bool ignore_extra = false)
        {
            if (!InputAllowed && !ignore_control) return;

            if (!ignore_delay) cmd.TimeStamp += Delay;
            if (!ignore_extra) cmd.ExtraDelay += Math.Max(0, TotalDelay - Delay);

            int pos = mCommandQueue.BinarySearch(cmd);
            if (pos < 0)
            {
                mCommandQueue.Insert(~pos, cmd);
            }
        }

        public void Remove(ICommand cmd)
        {
            mCommandQueue.Remove(cmd);
            if (mActiveCommands.ContainsValue(cmd)) mActiveCommands.Remove(cmd.Priority);
        }

        public void OnUpdate()
        {
            if (!IsMaster) return;
            PopCommand();
        }

        public void OnFixedUpdate()
        {
            // Re-attach periodically
            Vessel.OnFlyByWire -= OnFlyByWirePre;
            Vessel.OnFlyByWire -= OnFlyByWirePost;
            if (Vessel != SignalProcessor.Vessel)
            {
                SanctionedPilots.Clear();
                Vessel = SignalProcessor.Vessel;
            }
            Vessel.OnFlyByWire = OnFlyByWirePre + Vessel.OnFlyByWire + OnFlyByWirePost;

            // Send updates for Target / Maneuver
            TargetCommand last = null;
            if (FlightGlobals.fetch.VesselTarget != DelayedTarget &&
                ((mCommandQueue.FindLastIndex(c => (last = c as TargetCommand) != null)) == -1 || last.Target != FlightGlobals.fetch.VesselTarget))
            {
                Enqueue(TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget));
            }

            if (Vessel.patchedConicSolver != null && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                if ((DelayedManeuver == null || (Vessel.patchedConicSolver.maneuverNodes[0].DeltaV != DelayedManeuver.DeltaV)) &&
                    (mManeuverQueue.Count == 0 || mManeuverQueue.Last.Value.Node.DeltaV != Vessel.patchedConicSolver.maneuverNodes[0].DeltaV))
                {
                    mManeuverQueue.AddLast(new DelayedManeuver(Vessel.patchedConicSolver.maneuverNodes[0]));
                }
            }

        }

        private void Enqueue(FlightCtrlState fs)
        {
            DelayedFlightCtrlState dfs = new DelayedFlightCtrlState(fs);
            dfs.TimeStamp += Delay;
            mFlightCtrlQueue.Enqueue(dfs);
        }

        private void PopFlightCtrl(FlightCtrlState fcs, ISatellite sat)
        {
            FlightCtrlState delayed = new FlightCtrlState();
            while (mFlightCtrlQueue.Count > 0 && mFlightCtrlQueue.Peek().TimeStamp <= RTUtil.GameTime)
            {
                delayed = mFlightCtrlQueue.Dequeue().State;
            }

            fcs.CopyFrom(delayed);
        }

        private void PopCommand()
        {
            // Maneuvers
            while (mManeuverQueue.Count > 0 && mManeuverQueue.First.Value.TimeStamp <= RTUtil.GameTime)
            {
                DelayedManeuver = mManeuverQueue.First.Value.Node;
                mManeuverQueue.RemoveFirst();
            }

            // Commands
            if (SignalProcessor.Powered && mCommandQueue.Count > 0)
            {
                if (RTSettings.Instance.ThrottleTimeWarp && TimeWarp.CurrentRate > 1.0f)
                {
                    var time = TimeWarp.deltaTime;
                    foreach (var dc in mCommandQueue.TakeWhile(c => c.TimeStamp <= RTUtil.GameTime + (2 * time + 1.0)))
                    {
                        var message = new ScreenMessage("[Flight Computer]: Throttling back time warp...", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                        while ((2 * TimeWarp.deltaTime + 1.0) > (Math.Max(dc.TimeStamp - RTUtil.GameTime, 0) + dc.ExtraDelay) && TimeWarp.CurrentRate > 1.0f)
                        {
                            TimeWarp.SetRate(TimeWarp.CurrentRateIndex - 1, true);
                            ScreenMessages.PostScreenMessage(message, true);
                        }
                    }
                }

                foreach (var dc in mCommandQueue.TakeWhile(c => c.TimeStamp <= RTUtil.GameTime).ToList())
                {
                    Debug.Log(dc.Description);
                    if (dc.ExtraDelay > 0)
                    {
                        dc.ExtraDelay -= SignalProcessor.Powered ? TimeWarp.deltaTime : 0.0;
                    }
                    else
                    {
                        if (dc.Pop(this)) mActiveCommands[dc.Priority] = dc;
                        mCommandQueue.Remove(dc);
                    }
                }
            }
        }

        private void OnFlyByWirePre(FlightCtrlState fcs)
        {
            if (!IsMaster) return;
            var satellite = RTCore.Instance.Satellites[SignalProcessor.Guid];

            if (Vessel == FlightGlobals.ActiveVessel && InputAllowed && !satellite.HasLocalControl)
            {
                Enqueue(fcs);
            }

            if (!satellite.HasLocalControl)
            {
                PopFlightCtrl(fcs, satellite);
            }
        }

        private void OnFlyByWirePost(FlightCtrlState fcs)
        {
            if (!IsMaster) return;

            if (!InputAllowed)
            {
                fcs.Neutralize();
            }

            if (SignalProcessor.Powered)
            {
                foreach (var dc in mActiveCommands.Values.ToList())
                {
                    if (dc.Execute(this, fcs)) mActiveCommands.Remove(dc.Priority);
                }
            }

            foreach (var pilot in SanctionedPilots)
            {
                pilot.Invoke(fcs);
            }
        }
    }
}
