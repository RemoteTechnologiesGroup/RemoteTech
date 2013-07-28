using System;
using System.Collections.Generic;
using System.Threading;

namespace RemoteTech {
    public class BifrostCPUWorker : IDisposable {
        public Tomato.DCPU CPU { get; private set; }
        public Timer Timer { get; private set; }
        public int ExecutionInterval { get; private set; }
        public bool Enabled {
            get {
                return Timer != null;
            }
            set {
                if (Timer != null) {
                    Timer.Dispose();
                }
                if (value == true) {
                    Timer = new Timer(OnTimer, this, 0, ExecutionInterval);
                }
            }
        }

        public BifrostCPUWorker() : this(new Tomato.DCPU()) {}

        public BifrostCPUWorker(Tomato.DCPU cpu) {
            CPU = cpu;
            ExecutionInterval = 1000 / 120;
        }

        private static void OnTimer(Object state_info) {
            var state = (BifrostCPUWorker) state_info;
            int requested_cycles = state.ExecutionInterval * state.CPU.ClockSpeed / 1000;
            state.CPU.Execute(requested_cycles);
        }

        public void Dispose() {
            if (Timer != null) {
                Timer.Dispose();
            }
        }
    }
}