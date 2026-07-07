using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RemoteTech.Modules;
using RemoteTech.Collections;

namespace RemoteTech
{
    /// <summary>
    /// Class keeping track of RemoteTech satellites.
    /// Acts as a list of vessels managed by RemoteTech.
    /// </summary>
    public class SatelliteManager : IEnumerable<VesselSatellite>, IDisposable
    {
        public event Action<VesselSatellite> OnRegister = delegate { };
        public event Action<VesselSatellite> OnUnregister = delegate { };

        public int Count => SatelliteCache.Count;
        public VesselSatellite this[Guid g] => GetSatelliteById(g);
        public VesselSatellite this[Vessel v] => v == null ? null : GetSatelliteById(v.id);

        internal readonly Dictionary<Guid, List<ISignalProcessor>> LoadedSpuCache = [];
        internal readonly ArrayMap<Guid, VesselSatellite> SatelliteCache = new();

        public SatelliteManager()
        {
            GameEvents.onVesselCreate.Add(OnVesselCreate);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Add(OnVesselOnRails);

            OnRegister += vs => RTLog.Notify("SatelliteManager: OnRegister({0})", vs);
            OnUnregister += vs => RTLog.Notify("SatelliteManager: OnUnregister({0})", vs);
        }

        /// <summary>
        /// Registers a signal processor for the vessel.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        /// <param name="spu">The signal processor.</param>
        /// <returns>Guid key under which the signal processor was registered.</returns>
        public Guid Register(Vessel vessel, ISignalProcessor spu)
        {
            RTLog.Notify("SatelliteManager: Register({0})", spu);

            var key = vessel.id;
            if (!LoadedSpuCache.ContainsKey(key))
            {
                UnregisterProto(vessel.id);
                LoadedSpuCache[key] = [];
            }
            // Add if non duplicate
            var signalProcessor = LoadedSpuCache[key].Find(x => x == spu);
            if (signalProcessor != null)
                return key;

            LoadedSpuCache[key].Add(spu);

            // Create a new satellite if it's the only loaded signal processor.
            if (LoadedSpuCache[key].Count != 1)
                return key;

            SatelliteCache[key] = new VesselSatellite(vessel, LoadedSpuCache[key]);
            OnRegister(SatelliteCache[key]);

            return key;
        }

        /// <summary>
        /// Unregisters the specified signal processor.
        /// </summary>
        /// <param name="key">The key the signal processor was registered under.</param>
        /// <param name="spu">The signal processor.</param>
        public void Unregister(Guid key, ISignalProcessor spu)
        {
            RTLog.Notify("SatelliteManager: Unregister({0})", spu);
            // Return if nothing to unregister.
            if (!LoadedSpuCache.ContainsKey(key)) return;
            // Find instance of the signal processor.
            var instanceId = LoadedSpuCache[key].FindIndex(x => x == spu);
            if (instanceId == -1)
                return;

            // Remove satellite if no signal processors remain.
            if (LoadedSpuCache[key].Count == 1)
            {
                if (SatelliteCache.ContainsKey(key))
                {
                    VesselSatellite sat = SatelliteCache[key];
                    OnUnregister(sat);
                    SatelliteCache.Remove(key);
                }
                LoadedSpuCache[key].RemoveAt(instanceId);
                LoadedSpuCache.Remove(key);

                // search vessel by id
                var vessel = RTUtil.GetVesselById(key);
                if (vessel != null)
                {
                    // trigger the onRails on more time
                    // to re-register the satellite as a protoSat
                    OnVesselOnRails(vessel);
                }
            }
            else
            {
                LoadedSpuCache[key].RemoveAt(instanceId);
            }
        }

        /// <summary>
        /// Registers a protosatellite compiled from the unloaded vessel data.
        /// </summary>
        /// <param name="vessel">The vessel.</param>
        public void RegisterProto(Vessel vessel)
        {
            Guid key = vessel.protoVessel.vesselID;
            RTLog.Notify("SatelliteManager: RegisterProto({0}, {1})", vessel.vesselName, key);
            // Return if there are still signal processors loaded.
            if (LoadedSpuCache.ContainsKey(vessel.id))
                LoadedSpuCache.Remove(vessel.id);

            var spu = vessel.GetSignalProcessor();
            if (spu == null)
                return;

            SatelliteCache[key] = new VesselSatellite(vessel, [spu]);
            OnRegister(SatelliteCache[key]);
        }

