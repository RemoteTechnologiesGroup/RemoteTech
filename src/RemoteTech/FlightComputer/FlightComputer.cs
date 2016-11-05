using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;
using RemoteTech.Modules;
using RemoteTech.SimpleTypes;
using RemoteTech.UI;

namespace RemoteTech.FlightComputer
{
    /// <summary>
    /// This class describe the RemoteTech Flight Computer (FC).
    /// A FC is mostly used to handle the delay ('normal' + manual delay) if any and queue commands.
    /// </summary>
    public class FlightComputer : IDisposable
    {
        /// <summary>
        /// Flight computer loaded configuration from persistent save.
        /// </summary>
        private ConfigNode fcLoadedConfigs = null;

        /// <summary>
        /// List of active commands in the flight computer.
        /// </summary>
        private readonly SortedDictionary<int, ICommand> mActiveCommands = new SortedDictionary<int, ICommand>();

        /// <summary>
        /// List of commands queued in the flight computer.
        /// </summary>
        private readonly List<ICommand> mCommandQueue = new List<ICommand>();
        
        /// <summary>
        /// Flight control queue: this is a priority queue used to delay <see cref="FlightCtrlState"/>.
        /// </summary>
        private readonly PriorityQueue<DelayedFlightCtrlState> mFlightCtrlQueue = new PriorityQueue<DelayedFlightCtrlState>();

        /// <summary>
        /// The window of the flight computer.
        /// </summary>
        private FlightComputerWindow mWindow;

        /// <summary>
        /// Current state of the flight computer.
        /// </summary>
        [Flags]
        public enum State
        {
            /// <summary>
            /// Normal state.
            /// </summary>
            Normal = 0,
            /// <summary>
            /// The flight computer (and its vessel) are packed: vessels are only packed when they come within about 300m of the active vessel.
            /// </summary>
            Packed = 2,
            /// <summary>
            /// The flight computer (and its vessel) are out of power.
            /// </summary>
            OutOfPower = 4,
            /// <summary>
            /// The flight computer (and its vessel) have no connection.
            /// </summary>
            NoConnection = 8,
            /// <summary>
            /// The flight computer signal processor is not the vessel main signal processor (see <see cref="ModuleSPU.IsMaster"/>).
            /// </summary>
            NotMaster = 16,
        }

