
namespace RemoteTech.Common.Extensions
{
    public static class ProtoPartModuleSnapshotExtension
    {
        public static bool GetBool(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(n.GetValue(value) ?? "False", out result) && result;
        }

        /// <summary>Searches a ProtoPartModuleSnapshot for an integer field.</summary>
        /// <returns>True if the member <paramref name="valueName"/> exists, false otherwise.</returns>
        /// <param name="ppms">The <see cref="ProtoPartModuleSnapshot"/> to query.</param>
        /// <param name="valueName">The name of a member in the  ProtoPartModuleSnapshot.</param>
        /// <param name="value">The value of the member <paramref name="valueName"/> on success. An undefined value on failure.</param>
        public static bool GetInt(this ProtoPartModuleSnapshot ppms, string valueName, out int value)
        {
            value = 0;
            var result = ppms.moduleValues.TryGetValue(valueName, ref value);
            if (!result)
            {
                RTLog.Notify($"No integer '{value}' in ProtoPartModule '{ppms.moduleName}'");
            }

            return result;
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(value, out result) && result;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTAntenna") &&
                   ppms.GetBool("IsRTPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTSignalProcessor");

        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTCommandStation");
        }
    }
}
