using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTech {
    public class AntennaManager : IDisposable {
        public delegate void AntennaHandler(IAntenna satellite);

        public event AntennaHandler Registered;
        public event AntennaHandler Unregistered;

        private readonly Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly Dictionary<Guid, List<IAntenna>> mProtoAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly RTCore mCore;

        public AntennaManager(RTCore core) {
            mCore = core;
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
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
            }
            if (mProtoAntennaCache.ContainsKey(key)) {
                return mProtoAntennaCache[key];
            }
            return Enumerable.Empty<IAntenna>();
        }

        public void RegisterProtoFor(Vessel vessel) {
            RTUtil.Log("AntennaManager: RegisterProtoFor: " + vessel);
            Guid key = vessel.id;

            if (!mProtoAntennaCache.ContainsKey(key)) {
                mProtoAntennaCache[key] = new List<IAntenna>();
            }

            mProtoAntennaCache[key].Clear();

            foreach (ProtoPartSnapshot pps in vessel.protoVessel.protoPartSnapshots) {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules.Where(x => x.IsAntenna())) {
                    IAntenna protoAntenna = new ProtoAntenna(vessel, pps, ppms);
                    if (protoAntenna != null) {
                        // Initialisation can fail.
                        mProtoAntennaCache[key].Add(protoAntenna);
                        OnRegister(protoAntenna);
                    }
                }
            }
        }

        public void UnregisterProtoFor(Vessel vessel) {
            RTUtil.Log("AntennaManager: UnregisterProtoFor: " + vessel);
            Guid key = vessel.id;

            if (!mProtoAntennaCache.ContainsKey(key)) return;

            foreach (IAntenna a in mProtoAntennaCache[key]) {
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
                mLoadedAntennaCache[key].Add(antenna);
                OnRegister(antenna);
            }
            return key;
        }

        public void Unregister(Guid key, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Unregister: " + key + ", " + antenna.Name);
            if (!mLoadedAntennaCache.ContainsKey(key)) return;

            int instance_id = mLoadedAntennaCache[key].FindIndex(x => x == antenna);

            if (instance_id != -1) {
                mLoadedAntennaCache[key].RemoveAt(instance_id);
                if (mLoadedAntennaCache[key].Count == 0) {
                    mLoadedAntennaCache.Remove(key);
                }
                OnUnregister(antenna);
            }
        }

        private void OnVesselGoOnRails(Vessel v) {
            if (v.parts.Count == 0) {
                RegisterProtoFor(v);
            }
        }

        private void OnRegister(IAntenna antenna) {
            if (Registered != null) {
                Registered(antenna);
            }
        }

        private void OnUnregister(IAntenna antenna) {
            if (Unregistered != null) {
                Unregistered(antenna);
            }
        }

        public void Dispose() {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
        }
    }
}