        /// <summary>
        /// Gets whether or not it is possible to give input to the flight computer (and consequently, to the vessel).
        /// </summary>
        public bool InputAllowed
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.Guid];
                var connection = RTCore.Instance.Network[satellite];
                return (satellite != null && satellite.HasLocalControl) || (SignalProcessor.Powered && connection.Any());
            }
        }

        /// <summary>
        /// Gets the delay applied to a flight computer (and hence, its vessel).
        /// </summary>
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

        /// <summary>
        /// Gets the current status of the flight computer.
        /// </summary>
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
        /// Returns true to keep the throttle on the current position without a connection, otherwise false.
        /// </summary>
        public bool KeepThrottleNoConnect { get { return !RTSettings.Instance.ThrottleZeroOnNoConnection; } }
        /// <summary>
        /// Gets or sets the total delay which is the usual light speed delay + any manual delay.
        /// </summary>
        public double TotalDelay { get; set; }
        /// <summary>
        /// The target (<see cref="TargetCommand.Target"/>) of a <see cref="TargetCommand"/>.
        /// </summary>
        public ITargetable DelayedTarget { get; set; }
        /// <summary>
        /// The last <see cref="TargetCommand"/> used by the Flight Computer.
        /// </summary>
        public TargetCommand lastTarget = null;
        /// <summary>
        /// The vessel owning this flight computer.
        /// </summary>
        public Vessel Vessel { get; private set; }
        /// <summary>
        /// The signal processor (<see cref="ISignalProcessor"/>; <seealso cref="ModuleSPU"/>) used by this flight computer.
        /// </summary>
        public ISignalProcessor SignalProcessor { get; private set; }
        /// <summary>
        /// List of autopilots for this flight computer. Used by external mods to add their own autopilots (<see cref="RemoteTech.API"/> class).
        /// </summary>
        public List<Action<FlightCtrlState>> SanctionedPilots { get; private set; }
        /// <summary>
        /// List of commands that are currently active (not queued).
        /// </summary>
        public IEnumerable<ICommand> ActiveCommands { get { return mActiveCommands.Values; } }
        /// <summary>
        /// List of queued commands in the flight computer.
        /// </summary>
        public IEnumerable<ICommand> QueuedCommands { get { return mCommandQueue; } }

        /// <summary>
        /// Action triggered if the active command is aborted.
        /// </summary>
        public Action onActiveCommandAbort;
        /// <summary>
        /// Action triggered if a new command popped to an active command.
        /// </summary>
        public Action onNewCommandPop;
        /// <summary>
        /// Get the active Flight mode as an (<see cref="AttitudeCommand"/>).
        /// </summary>
        public AttitudeCommand currentFlightMode { get { return (mActiveCommands[0] is AttitudeCommand) ? (AttitudeCommand)mActiveCommands[0] : null; } }

        
        /// <summary>
        /// Proportional Integral Derivative vessel controller.
        /// </summary>
        public PIDControllerV3 pid { get; private set; }
        // Flight controller parameters from MechJeb, copied from master on June 27, 2014
        public Vector3d lastAct { get; set; }
        public double Tf = 0.3;
        public double TfMin = 0.1;
        public double TfMax = 0.5;
        public double kpFactor = 3;
        public double kiFactor = 6;
        public double kdFactor = 0.5;
        
        /// <summary>
        /// The window of the flight computer.
        /// </summary>
        public FlightComputerWindow Window { get { if (mWindow != null) mWindow.Hide(); return mWindow = new FlightComputerWindow(this); } }

        /// <summary>
        /// Computer able to pilot a rover (part of the flight computer).
        /// </summary>
        public RoverComputer mRoverComputer { get; private set; }

        /// <summary>
        /// Flight Computer constructor.
        /// </summary>
        /// <param name="s">A signal processor (most probably a <see cref="ModuleSPU"/> instance.)</param>
        public FlightComputer(ISignalProcessor s)
        {
            SignalProcessor = s;
            Vessel = s.Vessel;
            SanctionedPilots = new List<Action<FlightCtrlState>>();
            pid = new PIDControllerV3(Vector3d.zero, Vector3d.zero, Vector3d.zero, 1, -1);
            setPIDParameters();
            lastAct = Vector3d.zero;
            lastTarget = TargetCommand.WithTarget(null);

            var attitude = AttitudeCommand.Off();
            mActiveCommands[attitude.Priority] = attitude;

            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselSwitching.Add(OnVesselSwitching);
            GameEvents.onGameSceneSwitchRequested.Add(OnSceneSwitchRequested);

            mRoverComputer = new RoverComputer();
            mRoverComputer.SetVessel(Vessel);
        }

        /// <summary>
        /// Called when a game switch is requested: close the current computer.
        /// </summary>
        /// <param name="data">data with from and to scenes.</param>
        private void OnSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data)
        {
            if (data.to != GameScenes.FLIGHT)
                this.Dispose();            
        }

        /// <summary>
        /// Called when there's a vessel switch, switching from `fromVessel` to `toVessel`.
        /// </summary>
        /// <param name="fromVessel">The vessel we switch from.</param>
        /// <param name="toVessel">The vessel we're switching to.</param>
        private void OnVesselSwitching(Vessel fromVessel, Vessel toVessel)
        {
            RTLog.Notify("OnVesselSwitching - from: " + (fromVessel != null ? fromVessel.vesselName : "N/A") + " to: " + toVessel.vesselName);            
            
            if(fromVessel != null)
            {
                // remove flight code controls.
                fromVessel.OnFlyByWire -= OnFlyByWirePre;
                fromVessel.OnFlyByWire -= OnFlyByWirePost;
            }
            if(mWindow != null)
            {
                mWindow.Hide();
            }
        }

        /// <summary>
        /// After switching the vessel hide the current flight computer UI.
        /// </summary>
        /// <param name="vessel">The **new** vessel we are changing to.</param>
        public void OnVesselChange(Vessel vessel)
        {
            RTLog.Notify("OnVesselChange - new vessel: " + (vessel != null ? vessel.vesselName : "N/A"));
            if (mWindow != null)
            {
                mWindow.Hide();
            }
        }

        /// <summary>
        /// Called when the flight computer is disposed. This happens when the <see cref="ModuleSPU"/> is destroyed.
        /// </summary>
        public void Dispose()
        {
            RTLog.Notify("FlightComputer: Dispose");

            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onVesselSwitching.Remove(OnVesselSwitching);
            GameEvents.onGameSceneSwitchRequested.Remove(OnSceneSwitchRequested);

            if (Vessel != null)
            {
                // remove flight code controls.
                Vessel.OnFlyByWire -= OnFlyByWirePre;
                Vessel.OnFlyByWire -= OnFlyByWirePost;
            }
            if (mWindow != null)
            {
                mWindow.Hide();
            }
        }

        /// <summary>
        /// Abort all active commands.
        /// </summary>
        public void Reset()
        {
            foreach (var cmd in mActiveCommands.Values)
            {
                cmd.Abort();
            }

            onActiveCommandAbort.Invoke();
        }

        /// <summary>
        /// Enqueue a command in the flight computer command queue.
        /// </summary>
        /// <param name="cmd">The command to be enqueued.</param>
        /// <param name="ignore_control">If true the command is not enqueued.</param>
        /// <param name="ignore_delay">If true, the command is executed immediately, otherwise the light speed delay is applied.</param>
        /// <param name="ignore_extra">If true, the command is executed without manual delay (if any). The normal light speed delay still applies.</param>
        public void Enqueue(ICommand cmd, bool ignore_control = false, bool ignore_delay = false, bool ignore_extra = false)
        {
            if (!InputAllowed && !ignore_control) return;

            if (!ignore_delay) cmd.TimeStamp += Delay;
            if (!ignore_extra) cmd.ExtraDelay += Math.Max(0, TotalDelay - Delay);

            int pos = mCommandQueue.BinarySearch(cmd);
            if (pos < 0)
            {
                mCommandQueue.Insert(~pos, cmd);
                cmd.CommandEnqueued(this);
                orderCommandList();
            }
        }

        /// <summary>
        /// Remove a command from the flight computer command queue.
        /// </summary>
        /// <param name="cmd">The command to be removed from the command queue.</param>
        public void Remove(ICommand cmd)
        {
            mCommandQueue.Remove(cmd);
            if (mActiveCommands.ContainsValue(cmd)) mActiveCommands.Remove(cmd.Priority);
        }

        /// <summary>
        /// Called by the <see cref="ModuleSPU.Update"/> method during the Update() "Game Logic" engine phase.
        /// <remarks>This checks if there are any commands that can be removed from the FC queue if their delay has elapsed.</remarks>
        /// </summary>
        public void OnUpdate()
        {
            if (RTCore.Instance == null) return;
            if (!SignalProcessor.IsMaster) return;
            PopCommand();
        }

        /// <summary>
        /// Called by the <see cref="ModuleSPU.FixedUpdate"/> method during the "Physics" engine phase.
        /// </summary>
        public void OnFixedUpdate()
        {
            if (RTCore.Instance == null) return;
            if (Vessel == null)
            {
                Vessel = SignalProcessor.Vessel;
                mRoverComputer.SetVessel(Vessel);
            }

            // only handle onFixedUpdate if the ship is unpacked
            if (Vessel.packed)
                return;

            // Do we have a loaded configuration?
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
            // set flight control.
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

        /// <summary>
        /// Updates the last target command used by the flight computer.
        /// </summary>
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

        /// <summary>
        /// Enqueue a <see cref="FlightCtrlState"/> to the flight control queue.
        /// </summary>
        /// <param name="fs">The <see cref="FlightCtrlState"/> to be queued.</param>
        private void Enqueue(FlightCtrlState fs)
        {
            DelayedFlightCtrlState dfs = new DelayedFlightCtrlState(fs);
            dfs.TimeStamp += Delay;
            mFlightCtrlQueue.Enqueue(dfs);

        }

        /// <summary>
        /// Remove a <see cref="FlightCtrlState"/> from the flight control queue.
        /// </summary>
        /// <param name="fcs">The <see cref="FlightCtrlState"/> to be removed from the queue.</param>
        /// <param name="sat">The satellite from which the <see cref="FlightCtrlState"/> should be removed.</param>
        private void PopFlightCtrl(FlightCtrlState fcs, ISatellite sat)
        {
            //TODO: `sat` parameter is never used. Check if is needed somewhere: if it's not, remove it.
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

        /// <summary>
        /// Check whether there are commands that can be removed from the flight computer command queue (when their delay time has elapsed).
        /// <remarks>This is done during the Update() phase of the game engine. <see cref="OnUpdate"/> method.</remarks>
        /// </summary>
        private void PopCommand()
        {
            // Commands
            if (mCommandQueue.Count > 0)
            {
                // Can come out of time warp even if ship is not powered; workaround for KSP 0.24 power consumption bug
                if (RTSettings.Instance.ThrottleTimeWarp && TimeWarp.CurrentRate > 4.0f)
                {
                    var time = TimeWarp.deltaTime;
                    foreach (var dc in mCommandQueue.TakeWhile(c => c.TimeStamp <= RTUtil.GameTime + (2 * time + 1.0)))
                    {
                        var message = new ScreenMessage("[Flight Computer]: Throttling back time warp...", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                        while ((2 * TimeWarp.deltaTime + 1.0) > (Math.Max(dc.TimeStamp - RTUtil.GameTime, 0) + dc.ExtraDelay) && TimeWarp.CurrentRate > 1.0f)
                        {
                            TimeWarp.SetRate(TimeWarp.CurrentRateIndex - 1, true);
                            ScreenMessages.PostScreenMessage(message);
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
                                message, 4.0f, ScreenMessageStyle.UPPER_LEFT));
                        }
                        mCommandQueue.Remove(dc);
                        UpdateLastTarget();
                    }
                }
            }
        }

        /// <summary>
        /// Control the flight. Called before the <see cref="Vessel.OnFlyByWire"/> method.
        /// </summary>
        /// <param name="fcs">The input flight control state.</param>
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

        /// <summary>
        /// Control the flight. Called after the <see cref="Vessel.OnFlyByWire"/> method.
        /// </summary>
        /// <param name="fcs">The input flight control state.</param>
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

        // used to set PID parameters.
        public void setPIDParameters() 
        {
            Vector3d TfV = new Vector3d(0.3, 0.3, 0.3);
            Vector3d invTf = TfV.Invert();
            pid.Kd = kdFactor * invTf;

            pid.Kp = (1 / (kpFactor * Math.Sqrt(2))) * pid.Kd;
            pid.Kp.Scale(invTf);

            pid.Ki = (1 / (kiFactor * Math.Sqrt(2))) * pid.Kp;
            pid.Ki.Scale(invTf);

            pid.intAccum = pid.intAccum.Clamp(-5, 5);
        }

        // Calculations of Tf are not safe during FlightComputer constructor
        // Probably because the ship is only half-initialized...
        public void updatePIDParameters()
        {
            if (Vessel != null)
            {
                Vector3 torque = SteeringHelper.GetVesselTorque(Vessel);
                var CoM = Vessel.CoM;
                var MoI = Vessel.MOI;

                Vector3d ratio = new Vector3d(
                                 torque.x != 0 ? MoI.x / torque.x : 0,
                                 torque.y != 0 ? MoI.y / torque.y : 0,
                                 torque.z != 0 ? MoI.z / torque.z : 0
                             );

                Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
                Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
            }
            setPIDParameters();
        }

        /// <summary>
        /// Orders the command queue to be chronological
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
        /// Restores the flight computer from the persistent save.
        /// </summary>
        /// <param name="n">Node with the informations for the flight computer</param>
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
                            // and set the new extra delay based on the current time
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
        /// Saves all values for the flight computer to the persistent
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

        /// <summary>
        /// Returns true if there's a least one <see cref="ManeuverCommand"/> on the queue.
        /// </summary>
        public bool hasManeuverCommands()
        {
            if (mCommandQueue.Count <= 0) return false;

            // look for ManeuverCommands
            var maneuverFound = this.mCommandQueue.Where(command => command is ManeuverCommand).FirstOrDefault();
            if (maneuverFound == null) return false;

            return true;
        }

        /// <summary>
        /// Looks for the passed <paramref name="node"/> on the command
        /// queue and returns true if the node is already on the list.
        /// </summary>
        /// <param name="node">Node to search in the queued commands</param>
        public bool hasManeuverCommandByNode(ManeuverNode node)
        {
            if (mCommandQueue.Count <= 0) return false;

            // look for ManeuverCommands
            var maneuverFound = this.mCommandQueue.Where(command => (command is ManeuverCommand && ((ManeuverCommand)command).Node == node)).FirstOrDefault();
            if (maneuverFound == null) return false;

            return true;
        }

        /// <summary>
        /// Triggers a <see cref="CancelCommand"/> for the given <paramref name="node"/>
        /// </summary>
        /// <param name="node">Node to cancel from the queue</param>
        public void removeManeuverCommandByNode(ManeuverNode node)
        {
            if (mCommandQueue.Count <= 0) return;

            // look for ManeuverCommands
            for(int i = this.mCommandQueue.Count-1; i>= 0; i--)
            {
                if(this.mCommandQueue[i] is ManeuverCommand && ((ManeuverCommand)this.mCommandQueue[i]).Node == node )
                {
                    // remove Node
                    this.Enqueue(CancelCommand.WithCommand((ManeuverCommand)this.mCommandQueue[i]));
                    return;
                }
            }
        }
    }
}
