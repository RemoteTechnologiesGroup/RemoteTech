using System.Linq;

namespace RemoteTech
{
    public static class VesselExtension
    {
        public static bool HasLocalControl(this Vessel vessel)
        {
            return vessel.parts.Any(p => (p.isControlSource > Vessel.ControlLevel.NONE) && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
        }
    }
}