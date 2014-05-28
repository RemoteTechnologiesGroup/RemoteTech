using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class TransmitterMixin
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(TransmitterMixin));
        private const String TransmitterNodeIdentifier = "TRANSMITTER";
        private const String TransmitterModuleIdentifier = "ModuleRTDataTransmitter";

        private ConfigNode transmitterConfig;
        private IScienceDataTransmitter transmitter;
        private readonly Func<Guid> attachedTo;
        private readonly Func<Part> getPart;

        public TransmitterMixin(Func<Guid> attachedTo, Func<Part> getPart)
        {
            this.attachedTo = attachedTo;
            this.getPart = getPart;
        }

        public void Start()
        {

        }
        public void Load(ConfigNode node)
        {
            if (node.HasNode())
            {
                Logger.Info("Found transmitter config block.");
                transmitterConfig = node.GetNode(TransmitterNodeIdentifier);
                transmitterConfig.AddValue("name", TransmitterModuleIdentifier);
            }
        }
        public void CheckTransmitter()
        {
            if (RTCore.Instance != null)
            {
                var satellite = RTCore.Instance.Network[attachedTo()];
                bool routeToKSC = RTCore.Instance.Network[satellite].ConnectedToKSC();
                if (transmitter == null && routeToKSC)
                {
                    AddTransmitter();
                }
                else if (!routeToKSC && transmitter != null)
                {
                    RemoveTransmitter();
                }
            }
        }

        private void AddTransmitter()
        {
            if (transmitterConfig == null || !transmitterConfig.HasValue("name")) return;
            var transmitters = getPart().FindModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0)
            {
                Logger.Info("Found Transmitter Node.");
                transmitter = transmitters.First();
            }
            else
            {
                var copy = new ConfigNode();
                transmitterConfig.CopyTo(copy);
                getPart().AddModule(copy);
                AddTransmitter();
                Logger.Info("Added Transmitter Module.");
            }
        }

        private void RemoveTransmitter()
        {
            Logger.Info("Removed Transmitter Module.");
            if (transmitter == null) return;
            getPart().RemoveModule((PartModule)transmitter);
            transmitter = null;
        }
    }
}
