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

        public ISatellite RegisterFor(Vessel v) {
            RTUtil.Log("SatelliteManager: RegisterFor: " + v);
            Guid key = v.id;
            if (!mSatelliteCache.ContainsKey(key)) {
                mSatelliteCache[key] = new Satellite(v);
                OnRegister(mSatelliteCache[key]);
            }
            return mSatelliteCache[key];
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
            if (v.HasSignalProcessor()) {
                RTUtil.Log("SatelliteManager: loaded " + v.vesselName);
                RegisterFor(v);
            }
        }

        void OnVesselDestroy(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselDestroy" + v);
            UnregisterFor(v);
        }

        void OnVesselModified(Vessel v) {
            RTUtil.Log("SatelliteManager: OnVesselModified" + v);
            if (!v.HasSignalProcessor()) {
                UnregisterFor(v);
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
