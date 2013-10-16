using System;
using System.Linq;

namespace RemoteTech
{
    internal class ProtoAntenna : IAntenna
    {
        public String Name { get; private set; }
        public Guid Guid { get { return mVessel.id; } }
        public bool Powered { get { return true; } }
        public bool Activated { get { return true; } }

        public bool CanTarget { get { return mDishRange != -1; } }

        public Guid Target
        {
            get { return mDishTarget; }
            set
            {
                mDishTarget = value;
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

        public Dish CurrentDish { get { return new Dish(mDishTarget, mDishRadians, mDishRange); } }
        public float CurrentOmni { get; private set; }

        private readonly ProtoPartSnapshot mProtoPart;
        private readonly ProtoPartModuleSnapshot mProtoModule;
        private readonly Vessel mVessel;

        private Guid mDishTarget;
        private readonly float mDishRange;
        private readonly double mDishRadians;

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms)
        {
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            Name = p.partInfo.title;
            mVessel = v;
            mProtoPart = p;
            mProtoModule = ppms;
            try
            {
                mDishTarget = new Guid(n.GetValue("RTAntennaTarget"));
                mDishRange = Single.Parse(n.GetValue("RTDishRange"));
                mDishRadians = Double.Parse(n.GetValue("RTDishRadians"));
                CurrentOmni = Single.Parse(n.GetValue("RTOmniRange"));
            }
            catch (ArgumentException)
            {
                mDishTarget = Guid.Empty;
                mDishRange = 0.0f;
                mDishRadians = 1.0f;
                CurrentOmni = 0.0f;
                RTUtil.Log("ProtoAntenna(Name: {0}) parsing error. Default values substituted.", v.vesselName);
            }
            RTUtil.Log("ProtoAntenna(Name: {0}, Dish: {1}, OmniRange: {2})", v.vesselName, CurrentDish.ToString(), v.vesselName, CurrentOmni.ToString("F2"));
        }
    }
}