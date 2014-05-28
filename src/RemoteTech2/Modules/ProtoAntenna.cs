using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTech
{
    public class ProtoAntenna
    {
        private readonly ProtoPartModuleSnapshot protoModule;
        private readonly AntennaMixin antenna;

        private ProtoAntenna(IVessel vessel, ProtoPartSnapshot p, ProtoPartModuleSnapshot protoModule)
        {
            this.protoModule = protoModule;
            var antenna = new AntennaMixin(
                getVessel: () => vessel,
                getName: () => p.partInfo.title,
                requestResource: (s, d) => d
            );
            this.antenna.Load(protoModule.moduleValues);
            this.antenna.Targets.AddingNew += OnTargetsModified;
            this.antenna.Targets.ListChanged += OnTargetsModified;
            this.antenna.Start();
            // FIXME: Dispose?
        }
        private void OnTargetsModified()
        {
            ConfigNode n = new ConfigNode();
            protoModule.Save(n);
            VesselAntenna.SaveAntennaTargets(n, antenna.Targets);
            protoModule.moduleValues = n;
        }
        public static IVesselAntenna Create(IVessel vessel, ProtoPartSnapshot p, ProtoPartModuleSnapshot ppms)
        {
            var proto = new ProtoAntenna(vessel, p, ppms);
            return proto != null ? proto.antenna : null;

            /*ConfigNode n = new ConfigNode();
            ppms.Save(n);

            Name = p.partInfo.title;
            CurrentConsumption = 0.0f;
            Guid = vessel.id;
            Vessel = vessel;
            protoModule = ppms;

            targets = new EventListWrapper<Target>(VesselAntenna.ParseAntennaTargets(n));
            CurrentDishRange = VesselAntenna.ParseAntennaDishRange(n);
            CurrentRadians = VesselAntenna.ParseAntennaRadians(n);
            CurrentOmniRange = VesselAntenna.ParseAntennaOmniRange(n);
            Powered = VesselAntenna.ParseAntennaIsPowered(n);
            Activated = VesselAntenna.ParseAntennaIsActivated(n);*/
        }
    }
}