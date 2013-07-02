using System;

namespace RemoteTech {
    public class DelayedFlightCtrlState : DelayedCommand, IComparable<DelayedFlightCtrlState> {
        public FlightCtrlState State { get; private set; }

        public DelayedFlightCtrlState(FlightCtrlState fcs, double time) {
            State = new FlightCtrlState();
            State.CopyFrom(fcs);

            TimeStamp = time;
            ExtraDelay = 0;
        }

        public int CompareTo(DelayedFlightCtrlState dfcs) {
            return TimeStamp.CompareTo(dfcs.TimeStamp);
        }
    }
}