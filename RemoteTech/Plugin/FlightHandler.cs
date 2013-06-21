using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class FlightHandler : IDisposable {
        internal class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState> {
            public FlightCtrlState State { get; private set; }
            public double Time { get; private set; }

            public DelayedFlightCtrlState(FlightCtrlState fcs, double time) {
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
            double currentTime = RTUtil.GetGameTime();
            VesselSatellite vs;
            if ((vs = mCore.Satellites.For(mVessel)) != null) {
                if (!vs.Connection.Start.LocalControl) {
                    mBuffer.Enqueue(
                        new DelayedFlightCtrlState(fcs, currentTime + vs.Connection.Delay));
                    fcs.Neutralize();

                    if (vs.Connection.Exists) {
                        while (mBuffer.Peek().Time < currentTime) {
                            FlightCtrlState delayed = mBuffer.Dequeue().State;
                            fcs.CopyFrom(delayed);
                        }
                    }
                }
            }
        }

        public void OnVesselChange(Vessel v) {
            if (mVessel != null) {
                mVessel.OnFlyByWire -= this.OnFlyByWire;
            }
            if (v != null) {
                mVessel = v;
                mVessel.OnFlyByWire = this.OnFlyByWire + mVessel.OnFlyByWire;
                FlightCommand fc = new FlightCommand(0.0f).WithAttitude(ReferenceFrame.North,
                                                                           FlightAttitude.Surface);
                fc.Direction = new Vector3(45, 0, 0);
                fc.AddDurationBurn(1.0f, 60);
                mCore.Satellites.For(v).FlightComputer.Enqueue(fc);
            }
        }

        public void Dispose() {
            OnVesselChange(null);
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
    }


}
