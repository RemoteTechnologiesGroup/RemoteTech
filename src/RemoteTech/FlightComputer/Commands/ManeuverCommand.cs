using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
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
            if (RemainingDelta > 0)
            {
                var forward = Node.GetBurnVector(f.Vessel.orbit).normalized;
                var up = (f.SignalProcessor.Body.position - f.SignalProcessor.Position).normalized;
                var orientation = Quaternion.LookRotation(forward, up);
                FlightCore.HoldOrientation(fcs, f, orientation);

                double thrustToMass = (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
                if (thrustToMass == 0.0) {
                    EngineActivated = false;
                    return false;
                }

                EngineActivated = true;
                fcs.mainThrottle = 1.0f;
                RemainingTime = RemainingDelta / thrustToMass;
                RemainingDelta -= thrustToMass * TimeWarp.deltaTime;
                return false;
            }
            f.Enqueue(AttitudeCommand.Off(), true, true, true);
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
                NodeIndex = nodeIndex,
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
                int nodeIndex = int.Parse(n.GetValue("NodeIndex"));
                RTLog.Notify("Trying to get Maneuver {0}",nodeIndex);
                // Set the ManeuverNode into this command
                Node = fc.Vessel.patchedConicSolver.maneuverNodes[nodeIndex];
                RTLog.Notify("Found Maneuver {0} with {1} dV", nodeIndex, Node.DeltaV);
            }
        }
    }
}
