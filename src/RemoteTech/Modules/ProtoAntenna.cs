using System;
using System.Linq;
using RemoteTech.SimpleTypes;

namespace RemoteTech.Modules
{
    internal class ProtoAntenna : IAntenna, IDisposable
    {
        public String Name { get; private set; }
        public Guid Guid { get => mState.Guid; private set => mState.Guid = value; }
        public bool Powered { get => mState.Powered; private set => mState.Powered = value; }
        public bool Activated { get => mState.Activated; set => mState.Activated = value; }
        public bool Connected { get { return RTCore.Instance.Network.IsAntennaConnected(this); } }
        public float Consumption { get => mState.Consumption; private set => mState.Consumption = value; }

        public bool CanTarget { get { return Dish != -1; } }

        public Guid Target
        {
            get { return mState.Target; }
            set
            {
                mState.Target = value;
                if (mProtoModule != null)
                {
                    mProtoModule.moduleValues.SetValue("RTAntennaTarget", value.ToString());
                    int i = mProtoPart.modules.FindIndex(x => x == mProtoModule);
                    if (i != -1)
                    {
                        mProtoPart.modules[i] = new ProtoPartModuleSnapshot(mProtoModule.moduleValues);
                    }
                }

            }
        }

        public float Dish { get => mState.Dish; private set => mState.Dish = value; }
        public double CosAngle { get => mState.CosAngle; private set => mState.CosAngle = value; }
        public float Omni { get => mState.Omni; private set => mState.Omni = value; }

        private readonly ProtoPartSnapshot mProtoPart;
        private readonly ProtoPartModuleSnapshot mProtoModule;
        private readonly AntennaState mState = new();

        public ProtoAntenna(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms)
        {
            Name = p.partInfo.title;
            Consumption = 0;
            Guid = v.id;
            mProtoPart = p;
            mProtoModule = ppms;

            try
            {
                mState.Target = new Guid(ppms.moduleValues.GetValue("RTAntennaTarget"));
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is OverflowException)
            {
                mState.Target = Guid.Empty;
            }

            float dish = 0f;
            if (ppms.moduleValues.TryGetValue("RTDishRange", ref dish))
                Dish = dish;

            double cosAngle = 0f;
            if (ppms.moduleValues.TryGetValue("RTDishCosAngle", ref cosAngle))
                CosAngle = cosAngle;

            float omni = 0f;
            if (ppms.moduleValues.TryGetValue("RTOmniRange", ref omni))
                Omni = omni;

            bool powered = false;
            if (ppms.moduleValues.TryGetValue("IsRTPowered", ref powered))
                Powered = powered;

            bool activated = false;
            if (ppms.moduleValues.TryGetValue("IsRTActive", ref activated))
                Activated = activated;

            mState.CanTarget = Dish != -1;
            mState.Register();

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
            mState.CanTarget = CanTarget;
            mState.Register();
        }

        public void Dispose()
        {
            mState.Dispose();
        }

        public void OnConnectionRefresh()
        {
            mState.Connected = Connected;
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
