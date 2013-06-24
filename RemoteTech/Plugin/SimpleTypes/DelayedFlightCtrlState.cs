using System;

namespace RemoteTech {
    public class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState> {
        public FlightCtrlState State { get; private set; }
        public double EffectiveFrom { get; set; }

        public DelayedFlightCtrlState(FlightCtrlState fcs, double time) {
            State = new FlightCtrlState();
            State.CopyFrom(fcs);
            EffectiveFrom = time;
        }

        public int CompareTo(DelayedFlightCtrlState fcs) {
            return this.EffectiveFrom.CompareTo(fcs.EffectiveFrom);
        }
    }
}