        /// <summary>
        /// Unregisters the protosatellite which was compiled from the unloaded vessel data.
        /// </summary>
        public void UnregisterProto(Guid key)
        {
            RTLog.Notify("SatelliteManager: UnregisterProto({0})", key);

            // Return if there are still signal processors loaded.
            if (LoadedSpuCache.ContainsKey(key))
                return;

            // Unregister satellite if it exists.
            if (!SatelliteCache.ContainsKey(key))
                return;

            OnUnregister(SatelliteCache[key]);
            SatelliteCache.Remove(key);
        }

        private VesselSatellite GetSatelliteById(Guid key)
        {
            VesselSatellite result;
            return SatelliteCache.TryGetValue(key, out result) ? result : null;
        }

        public List<VesselSatellite> FindCommandStations()
        {
            var values = SatelliteCache.Values;
            var stations = new List<VesselSatellite>(values.Length);

            foreach (var sat in values)
            {
                if (sat.IsCommandStation)
                    stations.Add(sat);
            }

            return stations;
        }

        private void OnVesselOnRails(Vessel v)
        {
            if (v.parts.Count == 0)
            {
                RegisterProto(v);
            }
        }

        private void OnVesselCreate(Vessel v)
        {
            RTLog.Notify("SatelliteManager: OnVesselCreate({0}, {1})", v.id, v.vesselName);
        }

        private void OnVesselDestroy(Vessel v)
        {
            RTLog.Notify("SatelliteManager: OnVesselDestroy({0}, {1})", v.id, v.vesselName);
            UnregisterProto(v.id);
        }

        public void Dispose()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
            GameEvents.onVesselGoOnRails.Remove(OnVesselOnRails);
        }

        public SpanEnumerator<VesselSatellite> GetEnumerator() =>
             SatelliteCache.Values.GetEnumerator();
        IEnumerator<VesselSatellite> IEnumerable<VesselSatellite>.GetEnumerator() => 
            GetArrayEnumerator<VesselSatellite[], VesselSatellite>([.. SatelliteCache.Values]);
        IEnumerator IEnumerable.GetEnumerator()
        {
            VesselSatellite[] array = [..SatelliteCache.Values];
            return array.GetEnumerator();
        }

        static IEnumerator<T> GetArrayEnumerator<A, T>(A array)
            where A : IEnumerable<T>
        {
            return array.GetEnumerator();
        }
    }

    public static partial class RTUtil
    {
        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTSignalProcessor");

        }

        public static bool IsSignalProcessor(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
        }

        public static ISignalProcessor GetSignalProcessor(this Vessel v)
        {
            RTLog.Notify("GetSignalProcessor({0}): Check", v.vesselName);

            ISignalProcessor result = null;

            if (v.loaded && v.parts.Count > 0)
            {
                var partModuleList = v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU
                result = partModuleList.FirstOrDefault(pm => pm.moduleName == "ModuleSPU") as ISignalProcessor ??
                         partModuleList.FirstOrDefault() as ISignalProcessor;
            }
            else
            {
                var protoPartList = v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Where(ppms => ppms.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU on a unloaded vessel
                var protoPartProcessor = protoPartList.FirstOrDefault(ppms => ppms.moduleName == "ModuleSPU") ??
                                         protoPartList.FirstOrDefault();

                // convert the found protoPartSnapshots to a ProtoSignalProcessor
                if (protoPartProcessor != null)
                {
                    result = new ProtoSignalProcessor(protoPartProcessor, v);
                }
            }

            return result;
        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTCommandStation");
        }

        public static bool IsCommandStation(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTCommandStation");
        }

        public static bool HasCommandStation(this Vessel v)
        {
            RTLog.Notify("HasCommandStation({0})", v.vesselName);
            if (v.loaded && v.parts.Count > 0)
            {
                return v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Any(pm => pm.IsCommandStation());
            }
            return v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Any(pm => pm.IsCommandStation());
        }
    }
}