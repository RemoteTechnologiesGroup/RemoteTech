using System;
using System.Linq;

namespace RemoteTech.Modules
{
    internal class ProtoAntenna : IAntenna
    {
        public String Name { get; private set; }
        public Guid Guid { get; private set; }
        public bool Powered { get; private set; }
        public bool Activated { get; set; }
        public bool Connected { get { return RTCore.Instance.Network.Graph [Guid].Any (l => l.Interfaces.Contains (this)); } }
        public float Consumption { get; private set; }

        public bool CanTarget { get { return Dish != -1; } }

        public Guid Target
        {
            get { return mDishTarget; }
            set
            {
                mDishTarget = value;
                if (mProtoModule != null)
                {
                    ConfigNode n = new ConfigNode();
                    mProtoModule.Save(n);
                    n.SetValue("RTAntennaTarget", value.ToString());
                    int i = mProtoPart.modules.FindIndex(x => x == mProtoModule);
                    if (i != -1)
                    {
                        mProtoPart.modules[i] = new ProtoPartModuleSnapshot(n);
                    }
                }

            }
        }

        public float Dish { get; private set; }
        public double CosAngle { get; private set; }
        public float Omni { get; private set; }

        private readonly ProtoPartSnapshot mProtoPart;
        private readonly ProtoPartModuleSnapshot mProtoModule;

        private Guid mDishTarget;

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms)
        {
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            Name = p.partInfo.title;
            Consumption = 0;
            Guid = v.id;
            mProtoPart = p;
            mProtoModule = ppms;
            try
            {
                mDishTarget = new Guid(n.GetValue("RTAntennaTarget"));
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is OverflowException)
            {
                mDishTarget = Guid.Empty;
            }
            double temp_double;
            float temp_float;
            bool temp_bool;
            Dish = Single.TryParse(n.GetValue("RTDishRange"), out temp_float) ? temp_float : 0.0f;
            CosAngle = Double.TryParse(n.GetValue("RTDishCosAngle"), out temp_double) ? temp_double : 0.0;
            Omni = Single.TryParse(n.GetValue("RTOmniRange"), out temp_float) ? temp_float : 0.0f;
            Powered = Boolean.TryParse(n.GetValue("IsRTPowered"), out temp_bool) ? temp_bool : false;
            Activated = Boolean.TryParse(n.GetValue("IsRTActive"), out temp_bool) ? temp_bool : false;

            RTLog.Notify(ToString());
        }

        public ProtoAntenna(String name, Guid guid, float omni)
        {
            Name = name;
            Guid = guid;
            Omni = omni;
            Dish = 0;
            Target = Guid.Empty;
            CosAngle = 1.0f;
            Activated = true;
            Powered = true;
        }

        public void OnConnectionRefresh()
        {
            ;
        }

        public int CompareTo(IAntenna antenna)
        {
            return Consumption.CompareTo(antenna.Consumption);
        }

        public override string ToString()
        {
            return String.Format("ProtoAntenna(Name: {0}, Guid: {1}, Dish: {2}, Omni: {3}, Target: {4}, CosAngle: {5})", Name, Guid, Dish, Omni, Target, CosAngle);
        }
    }
}