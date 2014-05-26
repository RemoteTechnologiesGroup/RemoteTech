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
        public override int Priority { get { return 0; } }

        public override string Description
        {
            get
            {
                if (RemainingTime > 0 || RemainingDelta > 0)
                    return "Executing maneuver: " + RemainingDelta.ToString("F2") + "m/s" + Environment.NewLine +
                           "Remaining duration: " + RTUtil.FormatDuration(RemainingTime) + Environment.NewLine + base.Description;
                else
                    return "Execute planned maneuver" + Environment.NewLine + base.Description;
            }
        }

        public override bool Pop(FlightComputer f)
        {
            var burn = f.ActiveCommands.FirstOrDefault(c => c is BurnCommand);
            if (burn != null) f.Remove(burn);
            OriginalDelta = Node.DeltaV.magnitude;
            RemainingDelta = Node.GetBurnVector(f.Vessel.orbit).magnitude;
            RemainingTime = RemainingDelta / (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass());
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (RemainingDelta > 0)
            {
                var forward = Node.GetBurnVector(f.Vessel.orbit).normalized;
                var up = (f.SignalProcessor.Vessel.Body.Position - f.SignalProcessor.Vessel.Position).normalized;
                var orientation = Quaternion.LookRotation(forward, up);
                FlightCore.HoldOrientation(fcs, f, orientation);
                fcs.mainThrottle = 1.0f;
                RemainingTime -= TimeWarp.deltaTime;
                RemainingDelta -= (FlightCore.GetTotalThrust(f.Vessel) / f.Vessel.GetTotalMass()) * TimeWarp.deltaTime;
                return false;
            }
            f.Enqueue(AttitudeCommand.Off(), true, true, true);
            return true;
        }

        public static ManeuverCommand WithNode(ManeuverNode node)
        {
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
                TimeStamp = node.UT,
            };
            return newNode;
        }
    }
}
