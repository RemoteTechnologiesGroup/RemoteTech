using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


namespace RemoteTech {
    public class AntennaManager : IDisposable, IEnumerable<IAntenna> {
        public delegate void AntennaHandler(IAntenna satellite);

        public event AntennaHandler Registered;
        public event AntennaHandler Unregistered;

        public IEnumerable<IAntenna> this[ISatellite s] { get { return For(s.Guid); } }
        public IEnumerable<IAntenna> this[Vessel v] { get { return For(v.id); } }
        public IEnumerable<IAntenna> this[Guid g] { get { return For(g); } }

        private readonly Dictionary<Guid, List<IAntenna>> mLoadedAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly Dictionary<Guid, List<IAntenna>> mProtoAntennaCache =
            new Dictionary<Guid, List<IAntenna>>();
        private readonly RTCore mCore;

        public AntennaManager(RTCore core) {
            mCore = core;
            GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
        }

        public void Dispose() {
            GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
        }

        public void Register(Guid key, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Register: {0}, {1}", key, antenna.Name);

            if (mProtoAntennaCache.ContainsKey(key)) {
                // Unregister remaining proto-antennas.
                UnregisterProtos(key);
            }
            if (!mLoadedAntennaCache.ContainsKey(key)) {
                mLoadedAntennaCache[key] = new List<IAntenna>();
            }

            // Add the antenna if no duplicate exists for that key; fire event.
            IAntenna instance = mLoadedAntennaCache[key].Find(x => x == antenna);
            if (instance == null) {
                mLoadedAntennaCache[key].Add(antenna);
                OnRegister(antenna);
            }
        }

        public void Unregister(Guid key, IAntenna antenna) {
            RTUtil.Log("AntennaManager: Unregister: {0}, {1}", key, antenna.Name);

            // Return if no antennas are loaded at all.
            if (!mLoadedAntennaCache.ContainsKey(key)) return;

            // Find registered antenna and delete it; fire event; delete list if empty.
            int instance_id = mLoadedAntennaCache[key].FindIndex(x => x == antenna);
            if (instance_id != -1) {
                mLoadedAntennaCache[key].RemoveAt(instance_id);
                if (mLoadedAntennaCache[key].Count == 0) {
                    mLoadedAntennaCache.Remove(key);
                }
                OnUnregister(antenna);
            }
        }

        public void RegisterProtos(Vessel v) {
            Guid key = v.id;
            RTUtil.Log("AntennaManager: RegisterProtos: {0}, {1}", key, v.vesselName);
            
            // Refuse to load ProtoAntennas if physical ones exist.
            if (!mLoadedAntennaCache.ContainsKey(key)) return;

            // Iterate over all parts and try to register ProtoAntennas for them.
            foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots) {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules) {
                    if (ppms.IsAntenna()) {
                        if (!mProtoAntennaCache.ContainsKey(key)) {
                            mProtoAntennaCache[key] = new List<IAntenna>();
                        }
                        ProtoAntenna new_pa = new ProtoAntenna(v, pps, ppms);
                        mProtoAntennaCache[key].Add(new_pa);
                        OnRegister(new_pa);
                    }
                }
            }
        }

        public void UnregisterProtos(Guid key) {
            RTUtil.Log("AntennaManager: UnregisterProtos: {0}", key);
            // Return if no ProtoAntennas are loaded at all.
            if (!mProtoAntennaCache.ContainsKey(key)) return;

            // Unregister ProtoAntennas
            foreach (IAntenna a in mProtoAntennaCache[key]) {
                OnUnregister(a);
            }
            mProtoAntennaCache.Remove(key);
        }

        private IEnumerable<IAntenna> For(Guid key) {
            if (mLoadedAntennaCache.ContainsKey(key)) {
                return mLoadedAntennaCache[key];
            }
            if (mProtoAntennaCache.ContainsKey(key)) {
                return mProtoAntennaCache[key];
            }
            return Enumerable.Empty<IAntenna>();
        }

        private void OnVesselGoOnRails(Vessel v) {
            if (v.parts.Count == 0) {
                RegisterProtos(v);
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

        public IEnumerator<IAntenna> GetEnumerator() {
            return mLoadedAntennaCache.Values.SelectMany(l => l).Concat(
                   mProtoAntennaCache.Values.SelectMany(l => l)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public static partial class RTUtil {
        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms) {
            return ppms.GetBool("IsRTAntenna") &&
                   ppms.GetBool("IsRTPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsAntenna(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTAntenna") &&
                   pm.Fields.GetValue<bool>("IsRTPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
        }
    }
}
