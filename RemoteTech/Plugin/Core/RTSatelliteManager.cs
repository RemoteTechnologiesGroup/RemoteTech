using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public class RTSatelliteManager : IEnumerable<ISatellite>, IDisposable {
        public delegate void RTSatelliteHandler(ISatellite satellite);

        public event RTSatelliteHandler Registered;
        public event RTSatelliteHandler Unregistered;

        Dictionary<Guid, ISatellite> mSatelliteCache;
        RTCore mCore;

        public RTSatelliteManager(RTCore core) {
            mSatelliteCache = new Dictionary<Guid, ISatellite>();
            mCore = core;
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            GameEvents.onVesselGoOnRails.Add(OnVesselModified);
            GameEvents.onVesselGoOffRails.Add(OnVesselModified);
            GameEvents.onVesselChange.Add(OnVesselModified);
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        public ISatellite For(Vessel v) {
            RTUtil.Log("SatelliteManager: For: " + v);
            return WithGuid(v.id);
        }

        public ISatellite WithGuid(Guid key) {
            RTUtil.Log("SatelliteManager: WithGuid: " + key);
            return mSatelliteCache.ContainsKey(key) ? mSatelliteCache[key] : null;
        }

        public bool RegisterFor(Vessel v) {
            RTUtil.Log("SatelliteManager: RegisterFor: " + v);
            Guid key = v.id;
            if (!mSatelliteCache.ContainsKey(key)) {
                ISignalProcessor spu = v.GetSignalProcessor();
                mSatelliteCache[key] = new Satellite(spu);
                OnRegister(mSatelliteCache[key]);
                return true;
            }
            return false;
        }

        void UnregisterFor(Vessel v) {
            RTUtil.Log("SatelliteManager: UnregisterFor: " + v);
            Guid key = v.id;
            if (mSatelliteCache.ContainsKey(key)) {
                OnUnregister(mSatelliteCache[key]);
                mSatelliteCache.Remove(key);
            }
        }

        void OnVesselLoaded(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselLoaded: " + v);
            if (RegisterFor(v)) {
                RTUtil.Log("SatelliteManager: loaded " + v.vesselName);
            }
        }

        void OnVesselDestroy(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselDestroy" + v);
            UnregisterFor(v);
        }

        void OnVesselModified(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselModified" + v);
            ISignalProcessor spu;
            ISatellite sat;
            if((spu = v.GetSignalProcessor()) == null) {
                UnregisterFor(v);
            } else if((sat = For(v)).SignalProcessor != spu) {
                sat.SignalProcessor = spu;
            } else {
                RegisterFor(v);
            }
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
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
            GameEvents.onVesselGoOnRails.Remove(OnVesselModified);
            GameEvents.onVesselGoOffRails.Remove(OnVesselModified);
            GameEvents.onVesselChange.Remove(OnVesselModified);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        }

        public IEnumerator<ISatellite> GetEnumerator() {
            foreach (ISatellite sat in mSatelliteCache.Values) {
                 yield return sat;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}
