using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class GenericKeyboard : Tomato.Hardware.Device {
        public override uint DeviceID { get { return 0x30cf7406; } }
        public override ushort Version { get { return 1; } }
        public override uint ManufacturerID { get { return 0; } }
        public override string FriendlyName { get { return "Generic Keyboard"; } }

        private Queue<UInt16> mBuffer = new Queue<UInt16>();
        private List<UInt16> mActiveKeys = new List<UInt16>();
        private UInt16 mMessage = 0;

        public override int HandleInterrupt() {
            switch (AttachedCPU.A) {
                case 0:
                    mBuffer.Clear();
                    break;
                case 1:
                    lock (mBuffer) {
                        if (mBuffer.Count != 0)
                            AttachedCPU.C = mBuffer.Dequeue();
                        else
                            AttachedCPU.C = 0;
                    }
                    break;
                case 2:
                    lock (mActiveKeys) {
                        if (mActiveKeys.Contains(AttachedCPU.B))
                            AttachedCPU.C = 1;
                        else
                            AttachedCPU.C = 0;
                        break;
                    }
                case 3:
                    mMessage = AttachedCPU.B;
                    break;
            }
            return 0;
        }

        public void HandleKeyEvent() {
            if (Event.current.isKey) {
                UInt16 key = 0;
                switch (Event.current.keyCode) {
                    case KeyCode.Backspace:
                        key = 0x10;
                        break;
                    case KeyCode.Return:
                        key = 0x11;
                        break;
                    case KeyCode.Insert:
                        key = 0x12;
                        break;
                    case KeyCode.Delete:
                        key = 0x13;
                        break;
                    case KeyCode.UpArrow:
                        key = 0x80;
                        break;
                    case KeyCode.DownArrow:
                        key = 0x81;
                        break;
                    case KeyCode.LeftArrow:
                        key = 0x82;
                        break;
                    case KeyCode.RightArrow:
                        key = 0x83;
                        break;
                    case KeyCode.LeftShift:
                    case KeyCode.RightShift:
                        key = 0x90;
                        break;
                    case KeyCode.LeftControl:
                    case KeyCode.RightControl:
                    case KeyCode.LeftCommand:
                    case KeyCode.RightCommand:
                        key = 0x91;
                        break;
                    default:
                        if ((int) Event.current.keyCode >= 0x20 && 
                                (int) Event.current.keyCode < 0x80) {
                            key = (UInt16)Event.current.keyCode;
                        }
                        break;
                }
                if (key != 0) {
                    if (Event.current.type == EventType.KeyUp) {
                        RTUtil.Log("Up: {0}", key);
                        lock (mActiveKeys) {
                            mActiveKeys.Remove(key);
                        }
                    } else if (Event.current.type == EventType.KeyDown) {
                        RTUtil.Log("Down: {0}", key);
                        lock (mBuffer) {
                            mBuffer.Enqueue(key);
                        }
                        lock (mActiveKeys) {
                            if (!mActiveKeys.Contains(key)) {
                                mActiveKeys.Add(key);
                            }
                        }
                        if (mMessage != 0) {
                            AttachedCPU.FireInterrupt(mMessage);
                        }
                    }
                }
                Event.current.Use();
            }
        }

        public override void Tick() {
            return;
        }

        public override void Reset() {
            mMessage = 0;
            mBuffer.Clear();
            mActiveKeys.Clear();
        }
    }
}