using System;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class BifrostUnit : IDisposable {
        public const int Enter = 10;
        public const int Plus = 11;
        public const int Minus = 12;
        public const int Clear = 13;
        public const int RST = 14;
        public const int Off = 15;

        public void Log(String line) {
            mConsole.AppendLine(line);
        }

        public void Log(String message, params System.Object[] param) {
            Log(String.Format(message, param));
        }

        public String Console { get { return mConsole.ToString(); } }
        public GenericKeyboard Keyboard { get; private set; }
        public GenericClock Clock { get; private set; }
        public LEM1802 Monitor { get; private set; }
        public Tomato.Hardware.Device PilotController { get; private set; }
        public Tomato.Hardware.Device MetricsController { get; private set; }
        public double Delay { get { return mSignalProcessor.FlightComputer.Delay; } }

        private readonly ISignalProcessor mSignalProcessor;
        public readonly BifrostCPUWorker mCPU;
        private readonly StringBuilder mConsole = new StringBuilder();

        public BifrostUnit(ISignalProcessor s) {
            mSignalProcessor = s;
            mCPU = new BifrostCPUWorker();

            Keyboard = new GenericKeyboard();
            Clock = new GenericClock();
            Monitor = new LEM1802();

            mCPU.CPU.ConnectDevice(Keyboard);
            mCPU.CPU.ConnectDevice(Clock);
            mCPU.CPU.ConnectDevice(Monitor);
        }

        public void Dispose() {
            if(mCPU != null) {
                mCPU.Dispose();
            }
        }

        public void OnFixedUpdate() {
            return;
        }

        public void Upload(String fileName) {
            byte[] data;
            try {
                data = KSP.IO.File.ReadAllBytes<RTCore>(fileName);
            } catch (KSP.IO.IOException) {
                Log("Could not find.");
                return;
            }
            mCPU.CPU.FlashMemory(ConvertToShorts(data));
            Log("Successfully programmed.");
        }

        private static ushort[] ConvertToShorts(byte[] bytes) {
            var result = new ushort[bytes.Length / 2];
            if (BitConverter.IsLittleEndian) {
                for (var i = 0; i < bytes.Length / 2; i++) {
                    var old = bytes[2 * i];
                    bytes[2 * i] = bytes[2 * i + 1];
                    bytes[2 * i + 1] = old;
                }
            }
            for (var i = 0; i < result.Length; i++)
                result[i] = BitConverter.ToUInt16(bytes, i * 2);
            return result;
        }

        public void Run() {
            mCPU.CPU.Reset();
            mCPU.Enabled = true;
        }

        public void Reset() {
            Pause();
            mCPU.CPU.Reset();
        }

        public void Resume() {
            mCPU.Enabled = true;
        }

        public void Pause() {
            mCPU.Enabled = false;
        }
    }
}