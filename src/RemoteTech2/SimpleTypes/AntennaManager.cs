using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


namespace RemoteTech
{
    public class AntennaManager : IDisposable, IEnumerable<IAntenna>
    {
        public event Action<IAntenna> OnRegister = delegate { };
        public event Action<IAntenna> OnUnregister = delegate { };

        public IEnumerable<IAntenna> this[ISatellite s] { get { return For(s.Guid); } }
        public IEnumerable<IAntenna> this[Vessel v] { get { return For(v.id); } }
        public IEnumerable<IAntenna> this[Guid g] { get { return For(g); } }

        private readonly Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly Dictionary<Guid, List<IAntenna>> mProtoAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();

        public AntennaManager()
        {
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);

            OnRegister += a => RTUtil.Log("AntennaManager: OnRegister({0})", a.Name);
            OnUnregister += a => RTUtil.Log("AntennaManager: OnUnregister({0})", a.Name);
        }

        public void Dispose()
        {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
        }

        public void Register(Guid key, IAntenna antenna)
        {
            RTUtil.Log("AntennaManager: Register({0}, {1})", key, antenna.Name);

            if (!mLoadedAntennaCache.ContainsKey(key))
            {
                mLoadedAntennaCache[key] = new List<IAntenna>();
            }

            IAntenna instance = mLoadedAntennaCache[key].Find(a => a == antenna);
            if (instance == null)
            {
                if (mProtoAntennaCache.ContainsKey(key))
                {
                    UnregisterProtos(key);
                }

                mLoadedAntennaCache[key].Add(antenna);
                OnRegister.Invoke(antenna);
            }
        }

        public void Unregister(Guid key, IAntenna antenna)
        {
            RTUtil.Log("AntennaManager: Unregister({0}, {1})", key, antenna.Name);

            if (!mLoadedAntennaCache.ContainsKey(key)) return;

            int instance_id = mLoadedAntennaCache[key].FindIndex(x => x == antenna);
            if (instance_id != -1)
            {
                mLoadedAntennaCache[key].RemoveAt(instance_id);
                if (mLoadedAntennaCache[key].Count == 0)
                {
                    mLoadedAntennaCache.Remove(key);
                }
                OnUnregister(antenna);
            }
        }

        public void RegisterProtos(Vessel v)
        {
            Guid key = v.id;
            RTUtil.Log("AntennaManager: RegisterProtos({0}, {1})", key, v.vesselName);

            if (mLoadedAntennaCache.ContainsKey(key)) return;

            foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules.Where(ppms => ppms.IsAntenna()))
                {
                    if (!mProtoAntennaCache.ContainsKey(key))
                    {
                        mProtoAntennaCache[key] = new List<IAntenna>();
                    }
                    ProtoAntenna proto = new ProtoAntenna(v, pps, ppms);
                    mProtoAntennaCache[key].Add(proto);
                    OnRegister(proto);
                }
            }
        }

        public void UnregisterProtos(Guid key)
        {
            RTUtil.Log("AntennaManager: UnregisterProtos({0})", key);

            if (!mProtoAntennaCache.ContainsKey(key)) return;

            foreach (IAntenna a in mProtoAntennaCache[key])
            {
                OnUnregister.Invoke(a);
            }

            mProtoAntennaCache.Remove(key);
        }

        private IEnumerable<IAntenna> For(Guid key)
        {
            if (mLoadedAntennaCache.ContainsKey(key))
            {
                return mLoadedAntennaCache[key];
            }
            if (mProtoAntennaCache.ContainsKey(key))
            {
                return mProtoAntennaCache[key];
            }
            return Enumerable.Empty<IAntenna>();
        }

        private void OnVesselGoOnRails(Vessel v)
        {
            if (v.parts.Count == 0)
            {
                RegisterProtos(v);
            }
        }

        public IEnumerator<IAntenna> GetEnumerator()
        {
            return mLoadedAntennaCache.Values.SelectMany(l => l).Concat(
                   mProtoAntennaCache.Values.SelectMany(l => l)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static partial class RTUtil
    {
        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTAntenna") &&
                   ppms.GetBool("IsRTPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsAntenna(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTAntenna") &&
                   pm.Fields.GetValue<bool>("IsRTPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
        }
    }
}