using System.Linq;

namespace RemoteTech
{
    public static class VesselExtension
    {
        /// <summary>
        /// Get wether a vessel has local control or not (that is, if it's manned or not).
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>true if the vessel has a local control, false otherwise.</returns>
        public static bool HasLocalControl(this Vessel vessel)
        {
            return vessel.parts.Any(p => (p.isControlSource > Vessel.ControlLevel.NONE) && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
        }
    }
}