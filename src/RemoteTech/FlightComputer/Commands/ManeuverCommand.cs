using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech.FlightComputer.Commands
{
    public class ManeuverCommand : AbstractCommand
    {
        // Index id of this maneuver node from patchedConicSolver.maneuverNodes list
        [Persistent] public int NodeIndex;
        public double OriginalDelta;
        public double RemainingTime;
        public double RemainingDelta;
        public ManeuverNode Node;
        public bool EngineActivated { get; private set; }
        public override int Priority { get { return 0; } }

        private double throttle = 1.0f;
        private double lowestDeltaV = 0.0;
        private bool abortOnNextExecute = false;

        public override string Description
        {
            get
            {
                if (RemainingTime > 0 || RemainingDelta > 0)
                {
                    string flightInfo = "Executing maneuver: " + RemainingDelta.ToString("F2") +
                                        "m/s" + Environment.NewLine + "Remaining duration: ";

                    flightInfo += this.EngineActivated ? RTUtil.FormatDuration(RemainingTime) : "-:-";

                    return flightInfo + Environment.NewLine + base.Description;
                }
                else
                    return "Execute planned maneuver" + Environment.NewLine + base.Description;
            }
        }
        public override string ShortName { get { return "Execute maneuver node"; } }

        public override bool Pop(FlightComputer f)
        {
            var burn = f.ActiveCommands.FirstOrDefault(c => c is BurnCommand);
            if (burn != null) {
                f.Remove (burn);
            }

            OriginalDelta = Node.DeltaV.magnitude;
            RemainingDelta = this.getRemainingDeltaV(f);
            this.EngineActivated = true;

            double thrustToMass = FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass();
            if (thrustToMass == 0.0) {
                this.EngineActivated = false;
                RTUtil.ScreenMessage("[Flight Computer]: No engine to carry out the maneuver.");
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
        private double getRemainingDeltaV(FlightComputer computer)
        {
            return this.Node.GetBurnVector(computer.Vessel.orbit).magnitude;
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
            if (this.RemainingDelta <= 0.01 || this.abortOnNextExecute)
            {    
                computer.Enqueue(AttitudeCommand.KillRot(), true, true, true);
                return true;
            }

            // Orientate vessel to maneuver prograde
            var forward = Node.GetBurnVector(computer.Vessel.orbit).normalized;
            var up = (computer.SignalProcessor.Body.position - computer.SignalProcessor.Position).normalized;
            var orientation = Quaternion.LookRotation(forward, up);
            FlightCore.HoldOrientation(ctrlState, computer, orientation);

            // This represents the theoretical acceleration but is off by a few m/s^2, probably because some parts are partially physicsless
            double thrustToMass = (FlightCore.GetTotalThrust(computer.Vessel) / computer.Vessel.GetTotalMass());
            // We need to know if the engine was activated or not to show the proper info text in the command
            if (thrustToMass == 0.0)
            {
                this.EngineActivated = false;
                return false;
            }
            this.EngineActivated = true;

            // Before any throttling, those two values may differ from after the throttling took place
            this.RemainingDelta = this.getRemainingDeltaV(computer);
            this.RemainingTime = this.RemainingDelta / thrustToMass;

            // In case we would overpower with 100% thrust, calculate how much we actually need and set it.
            if (computer.Vessel.acceleration.magnitude > this.RemainingDelta)
            {
                // Formula which leads to this: a = ( vE � vS ) / dT
                this.throttle = this.RemainingDelta / computer.Vessel.acceleration.magnitude;
            }
                
            ctrlState.mainThrottle = (float)this.throttle;

            // TODO: THIS CAN PROBABLY BE REMOVED? RemainingDelta = this.getRemainingDeltaV(computer);

            // After throttling, the remaining time differs from beforehand (dividing delta by throttled thrustToMass)
            this.RemainingTime = this.RemainingDelta / (ctrlState.mainThrottle * thrustToMass);

            // We need to abort if the remaining delta was already low enough so it only takes exactly one more tick!
            double ticksRemaining = this.RemainingTime / TimeWarp.deltaTime;

            if (ticksRemaining <= 1)
            {
                this.throttle *= ticksRemaining;
                ctrlState.mainThrottle = (float)this.throttle;
                this.abortOnNextExecute = true;
                return false;
            }

            // we only compare up to the fiftieth part due to some burn-up delay when just firing up the engines
            if (this.lowestDeltaV > 0 // Do ignore the first tick
                && (this.RemainingDelta - 0.02) > this.lowestDeltaV)
            {
                // Aborting because deltaV was rising again!
                computer.Enqueue(AttitudeCommand.KillRot(), true, true, true);
                return true;
            }

            // Lowest delta always has to be stored to be able to compare it in the next tick
            if (this.lowestDeltaV == 0 // Always do it on the first tick
                || this.RemainingDelta < this.lowestDeltaV)
            {
                this.lowestDeltaV = this.RemainingDelta;
            }

            return false;
        }

        /// <summary>
        /// Returns the total time for this burn in seconds
        /// </summary>
        /// <param name="f">Flightcomputer for the current vessel</param>
        /// <returns>max burn time</returns>
        public double getMaxBurnTime(FlightComputer f)
        {
            if (Node == null) return 0;

            return Node.DeltaV.magnitude / (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
        }

        public static ManeuverCommand WithNode(int nodeIndex, FlightComputer f)
        {
            double thrust = FlightCore.GetTotalThrust(f.Vessel);
            ManeuverNode node = f.Vessel.patchedConicSolver.maneuverNodes[nodeIndex];
            double advance = f.Delay;

            if (thrust > 0) {
                advance += (node.DeltaV.magnitude / (thrust / f.Vessel.GetTotalMass())) / 2;
            }

            var newNode = new ManeuverCommand()
            {
                Node = node,
                TimeStamp = node.UT - advance,
            };
            return newNode;
        }

        /// <summary>
        /// Find the maneuver node by the saved node id (index id of the meneuver list)
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="fc">Current flightcomputer</param>
        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n,fc);
            if(n.HasValue("NodeIndex"))
            {
                this.NodeIndex = int.Parse(n.GetValue("NodeIndex"));
                RTLog.Notify("Trying to get Maneuver {0}", this.NodeIndex);
                if (this.NodeIndex >= 0)
                {
                    // Set the ManeuverNode into this command
                    this.Node = fc.Vessel.patchedConicSolver.maneuverNodes[this.NodeIndex];
                    RTLog.Notify("Found Maneuver {0} with {1} dV", this.NodeIndex, this.Node.DeltaV);
                }
            }
        }

        /// <summary>
        /// Save the index of the maneuver node to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            // search the node on the List
            this.NodeIndex = fc.Vessel.patchedConicSolver.maneuverNodes.IndexOf(this.Node);

            // only save this command if we are on the maneuverNode list
            if (this.NodeIndex >= 0)
            {
                base.Save(n, fc);
            }
        }
    }
}
