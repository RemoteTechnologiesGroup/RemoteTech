using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    class ProtoAntenna : IAntenna {

        ProtoPartModuleSnapshot mParent;
        Guid mTarget;

        public bool CanTarget { get { return DishRange != -1; } }
        public string Name { get; private set; }
        public float DishRange { get; private set; }
        public float OmniRange { get; private set; }
        public float Consumption { get; private set; }
        public Vessel Vessel { get; private set; }

        public Guid Target {
            get {
                return mTarget;
            }
            set {
                mTarget = value;
                mParent.moduleValues.SetValue("RTAntennaTarget", value.ToString());
            }
        }

        public ProtoAntenna(Vessel v, ProtoPartModuleSnapshot ppms) {
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            try {
                Name = ppms.moduleName;
                mTarget = new Guid(n.GetValue("RTAntennaTarget"));
                DishRange = Single.Parse(n.GetValue("RTDishRange"));
                OmniRange = Single.Parse(n.GetValue("RTOmniRange"));
                Consumption = 0.0f;
            } catch (ArgumentException) {
                throw new ArgumentException("Malformed argument. Could not read values.");
            }
            mParent = ppms;
            Vessel = v;
        }
    }
}
