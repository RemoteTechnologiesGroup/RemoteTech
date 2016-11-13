
namespace RemoteTech.Common.Extensions
{
    public static class PartModuleExtension
    {
        public static bool IsAntenna(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTAntenna") &&
                   pm.Fields.GetValue<bool>("IsRTPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
        }

        public static bool IsCommandStation(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTCommandStation");
        }

        public static bool IsSignalProcessor(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
        }
    }
}
