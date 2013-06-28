using System;
using System.Linq;

namespace RemoteTech {
    internal class ProtoAntenna : IAntenna {
        public bool CanTarget { get { return DishRange != -1; } }

        public string Name { get; private set; }
        public float DishRange { get; private set; }
        public double DishFactor { get; private set; }
        public float OmniRange { get; private set; }
        public float Consumption { get; private set; }
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
            try {
                Name = p.partInfo.title;
                mTarget = new Guid(n.GetValue("RTAntennaTarget"));
                DishRange = Single.Parse(n.GetValue("RTDishRange"));
                DishFactor = Double.Parse(n.GetValue("RTDishFactor"));
                OmniRange = Single.Parse(n.GetValue("RTOmniRange"));
                Consumption = 0.0f;
            }
            catch (ArgumentException) {
                throw new ArgumentException("Malformed argument. Could not read values.");
            }
            catch (FormatException) {
                mTarget = Guid.Empty;
            }
            mProtoPart = p;
            mProtoModule = ppms;
            Vessel = v;
            RTUtil.Log("ProtoAntenna: " + DishRange + ", " + OmniRange + ", " + Name);
        }

        public override String ToString() {
            return "ProtoAntenna {" + mTarget + ", " + DishRange + ", " + OmniRange + ", " +
                   Vessel.vesselName + "}";
        }
    }
}
