using System;
using System.Linq;

namespace RemoteTech {
    internal class ProtoAntenna : IAntenna {
        public bool CanTarget { get { return DishRange != -1; } }

        public string Name { get; private set; }
        public float DishRange { get; private set; }
        public double DishFactor { get; private set; }
        public float OmniRange { get; private set; }
        public Vessel Vessel { get; private set; }

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

        private readonly ProtoPartSnapshot mProtoPart;
        private readonly ProtoPartModuleSnapshot mProtoModule;
        private Guid mTarget;

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms) {
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            Name = p.partInfo.title;
            try {
                mTarget = new Guid(n.GetValue("RTAntennaTarget"));
                DishRange = Single.Parse(n.GetValue("RTDishRange"));
                DishFactor = Double.Parse(n.GetValue("RTDishFactor"));
                OmniRange = Single.Parse(n.GetValue("RTOmniRange"));
            }
            catch (ArgumentException) {
                mTarget = Guid.Empty;
                DishRange = 0.0f;
                DishFactor = 1.0f;
                OmniRange = 0.0f;
            }

            mProtoPart = p;
            mProtoModule = ppms;
            Vessel = v;
            RTUtil.Log("ProtoAntenna: DishRange: {0}, OmniRange: {1}, Name: {2}, DishTarget: {3})", 
                DishRange, OmniRange, Vessel.vesselName, DishTarget);
        }
    }
}
