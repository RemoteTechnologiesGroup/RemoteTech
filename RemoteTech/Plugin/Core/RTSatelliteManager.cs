using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTSatelliteManager : IEnumerable<ISatellite>, IDisposable {
        public delegate void RTSatelliteHandler(ISatellite satellite);

        public event RTSatelliteHandler Registered;
        public event RTSatelliteHandler Unregistered;

        public int Count { get { return mSatelliteCache.Count; } }

        Dictionary<Guid, ISatellite> mSatelliteCache;
        Dictionary<Guid, List<ISignalProcessor>> mLoadedSpuCache;
        RTCore mCore;

        public RTSatelliteManager(RTCore core) {
            mSatelliteCache = new Dictionary<Guid, ISatellite>();
            mLoadedSpuCache = new Dictionary<Guid, List<ISignalProcessor>>();
            mCore = core;
            GameEvents.onVesselCreate.Add(OnVesselCreate);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Add(OnVesselOnRails);
        }

        public ISatellite For(Guid key) {
            return mSatelliteCache.ContainsKey(key) ? mSatelliteCache[key] : null;
        }

        public Guid Register(Guid key, ISignalProcessor spu) {
            RTUtil.Log("SatelliteManager: Register: " + key + ", " + spu);
            if (!mLoadedSpuCache.ContainsKey(key)) {
                mLoadedSpuCache[key] = new List<ISignalProcessor>();
                mLoadedSpuCache[key].Add(spu);
                mSatelliteCache[key] = new Satellite(spu);
                OnRegister(mSatelliteCache[key]);
            } else {
                ISignalProcessor instance = mLoadedSpuCache[key].Find(x => x == spu);
                if (instance == null) {
                    mLoadedSpuCache[key].Add(spu);
                }
            }
            return key;
        }

        public void Unregister(Guid key, ISignalProcessor spu) {
            RTUtil.Log("SatelliteManager: Unregister: " + key + ", " + spu);
            if (!mLoadedSpuCache.ContainsKey(key))
                return;

            int instance_id = mLoadedSpuCache[key].FindIndex(x => x == spu);
            if (instance_id != -1) {
                ISatellite sat = mSatelliteCache[key];
                mLoadedSpuCache[key].RemoveAt(instance_id);
                if (mLoadedSpuCache[key].Count == 0) {
                    mLoadedSpuCache.Remove(key);
                    mSatelliteCache.Remove(key);
                    OnUnregister(sat);
                    RegisterProtoFor(spu.Vessel);
                } else {
                    mSatelliteCache[key].SignalProcessor = mLoadedSpuCache[key][0];
                }
            }
        }

        public void RegisterProtoFor(Vessel vessel) {
            RTUtil.Log("SatelliteManager: RegisterProtoFor: " + vessel);
            if (mLoadedSpuCache.ContainsKey(vessel.id))
                return;
            Guid key = vessel.protoVessel.vesselID;
            ISignalProcessor spu = vessel.GetSignalProcessor();
            if(spu != null) {
                mSatelliteCache[key] = new Satellite(spu);
                OnRegister(mSatelliteCache[key]);
            }

        }

        public void UnregisterProtoFor(Vessel vessel) {
            RTUtil.Log("SatelliteManager: RegisterProtoFor: " + vessel);
            Guid key = vessel.protoVessel.vesselID;
            if (mLoadedSpuCache.ContainsKey(key))
                return;
            mSatelliteCache.Remove(key);
        }

        void OnVesselOnRails(Vessel v) {
            if(v.parts.Count == 0) {
                RegisterProtoFor(v);
            }
        }

        void OnVesselCreate(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselCreate: " + v);
        }

        void OnVesselDestroy(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselDestroy" + v);
            UnregisterProtoFor(v);
        }

        void OnRegister(ISatellite satellite) {
            if(Registered != null) {
                Registered(satellite);
            }
        }

        void OnUnregister(ISatellite satellite) {
            if (Unregistered != null) {
                Unregistered(satellite);
            }
        }

        public void Dispose() {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Remove(OnVesselOnRails);
        }

        public IEnumerator<ISatellite> GetEnumerator() {
            return mSatelliteCache.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}
