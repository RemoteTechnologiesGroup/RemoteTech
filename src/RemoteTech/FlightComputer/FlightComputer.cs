﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;
using RemoteTech.Modules;
using RemoteTech.SimpleTypes;
using RemoteTech.UI;
using KSP.Localization;

namespace RemoteTech.FlightComputer
{
    /// <summary>
    /// This class describe the RemoteTech Flight Computer (FC).
    /// A FC is mostly used to handle the delay ('normal' + manual delay) if any and queue commands.
    /// </summary>
    public class FlightComputer : IDisposable
    {
        /// <summary>Flight computer loaded configuration from persistent save.</summary>
        private ConfigNode _fcLoadedConfigs;

        /// <summary>List of active commands in the flight computer.</summary>
        private readonly SortedDictionary<int, ICommand> _activeCommands = new SortedDictionary<int, ICommand>();

        /// <summary>List of commands queued in the flight computer.</summary>
        private readonly List<ICommand> _commandQueue = new List<ICommand>();
        
        /// <summary>Flight control queue: this is a priority queue used to delay <see cref="FlightCtrlState"/>.</summary>
        private readonly PriorityQueue<DelayedFlightCtrlState> _flightCtrlQueue = new PriorityQueue<DelayedFlightCtrlState>();

        /// <summary>The window of the flight computer.</summary>
        private FlightComputerWindow _flightComputerWindow;

        /// <summary>Current state of the flight computer.</summary>
        [Flags]
        public enum State
        {
            /// <summary>Normal state.</summary>
            Normal = 0,
            /// <summary>The flight computer (and its vessel) are packed: vessels are only packed when they come within about 300m of the active vessel.</summary>
            Packed = 2,
            /// <summary>The flight computer (and its vessel) are out of power.</summary>
            OutOfPower = 4,
            /// <summary>The flight computer (and its vessel) have no connection.</summary>
            NoConnection = 8,
            /// <summary>The flight computer signal processor is not the vessel main signal processor (see <see cref="ModuleSPU.IsMaster"/>).</summary>
            NotMaster = 16,
        }

