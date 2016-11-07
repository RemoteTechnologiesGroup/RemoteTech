using System.Linq;

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
    }
}