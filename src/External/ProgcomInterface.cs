using System;
using System.Reflection;
using System.Collections;
using DeftTech.DuckTyping;
using UnityEngine;

namespace RemoteTech {
    public interface IProgcomCPU {
        int ClockRate { get; }
        bool enabledInterruptNum(int i);
        bool enabledInterrupts();
        int getEX();
        int[] Memory { get; }
        ushort PC { get; set; }
        int[] Registers { get; set; }
        void reset();
        void spawnException(int i);
        int tick();
    }

    public interface IProgcomAssembler {
        int[] assemble(string fileName, int loadLocation);
        void bindGlobalCall(string s, int i);
    }

    public interface IProgcomMonitor {
        void draw();
        int[] getDefaultColors();
        uint[] getDefaultFont();
        void update();
    }

    public interface IProgcomSerialBus {
        void connect(IProgcomSerial sBus);
        void disconnect();
        bool isOccupied();
        bool ready();
        void rec_bit(bool bit);
        void rec_send_done();
        void rec_sending();
        void startSend();
        void tick(int ticks);
    }

    public interface IProgcomSerial {
        void rec_bit(bool bit);
        void rec_sending();
        void rec_send_done();
        bool ready();
        void connect(IProgcomSerialBus sBus);
        void disconnect();
        void tick(int ticks);
    }

    public static class ProgcomSupport {
        private static Type mCPUType;
        private static Type mMonitorType;
        private static Type mAssemblerType;

        public static bool IsProgcomLoaded { 
            get {
                LoadTypes();
                return mCPUType != null && 
                       mMonitorType != null && 
                       mAssemblerType != null;
            }
        }

        private static void LoadTypes() {
            if (mCPUType == null || mMonitorType == null || mAssemblerType == null) {
                Assembly progcomAssembly = null;
                foreach (AssemblyLoader.LoadedAssembly la in AssemblyLoader.loadedAssemblies) {
                    if (la.assembly.GetName().Name == "ProgCom") {
                        progcomAssembly = la.assembly;
                    }
                }
                if (progcomAssembly == null)
                    return;
                mCPUType = progcomAssembly.GetType("ProgCom.CPUem");
                mMonitorType = progcomAssembly.GetType("ProgCom.Monitor");
                mAssemblerType = progcomAssembly.GetType("ProgCom.Assembler2");
            }
        }
        
        public static IProgcomCPU CreateCPU() {
            LoadTypes();
            if (mCPUType == null) {
                throw new MissingReferenceException("Could not find ProgCom.CPUem");
            }
            object cpu = Activator.CreateInstance(mCPUType);
            return DuckTyping.Cast<IProgcomCPU>(cpu);
        }

        public static IProgcomMonitor CreateMonitor(
                    Int32[] arr, UInt16 ptr, UInt16 chars, UInt16 colPtr, UInt16 modePointer) {
            LoadTypes();
            if (mMonitorType == null) {
                throw new MissingReferenceException("Could not find ProgCom.Monitor");
            }
            object mon = Activator.CreateInstance(mMonitorType, 
                arr, ptr, chars, colPtr, modePointer);

            mMonitorType.GetField("visible", 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(mon, true);

            return DuckTyping.Cast<IProgcomMonitor>(mon);
        }

        public static IProgcomAssembler GetCompatibleAssembler(this IProgcomCPU cpu) {
            LoadTypes();
            if (mAssemblerType == null) {
                throw new MissingReferenceException("Could not find ProgCom.Assembler2");
            }
            object assembler = mCPUType.InvokeMember("getCompatibleAssembler", 
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null, DuckTyping.Uncast(cpu), null);
            return DuckTyping.Cast<IProgcomAssembler>(assembler);
        }

        public static Texture2D GetMonitorTexture(this IProgcomMonitor mon) {
            FieldInfo image = mMonitorType.GetField("image", 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (image == null)
                return null;
            return (Texture2D) image.GetValue(DuckTyping.Uncast(mon));
        }
    }
}