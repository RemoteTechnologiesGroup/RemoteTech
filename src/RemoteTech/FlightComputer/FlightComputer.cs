using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;
using RemoteTech.SimpleTypes;
using RemoteTech.UI;

namespace RemoteTech.FlightComputer
{
    public class FlightComputer : IDisposable
    {
        private ConfigNode fcLoadedConfigs = null;
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

        /// <summary>
        /// Returns true to keep the throttle on the current position without a connection, otherwise false
        /// </summary>
        public bool KeepThrottleNoConnect { get { return !RTSettings.Instance.ThrottleZeroOnNoConnection; } }

        public double TotalDelay { get; set; }
        public ITargetable DelayedTarget { get; set; }
        public TargetCommand lastTarget = null;
        public Vessel Vessel { get; private set; }
        public ISignalProcessor SignalProcessor { get; private set; }
        public List<Action<FlightCtrlState>> SanctionedPilots { get; private set; }
        public IEnumerable<ICommand> ActiveCommands { get { return mActiveCommands.Values; } }
        public IEnumerable<ICommand> QueuedCommands { get { return mCommandQueue; } }

        /// Will be triggered if the active command is aborted
        public Action onActiveCommandAbort;
        /// Will be triggered if a new command popped to an active command
        public Action onNewCommandPop;
        /// Get the active Flightmode
        public AttitudeCommand currentFlightMode { get { return (mActiveCommands[0] is AttitudeCommand) ? (AttitudeCommand)mActiveCommands[0] : null; } }

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

        private FlightComputerWindow mWindow;
        public FlightComputerWindow Window { get { if (mWindow != null) mWindow.Hide(); return mWindow = new FlightComputerWindow(this); } }

        public RoverComputer mRoverComputer { get; private set; }

        public FlightComputer(ISignalProcessor s)
        {
            SignalProcessor = s;
            Vessel = s.Vessel;
            SanctionedPilots = new List<Action<FlightCtrlState>>();
            pid = new PIDControllerV2(0, 0, 0, 1, -1);
            initPIDParameters();
            lastAct = Vector3d.zero;
            lastTarget = TargetCommand.WithTarget(null);

            var attitude = AttitudeCommand.Off();
            mActiveCommands[attitude.Priority] = attitude;

            GameEvents.onVesselChange.Add(OnVesselChange);

            mRoverComputer = new RoverComputer();
            mRoverComputer.SetVessel(Vessel);
        }

        /// <summary>
        /// After switching the vessel close the current flightcomputer.
        /// </summary>
        public void OnVesselChange(Vessel v)
        {
            Dispose();
        }

        public void Dispose()
        {
            RTLog.Notify("FlightComputer: Dispose");

            GameEvents.onVesselChange.Remove(OnVesselChange);

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

            onActiveCommandAbort.Invoke();
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
                orderCommandList();
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
            if (Vessel == null)
            {
                Vessel = SignalProcessor.Vessel;
                mRoverComputer.SetVessel(Vessel);
            }

            // only handle onFixedUpdate if the ship is unpacked
            if (Vessel.packed)
                return;

            // Do we have a config?
            if (fcLoadedConfigs != null)
            {
                // than load
                load(fcLoadedConfigs);
                fcLoadedConfigs = null;
            }

            // Re-attach periodically
            Vessel.OnFlyByWire -= OnFlyByWirePre;
            Vessel.OnFlyByWire -= OnFlyByWirePost;
            if (Vessel != SignalProcessor.Vessel)
            {
                SanctionedPilots.Clear();
                Vessel = SignalProcessor.Vessel;
                mRoverComputer.SetVessel(Vessel);
            }
            Vessel.OnFlyByWire = OnFlyByWirePre + Vessel.OnFlyByWire + OnFlyByWirePost;

            // Update proportional controller for changes in ship state
            updatePIDParameters();

            // Send updates for Target
            if (Vessel == FlightGlobals.ActiveVessel && FlightGlobals.fetch.VesselTarget != lastTarget.Target)
            {
                Enqueue(TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget));
                UpdateLastTarget();
            }
        }

