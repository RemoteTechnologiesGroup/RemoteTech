using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTAntennaManager : IEnumerable<IAntenna>, IDisposable {
        public delegate void RTAntennaHandler(IAntenna satellite);

        public event RTAntennaHandler Registered;
        public event RTAntennaHandler Unregistered;

        //TODO: Return dummy antennas for unloaded vessels!
        Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache;
        Dictionary<Guid, List<IAntenna>> mProtoAntennaCache;
        RTCore mCore;

        public RTAntennaManager(RTCore core) {
            mLoadedAntennaCache = new Dictionary<Guid, List<IAntenna>>();
            mProtoAntennaCache = new Dictionary<Guid, List<IAntenna>>();
            mCore = core;
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        public IEnumerable<IAntenna> For(Vessel v) {
            RTUtil.Log("AntennaManager: For: " + v);
            Guid key = v.id;

            if(v.loaded) {
                return mLoadedAntennaCache.ContainsKey(key) ? mLoadedAntennaCache[key] : Enumerable.Empty<IAntenna>();
            } else {
                return mProtoAntennaCache.ContainsKey(key) ? mProtoAntennaCache[key] : Enumerable.Empty<IAntenna>();
            }
        }

        public void RegisterProtoFor(Vessel v) {
            RTUtil.Log("AntennaManager: RegisterProtoFor: " + v);
            Guid key = v.id;

            if (!mProtoAntennaCache.ContainsKey(key)) {
                mProtoAntennaCache[key] = new List<IAntenna>();
            }

            mProtoAntennaCache[key].Clear();

            foreach(ProtoPartModuleSnapshot ppms in v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Where(x => x.IsAntenna())) {
                IAntenna protoAntenna = new ProtoAntenna(v, ppms);
                if(protoAntenna != null) { // Initialisation can fail.
                    mProtoAntennaCache[key].Add(protoAntenna);
                    OnRegister(protoAntenna);
                }
            }
        }

        void UnregisterProtoFor(Vessel v) {
            RTUtil.Log("AntennaManager: UnregisterProtoFor: " + v);
            Guid key = v.id;

            if (!mProtoAntennaCache.ContainsKey(key))
                return;

            foreach(IAntenna a in mProtoAntennaCache[key]) {
                OnUnregister(a);
            }

            mProtoAntennaCache.Remove(key);
        }

        public Guid Register(Vessel v, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Register: " + v + ", " + antenna.Name);
            Guid key = v.protoVessel.vesselID;
            if (!mLoadedAntennaCache.ContainsKey(key)) {
                mLoadedAntennaCache[key] = new List<IAntenna>();
            }
            IAntenna instance = mLoadedAntennaCache[key].Find(x => x == antenna);
            if (instance == null) {
                instance = antenna;
                mLoadedAntennaCache[key].Add(antenna);
                OnRegister(antenna);
            }
            return key;
        }

        public void Unregister(Guid key, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Unregister: " + key + ", " + antenna.Name);
            if (!mLoadedAntennaCache.ContainsKey(key))
                return;

            int instance_id = mLoadedAntennaCache[key].FindIndex(x => x == antenna);

            if (instance_id != -1) {
                OnUnregister(mLoadedAntennaCache[key][instance_id]);
                mLoadedAntennaCache[key].RemoveAt(instance_id);
                if(mLoadedAntennaCache[key].Count == 0) {
                    mLoadedAntennaCache.Remove(key);
                    RegisterProtoFor(antenna.Vessel);
                }
            }
        }

        void OnVesselLoaded(Vessel v) {
            RTUtil.Log("AntennaManager: OnVesselLoaded: " + v);
            if (v.HasSignalProcessor()) {
                RTUtil.Log("AntennaManager: loaded " + v.vesselName);
                RegisterProtoFor(v);
            }
        }

        void OnVesselDestroy(Vessel v) {
            RTUtil.Log("AntennaManager: OnVesselDestroy: " + v);
            UnregisterProtoFor(v);
        }

        void OnVesselModified(Vessel v) {
            RTUtil.Log("AntennaManager: OnVesselModified: " + v);
            if (!v.HasSignalProcessor()) {
                UnregisterProtoFor(v);
            } else {
                RegisterProtoFor(v);
            }
        }

        void OnRegister(IAntenna antenna) {
            if (Registered != null) {
                Registered(antenna);
            }
        }

        void OnUnregister(IAntenna antenna) {
            if (Unregistered != null) {
                Unregistered(antenna);
            }
        }

        public void Dispose() {
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        }

        public IEnumerator<IAntenna> GetEnumerator() {
            return mLoadedAntennaCache.Values.SelectMany(x => x).Concat(mProtoAntennaCache.Values.SelectMany(x => x)).GetEnumerator();

        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
