using System.Linq;

namespace RemoteTech
{
    public static class VesselExtension
    {
        public static bool HasLocalControl(this Vessel vessel)
        {
            return vessel.parts.Any(p => p.isControlSource && (p.protoModuleCrew.Any() || !p.FindModulesImplementing<ISignalProcessor>().Any()));
        }
    }
}