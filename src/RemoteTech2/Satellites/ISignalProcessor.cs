using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public interface ISignalProcessor
    {
        String Name                   { get; }
        Guid Guid                     { get; }
        bool Powered                  { get; }
        Group Group                   { get; set; }
        IVessel Vessel                { get; }

        bool? IsCommandStation        { get; }
        FlightComputer FlightComputer { get; }

        event Action<ISignalProcessor> Destroyed;
    }

    public static class SignalProcessor
    {
        private const String SignalProcessorIdentifier = "IsRTSignalProcessor";
        private const String CommandStationIdentifier  = "IsRTCommandStation";
        private const String IsPoweredIdentifier       = "IsRTPowered";
        private const String IsActiveIdentifier        = "IsRTActive";
        private const String GroupIdentifier           = "RTGroup";

        private static ILogger Logger = RTLogger.CreateLogger(typeof(SignalProcessor));
        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot protoPartModuleSnapshot)
        {
            return protoPartModuleSnapshot.GetBool(SignalProcessorIdentifier);

        }

        public static bool IsSignalProcessor(this PartModule partModule)
        {
            return partModule.Fields.GetValue<bool>(SignalProcessorIdentifier);
        }

        public static ISignalProcessor GetSignalProcessor(this IVessel vessel)
        {
            Logger.Debug("GetSignalProcessor({0}): Check", vessel.Name);
            if (vessel.IsLoaded)
            {
                foreach (var pm in vessel.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.IsSignalProcessor()))
                {
                    Logger.Debug("GetSignalProcessor({0}): Found", vessel.Name);
                    return pm as ISignalProcessor;
                }

            }
            else
            {
                foreach (var ppms in vessel.Proto.protoPartSnapshots.SelectMany(x => x.modules).Where(ppms => ppms.IsSignalProcessor()))
                {
                    Logger.Debug("GetSignalProcessor({0}): Found", vessel.Name);
                    return ProtoSignalProcessor.Create(vessel, ppms); 
                }
            }
            return null;
        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot protoPartModuleSnapshot)
        {
            return protoPartModuleSnapshot.GetBool(CommandStationIdentifier);
        }

        public static bool IsCommandStation(this PartModule partModule)
        {
            return partModule.Fields.GetValue<bool>(CommandStationIdentifier);
        }

        public static bool HasCommandStation(this IVessel vessel)
        {
            Logger.Debug("HasCommandStation({0})", vessel.Name);
            if (vessel.IsLoaded)
            {
                return vessel.Parts.SelectMany(p => p.Modules.Cast<PartModule>())
                                   .Any(pm => pm.IsCommandStation());
            }
            else
            {
                return vessel.Proto.protoPartSnapshots.SelectMany(x => x.modules)
                                                      .Any(pm => pm.IsCommandStation());
            }
        }
    }
}