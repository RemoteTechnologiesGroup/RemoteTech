using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class FlightHandler : IDisposable {
        internal class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState> {
            public readonly FlightCtrlState State;
            public readonly long Time;

            public DelayedFlightCtrlState(FlightCtrlState fcs, long time) {
                State = new FlightCtrlState();
                State.CopyFrom(fcs);
                Time = time;
            }

            public int CompareTo(DelayedFlightCtrlState fcs) {
                return this.Time.CompareTo(fcs.Time);
            }
        }

        private readonly RTCore mCore;
        private readonly PriorityQueue<DelayedFlightCtrlState> mBuffer; 
        private Vessel mVessel;
        
        public FlightHandler(RTCore core) {
            mCore = core;
            mBuffer = new PriorityQueue<DelayedFlightCtrlState>();
            GameEvents.onVesselChange.Add(OnVesselChange);

            OnVesselChange(FlightGlobals.ActiveVessel);
        }

        public void OnFlyByWire(FlightCtrlState fcs) {
            if (mVessel.GetCrewCount() != 0) return;
            
            long currentTime = RTUtil.GetGameTime();
            mBuffer.Enqueue(new DelayedFlightCtrlState(fcs, currentTime + 500));
            if (mBuffer.Peek().Time < currentTime) {
                FlightCtrlState delayed = mBuffer.Dequeue().State;
                fcs.CopyFrom(delayed);
            } else {
                fcs.Neutralize();
            }
        }

        public void OnVesselChange(Vessel v) {
            if (mVessel != null) {
                mVessel.OnFlyByWire -= this.OnFlyByWire;
            }
            mVessel = v;
            mVessel.OnFlyByWire = this.OnFlyByWire + mVessel.OnFlyByWire;
        }

        public void Dispose() {
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
    }


}
