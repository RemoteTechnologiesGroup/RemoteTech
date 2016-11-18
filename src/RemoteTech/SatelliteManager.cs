using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RemoteTech.Modules;

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

        public int Count => _satelliteCache.Count;
        public VesselSatellite this[Guid g] => GetSatelliteById(g);
        public VesselSatellite this[Vessel v] => v == null ? null : GetSatelliteById(v.id);

        private readonly Dictionary<Guid, List<ISignalProcessor>> _loadedSpuCache =
            new Dictionary<Guid, List<ISignalProcessor>>();
        private readonly Dictionary<Guid, VesselSatellite> _satelliteCache =
            new Dictionary<Guid, VesselSatellite>();

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
            if (!_loadedSpuCache.ContainsKey(key))
            {
                UnregisterProto(vessel.id);
                _loadedSpuCache[key] = new List<ISignalProcessor>();
            }
            // Add if non duplicate
            var signalProcessor = _loadedSpuCache[key].Find(x => x == spu);
            if (signalProcessor != null)
                return key;

            _loadedSpuCache[key].Add(spu);

            // Create a new satellite if it's the only loaded signal processor.
            if (_loadedSpuCache[key].Count != 1)
                return key;

            _satelliteCache[key] = new VesselSatellite(_loadedSpuCache[key]);
            OnRegister(_satelliteCache[key]);

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
            if (!_loadedSpuCache.ContainsKey(key)) return;
            // Find instance of the signal processor.
            var instanceId = _loadedSpuCache[key].FindIndex(x => x == spu);
            if (instanceId == -1)
                return;

            // Remove satellite if no signal processors remain.
            if (_loadedSpuCache[key].Count == 1)
            {
                if (_satelliteCache.ContainsKey(key))
                {
                    VesselSatellite sat = _satelliteCache[key];
                    OnUnregister(sat);
                    _satelliteCache.Remove(key);
                }
                _loadedSpuCache[key].RemoveAt(instanceId);
                _loadedSpuCache.Remove(key);

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
                _loadedSpuCache[key].RemoveAt(instanceId);
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
            if (_loadedSpuCache.ContainsKey(vessel.id)) {
                _loadedSpuCache.Remove(vessel.id);
            }

            var spu = vessel.GetSignalProcessor();
            if (spu == null)
                return;

            var protos = new List<ISignalProcessor> {spu};
            _satelliteCache[key] = new VesselSatellite(protos);
            OnRegister(_satelliteCache[key]);
        }

        /// <summary>
        /// Unregisters the protosatellite which was compiled from the unloaded vessel data.
        /// </summary>
        public void UnregisterProto(Guid key)
        {
            RTLog.Notify("SatelliteManager: UnregisterProto({0})", key);

            // Return if there are still signal processors loaded.
            if (_loadedSpuCache.ContainsKey(key))
                return;

            // Unregister satellite if it exists.
            if (!_satelliteCache.ContainsKey(key))
                return;

            OnUnregister(_satelliteCache[key]);
            _satelliteCache.Remove(key);
        }

        private VesselSatellite GetSatelliteById(Guid key)
        {
            VesselSatellite result;
            return _satelliteCache.TryGetValue(key, out result) ? result : null;
        }

        public IEnumerable<ISatellite> FindCommandStations()
        {
            return _satelliteCache.Values.Where(vs => vs.IsCommandStation).Cast<ISatellite>();
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

        public IEnumerator<VesselSatellite> GetEnumerator()
        {
            return _satelliteCache.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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