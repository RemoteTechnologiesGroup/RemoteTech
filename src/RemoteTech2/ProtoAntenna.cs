using System;
using System.Linq;

namespace RemoteTech
{
    internal class ProtoAntenna : IAntenna
    {
        public String Name { get; private set; }
        public Guid Guid { get; private set; }
        public bool Powered { get; private set; }
        public bool Activated { get; set; }
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
                    n.SetValue("RTAntennaTarget", value.ToString());
                    mProtoModule.Save(n);
                    int i = mProtoPart.modules.FindIndex(x => x == mProtoModule);
                    if (i != -1)
                    {
                        mProtoPart.modules[i] = new ProtoPartModuleSnapshot(n);
                    }
                }

            }
        }

        public float Dish { get; private set; }
        public double Radians { get; private set; }
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
                Dish = Single.Parse(n.GetValue("RTDishRange"));
                Radians = Double.Parse(n.GetValue("RTDishRadians"));
                Omni = Single.Parse(n.GetValue("RTOmniRange"));
                Powered = Boolean.Parse(n.GetValue("IsRTPowered"));
                Activated = Boolean.Parse(n.GetValue("IsRTActive"));
            }
            catch (ArgumentException)
            {
                mDishTarget = Guid.Empty;
                Dish = 0.0f;
                Radians = 1.0f;
                Omni = 0.0f;
                RTUtil.Log("ProtoAntenna(Name: {0}) parsing error. Default values substituted.", v.vesselName);
            }
            RTUtil.Log("ProtoAntenna(Name: {0}, Dish: {1}, Omni: {2}, Target: {3}, Radians: {4})", v.vesselName, Dish, Omni, Target, Radians);
        }

        public ProtoAntenna(String name, Guid guid, float omni)
        {
            Name = name;
            Guid = guid;
            Omni = omni;
            Dish = 0;
            Target = Guid.Empty;
            Radians = 1.0f;
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
    }
}