using System;
using UnityEngine;

namespace RemoteTech
{
    public class ProtoSignalProcessor
    {
        private readonly SignalProcessorMixin signalProcessorMixin;

        private ProtoSignalProcessor(IVessel vessel, ProtoPartModuleSnapshot ppms)
        {
            this.signalProcessorMixin = new SignalProcessorMixin(
                getName: () => vessel.Name,
                getVessel: () => vessel,
                hasControl: () => false
            );

            this.signalProcessorMixin.Load(ppms.moduleValues);
        }

        public static ISignalProcessor Create(IVessel vessel, ProtoPartModuleSnapshot ppms)
        {
            var proto = new ProtoSignalProcessor(vessel, ppms);
            return proto.signalProcessorMixin;
        }
    }
}