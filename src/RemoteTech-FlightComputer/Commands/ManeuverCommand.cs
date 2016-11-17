using System;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Utils;
using UnityEngine;

namespace RemoteTech.FlightComputer.Commands
{
    public class ManeuverCommand : AbstractCommand
    {
        /// <summary>Index id of this maneuver node from patchedConicSolver.maneuverNodes list</summary>
        [Persistent] public int NodeIndex;
        /// <summary></summary>
        [Persistent] public string KaCItemId = string.Empty;

        public double OriginalDelta;
        public double RemainingTime;
        public double RemainingDelta;
        public ManeuverNode Node;
        public bool EngineActivated { get; private set; }
        public override int Priority => 0;

        private double _throttle = 1.0f;
        private double _lowestDeltaV;
        private bool _abortOnNextExecute;

        public override string Description
        {
            get
            {
                if (RemainingTime > 0 || RemainingDelta > 0)
                {
                    var flightInfo = "Executing maneuver: " + RemainingDelta.ToString("F2") +
                                        "m/s" + Environment.NewLine + "Remaining duration: ";

                    flightInfo += EngineActivated ? TimeUtil.FormatDuration(RemainingTime) : "-:-";

                    return flightInfo + Environment.NewLine + base.Description;
                }

                return "Execute planned maneuver" + Environment.NewLine + base.Description;
            }
        }
        public override string ShortName => "Execute maneuver node";

        public override bool Pop(FlightComputer f)
        {
            var burn = f.ActiveCommands.FirstOrDefault(c => c is BurnCommand);
            if (burn != null) {
                f.Remove (burn);
            }

            OriginalDelta = Node.DeltaV.magnitude;
            RemainingDelta = GetRemainingDeltaV(f);
            EngineActivated = true;

            var thrustToMass = FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass();
            if (thrustToMass == 0.0) {
                EngineActivated = false;
                GuiUtil.ScreenMessage("[Flight Computer]: No engine to carry out the maneuver.");
            } else {
                RemainingTime = RemainingDelta / thrustToMass;
            }

            return true;
        }