        private void UpdateLastTarget()
        {
            int lastTargetIndex = mCommandQueue.FindLastIndex(c => (c is TargetCommand));
            if (lastTargetIndex >= 0)
            {
                lastTarget = mCommandQueue[lastTargetIndex] as TargetCommand;
            }
            else if (mActiveCommands.ContainsKey(lastTarget.Priority) &&
                     mActiveCommands[lastTarget.Priority] is TargetCommand)
            {
                lastTarget = mActiveCommands[lastTarget.Priority] as TargetCommand;
            }
            else
            {
                lastTarget = TargetCommand.WithTarget(null);
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

            // Keep the throttle on no connection
            if(this.KeepThrottleNoConnect == true)
            {
                delayed.mainThrottle = fcs.mainThrottle;
            }

            while (mFlightCtrlQueue.Count > 0 && mFlightCtrlQueue.Peek().TimeStamp <= RTUtil.GameTime)
            {
                delayed = mFlightCtrlQueue.Dequeue().State;
            }

            fcs.CopyFrom(delayed);
        }

        private void PopCommand()
        {
            // Commands
            if (mCommandQueue.Count > 0)
            {
                // Can come out of time warp even if ship unpowered; workaround for KSP 0.24 power consumption bug
                if (RTSettings.Instance.ThrottleTimeWarp && TimeWarp.CurrentRate > 4.0f)
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

                // Proceed the extraDelay for every command where the normal delay is over
                foreach (var dc in mCommandQueue.Where(s=>s.Delay==0).ToList())
                {
                    // Use time decrement instead of comparing scheduled time, in case we later want to 
                    //      reinstate event clocks stopping under certain conditions
                    if (dc.ExtraDelay > 0)
                    {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    } else
                    {
                        if (SignalProcessor.Powered)
                        {
                            // Note: depending on implementation, dc.Pop() may execute the event
                            if (dc.Pop(this)) {
                                mActiveCommands[dc.Priority] = dc;
                                if (onNewCommandPop != null) {
                                    onNewCommandPop.Invoke();
                                }
                            }
                        } else {
                            string message = String.Format ("[Flight Computer]: Out of power, cannot run \"{0}\" on schedule.", dc.ShortName);
                            ScreenMessages.PostScreenMessage(new ScreenMessage(
                                message, 4.0f, ScreenMessageStyle.UPPER_LEFT
                            ), true);
                        }
                        mCommandQueue.Remove(dc);
                        UpdateLastTarget();
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

            if (!InputAllowed && this.KeepThrottleNoConnect == false)
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
            if (Vessel != null)
            {
                Vector3d torque = SteeringHelper.GetTorque(Vessel,
                    Vessel.ctrlState != null ? Vessel.ctrlState.mainThrottle : 0.0f);
                var CoM = Vessel.findWorldCenterOfMass();
                var MoI = Vessel.findLocalMOI(CoM);

                Vector3d ratio = new Vector3d(
                                 torque.x != 0 ? MoI.x / torque.x : 0,
                                 torque.y != 0 ? MoI.y / torque.y : 0,
                                 torque.z != 0 ? MoI.z / torque.z : 0
                             );

                Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
                Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
            }
            initPIDParameters();
        }

        /// <summary>
        /// Orders the mCommand queue to be chronological
        /// </summary>
        public void orderCommandList()
        {
            if (mCommandQueue.Count <= 0) return;

            List<ICommand> backupList = mCommandQueue;
            // sort the backup queue
            backupList = backupList.OrderBy(s => (s.Delay + s.ExtraDelay)).ToList();
            // clear the old queue
            mCommandQueue.Clear();

            // add the sorted queue
            foreach (var command in backupList)
            {
                mCommandQueue.Add(command);
            }
        }

        /// <summary>
        /// Restores the flightcomputer from the persistant
        /// </summary>
        /// <param name="n">Node with the informations for the flightcomputer</param>
        public void load(ConfigNode n)
        {
            RTLog.Notify("Loading Flightcomputer from persistent!");

            if (!n.HasNode("FlightComputer"))
                return;

            // Wait while we are packed and store the current configNode
            if (Vessel.packed)
            {
                RTLog.Notify("Save flightconfig after unpacking");
                fcLoadedConfigs = n;
                return;
            }

            // Load the current vessel from signalprocessor if we've no on the flightcomputer
            if (Vessel == null)
            {
                Vessel = SignalProcessor.Vessel;
                mRoverComputer.SetVessel(Vessel);
            }

            // Read Flightcomputer informations
            ConfigNode FlightNode = n.GetNode("FlightComputer");
            TotalDelay = double.Parse(FlightNode.GetValue("TotalDelay"));
            ConfigNode ActiveCommands = FlightNode.GetNode("ActiveCommands");
            ConfigNode Commands = FlightNode.GetNode("Commands");

            // Read active commands
            if (ActiveCommands.HasNode())
            {
                if (mActiveCommands.Count > 0)
                    mActiveCommands.Clear();
                foreach (ConfigNode cmdNode in ActiveCommands.nodes)
                {
                    ICommand cmd = AbstractCommand.LoadCommand(cmdNode, this);

                    if (cmd != null)
                    {
                        mActiveCommands[cmd.Priority] = cmd;
                        cmd.Pop(this);
                    }
                }
            }

            // Read queued commands
            if (Commands.HasNode())
            {
                int qCounter = 0;

                // clear the current list
                if (mCommandQueue.Count > 0)
                    mCommandQueue.Clear();

                RTLog.Notify("Loading queued commands from persistent ...");
                foreach (ConfigNode cmdNode in Commands.nodes)
                {
                    ICommand cmd = AbstractCommand.LoadCommand(cmdNode, this);

                    if (cmd != null)
                    {
                        // if delay = 0 we're ready for the extraDelay
                        if (cmd.Delay == 0)
                        {
                            if (cmd is ManeuverCommand)
                            {
                                // TODO: Need better text
                                RTUtil.ScreenMessage("You missed the maneuver burn!");
                                continue;
                            }

                            // if extraDelay is set, we've to calculate the elapsed time
                            // and set the new extradelay based on the current time
                            if (cmd.ExtraDelay > 0)
                            {
                                cmd.ExtraDelay = cmd.TimeStamp  + cmd.ExtraDelay - RTUtil.GameTime;

                                // Are we ready to handle the command ?
                                if (cmd.ExtraDelay <= 0)
                                {
                                    if (cmd is BurnCommand)
                                    {
                                        // TODO: Need better text
                                        RTUtil.ScreenMessage("You missed the burn command!");
                                        continue;
                                    } else
                                    {
                                        // change the extra delay to x/100
                                        cmd.ExtraDelay = (qCounter) / 100;
                                    }
                                }
                            }
                        }
                        mCommandQueue.Add(cmd);
                    }
                }
            }
            UpdateLastTarget();
        }

        /// <summary>
        /// Saves all values for the flightcomputer to the persistant
        /// </summary>
        /// <param name="n">Node to save in</param>
        public void Save(ConfigNode n)
        {
            if (n.HasNode("FlightComputer"))
                n.RemoveNode("FlightComputer");

            ConfigNode ActiveCommands = new ConfigNode("ActiveCommands");
            ConfigNode Commands = new ConfigNode("Commands");

            foreach (KeyValuePair<int, ICommand> cmd in mActiveCommands)
            {
                // Save each active command on his own node
                ConfigNode activeCommandNode = new ConfigNode(cmd.Value.GetType().Name);
                cmd.Value.Save(activeCommandNode, this);
                ActiveCommands.AddNode(activeCommandNode);
            }

            foreach (ICommand cmd in mCommandQueue)
            {
                // Save each command on his own node
                ConfigNode commandNode = new ConfigNode(cmd.GetType().Name);
                cmd.Save(commandNode, this);
                Commands.AddNode(commandNode);
            }

            ConfigNode FlightNode = new ConfigNode("FlightComputer");
            FlightNode.AddValue("TotalDelay", TotalDelay);
            FlightNode.AddNode(ActiveCommands);
            FlightNode.AddNode(Commands);

            n.AddNode(FlightNode);
        }
    }
}
