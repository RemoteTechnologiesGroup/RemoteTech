using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTAntennaManager : IDisposable {
        public delegate void RTAntennaHandler(IAntenna satellite);

        public event RTAntennaHandler Registered;
        public event RTAntennaHandler Unregistered;

        Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache;
        Dictionary<Guid, List<IAntenna>> mProtoAntennaCache;
        RTCore mCore;

        public RTAntennaManager(RTCore core) {
            mLoadedAntennaCache = new Dictionary<Guid, List<IAntenna>>();
            mProtoAntennaCache = new Dictionary<Guid, List<IAntenna>>();
            mCore = core;
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
            mCore.Satellites.Registered += OnSatelliteRegistered;
            mCore.Satellites.Unregistered += OnSatelliteUnregistered;
        }

        public IEnumerable<IAntenna> For(Vessel vessel) {
            return For(vessel.id);
        }

        public IEnumerable<IAntenna> For(ISatellite satellite) {
            return For(satellite.Guid);
        }

        public IEnumerable<IAntenna> For(Guid key) {
            if (mLoadedAntennaCache.ContainsKey(key)) {
                return mLoadedAntennaCache[key];
            } else if (mProtoAntennaCache.ContainsKey(key)) {
                return mProtoAntennaCache[key];
            } else {
                return Enumerable.Empty<IAntenna>();
            }
        }

        public void RegisterProtoFor(Vessel vessel) {
            RTUtil.Log("AntennaManager: RegisterProtoFor: " + vessel);
            Guid key = vessel.id;

            if (!mProtoAntennaCache.ContainsKey(key)) {
                mProtoAntennaCache[key] = new List<IAntenna>();
            }

            mProtoAntennaCache[key].Clear();

            foreach(ProtoPartSnapshot pps in vessel.protoVessel.protoPartSnapshots) {
                foreach(ProtoPartModuleSnapshot ppms in pps.modules.Where(x => x.IsAntenna())) {
                    IAntenna protoAntenna = new ProtoAntenna(vessel, pps, ppms);
                    if(protoAntenna != null) { // Initialisation can fail.
                        mProtoAntennaCache[key].Add(protoAntenna);
                        OnRegister(protoAntenna);
                    }
                }
            }
        }

        public void UnregisterProtoFor(Vessel vessel) {
            RTUtil.Log("AntennaManager: UnregisterProtoFor: " + vessel);
            Guid key = vessel.id;

            if (!mProtoAntennaCache.ContainsKey(key))
                return;

            foreach(IAntenna a in mProtoAntennaCache[key]) {
                OnUnregister(a);
            }

            mProtoAntennaCache.Remove(key);
        }

        public Guid Register(Guid key, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Register: " + key + ", " + antenna.Name);
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
                mLoadedAntennaCache[key].RemoveAt(instance_id);
                if(mLoadedAntennaCache[key].Count == 0) {
                    mLoadedAntennaCache.Remove(key);
                }
                OnUnregister(antenna);
            }
        }

        void OnVesselGoOnRails(Vessel v) {
            RegisterProtoFor(v);
        }

        void OnSatelliteRegistered(ISatellite sat) {
            //RegisterProtoFor(sat.mSignalProcessor.Vessel);
        }

        void OnSatelliteUnregistered(ISatellite sat) {
            //UnregisterProtoFor(sat.mSignalProcessor.Vessel);
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
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
            mCore.Satellites.Registered -= OnSatelliteRegistered;
            mCore.Satellites.Unregistered -= OnSatelliteUnregistered;
        }
    }
}