        /// <summary>
        /// Gets the current remaining delta velocity for this maneuver burn determined by the vessels burn vector of the passed FlightComputer instance.
        /// </summary>
        /// <param name="computer">FlightComputer instance to determine remaining delta velocity by.</param>
        /// <returns>Remaining delta velocity in m/s^2</returns>
        private double GetRemainingDeltaV(FlightComputer computer)
        {
            return Node.GetBurnVector(computer.Vessel.orbit).magnitude;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="computer">FlightComputer instance of the computer of the vessel.</param>
        private void AbortManeuver(FlightComputer computer)
        {
            GuiUtil.ScreenMessage("[Flight Computer]: Maneuver removed");
            if (computer.Vessel.patchedConicSolver != null)
            {
                Node.RemoveSelf();
            }

            // Flight Computer mode after execution based on settings
            if (RTSettings.Instance.FCOffAfterExecute)
            {
                computer.Enqueue(AttitudeCommand.Off(), true, true, true);
            }
            if (!RTSettings.Instance.FCOffAfterExecute)
            {
                computer.Enqueue(AttitudeCommand.KillRot(), true, true, true);
            }
        }

        /// <summary>
        /// Executes the maneuver burn for the configured maneuver node.
        /// </summary>
        /// <param name="computer">FlightComputer instance of the computer of the vessel the ManeuverCommand is for.</param>
        /// <param name="ctrlState">FlightCtrlState instance of the current state of the vessel.</param>
        /// <returns>true if the command has finished its work, false otherwise.</returns>
        public override bool Execute(FlightComputer computer, FlightCtrlState ctrlState)
        {
            // Halt the command if we reached our target or were command to abort by the previous tick
            if (RemainingDelta <= 0.1 || _abortOnNextExecute)
            {
                AbortManeuver(computer);
                return true;
            }

            // Orientate vessel to maneuver prograde
            var forward = Node.GetBurnVector(computer.Vessel.orbit).normalized;
            var up = (computer.SignalProcessor.Body.position - computer.SignalProcessor.Position).normalized;
            var orientation = Quaternion.LookRotation(forward, up);
            FlightCore.HoldOrientation(ctrlState, computer, orientation, true);

            // This represents the theoretical acceleration but is off by a few m/s^2, probably because some parts are partially physics-less
            var thrustToMass = (FlightCore.GetTotalThrust(computer.Vessel) / computer.Vessel.GetTotalMass());
            // We need to know if the engine was activated or not to show the proper info text in the command
            if (thrustToMass == 0.0)
            {
                EngineActivated = false;
                return false;
            }
            EngineActivated = true;

            // Before any throttling, those two values may differ from after the throttling took place
            RemainingDelta = GetRemainingDeltaV(computer);
            RemainingTime = RemainingDelta / thrustToMass;

            // In case we would overpower with 100% thrust, calculate how much we actually need and set it.
            if (computer.Vessel.acceleration.magnitude > RemainingDelta)
            {
                // Formula which leads to this: a = ( vE – vS ) / dT
                _throttle = RemainingDelta / computer.Vessel.acceleration.magnitude;
            }
                
            ctrlState.mainThrottle = (float)_throttle;

            // TODO: THIS CAN PROBABLY BE REMOVED? RemainingDelta = this.getRemainingDeltaV(computer);

            // After throttling, the remaining time differs from beforehand (dividing delta by throttled thrustToMass)
            RemainingTime = RemainingDelta / (ctrlState.mainThrottle * thrustToMass);

            // We need to abort if the remaining delta was already low enough so it only takes exactly one more tick!
            var ticksRemaining = RemainingTime / TimeWarp.deltaTime;

            if (ticksRemaining <= 1)
            {
                _throttle *= ticksRemaining;
                ctrlState.mainThrottle = (float)_throttle;
                _abortOnNextExecute = true;
                return false;
            }

            // we only compare up to the fiftieth part due to some burn-up delay when just firing up the engines
            if (_lowestDeltaV > 0 // Do ignore the first tick
                && (RemainingDelta - 0.02) > _lowestDeltaV
                && RemainingDelta < 1.0)   // be safe that we do not abort the command to early
            {
                // Aborting because deltaV was rising again!
                AbortManeuver(computer);
                return true;
            }

            // Lowest delta always has to be stored to be able to compare it in the next tick
            if (_lowestDeltaV == 0 // Always do it on the first tick
                || RemainingDelta < _lowestDeltaV)
            {
                _lowestDeltaV = RemainingDelta;
            }

            return false;
        }

        /// <summary>
        /// Returns the total time for this burn in seconds
        /// </summary>
        /// <param name="f">FlightComputer for the current vessel</param>
        /// <returns>max burn time</returns>
        public double getMaxBurnTime(FlightComputer f)
        {
            if (Node == null) return 0;

            return Node.DeltaV.magnitude / (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
        }

        public static ManeuverCommand WithNode(int nodeIndex, FlightComputer f)
        {
            var thrust = FlightCore.GetTotalThrust(f.Vessel);
            var node = f.Vessel.patchedConicSolver.maneuverNodes[nodeIndex];
            var advance = f.Delay;

            if (thrust > 0) {
                advance += (node.DeltaV.magnitude / (thrust / f.Vessel.GetTotalMass())) / 2;
                // add 1 second for the throttle down time @ the end of the burn
                advance += 1;
            }

            var newNode = new ManeuverCommand()
            {
                Node = node,
                TimeStamp = node.UT - advance,
            };
            return newNode;
        }

        /// <summary>
        /// Find the maneuver node by the saved node id (index id of the maneuver list)
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="fc">Current FlightComputer</param>
        /// <returns>true if loaded successfully, false otherwise.</returns>
        public override bool Load(ConfigNode n, FlightComputer fc)
        {
            if (!base.Load(n, fc))
                return false;

            if (!n.HasValue("NodeIndex"))
                return false;

            NodeIndex = int.Parse(n.GetValue("NodeIndex"));
            RTLog.Notify("Trying to get Maneuver {0}", NodeIndex);
            if (NodeIndex < 0)
                return false;

            // Set the ManeuverNode into this command
            Node = fc.Vessel.patchedConicSolver.maneuverNodes[NodeIndex];
            RTLog.Notify("Found Maneuver {0} with {1} dV", NodeIndex, Node.DeltaV);

            return true;
        }

        /// <summary>
        /// Save the index of the maneuver node to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            // search the node on the List
            NodeIndex = fc.Vessel.patchedConicSolver.maneuverNodes.IndexOf(Node);

            // only save this command if we are on the maneuverNode list
            if (NodeIndex >= 0)
            {
                base.Save(n, fc);
            }
        }

        /// <summary>
        /// This method will be triggered right after the command was enqueued to
        /// the flight computer list.
        /// </summary>
        /// <param name="computer">Current FlightComputer.</param>
        public override void CommandEnqueued(FlightComputer computer)
        {
            var timetoexec = (TimeStamp + ExtraDelay) - RTSettings.Instance.FCLeadTime;

            if (timetoexec - TimeUtil.GameTime >= 0 && RTSettings.Instance.AutoInsertKaCAlerts)
            {
                var kaCAddonLabel = computer.Vessel.vesselName + " Maneuver";

                if (RTCore.Instance != null && RTCore.Instance.KacAddon != null)
                {
                    KaCItemId = RTCore.Instance.KacAddon.CreateAlarm(RemoteTech_KACWrapper.KACWrapper.KACAPI.AlarmTypeEnum.Maneuver, kaCAddonLabel, timetoexec, computer.Vessel.id);
                }
            }

            // also add a maneuver node command to the queue
            computer.Enqueue(AttitudeCommand.ManeuverNode(timetoexec), true, true, true);
        }

        /// <summary>
        /// This method will be triggered after deleting a command from the list.
        /// </summary>
        /// <param name="computer">Current flight computer</param>
        public override void CommandCanceled(FlightComputer computer)
        {
            if (KaCItemId == string.Empty || RTCore.Instance == null || RTCore.Instance.KacAddon == null)
                return;

            // Cancel also the KAC entry
            RTCore.Instance.KacAddon.DeleteAlarm(KaCItemId);
            KaCItemId = string.Empty;
        }
    }
}
