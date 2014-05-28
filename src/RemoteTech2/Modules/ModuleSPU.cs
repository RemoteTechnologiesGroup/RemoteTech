using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    [KSPModule("Signal Processor")]
    public class ModuleSPU : PartModule
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(ModuleSPU));
        public ISignalProcessor AsSignalProcessor()
        {
            return signalProcessorMixin;
        }

        [KSPField]
        public bool
            ShowGUI_Status = true,
            ShowEditor_Type = true;

        [KSPField(guiName = "SPU", guiActive = true)]
        public String GUI_Status;

        private SignalProcessorMixin signalProcessorMixin;

        public override String GetInfo()
        {
            if (!ShowEditor_Type) return String.Empty;
            return signalProcessorMixin.CrewRequirement > -1
                    ? String.Format("Remote Command capable ({0}+ crew)", signalProcessorMixin.CrewRequirement) 
                    : "Remote Control capable";
        }

        public override void OnAwake()
        {
            base.OnAwake();

            signalProcessorMixin = new SignalProcessorMixin(
                getName:    () => part.partInfo.title,
                getVessel:  () => (VesselProxy)vessel,
                hasControl: () => part.isControlSource
            );
        }

        public override void OnStart(StartState state)
        {
            signalProcessorMixin.Start();
            Fields["GUI_Status"].guiActive = ShowGUI_Status;
        }

        public void OnDestroy()
        {
            signalProcessorMixin.Dispose();
        }

        public void Update()
        {
            signalProcessorMixin.Update();
        }

        public void FixedUpdate()
        {
            switch (signalProcessorMixin.Update())
            {
                case SignalProcessorMixin.State.Operational:
                    GUI_Status = "Operational.";
                    break;
                case SignalProcessorMixin.State.ParentDefect:
                case SignalProcessorMixin.State.NoConnection:
                    GUI_Status = "No connection.";
                    break;
            }
        }

        public override string ToString()
        {
            return String.Format("ModuleSPU({0}, {1})", vessel != null ? vessel.vesselName : "null", vessel.id);
        }
    }
}