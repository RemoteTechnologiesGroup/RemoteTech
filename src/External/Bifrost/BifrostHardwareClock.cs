using System;

namespace RemoteTech {
    public class GenericClock : Tomato.Hardware.Device {
        public override uint DeviceID { get { return 0x12d0b402; } }
        public override uint ManufacturerID { get { return 0; } }
        public override ushort Version { get { return 1; } }
        public override string FriendlyName { get { return "Generic Clock"; } }

        private UInt16 mRate;
        private UInt16 mElapsedHardwareTicks;
        private UInt16 mElapsedTicks;
        private UInt16 mMessage;

        public override int HandleInterrupt() {
            switch (AttachedCPU.A) {
                case 0:
                    mRate = AttachedCPU.B;
                    mElapsedTicks = 0;
                    return 0;
                case 1:
                    AttachedCPU.C = mElapsedTicks;
                    return 0;
                case 2:
                    mMessage = AttachedCPU.B;
                    return 0;
                default:
                    return 0;
            }
        }

        public override void Reset() {
            mElapsedTicks = mMessage = mRate = 0;
        }

        public override void Tick() {
            mElapsedHardwareTicks++;
            if (mElapsedHardwareTicks >= mRate) {
                mElapsedHardwareTicks = 0;
                mElapsedTicks++;
                if (mMessage != 0)
                    AttachedCPU.FireInterrupt(mMessage);
            }
        }
    }
}