        /// <summary>Gets whether or not it is possible to give input to the flight computer (and consequently, to the vessel).</summary>
        public bool InputAllowed
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.VesselId];
                var connection = RTCore.Instance.Network[satellite];
                return (satellite != null && satellite.HasLocalControl) || (SignalProcessor.Powered && connection.Any());
            }
        }

        /// <summary>Gets the delay applied to a flight computer (and hence, its vessel).</summary>
        public double Delay
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.VesselId];

                if (satellite != null && satellite.HasLocalControl)
                    return 0.0;

                var connection = RTCore.Instance.Network[satellite];
                return !connection.Any() ? double.PositiveInfinity : connection.Min().Delay;
            }
        }

        /// <summary>Gets the current status of the flight computer.</summary>
        public State Status
        {
            get
            {
                var satellite = RTCore.Instance.Network[SignalProcessor.VesselId];
                var connection = RTCore.Instance.Network[satellite];
                var status = State.Normal;
                if (!SignalProcessor.Powered) status |= State.OutOfPower;
                if (!SignalProcessor.IsMaster) status |= State.NotMaster;
                if (!connection.Any()) status |= State.NoConnection;
                if (Vessel.packed) status |= State.Packed;
                return status;
            }
        }

        /// <summary>Returns true to keep the throttle on the current position without a connection, otherwise false.</summary>
        public bool KeepThrottleNoConnect => !RTSettings.Instance.ThrottleZeroOnNoConnection;
        /// <summary>Returns true to lock the throttle on the current position without a connection, otherwise false.</summary>
        public bool LockedThrottleNoConnect = false;
        /// <summary>Returns the last known position of throttle prior to connection loss.</summary>
        public float LockedThrottlePositionNoConnect = 0f;

        /// <summary>Gets or sets the total delay which is the usual light speed delay + any manual delay.</summary>
        public double TotalDelay { get; set; }
        /// <summary>The target (<see cref="TargetCommand.Target"/>) of a <see cref="TargetCommand"/>.</summary>
        public ITargetable DelayedTarget { get; set; }
        /// <summary>The last <see cref="TargetCommand"/> used by the Flight Computer.</summary>
        public TargetCommand LastTarget;
        /// <summary>The vessel owning this flight computer.</summary>
        public Vessel Vessel { get; private set; }
        /// <summary>The signal processor (<see cref="ISignalProcessor"/>; <seealso cref="ModuleSPU"/>) used by this flight computer.</summary>
        public ISignalProcessor SignalProcessor { get; }
        /// <summary>List of autopilots for this flight computer. Used by external mods to add their own autopilots (<see cref="RemoteTech.API"/> class).</summary>
        public List<Action<FlightCtrlState>> SanctionedPilots { get; }
        /// <summary>List of commands that are currently active (not queued).</summary>
        public IEnumerable<ICommand> ActiveCommands => _activeCommands.Values;
        /// <summary>List of queued commands in the flight computer.</summary>
        public IEnumerable<ICommand> QueuedCommands => _commandQueue;

        /// <summary>Action triggered if the active command is aborted.</summary>
        public Action OnActiveCommandAbort;
        /// <summary>Action triggered if a new command popped to an active command.</summary>
        public Action OnNewCommandPop;
        /// <summary>Get the active Flight mode as an (<see cref="AttitudeCommand"/>).</summary>
        public AttitudeCommand CurrentFlightMode => _activeCommands[0] as AttitudeCommand;


        /// <summary>Proportional Integral Derivative vessel controller.</summary>
        public PIDController PIDController;
        public static double PIDKp = 2.0, PIDKi = 0.8, PIDKd = 1.0;
        public static readonly double RoverPIDKp = 1.0, RoverPIDKi = 0.0, RoverPIDKd = 0.0;

        /// <summary>The window of the flight computer.</summary>
        public FlightComputerWindow Window
        {
            get
            {
                _flightComputerWindow?.Hide();
                return _flightComputerWindow = new FlightComputerWindow(this);
            }
        }

        /// <summary>Computer able to pilot a rover (part of the flight computer).</summary>
        public RoverComputer RoverComputer { get; }

        /// <summary>Flight Computer constructor.</summary>
        /// <param name="s">A signal processor (most probably a <see cref="ModuleSPU"/> instance.)</param>
        public FlightComputer(ISignalProcessor s)
        {
            SignalProcessor = s;
            Vessel = s.Vessel;
            SanctionedPilots = new List<Action<FlightCtrlState>>();
            LastTarget = TargetCommand.WithTarget(null);

            var attitude = AttitudeCommand.Off();
            _activeCommands[attitude.Priority] = attitude;

            //Use http://www.ni.com/white-paper/3782/en/ to fine-tune
            PIDController = new PIDController(PIDKp, PIDKi, PIDKd, 1.0, -1.0, true);
            PIDController.SetVessel(Vessel);

            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselSwitching.Add(OnVesselSwitching);
            GameEvents.onGameSceneSwitchRequested.Add(OnSceneSwitchRequested);

            RoverComputer = new RoverComputer(this, RoverPIDKp, RoverPIDKi, RoverPIDKd);
            RoverComputer.SetVessel(Vessel);

            // Add RT listeners from KSP Autopilot
            StockAutopilotCommand.UIreference = GameObject.FindObjectOfType<VesselAutopilotUI>();
            for (var index = 0; index < StockAutopilotCommand.UIreference.modeButtons.Length; index++)
            {
                var buttonIndex = index; // prevent compiler optimisation from assigning static final index value
                StockAutopilotCommand.UIreference.modeButtons[index].onClick.AddListener(delegate { StockAutopilotCommand.AutopilotButtonClick(buttonIndex, this); });
                // bad idea to use RemoveAllListeners() since no easy way to re-add the original stock listener to onClick
            }
        }

        /// <summary>Called when a game switch is requested: close the current computer.</summary>
        /// <param name="data">data with from and to scenes.</param>
        private void OnSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data)
        {
            if (data.to != GameScenes.FLIGHT)
                Dispose();            
        }

        /// <summary>Called when there's a vessel switch, switching from `fromVessel` to `toVessel`.</summary>
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

            _flightComputerWindow?.Hide();
        }

        /// <summary>After switching the vessel hide the current flight computer UI.</summary>
        /// <param name="vessel">The **new** vessel we are changing to.</param>
        public void OnVesselChange(Vessel vessel)
        {
            RTLog.Notify("OnVesselChange - new vessel: " + (vessel != null ? vessel.vesselName : "N/A"));

            _flightComputerWindow?.Hide();
        }

        /// <summary>Called when the flight computer is disposed. This happens when the <see cref="ModuleSPU"/> is destroyed.</summary>
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

            _flightComputerWindow?.Hide();

            // Remove RT listeners from KSP Autopilot
            if (StockAutopilotCommand.UIreference != null)
            {
                for (var index = 0; index < StockAutopilotCommand.UIreference.modeButtons.Length; index++)
                {
                    var buttonIndex = index; // prevent compiler optimisation from assigning static final index value
                    StockAutopilotCommand.UIreference.modeButtons[index].onClick.RemoveListener(delegate { StockAutopilotCommand.AutopilotButtonClick(buttonIndex, this); });
                }
                StockAutopilotCommand.UIreference = null;
            }
        }

        /// <summary>Abort all active commands.</summary>
        public void Reset()
        {
            foreach (var cmd in _activeCommands.Values)
            {
                cmd.Abort();
            }

            OnActiveCommandAbort.Invoke();
        }

        /// <summary>Enqueue a command in the flight computer command queue.</summary>
        /// <param name="cmd">The command to be enqueued.</param>
        /// <param name="ignoreControl">If true the command is not enqueued.</param>
        /// <param name="ignoreDelay">If true, the command is executed immediately, otherwise the light speed delay is applied.</param>
        /// <param name="ignoreExtra">If true, the command is executed without manual delay (if any). The normal light speed delay still applies.</param>
        public void Enqueue(ICommand cmd, bool ignoreControl = false, bool ignoreDelay = false, bool ignoreExtra = false)
        {
            if (!InputAllowed && !ignoreControl) return;

            if (!ignoreDelay) cmd.TimeStamp += Delay;
            if (!ignoreExtra)
            {
                cmd.ExtraDelay += Math.Max(0, TotalDelay - Delay);
                cmd.ExtraDelayScheduledTimeStamp = cmd.TimeStamp + cmd.ExtraDelay;
            }

            var pos = _commandQueue.BinarySearch(cmd);
            if (pos < 0)
            {
                _commandQueue.Insert(~pos, cmd);
                cmd.CommandEnqueued(this);
                OrderCommandList();
            }
        }

        /// <summary>Remove a command from the flight computer command queue.</summary>
        /// <param name="cmd">The command to be removed from the command queue.</param>
        public void Remove(ICommand cmd)
        {
            _commandQueue.Remove(cmd);
            if (_activeCommands.ContainsValue(cmd)) _activeCommands.Remove(cmd.Priority);
        }

        /// <summary>Called by the <see cref="ModuleSPU.Update"/> method during the Update() "Game Logic" engine phase.</summary>
        /// <remarks>This checks if there are any commands that can be removed from the FC queue if their delay has elapsed.</remarks>
        public void OnUpdate()
        {
            if (RTCore.Instance == null) return;
            if (!SignalProcessor.IsMaster) return;
            PopCommand();
        }

        /// <summary>Called by the <see cref="ModuleSPU.OnFixedUpdate"/> method during the "Physics" engine phase.</summary>
        public void OnFixedUpdate()
        {
            if (RTCore.Instance == null) return;
            if (Vessel == null)
            {
                Vessel = SignalProcessor.Vessel;
                RoverComputer.SetVessel(Vessel);
                PIDController.SetVessel(Vessel);
            }

            // only handle onFixedUpdate if the ship is unpacked
            if (Vessel.packed)
                return;

            // Do we have a loaded configuration?
            if (_fcLoadedConfigs != null)
            {
                // than load
                Load(_fcLoadedConfigs);
                _fcLoadedConfigs = null;
            }

            // Re-attach periodically
            Vessel.OnFlyByWire -= OnFlyByWirePre;
            Vessel.OnFlyByWire -= OnFlyByWirePost;
            if (Vessel != SignalProcessor.Vessel)
            {
                SanctionedPilots.Clear();
                Vessel = SignalProcessor.Vessel;
                RoverComputer.SetVessel(Vessel);
                PIDController.SetVessel(Vessel);
            }
            // set flight control.
            Vessel.OnFlyByWire = OnFlyByWirePre + Vessel.OnFlyByWire + OnFlyByWirePost;

            //Update PID loop
            PIDController.OnFixedUpdate();

            // Send updates for Target
            if (Vessel == FlightGlobals.ActiveVessel && FlightGlobals.fetch.VesselTarget != LastTarget.Target)
            {
                Enqueue(TargetCommand.WithTarget(FlightGlobals.fetch.VesselTarget));
                UpdateLastTarget();
            }
        }

        /// <summary>Updates the last target command used by the flight computer.</summary>
        private void UpdateLastTarget()
        {
            int lastTargetIndex = _commandQueue.FindLastIndex(c => (c is TargetCommand));
            if (lastTargetIndex >= 0)
            {
                LastTarget = _commandQueue[lastTargetIndex] as TargetCommand;
            }
            else if (_activeCommands.ContainsKey(LastTarget.Priority) &&
                     _activeCommands[LastTarget.Priority] is TargetCommand)
            {
                LastTarget = _activeCommands[LastTarget.Priority] as TargetCommand;
            }
            else
            {
                LastTarget = TargetCommand.WithTarget(null);
            }
        }

        /// <summary>Enqueue a <see cref="FlightCtrlState"/> to the flight control queue.</summary>
        /// <param name="fs">The <see cref="FlightCtrlState"/> to be queued.</param>
        private void Enqueue(FlightCtrlState fs)
        {
            var dfs = new DelayedFlightCtrlState(fs);
            dfs.TimeStamp += Delay;

            if (StockAutopilotCommand.IsAutoPilotEngaged(this) && RTSettings.Instance.EnableSignalDelay) // remove the delay if the autopilot is engaged
            {
                var autopilotfs = new DelayedFlightCtrlState(fs); // make copy of FS and apply no delay

                //nullify autopilot inputs in the delayed fs
                dfs.State.roll = 0f;
                dfs.State.rollTrim = 0f;
                dfs.State.pitch = 0f;
                dfs.State.pitchTrim = 0f;
                dfs.State.yaw = 0f;
                dfs.State.yawTrim = 0f;

                //nullify throttle
                autopilotfs.State.mainThrottle = 0f;
                
                _flightCtrlQueue.Enqueue(autopilotfs);
            }

            _flightCtrlQueue.Enqueue(dfs);
        }

        /// <summary>Remove a <see cref="FlightCtrlState"/> from the flight control queue.</summary>
        /// <param name="fcs">The <see cref="FlightCtrlState"/> to be removed from the queue.</param>
        /// <param name="sat">The satellite from which the <see cref="FlightCtrlState"/> should be removed.</param>
        private void PopFlightCtrl(FlightCtrlState fcs, ISatellite sat)
        {
            var delayed = new FlightCtrlState();
            float maxThrottle = (Delay >= 0.01) ? 0.0f : fcs.mainThrottle;
            //Comment: if rocket at launchpad is Local Control and switchs to Remote Control upon launching, throttle is initially zero in this transition
            //however, it is lethal RO issue as zero throttle even once will count against engine's limited number of ignitions
            //Workaround: just pipe FCS throttle to delayed state until delay is large enough to revert back to normal RT operation

            while (_flightCtrlQueue.Count > 0 && _flightCtrlQueue.Peek().TimeStamp <= RTUtil.GameTime)
            {
                delayed = _flightCtrlQueue.Dequeue().State;
                maxThrottle = Math.Max(maxThrottle, delayed.mainThrottle);
            }
            delayed.mainThrottle = maxThrottle;

            if (!KeepThrottleNoConnect && !InputAllowed) // enforce the rule of shutting throttle down on connection loss
            {
                delayed.mainThrottle = 0f;
            }
            else if (KeepThrottleNoConnect && LockedThrottleNoConnect) // when connection is lost with the disabled rule of shutting throttle down, throttle is locked
            {
                delayed.mainThrottle = LockedThrottlePositionNoConnect;
            }

            fcs.CopyFrom(delayed);
        }

        /// <summary>Check whether there are commands that can be removed from the flight computer command queue (when their delay time has elapsed).</summary>
        /// <remarks>This is done during the Update() phase of the game engine. <see cref="OnUpdate"/> method.</remarks>
        private void PopCommand()
        {
            // something in command queue?
            if (_commandQueue.Count <= 0)
                return;

            // Can come out of time warp even if ship is not powered; workaround for KSP 0.24 power consumption bug
            if (RTSettings.Instance.ThrottleTimeWarp && TimeWarp.CurrentRate > 4.0f)
            {
                var time = TimeWarp.deltaTime;
                foreach (var dc in _commandQueue.TakeWhile(c => c.TimeStamp <= RTUtil.GameTime + (2 * time + 1.0)))
                {
                    var message = new ScreenMessage(Localizer.Format("#RT_FC_msg1"), 4.0f, ScreenMessageStyle.UPPER_LEFT);//"[Flight Computer]: Throttling back time warp..."
                    while ((2 * TimeWarp.deltaTime + 1.0) > (Math.Max(dc.TimeStamp - RTUtil.GameTime, 0) + dc.ExtraDelay) && TimeWarp.CurrentRate > 1.0f)//
                    {
                        TimeWarp.SetRate(TimeWarp.CurrentRateIndex - 1, true);
                        ScreenMessages.PostScreenMessage(message);
                    }
                }
            }

            // Proceed the extraDelay for every command where the normal delay is over
            foreach (var dc in _commandQueue.Where(s => s.Delay == 0).ToList())
            {
                // Use time decrement instead of comparing scheduled time, in case we later want to 
                //      reinstate event clocks stopping under certain conditions
                if (dc.ExtraDelay > 0)
                {
                    //issue: deltaTime is floating point -/+0.001, not great for long-term time-senitive operation
                    if (dc.ExtraDelayScheduledTimeStamp > RTUtil.GameTime)
                    {
                        dc.ExtraDelay = dc.ExtraDelayScheduledTimeStamp - RTUtil.GameTime;
                    }
                    else //fallback
                    {
                        dc.ExtraDelay -= TimeWarp.deltaTime;
                    }
                }
                else
                {
                    if (SignalProcessor.Powered)
                    {
                        // Note: depending on implementation, dc.Pop() may execute the event
                        if (dc.Pop(this))
                        {
                            _activeCommands[dc.Priority] = dc;
                            if (OnNewCommandPop != null)
                            {
                                OnNewCommandPop.Invoke();
                            }
                        }
                    }
                    else
                    {
                        string message = Localizer.Format("#RT_FC_msg2", dc.ShortName);//$"[Flight Computer]: Out of power, cannot run \"{}\" on schedule."
                        ScreenMessages.PostScreenMessage(new ScreenMessage(
                            message, 4.0f, ScreenMessageStyle.UPPER_LEFT));
                    }

                    _commandQueue.Remove(dc);
                    UpdateLastTarget();
                }
            }
        }

        /// <summary>Control the flight. Called before the <see cref="Vessel.OnFlyByWire"/> method.</summary>
        /// <param name="fcs">The input flight control state.</param>
        private void OnFlyByWirePre(FlightCtrlState fcs)
        {
            if (!SignalProcessor.IsMaster) return;
            var satellite = RTCore.Instance.Satellites[SignalProcessor.VesselId];

            if (Vessel == FlightGlobals.ActiveVessel && InputAllowed && !satellite.HasLocalControl)
            {
                Enqueue(fcs);
            }

            if (!satellite.HasLocalControl)
            {
                if (InputAllowed) // working connection
                {
                    LockedThrottleNoConnect = false;
                }
                else // connection loss
                {
                    if (!LockedThrottleNoConnect)
                    {
                        LockedThrottleNoConnect = true;
                        LockedThrottlePositionNoConnect = fcs.mainThrottle;
                    }
                }

                PopFlightCtrl(fcs, satellite);
            }
        }

        /// <summary>Control the flight. Called after the <see cref="Vessel.OnFlyByWire"/> method.</summary>
        /// <param name="fcs">The input flight control state.</param>
        private void OnFlyByWirePost(FlightCtrlState fcs)
        {
            if (!SignalProcessor.IsMaster) return;

            if (!InputAllowed && KeepThrottleNoConnect == false)
            {
                fcs.Neutralize();
            }

            if (SignalProcessor.Powered)
            {
                foreach (var dc in _activeCommands.Values.ToList())
                {
                    if (dc.Execute(this, fcs)) _activeCommands.Remove(dc.Priority);
                }
            }

            foreach (var pilot in SanctionedPilots)
            {
                pilot.Invoke(fcs);
            }
        }

        /// <summary>Orders the command queue to be chronological.</summary>
        public void OrderCommandList()
        {
            if (_commandQueue.Count <= 0) return;

            var backupList = _commandQueue;
            // sort the backup queue
            backupList = backupList.OrderBy(s => (s.Delay + s.ExtraDelay)).ToList();
            // clear the old queue
            _commandQueue.Clear();

            // add the sorted queue
            foreach (var command in backupList)
            {
                _commandQueue.Add(command);
            }
        }

        /// <summary>Restores the flight computer from the persistent save.</summary>
        /// <param name="configNode">Node with the informations for the flight computer</param>
        public void Load(ConfigNode configNode)
        {
            RTLog.Notify("Loading Flightcomputer from persistent!");

            if (!configNode.HasNode("FlightComputer"))
                return;

            // Wait while we are packed and store the current configNode
            if (Vessel.packed)
            {
                RTLog.Notify("Save Flightconfig after unpacking");
                _fcLoadedConfigs = configNode;
                return;
            }

            // Load the current vessel from signal processor if we haven't one the flight computer
            if (Vessel == null)
            {
                Vessel = SignalProcessor.Vessel;
                RoverComputer.SetVessel(Vessel);
                PIDController.SetVessel(Vessel);
            }

            // Read Flight computer informations
            var flightNode = configNode.GetNode("FlightComputer");
            TotalDelay = double.Parse(flightNode.GetValue("TotalDelay"));
            var activeCommands = flightNode.GetNode("ActiveCommands");
            var commands = flightNode.GetNode("Commands");

            // Read active commands
            if (activeCommands.HasNode())
            {
                if (_activeCommands.Count > 0)
                    _activeCommands.Clear();

                foreach (ConfigNode cmdNode in activeCommands.nodes)
                {
                    var cmd = AbstractCommand.LoadCommand(cmdNode, this);
                    if (cmd == null)
                        continue;

                    _activeCommands[cmd.Priority] = cmd;
                    cmd.Pop(this);
                }
            }

            // Read queued commands
            if (commands.HasNode())
            {
                // clear the current list
                if (_commandQueue.Count > 0)
                    _commandQueue.Clear();

                RTLog.Notify("Loading queued commands from persistent ...");
                foreach (ConfigNode cmdNode in commands.nodes)
                {
                    var cmd = AbstractCommand.LoadCommand(cmdNode, this);
                    if (cmd == null)
                        continue;

                    // if delay = 0 we're ready for the extraDelay
                    if (cmd.Delay == 0)
                    {
                        if (cmd is ManeuverCommand)
                        {
                            RTUtil.ScreenMessage(Localizer.Format("#RT_FC_msg3"));//"A maneuver burn is required"
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
                                    RTUtil.ScreenMessage(Localizer.Format("#RT_FC_msg4"));//"A burn command is required"
                                    continue;
                                }

                                // change the extra delay to x/100
                                cmd.ExtraDelay = Math.Abs(cmd.ExtraDelay) / 100;
                            }
                        }
                    }
                    // add command to queue
                    _commandQueue.Add(cmd);
                }
            }

            UpdateLastTarget();
        }

        /// <summary>Saves all values for the flight computer to the persistent.</summary>
        /// <param name="n">Node to save in</param>
        public void Save(ConfigNode n)
        {
            if (n.HasNode("FlightComputer"))
                n.RemoveNode("FlightComputer");

            var activeCommands = new ConfigNode("ActiveCommands");
            var commands = new ConfigNode("Commands");

            // save active commands
            foreach (var cmd in _activeCommands)
            {
                // Save each active command on his own node
                var activeCommandNode = new ConfigNode(cmd.Value.GetType().Name);
                cmd.Value.Save(activeCommandNode, this);
                activeCommands.AddNode(activeCommandNode);
            }

            // save commands
            foreach (var cmd in _commandQueue)
            {
                // Save each command on his own node
                var commandNode = new ConfigNode(cmd.GetType().Name);
                cmd.Save(commandNode, this);
                commands.AddNode(commandNode);
            }

            var flightNode = new ConfigNode("FlightComputer");
            flightNode.AddValue("TotalDelay", TotalDelay);
            flightNode.AddNode(activeCommands);
            flightNode.AddNode(commands);

            n.AddNode(flightNode);
        }

        /// <summary>Returns true if there's a least one <see cref="ManeuverCommand"/> on the queue.</summary>
        public bool HasManeuverCommands()
        {
            if (_commandQueue.Count <= 0)
                return false;

            // look for ManeuverCommands
            var maneuverFound = _commandQueue.FirstOrDefault(command => command is ManeuverCommand);
            return maneuverFound != null;
        }

        /// <summary>Looks for the passed <paramref name="node"/> on the command queue and returns true if the node is already on the list.</summary>
        /// <param name="node">Node to search in the queued commands</param>
        public bool HasManeuverCommandByNode(ManeuverNode node)
        {
            if (_commandQueue.Count <= 0)
                return false;

            // look for ManeuverCommands
            var maneuverFound = _commandQueue.FirstOrDefault(command => (command is ManeuverCommand && ((ManeuverCommand)command).Node == node));
            return maneuverFound != null;
        }

        /// <summary>Triggers a <see cref="CancelCommand"/> for the given <paramref name="node"/></summary>
        /// <param name="node">Node to cancel from the queue</param>
        public void RemoveManeuverCommandByNode(ManeuverNode node)
        {
            if (_commandQueue.Count <= 0) return;

            // look for ManeuverCommands
            for(var i = _commandQueue.Count - 1; i>= 0; i--)
            {
                var maneuverCmd = _commandQueue[i] as ManeuverCommand;
                if(maneuverCmd != null && maneuverCmd.Node == node)
                {
                    // remove Node
                    Enqueue(CancelCommand.WithCommand(maneuverCmd));
                    return;
                }
            }
        }
    }
}
