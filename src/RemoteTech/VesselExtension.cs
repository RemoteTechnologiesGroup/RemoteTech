using System;
using System.Linq;
using RemoteTech.Modules;

namespace RemoteTech
{
    public static class VesselExtension
    {
        /// <summary>
        /// Get whether a vessel has local control or not (that is, if it's Kerbal controlled or not).
        /// </summary>
        /// <param name="vessel">The vessel to check for.</param>
        /// <returns>true if the vessel has a local control, false otherwise.</returns>
        public static bool HasLocalControl(this Vessel vessel)
        {
            // vessel must be a control source and it must be crewed or not implementing a module processor
            var hasLocalControl = vessel.parts.Any(p => (p.isControlSource > Vessel.ControlLevel.NONE) && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
            if (!hasLocalControl)
            {
                // check if theres's a SPU which is a command station. 
                // Command stations must have local control even if there's nobody in the command pod [see other checks in ModuleSPU.IsCommandStation]
                hasLocalControl = vessel.parts.Any(part => part.FindModulesImplementing<ISignalProcessor>().Any(signalProcessor => signalProcessor.IsCommandStation));
            }

            return hasLocalControl;
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
        public static bool HasCommandStation(this Vessel v)
        {
            RTLog.Notify("HasCommandStation({0})", v.vesselName);
            if (v.loaded && v.parts.Count > 0)
            {
                return v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Any(pm => pm.IsCommandStation());
            }
            return v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Any(pm => pm.IsCommandStation());
        }

        /// <summary>
        /// Returns a vessel object by the given <paramref name="vesselid"/> or
        /// null if no vessel was found
        /// </summary>
        /// <param name="vesselid">Guid of a vessel</param>
        public static Vessel GetVesselById(Guid vesselid)
        {
            return FlightGlobals.Vessels.FirstOrDefault(vessel => vessel.id == vesselid);
        }
    }
}