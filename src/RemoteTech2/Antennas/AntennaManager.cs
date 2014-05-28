using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


namespace RemoteTech
{
    public class AntennaManager : IEnumerable<IVesselAntenna>
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(AntennaManager));

        public event Action<IVesselAntenna> OnRegister = delegate { };
        public event Action<IVesselAntenna> OnUnregister = delegate { };
        public IEnumerable<IVesselAntenna> this[ISatellite s] { get { return For(s.Guid); } }
        public IEnumerable<IVesselAntenna> this[IVessel v] { get { return For(v.Guid); } }
        public IEnumerable<IVesselAntenna> this[Guid g] { get { return For(g); } }

        private readonly Dictionary<Guid, List<IVesselAntenna>> loadedAntennaCache =
            new Dictionary<Guid, List<IVesselAntenna>>();
        private readonly Dictionary<Guid, List<IVesselAntenna>> protoAntennaCache =
            new Dictionary<Guid, List<IVesselAntenna>>();

        public AntennaManager()
        {
            OnRegister += a => Logger.Info("OnRegister({0})", a.Name);
            OnUnregister += a => Logger.Info("OnUnregister({0})", a.Name);
        }

        /// <summary>
        /// Registers a specified antenna.
        /// </summary>
        /// <param name="antenna">The antenna.</param>
        public void Register(IVesselAntenna antenna)
        {
            Logger.Info("Register({0})", antenna);

            // Create a container for all antenna sharing the same Guid.
            var key = antenna.Guid;
            if (!loadedAntennaCache.ContainsKey(antenna.Guid))
            {
                loadedAntennaCache[key] = new List<IVesselAntenna>();
            }

            // Unregister all ProtoAntennas, because the Vessel is obviously active.
            if (protoAntennaCache.ContainsKey(key))
            {
                UnregisterProtos(antenna.Vessel);
            }

            // Register the antenna if it wasn't. Fire the appropriate events, and listen to OnDestroy.
            if (!loadedAntennaCache[key].Contains(antenna))
            {
                loadedAntennaCache[key].Add(antenna);
                antenna.Destroyed += Unregister;
                OnRegister.Invoke(antenna);
            }
        }

        /// <summary>
        /// Unregisters the specified antenna.
        /// </summary>
        /// <param name="antenna">The antenna.</param>
        public void Unregister(IVesselAntenna antenna)
        {
            var key = antenna.Guid;
            Unregister(key, antenna);
        }

        private void Unregister(Guid key, IVesselAntenna antenna)
        {
            Logger.Info("Unregister({0})", antenna);
            // No antennas were registered for that key.
            if (!loadedAntennaCache.ContainsKey(key)) return;

            // If the antenna can be found, remove, fire event.
            // Register ProtoAntennas if it was the last antenna, because the Vessel might have been unloaded.
            if (loadedAntennaCache[key].Remove(antenna))
            {
                if (loadedAntennaCache[key].Count == 0)
                {
                    RegisterProtos(antenna.Vessel);
                    loadedAntennaCache.Remove(key);
                }
                antenna.Destroyed -= Unregister;
                OnUnregister(antenna);
            }
        }

        /// <summary>
        /// Registers ProtoAntennas for every ProtoPartModuleSnapshot that is detected to be an antenna. Used for inactive/unloaded crafts.
        /// </summary>
        /// <param name="vessel">The vessel</param>
        public void RegisterProtos(IVessel vessel)
        {
            var key = vessel.Guid;
            Logger.Info("RegisterProtos({0}, {1})", vessel.Name, key);

            // Loading ProtoAntennas has no point if there are still real antennas loaded; they take precedence.
            if (loadedAntennaCache.ContainsKey(key)) return;

            // Get rid of any old instances.
            UnregisterProtos(vessel);

            // FIXME: Split outside of this class
            // Iterate over the ProtoPartModuleSnapshots and find the antennas. Register them.
            foreach (ProtoPartSnapshot pps in vessel.Proto.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules.Where(ppms => ppms.IsAntenna()))
                {
                    if (!protoAntennaCache.ContainsKey(key))
                    {
                        protoAntennaCache[key] = new List<IVesselAntenna>();
                    }
                    var proto = ProtoAntenna.Create(vessel, pps, ppms);
                    protoAntennaCache[key].Add(proto);
                    OnRegister(proto);
                }
            }
        }

        /// <summary>
        /// Unregisters any ProtoAntennas attached to a specific vessel.
        /// </summary>
        /// <param name="vesel">The key.</param>
        public void UnregisterProtos(IVessel vessel)
        {
            Logger.Info("UnregisterProtos({0})", vessel);

            Guid key = vessel.Guid;
            if (!protoAntennaCache.ContainsKey(key)) return;

            // Fire events for all ProtoAntennas.
            foreach (IVesselAntenna a in protoAntennaCache[key])
            {
                OnUnregister.Invoke(a);
            }

            protoAntennaCache.Remove(key);
        }

        /* External Call */
        public void OnFixedUpdate()
        {
            foreach (var pair in loadedAntennaCache.ToList())
            {
                foreach (var antenna in pair.Value.ToList())
                {
                    if (antenna.Guid != pair.Key)
                    {
                        Unregister(pair.Key, antenna);
                        Register(antenna);
                    }
                }
            }
        }

        private void OnVesselAntennaDestroy(IVesselAntenna antenna)
        {
            Unregister(antenna);
        }

        private IEnumerable<IVesselAntenna> For(Guid key)
        {
            if (loadedAntennaCache.ContainsKey(key))
            {
                return loadedAntennaCache[key];
            }
            if (protoAntennaCache.ContainsKey(key))
            {
                return protoAntennaCache[key];
            }
            return Enumerable.Empty<IVesselAntenna>();
        }

        public IEnumerator<IVesselAntenna> GetEnumerator()
        {
            return loadedAntennaCache.Values.SelectMany(l => l).Concat(
                   protoAntennaCache.Values.SelectMany(l => l)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}