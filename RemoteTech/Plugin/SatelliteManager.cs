using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace RemoteTech {
    public class SatelliteManager : IEnumerable<VesselSatellite>, IDisposable {
        public delegate void SatelliteHandler(VesselSatellite satellite);

        public event SatelliteHandler Registered;
        public event SatelliteHandler Unregistered;

        public int Count { get { return mSatelliteCache.Count; } }

        public IEnumerable<ISatellite> FindCommandStations() {
            foreach (var list in mLoadedSpuCache.Values) {
                foreach (var spu in list) {
                    if (spu.CommandStation) {
                        yield return mSatelliteCache[spu.Guid];
                        break;
                    }
                }
            }
        } 

        private readonly Dictionary<Guid, List<ISignalProcessor>> mLoadedSpuCache =
            new Dictionary<Guid, List<ISignalProcessor>>();
        private readonly Dictionary<Guid, VesselSatellite> mSatelliteCache =
            new Dictionary<Guid, VesselSatellite>();
        private readonly RTCore mCore;

        public SatelliteManager(RTCore core) {
            mCore = core;
            GameEvents.onVesselCreate.Add(OnVesselCreate);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Add(OnVesselOnRails);
        }

        public VesselSatellite For(Vessel v) {
            return v == null ? null : For(v.id);
        }

        public VesselSatellite For(Guid key) {
            return mSatelliteCache.ContainsKey(key) ? mSatelliteCache[key] : null;
        }

        public Guid Register(Vessel v, ISignalProcessor spu) {
            Guid key = v.id;
            RTUtil.Log("SatelliteManager: Register: " + key + ", " + spu);
            if (!mLoadedSpuCache.ContainsKey(key)) {
                mLoadedSpuCache[key] = new List<ISignalProcessor>();
            }
            if (mLoadedSpuCache[key].Count == 0) {
                if (mSatelliteCache.ContainsKey(key)) {
                    UnregisterProtoFor(v);
                }
                mSatelliteCache[key] = new VesselSatellite(spu);
                OnRegister(mSatelliteCache[key]);
            }
            ISignalProcessor instance = mLoadedSpuCache[key].Find(x => x == spu);
            if (instance == null) {
                mLoadedSpuCache[key].Add(spu);

            }
            return key;
        }

        public void Unregister(Guid key, ISignalProcessor spu) {
            RTUtil.Log("SatelliteManager: Unregister: " + key + ", " + spu);
            if (!mLoadedSpuCache.ContainsKey(key)) return;

            int instance_id = mLoadedSpuCache[key].FindIndex(x => x == spu);
            if (instance_id != -1) {
                VesselSatellite sat = mSatelliteCache[key];
                mLoadedSpuCache[key].RemoveAt(instance_id);
                if (mLoadedSpuCache[key].Count == 0) {
                    mSatelliteCache.Remove(key);
                    mLoadedSpuCache.Remove(key);
                    OnUnregister(sat);
                }
                else {
                    mSatelliteCache[key].SignalProcessor = mLoadedSpuCache[key][0];
                }
            }
        }

        public void RegisterProtoFor(Vessel vessel) {
            RTUtil.Log("SatelliteManager: RegisterProtoFor: " + vessel);
            if (mLoadedSpuCache.ContainsKey(vessel.id) && 
                    mLoadedSpuCache[vessel.id].Count > 0) return;
            Guid key = vessel.protoVessel.vesselID;
            ISignalProcessor spu = vessel.GetSignalProcessor();
            if (spu != null) {
                mSatelliteCache[key] = new VesselSatellite(spu);
                OnRegister(mSatelliteCache[key]);
            }
        }

        public void UnregisterProtoFor(Vessel vessel) {
            RTUtil.Log("SatelliteManager: UnregisterProtoFor: " + vessel);
            Guid key = vessel.protoVessel.vesselID;
            if (mLoadedSpuCache.ContainsKey(vessel.id) &&
                    mLoadedSpuCache[vessel.id].Count > 0) return;
            mSatelliteCache.Remove(key);
        }

        private void OnVesselOnRails(Vessel v) {
            if (v.parts.Count == 0) {
                RegisterProtoFor(v);
            }
        }

        private void OnVesselCreate(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselCreate: " + v);
        }

        private void OnVesselDestroy(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselDestroy" + v);
            UnregisterProtoFor(v);
        }

        private void OnRegister(VesselSatellite vs) {
            if (Registered != null) {
                Registered(vs);
            }
        }

        private void OnUnregister(VesselSatellite vs) {
            if (Unregistered != null) {
                Unregistered(vs);
                vs.Dispose();
            }
        }

        public void Dispose() {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Remove(OnVesselOnRails);
        }

        public IEnumerator<VesselSatellite> GetEnumerator() {
            return mSatelliteCache.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
