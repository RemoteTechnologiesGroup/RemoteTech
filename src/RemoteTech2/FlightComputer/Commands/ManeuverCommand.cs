using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ManeuverCommand : AbstractCommand
    {
        public ManeuverNode Node { get; set; }
        public double OriginalDelta { get; set; }
        public double RemainingTime { get; set; }
        public double RemainingDelta { get; set; }
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

        public static ManeuverCommand WithNode(ManeuverNode node, FlightComputer f)
        {
            double thrust = FlightCore.GetTotalThrust(f.Vessel);
            double advance = f.Delay;

            if (thrust > 0) {
                advance += (node.DeltaV.magnitude / (thrust / f.Vessel.GetTotalMass())) / 2;
            }

            var newNode = new ManeuverCommand()
            {
                Node = new ManeuverNode()
                {
                    DeltaV = node.DeltaV,
                    patch = node.patch,
                    solver = node.solver,
                    scaledSpaceTarget = node.scaledSpaceTarget,
                    nextPatch = node.nextPatch,
                    UT = node.UT,
                    nodeRotation = node.nodeRotation,
                },
                TimeStamp = node.UT - advance,
            };
            return newNode;
        }
    }
}
