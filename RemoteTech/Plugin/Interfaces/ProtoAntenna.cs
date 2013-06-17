using System;

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
                mParent.moduleValues.SetValue("RTAntennaTarget", value.ToString());
                mParent.Save(mParent.moduleValues);
            }
        }

        private readonly ProtoPartModuleSnapshot mParent;
        private Guid mTarget;

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms) {
            var n = new ConfigNode();
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
            mParent = ppms;
            Vessel = v;
            RTUtil.Log("ProtoAntenna: " + DishRange + ", " + OmniRange + ", " + Name);
        }

        public override String ToString() {
            return "ProtoAntenna {" + mTarget + ", " + DishRange + ", " + OmniRange + ", " +
                   Vessel.vesselName + "}";
        }
    }
}
