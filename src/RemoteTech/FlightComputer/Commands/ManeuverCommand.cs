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
        public bool EngineActivated { get; set; }
        public override int Priority { get { return 0; } }

        private double ticksRemaining = -1;
        private float throttle = 1.0f;
        private bool abortOnNextExecute = false;
        private bool firstBurn = true;
        private double lowestDeltaV = 0.0;

        public override string Description
        {
            get
            {
                if (RemainingTime > 0 || RemainingDelta > 0)
                {
                    string flightInfo = "Executing maneuver: " + RemainingDelta.ToString("F2") +
                                        "m/s" + Environment.NewLine + "Remaining duration: ";

                    flightInfo += EngineActivated ? RTUtil.FormatDuration(RemainingTime) : "-:-";

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
            RemainingDelta = Node.GetBurnVector(f.Vessel.orbit).magnitude;
            EngineActivated = true;

            double thrustToMass = FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass();
            if (thrustToMass == 0.0) {
                EngineActivated = false;
                RTUtil.ScreenMessage("[Flight Computer]: No engine to carry out the maneuver.");
            } else {
                RemainingTime = RemainingDelta / thrustToMass;
            }

            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (RemainingDelta > 0.01 && this.abortOnNextExecute == false)
            {
                var forward = Node.GetBurnVector(f.Vessel.orbit).normalized;
                var up = (f.SignalProcessor.Body.position - f.SignalProcessor.Position).normalized;
                var orientation = Quaternion.LookRotation(forward, up);
                FlightCore.HoldOrientation(fcs, f, orientation);

                double thrustToMass = (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
                if (thrustToMass == 0.0)
                {
                    EngineActivated = false;
                    return false;
                }
      
                EngineActivated = true;

                RemainingDelta = Node.GetBurnVector(f.Vessel.orbit).magnitude;
                RemainingTime = RemainingDelta / thrustToMass;

                // In case we would overpower with 100% thrust, calculate how much we actually need and set it.
                if (f.Vessel.acceleration.magnitude > RemainingDelta)
                {
                    // Formula which leads to this: a = ( vE – vS ) / dT
                    this.throttle = (float)RemainingDelta / (float)f.Vessel.acceleration.magnitude;
                }
                

                ticksRemaining = RemainingTime / TimeWarp.deltaTime;
       

                fcs.mainThrottle = this.throttle;


                double throttledThrustToMass = fcs.mainThrottle * thrustToMass;

                RemainingDelta = Node.GetBurnVector(f.Vessel.orbit).magnitude;
                RemainingTime = RemainingDelta / throttledThrustToMass;

                ticksRemaining = RemainingTime / TimeWarp.deltaTime;


                // We need to abort if the remaining delta was already low enough so it only takes exactly one more tick!
                if (ticksRemaining <= 1)
                {
                    fcs.mainThrottle = this.throttle * (float)ticksRemaining;
                    this.abortOnNextExecute = true;
                }

                // we only compare up to the fiftieth part due to some inconsistency when just firing up the engines
                if (this.lowestDeltaV > 0 && (RemainingDelta - 0.02) > this.lowestDeltaV)
                {
                    // Aborting because deltaV was rising again!
                    f.Enqueue(AttitudeCommand.KillRot(), true, true, true);
                    return true;
                }
                if (this.lowestDeltaV == 0 || RemainingDelta < this.lowestDeltaV)
                {
                    this.lowestDeltaV = RemainingDelta;
                }

                return false;
            }

            f.Enqueue(AttitudeCommand.KillRot(), true, true, true);
            return true;
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
