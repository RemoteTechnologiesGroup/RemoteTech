using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {

    public class ProgcomIO : IProgcomSerial {
        private class DelayedCommand {
            public double TimeStamp { get; set; }

            public int? Numpad { get; set; }
            public char? Keyboard { get; set; }
            public Int32[] Program { get; set; }

            public DelayedCommand() {
                TimeStamp = RTUtil.GetGameTime();
            }

            public int CompareTo(DelayedCommand dc) {
                return TimeStamp.CompareTo(dc.TimeStamp);
            }
        }

        public const int Enter = 10;
        public const int Plus  = 11;
        public const int Minus = 12;
        public const int Clear = 13;
        public const int Reset = 14;
        public const int Off   = 15;

        public String Console {
            get { return mConsole.ToString(); }
        }

        public int Output1 {
            get { return mProgcom.Memory[mOffset + OUTPUT1]; }
        }

        public int Output2 {
            get { return mProgcom.Memory[mOffset + OUTPUT2]; }
        }

        public int Output3 {
            get { return mProgcom.Memory[mOffset + OUTPUT3]; }
        }

        public int Output4 {
            get { return mProgcom.Memory[mOffset + OUTPUT4]; }
        }

        public int OutputMsg {
            get { return mProgcom.Memory[mOffset + OUTPUT_MSG]; }
        }

        public int Numpad {
            get { return mNumpad; }
            set {
                if (value < 10) {
                    mNumpad *= 10;
                    mNumpad += value; 
                } else if (value == Plus) {
                    mNumpad = (mNumpad < 0) ? -mNumpad : mNumpad;
                } else if (value == Minus) {
                    mNumpad = -mNumpad;
                } else if (value == Clear) {
                    mNumpad = 0;
                } else if (value == Enter) {
                    DelayedCommand dc = new DelayedCommand() { Numpad = mNumpad };
                    dc.TimeStamp += mProgcom.Delay;
                    mQueue.Enqueue(dc);
                    mNumpad = 0;
                }
                
            }
        }

        public Texture2D Monitor {
            get {
                if(Event.current.type == EventType.Repaint) {
                    mMonitor.update();
                }
                return mMonitorTexture;
            }
        }

        private const int MAX_LINES = 20;
        private const int OUTPUT1 = 0;
        private const int OUTPUT2 = 1;
        private const int OUTPUT3 = 2;
        private const int OUTPUT4 = 3;
        private const int OUTPUT_MSG = 4;
        private const int INPUT_NUM = 5;

        private int mNumpad;
        private readonly PriorityQueue<DelayedCommand> mQueue = new PriorityQueue<DelayedCommand>();
        private readonly IProgcomMonitor mMonitor;
        private readonly Texture2D mMonitorTexture;
        private readonly StringBuilder mConsole = new StringBuilder();
        private readonly ProgcomUnit mProgcom;
        private readonly int mOffset;
        private IProgcomSerialBus mBus;
        private int mBitOffset;
        private bool mSending;

        public ProgcomIO(ProgcomUnit pcu, int offset, IProgcomSerialBus bus) {
            mProgcom = pcu;
            mOffset = offset;
            mBus = bus;
            mMonitor = CreateMonitor();
            mMonitorTexture = mMonitor.GetMonitorTexture();

            Log("Cleared console.");
        }

        public void Tick() {
            PopQueue();
        }

        public void Upload(String fileName) {
            Log("Loading program \"{0}\".", fileName);
            Int32[] newCode;
            try {
                newCode = mProgcom.Assemble(fileName);
            } catch (FormatException) {
                Log("ERROR: Compilation failed.");
                return;
            } catch (Exception) {
                Log("ERROR: Unknown error.");
                return;
            }
            Log("Successfully assembled.");
            Log("Transmitting program (ETA: {0})", RTUtil.FormatDuration(mProgcom.Delay));
            DelayedCommand dc = new DelayedCommand() { Program = newCode };
            dc.TimeStamp += mProgcom.Delay;
            mQueue.Enqueue(dc);
        }

        public void Keyboard(KeyCode key, bool up) {

        }

        public void Log(String line) {
            mConsole.AppendLine(line);
        }

        public void Log(String message, params System.Object[] param) {
            Log(String.Format(message, param));
        }

        public void connect(IProgcomSerialBus bus) {
            mBus = bus;
        }

        public void disconnect() {
            mBus = null;
        }

        public void rec_bit(bool b) { }
        public void rec_send_done() { }
        public void rec_sending() { }
        public bool ready() { return false; }

        public void tick(int c) {
            /*while (c > 0 && sending && mBus.ready()) {
                connected.rec_bit(((charSend) & (1 << bitSend)) != 0);
                ++bitSend;
                if (bitSend == 32) {
                    sending = false;
                    bitSend = 0;
                    mBus.rec_send_done();
                }
                --c;
            }*/
        }

        private void LoadProgram(Int32[] newCode) {
            if (ProgcomUnit.EXEC_OFFSET + newCode.Length > mProgcom.Memory.Length) {
                Log("ERROR: Program too large to fit in memory.");
                return;
            }

            Log("Writing...");
            for (int i = 0; i < newCode.Length; i++) {
                mProgcom.Memory[ProgcomUnit.EXEC_OFFSET + i] = newCode[i];
            }
            Log("Program loaded at offset {0}", ProgcomUnit.EXEC_OFFSET);
        }

        private void PopQueue() {
            while (mQueue.Count > 0 && mQueue.Peek().TimeStamp < RTUtil.GetGameTime()) {
                DelayedCommand dc = mQueue.Dequeue();
                if (dc.Program != null) {
                    LoadProgram(dc.Program);
                }

                if (dc.Numpad != null) {
                    mProgcom.Memory[mOffset + INPUT_NUM] = dc.Numpad ?? 0;
                    mProgcom.Memory[mOffset + INPUT_NUM + 1] = 1;
                }
            }
        }

        private IProgcomMonitor CreateMonitor() {
            IProgcomMonitor monitor = ProgcomSupport.CreateMonitor(mProgcom.Memory, 
                                                          ProgcomUnit.MONITOR_OFFSET,
                                                          ProgcomUnit.FONT_OFFSET, 
                                                          ProgcomUnit.CLR_OFFSET, 
                                                          ProgcomUnit.MONITOR_MODE);

            int i = ProgcomUnit.FONT_OFFSET;
            foreach (UInt32 font in monitor.getDefaultFont()) {
                mProgcom.Memory[i] = (Int32)font;
                ++i;
            }
            i = ProgcomUnit.CLR_OFFSET;
            foreach (Int32 col in monitor.getDefaultColors()) {
                mProgcom.Memory[i] = col;
                ++i;
            }

            return monitor;
        }
    }

}
