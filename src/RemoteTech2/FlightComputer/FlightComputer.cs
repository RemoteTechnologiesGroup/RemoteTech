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
                var connection = RTCore.Instance.Network[satellite];
                if (!connection.Any()) return Double.PositiveInfinity;
                return connection.Min().Delay;
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
                if (!SignalProcessor.IsMaster) status |= State.NotMaster;
                if (!connection.Any()) status |= State.NoConnection;
                if (Vessel.packed) status |= State.Packed;
                return status;
            }
        }

        public double TotalDelay { get; set; }
        public ManeuverNode DelayedManeuver { get; set; }
        public ITargetable DelayedTarget { get; set; }
        public Vessel Vessel { get; private set; }
        public RoverComputer mRoverComputer { get; private set; }
        public ISignalProcessor SignalProcessor { get; private set; }
        public List<Action<FlightCtrlState>> SanctionedPilots { get; private set; }
        public IEnumerable<ICommand> ActiveCommands { get { return mActiveCommands.Values; } }
        public IEnumerable<ICommand> QueuedCommands { get { return mCommandQueue; } }

        // Flight controller parameters from MechJeb, copied from master on June 27, 2014
        public PIDControllerV2 pid { get; private set; }
        public Vector3d lastAct { get; set; }
        public double Tf = 0.3;
        public double TfMin = 0.1;
        public double TfMax = 0.5;
        public double kpFactor = 3;
        public double kiFactor = 6;
        public double kdFactor = 0.5;

        private readonly SortedDictionary<int, ICommand> mActiveCommands = new SortedDictionary<int, ICommand>();
        private readonly List<ICommand> mCommandQueue = new List<ICommand>();
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlQueue = new PriorityQueue<DelayedFlightCtrlState>();

        // Oh .NET, why don't you have deque's?
        private readonly LinkedList<DelayedManeuver> mManeuverQueue = new LinkedList<DelayedManeuver>();

        private FlightComputerWindow mWindow;
        public FlightComputerWindow Window { get { if (mWindow != null) mWindow.Hide(); return mWindow = new FlightComputerWindow(this); } }

        public FlightComputer(ISignalProcessor s)
        {
            SignalProcessor = s;
            Vessel = s.Vessel;
            SanctionedPilots = new List<Action<FlightCtrlState>>();
            pid = new PIDControllerV2(0, 0, 0, 1, -1);
            initPIDParameters();
            lastAct = Vector3d.zero;

            var target = TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget);
            mActiveCommands[target.Priority] = target;
            var attitude = AttitudeCommand.Off();
            mActiveCommands[attitude.Priority] = attitude;

            mRoverComputer = new RoverComputer(Vessel);
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
            if (!SignalProcessor.IsMaster) return;
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

            // Update proportional controller for changes in ship state
            updatePIDParameters();

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
            if (mCommandQueue.Count > 0)
            {
                // Can come out of time warp even if ship unpowered; workaround for KSP 0.24 power consumption bug
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
                    // Use time decrement instead of comparing scheduled time, in case we later want to 
                    //      reinstate event clocks stopping under certain conditions
                    if (dc.ExtraDelay > 0)
                    {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    }
                    else
                    {
                        if (SignalProcessor.Powered) {
                            // Note: depending on implementation, dc.Pop() may execute the event
                            if (dc.Pop(this)) mActiveCommands [dc.Priority] = dc;
                        } else {
                            string message = String.Format ("[Flight Computer]: Out of power, cannot run \"{0}\" on schedule.", dc.ShortName);
                            ScreenMessages.PostScreenMessage(new ScreenMessage(
                                message, 4.0f, ScreenMessageStyle.UPPER_LEFT
                            ), true);
                        }
                        mCommandQueue.Remove(dc);
                    }
                }
            }
        }

        private void OnFlyByWirePre(FlightCtrlState fcs)
        {
            if (!SignalProcessor.IsMaster) return;
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
            if (!SignalProcessor.IsMaster) return;

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

        public void initPIDParameters()
        {
            pid.Kd = kdFactor / Tf;
            pid.Kp = pid.Kd / (kpFactor * Math.Sqrt(2) * Tf);
            pid.Ki = pid.Kp / (kiFactor * Math.Sqrt(2) * Tf);
            pid.intAccum = Vector3.ClampMagnitude(pid.intAccum, 5);
        }

        // Calculations of Tf are not safe during FlightComputer constructor
        // Probably because the ship is only half-initialized...
        public void updatePIDParameters()
        {
            if (Vessel != null) {
                Vector3d torque = kOS.SteeringHelper.GetTorque (Vessel, 
                    Vessel.ctrlState != null ? Vessel.ctrlState.mainThrottle : 0.0f);
                var CoM = Vessel.findWorldCenterOfMass ();
                var MoI = Vessel.findLocalMOI (CoM);

                Vector3d ratio = new Vector3d (
                                 torque.x != 0 ? MoI.x / torque.x : 0,
                                 torque.y != 0 ? MoI.y / torque.y : 0,
                                 torque.z != 0 ? MoI.z / torque.z : 0
                             );

                Tf = Mathf.Clamp ((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
                Tf = Mathf.Clamp ((float)Tf, (float)TfMin, (float)TfMax);
            }
            initPIDParameters();
        }
    }
}
