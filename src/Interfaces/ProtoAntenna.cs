using System;
using System.Linq;

namespace RemoteTech {
    internal class ProtoAntenna : IAntenna {
        public String Name { get; private set; }
        public bool Powered { get { return true; } }
        public bool Activated { get { return true; } }

        public bool CanTarget { get { return CurrentDishRange != -1; } }
        private Guid mTarget;
        public Guid DishTarget {
            get { return mTarget; }
            set {
                mTarget = value;
                ConfigNode n = new ConfigNode();
                mProtoModule.Save(n);
                n.SetValue("RTAntennaTarget", value.ToString());
                int i = mProtoPart.modules.FindIndex(x => x == mProtoModule);
                if (i != -1) {
                    mProtoPart.modules[i] = new ProtoPartModuleSnapshot(n);
                }
            }
        }
        public double DishFactor { get; private set; }

        public float CurrentDishRange { get; private set; }
        public float CurrentOmniRange { get; private set; }
        public float CurrentConsumption { get { return 0.0f; } }

        public ISatellite Owner { get { return RTCore.Instance.Satellites[mVessel]; } }

        private readonly ProtoPartSnapshot mProtoPart;
        private readonly ProtoPartModuleSnapshot mProtoModule;
        private readonly Vessel mVessel;

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms) {
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            Name = p.partInfo.title;
            mVessel = v;
            mProtoPart = p;
            mProtoModule = ppms;
            try {
                mTarget = new Guid(n.GetValue("RTAntennaTarget"));
                CurrentDishRange = Single.Parse(n.GetValue("RTDishRange"));
                DishFactor = Double.Parse(n.GetValue("RTDishFactor"));
                CurrentOmniRange = Single.Parse(n.GetValue("RTOmniRange"));
            }
            catch (ArgumentException) {
                mTarget = Guid.Empty;
                CurrentDishRange = 0.0f;
                DishFactor = 1.0f;
                CurrentOmniRange = 0.0f;
                RTUtil.Log("ProtoAntenna parsing error. Default values assumed.");
            }
            RTUtil.Log("ProtoAntenna: DishRange: {0}, OmniRange: {1}, Name: {2}, DishTarget: {3}", 
                CurrentDishRange, CurrentOmniRange, v.vesselName, DishTarget);
        }
    }
}
