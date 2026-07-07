using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RemoteTech.Modules;

namespace RemoteTech
{
    /// <summary>
    /// Class keeping track of RemoteTech antennas.
    /// Acts as a list of antennas managed by RemoteTech.
    /// </summary>
    public class AntennaManager : IDisposable, IEnumerable<IAntenna>
    {
        public event Action<IAntenna> OnRegister = delegate { };
        public event Action<IAntenna> OnUnregister = delegate { };

        public IReadOnlyList<IAntenna> this[ISatellite s] { get { return For(s.Guid); } }
        public IReadOnlyList<IAntenna> this[Vessel v] { get { return For(v.id); } }
        public IReadOnlyList<IAntenna> this[Guid g] { get { return For(g); } }

        private readonly Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly Dictionary<Guid, List<ProtoAntenna>> mProtoAntennaCache =
            new Dictionary<Guid, List<ProtoAntenna>>();

        public AntennaManager()
        {
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);

            OnRegister += a => RTLog.Notify("AntennaManager: OnRegister({0})", a.Name);
            OnUnregister += a => RTLog.Notify("AntennaManager: OnUnregister({0})", a.Name);
        }

        public void Dispose()
        {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);

            foreach (List<ProtoAntenna> antennas in mProtoAntennaCache.Values)
            {
                foreach (ProtoAntenna antenna in antennas)
                {
                    antenna.Dispose();
                }
            }
            mProtoAntennaCache.Clear();
        }

        public void Register(Guid key, IAntenna antenna)
        {
            RTLog.Notify("AntennaManager: Register({0})", antenna);

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
            RTLog.Notify("AntennaManager: Unregister({0})", antenna);

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

                Vessel vessel = RTUtil.GetVesselById(key);
                if (vessel != null)
                {
                    // trigger the onRails on more time
                    // to reregister the antenna as a protoSat
                    this.OnVesselGoOnRails(vessel);
                }
            }
        }

        public void RegisterProtos(Vessel v)
        {
            Guid key = v.id;
            RTLog.Notify("AntennaManager: RegisterProtos({0}, {1})", v.vesselName, key);

            if (mLoadedAntennaCache.ContainsKey(key)) return;

            foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules.Where(ppms => ppms.IsAntenna()))
                {
                    if (!mProtoAntennaCache.ContainsKey(key))
                    {
                        mProtoAntennaCache[key] = new List<ProtoAntenna>();
                    }
                    ProtoAntenna proto = new ProtoAntenna(v, pps, ppms);
                    mProtoAntennaCache[key].Add(proto);
                    OnRegister(proto);
                }
            }
        }

        public void UnregisterProtos(Guid key)
        {
            RTLog.Notify("AntennaManager: UnregisterProtos({0})", key);

            if (!mProtoAntennaCache.ContainsKey(key)) return;

            foreach (ProtoAntenna a in mProtoAntennaCache[key])
            {
                OnUnregister.Invoke(a);
                a.Dispose();
            }

            mProtoAntennaCache.Remove(key);
        }

        private IReadOnlyList<IAntenna> For(Guid key)
        {
            if (mLoadedAntennaCache.ContainsKey(key))
                return mLoadedAntennaCache[key];
            if (mProtoAntennaCache.ContainsKey(key))
                return mProtoAntennaCache[key];
            return [];
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

        public static bool IsAntenna(this PartModule pm) => pm is IAntenna;
    }
}