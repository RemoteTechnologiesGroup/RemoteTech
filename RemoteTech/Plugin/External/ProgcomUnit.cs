using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RemoteTech {
#if PROGCOM
    public class ProgcomUnit {
        public ProgcomIO IO { get; private set; }
        public Int32[] Memory { get { return mCPU.Memory; } }
        public double Delay { get { return mSatellite.FlightComputer.Delay; } }

        public const int VEC_ACC_OFFSET = 41;
        public const int SPD_ACC_OFFSET = 43;
        public const int MONITOR_MODE = 42;
        public const int MONITOR_OFFSET = 63488;
        public const int FONT_OFFSET = 65024;
        public const int CLR_OFFSET = 63472;
        public const int EXEC_OFFSET = 128;
 
        private readonly VesselSatellite mSatellite;
        private readonly ProgCom.Assembler2 mAssembler;
        private readonly ProgCom.CPUem mCPU;

        private bool mEnabled = false;
        private int mCyclesPending = 0;

        public ProgcomUnit(VesselSatellite vs) {
            mSatellite = vs;
            mCPU = CreateCPU();
            mAssembler = CreateAssembler();

            IO = new ProgcomIO(this, 32, null);
        }

        public void Tick() {
            if (!mEnabled) return;
            // interrupt for updated flight data.
            if (mCPU.enabledInterruptNum(2)) {
                mCPU.spawnException(257);
            }
            // interrupt for keypad input.
            if (mCPU.enabledInterruptNum(3) && mCPU.Memory[38] != 0) {
                mCPU.spawnException(259);
            }

            float time = TimeWarp.deltaTime;
            mCyclesPending += (int)(mCPU.ClockRate * time);
            mCyclesPending -= Execute(mCyclesPending);
        }

        public Int32[] Assemble(String fileName) {
            return mAssembler.assemble(fileName, EXEC_OFFSET);
        }

        public void Run() {
            mEnabled = true;
            mCPU.PC = EXEC_OFFSET;
        }

        public void Reset() {
            Pause();
            mCPU.reset();
        }

        public void Resume() {
            mEnabled = true;
        }

        public void Pause() {
            mEnabled = false;
        }

        private int Execute(int cycles) {
            int elapsed = 0;
            while (elapsed < cycles) {
                int duration = mCPU.tick();
                if (duration == 0) {
                    mEnabled = false;
                } else if (duration == -1) {
                    IO.Log("ERROR: Illegal instruction({0}) at address {1}.", 
                        mCPU.Memory[mCPU.PC], mCPU.PC - 1);
                    mEnabled = false;
                }
                elapsed += duration;
            }
            return elapsed;
        }

        private static ProgCom.CPUem CreateCPU() {
            ProgCom.CPUem cpu = new ProgCom.CPUem();
            cpu.Memory[41] = 1024; // vector precision
            cpu.Memory[43] = 16; // speed precision
            return cpu;
        }

        private ProgCom.Assembler2 CreateAssembler() {
            ProgCom.Assembler2 assembler = mCPU.getCompatibleAssembler();
            assembler.bindGlobalCall("GLOBAL_MAINTHROTTLE", 0);
            assembler.bindGlobalCall("GLOBAL_YAW", 1);
            assembler.bindGlobalCall("GLOBAL_PITCH", 2);
            assembler.bindGlobalCall("GLOBAL_ROLL", 3);
            assembler.bindGlobalCall("GLOBAL_SURFACE_EAST", 4);
            assembler.bindGlobalCall("GLOBAL_SURFACE_UP", 7);
            assembler.bindGlobalCall("GLOBAL_SURFACE_NORTH", 10);
            assembler.bindGlobalCall("GLOBAL_VESSEL_X", 13);
            assembler.bindGlobalCall("GLOBAL_VESSEL_Y", 16);
            assembler.bindGlobalCall("GLOBAL_VESSEL_HEADING", 16);
            assembler.bindGlobalCall("GLOBAL_VESSEL_Z", 19);
            assembler.bindGlobalCall("GLOBAL_ORBITSPEED", 22);
            assembler.bindGlobalCall("GLOBAL_SURFACESPEED", 25);
            assembler.bindGlobalCall("GLOBAL_ANGULARVELOCITY", 28);
            assembler.bindGlobalCall("GLOBAL_ALTITUDE", 31);
            assembler.bindGlobalCall("GLOBAL_NUMPAD_OUT", 32);
            assembler.bindGlobalCall("GLOBAL_NUMPAD_MSG", 36);
            assembler.bindGlobalCall("GLOBAL_NUMPAD_IN", 37);
            assembler.bindGlobalCall("GLOBAL_NUMPAD_NEWIN", 38);
            assembler.bindGlobalCall("GLOBAL_NUMPAD_FORMAT", 39);
            assembler.bindGlobalCall("GLOBAL_TIMER", 40);
            assembler.bindGlobalCall("GLOBAL_VECTORACCURACY", VEC_ACC_OFFSET);
            assembler.bindGlobalCall("GLOBAL_SPEEDACCURACY", SPD_ACC_OFFSET);
            assembler.bindGlobalCall("GLOBAL_IENABLE", 44);
            assembler.bindGlobalCall("GLOBAL_CLOCK", 45);
            assembler.bindGlobalCall("GLOBAL_IADRESS", 46);
            assembler.bindGlobalCall("GLOBAL_TIMER_MAX", 47);
            assembler.bindGlobalCall("GLOBAL_PILOT_THROTTLE", 48);
            assembler.bindGlobalCall("GLOBAL_PILOT_YAW", 49);
            assembler.bindGlobalCall("GLOBAL_PILOT_PITCH", 50);
            assembler.bindGlobalCall("GLOBAL_PILOT_ROLL", 51);
            assembler.bindGlobalCall("GLOBAL_PILOT_RCS_RIGHT", 52);
            assembler.bindGlobalCall("GLOBAL_PILOT_RCS_UP", 53);
            assembler.bindGlobalCall("GLOBAL_PILOT_RCS_FORWARD", 54);
            assembler.bindGlobalCall("GLOBAL_RCS_RIGHT", 52);
            assembler.bindGlobalCall("GLOBAL_RCS_UP", 53);
            assembler.bindGlobalCall("GLOBAL_RCS_FORWARD", 54);
            assembler.bindGlobalCall("GLOBAL_ACTIONGROUP", 55);

            assembler.bindGlobalCall("GLOBAL_GSB0", 64);
            assembler.bindGlobalCall("GLOBAL_GSB1", 68);
            assembler.bindGlobalCall("GLOBAL_GSB2", 72);
            assembler.bindGlobalCall("GLOBAL_GSB3", 76);
            assembler.bindGlobalCall("GLOBAL_GSB4", 80);
            assembler.bindGlobalCall("GLOBAL_GSB5", 84);
            assembler.bindGlobalCall("GLOBAL_GSB6", 88);
            assembler.bindGlobalCall("GLOBAL_GSB7", 92);

            assembler.bindGlobalCall("GLOBAL_SCREEN_MODE", MONITOR_MODE);
            assembler.bindGlobalCall("GLOBAL_SCREEN", MONITOR_OFFSET);
            assembler.bindGlobalCall("GLOBAL_SCREEN_COLOR", CLR_OFFSET);
            assembler.bindGlobalCall("GLOBAL_SCREEN_FONT", FONT_OFFSET);

            assembler.bindGlobalCall("CPU_CLOCKRATE", mCPU.ClockRate);
            assembler.bindGlobalCall("CPU_RAM", mCPU.Memory.Length);
            assembler.bindGlobalCall("CPU_MAXADDRESS", mCPU.Memory.Length - 1);

            return assembler;
        }

        private void UpdateMemoryMap(FlightCtrlState fcs) {
            Vector3d position = mSatellite.Vessel.findWorldCenterOfMass();

            Vector3d eastUnit = mSatellite.Vessel.mainBody.getRFrmVel(position).normalized;
            Vector3d upUnit = (position - mSatellite.Vessel.mainBody.position).normalized;
            Vector3d northUnit = Vector3d.Cross(upUnit, eastUnit);

            Vector3d orbitalVelocity = mSatellite.Vessel.GetObtVelocity();
            Vector3d surfaceVelocity = mSatellite.Vessel.GetSrfVelocity();
            Vector3d angularVelocity = mSatellite.Vessel.angularVelocity;

            Vector3d shipEastUnit = mSatellite.Vessel.transform.TransformDirection(Vector3d.right);
            Vector3d shipUpUnit = mSatellite.Vessel.transform.TransformDirection(Vector3d.up);
            Vector3d shipFwdUnit = mSatellite.Vessel.transform.TransformDirection(Vector3d.forward);

            double altitude = mSatellite.Vessel.altitude;

            Int32[] mem = mCPU.Memory;
            int vectorAccuracy = mem[VEC_ACC_OFFSET];
            float speedAccuracy = mem[SPD_ACC_OFFSET] >= 0 ? (float) mem[SPD_ACC_OFFSET]
                                                           : (float) -1.0f / mem[SPD_ACC_OFFSET];

            mem[4] =  (Int32) (eastUnit.x        * vectorAccuracy);
            mem[5] =  (Int32) (eastUnit.y        * vectorAccuracy);
            mem[6] =  (Int32) (eastUnit.z        * vectorAccuracy);
            mem[7] =  (Int32) (upUnit.x          * vectorAccuracy);
            mem[8] =  (Int32) (upUnit.y          * vectorAccuracy);
            mem[9] =  (Int32) (upUnit.z          * vectorAccuracy);
            mem[10] = (Int32) (northUnit.x       * vectorAccuracy);
            mem[11] = (Int32) (northUnit.y       * vectorAccuracy);
            mem[12] = (Int32) (northUnit.z       * vectorAccuracy);
            mem[13] = (Int32) (shipEastUnit.x    * vectorAccuracy);
            mem[14] = (Int32) (shipEastUnit.y    * vectorAccuracy);
            mem[15] = (Int32) (shipEastUnit.z    * vectorAccuracy);
            mem[16] = (Int32) (shipUpUnit.x      * vectorAccuracy);
            mem[17] = (Int32) (shipUpUnit.y      * vectorAccuracy);
            mem[18] = (Int32) (shipUpUnit.z      * vectorAccuracy);
            mem[19] = (Int32) (shipFwdUnit.x     * vectorAccuracy);
            mem[20] = (Int32) (shipFwdUnit.y     * vectorAccuracy);
            mem[21] = (Int32) (shipFwdUnit.z     * vectorAccuracy);
            mem[22] = (Int32) (orbitalVelocity.x * speedAccuracy);
            mem[23] = (Int32) (orbitalVelocity.y * speedAccuracy);
            mem[24] = (Int32) (orbitalVelocity.z * speedAccuracy);
            mem[25] = (Int32) (surfaceVelocity.x * speedAccuracy);
            mem[26] = (Int32) (surfaceVelocity.y * speedAccuracy);
            mem[27] = (Int32) (surfaceVelocity.z * speedAccuracy);
            mem[28] = (Int32) (angularVelocity.x * speedAccuracy);
            mem[29] = (Int32) (angularVelocity.y * speedAccuracy);
            mem[30] = (Int32) (angularVelocity.z * speedAccuracy);
            mem[31] = (Int32) altitude;
            //mem[32] = output 1
            //mem[33] = output 2
            //mem[34] = output 3
            //mem[35] = output 4
            //mem[36] = output msg
            //mem[37] = numpad input
            //mem[38] = boolean used to see if the input has changed
            //mem[39] = output formatting switch
            //mem[40] = timer
            //mem[41] = vectorAccuracy
            //mem[42] = thread id
            //mem[43] = program offset
            //mem[44] = interrupt enable
            //mem[45] = clock
            //mem[46] = interrupt handler adress (64 default)
            //mem[47] = timer interrupt frequency
            mem[48] = (Int32) (fcs.mainThrottle  * 1024.0f);
            mem[49] = (Int32) (fcs.yaw           * 1024.0f);
            mem[50] = (Int32) (fcs.pitch         * 1024.0f);
            mem[51] = (Int32) (fcs.roll          * 1024.0f);
            mem[52] = (Int32) (fcs.X             * 1024.0f);
            mem[53] = (Int32) (fcs.Y             * 1024.0f);
            mem[54] = (Int32) (fcs.Z             * 1024.0f);
        }

        public void OnFlyByWire(FlightCtrlState fcs) {
            IO.Tick();
            if (!mEnabled)
                return;
            UpdateMemoryMap(fcs);
            Tick();
            Actuate(fcs);
        }

        private void Actuate(FlightCtrlState fcs) {
            fcs.mainThrottle = RTUtil.Clamp(mCPU.Memory[0] / 1024.0f, 0.0f, 1.0f);
            fcs.yaw = RTUtil.Clamp(mCPU.Memory[1] / 1024.0f, -1.0f, 1.0f);
            fcs.pitch = RTUtil.Clamp(mCPU.Memory[2] / 1024.0f, -1.0f, 1.0f);
            fcs.roll = RTUtil.Clamp(mCPU.Memory[3] / 1024.0f, -1.0f, 1.0f);

            fcs.X = RTUtil.Clamp(mCPU.Memory[52] / 1024.0f, -1.0f, 1.0f);
            fcs.Y = RTUtil.Clamp(mCPU.Memory[53] / 1024.0f, -1.0f, 1.0f);
            fcs.Z = RTUtil.Clamp(mCPU.Memory[54] / 1024.0f, -1.0f, 1.0f);

            if (mCPU.Memory[55] != 0) {
                ActivateActionGroup(mCPU.Memory[55]);
                mCPU.Memory[55] = 0;
            }
        }

        private void ActivateActionGroup(int i) {
            ActionGroupList a = mSatellite.Vessel.ActionGroups;
            switch (i) {
                case 1:
                    a.ToggleGroup(KSPActionGroup.Custom01);
                    break;
                case 2:
                    a.ToggleGroup(KSPActionGroup.Custom02);
                    break;
                case 3:
                    a.ToggleGroup(KSPActionGroup.Custom03);
                    break;
                case 4:
                    a.ToggleGroup(KSPActionGroup.Custom04);
                    break;
                case 5:
                    a.ToggleGroup(KSPActionGroup.Custom05);
                    break;
                case 6:
                    a.ToggleGroup(KSPActionGroup.Custom06);
                    break;
                case 7:
                    a.ToggleGroup(KSPActionGroup.Custom07);
                    break;
                case 8:
                    a.ToggleGroup(KSPActionGroup.Custom08);
                    break;
                case 9:
                    a.ToggleGroup(KSPActionGroup.Custom09);
                    break;
                case 10:
                    a.ToggleGroup(KSPActionGroup.Custom10);
                    break;
                case 11:
                    a.ToggleGroup(KSPActionGroup.Abort);
                    break;
                case 12:
                    Staging.ActivateNextStage();
                    break;
            }
        }
    }
#endif
}
