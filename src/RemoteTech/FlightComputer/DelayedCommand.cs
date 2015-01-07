using System;

namespace RemoteTech.FlightComputer
{
    public class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState>
    {
        public FlightCtrlState State { get; private set; }
        public double TimeStamp { get; set; }

        public DelayedFlightCtrlState(FlightCtrlState fcs)
        {
            State = new FlightCtrlState();
            State.CopyFrom(fcs);
            TimeStamp = RTUtil.GameTime;
        }

        public int CompareTo(DelayedFlightCtrlState dfcs)
        {
            return TimeStamp.CompareTo(dfcs.TimeStamp);
        }
    }

    public class DelayedManeuver : IComparable<DelayedManeuver>
    {
        public ManeuverNode Node { get; set; }
        public double TimeStamp { get; set; }

        public DelayedManeuver(ManeuverNode node)
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
            };
            TimeStamp = RTUtil.GameTime;
        }

        public int CompareTo(DelayedManeuver dm)
        {
            return TimeStamp.CompareTo(dm.TimeStamp);
        }
    }
}