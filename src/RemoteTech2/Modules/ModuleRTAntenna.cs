using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    [KSPModule("Antenna")]
    public class ModuleRTAntenna : PartModule
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(ModuleRTAntenna));

        public IVesselAntenna AsVesselAntenna()
        {
            return antennaMixin;
        }

        private float RangeMultiplier       { get { return RTSettings.Instance.RangeMultiplier; } }
        private float ConsumptionMultiplier { get { return RTSettings.Instance.ConsumptionMultiplier; } }

        [KSPField(guiName = "Dish range")] public String GUI_DishRange;
        [KSPField(guiName = "Energy")]     public String GUI_EnergyReq;
        [KSPField(guiName = "Omni range")] public String GUI_OmniRange;
        [KSPField(guiName = "Status")]     public String GUI_Status;

        // KSP Fields
        [KSPField]
        public String
            Mode0Name = "Off",
            Mode1Name = "Operational",
            ActionMode0Name = "Deactivate",
            ActionMode1Name = "Activate",
            ActionToggleName = "Toggle";


        private AnimationMixin animationMixin;
        private TransmitterMixin transmitterMixin;
        private AntennaMixin antennaMixin;

        public override string GetInfo()
        {
            var info = new StringBuilder();

            if (antennaMixin.ActiveOmniRange > 0)
                info.AppendFormat("Omni range: {0} / {1}",
                    RTUtil.FormatSI(antennaMixin.ActiveOmniRange * RangeMultiplier, "m"),
                    RTUtil.FormatSI(antennaMixin.ActiveOmniRange * RangeMultiplier, "m"))
                    .AppendLine();

            if (antennaMixin.ActiveDishRange > 0)
                info.AppendFormat("Dish range: {0} / {1}",
                    RTUtil.FormatSI(antennaMixin.ActiveDishRange * RangeMultiplier, "m"),
                    RTUtil.FormatSI(antennaMixin.ActiveDishRange * RangeMultiplier, "m"))
                    .AppendLine();
            
            if (antennaMixin.ActiveConsumption > 0)
                info.AppendFormat("Energy req.: {0}",
                    RTUtil.FormatConsumption(antennaMixin.ActiveConsumption * ConsumptionMultiplier))
                    .AppendLine();

            if (antennaMixin.ActiveDishRange > 0)
                info.AppendFormat("Cone angle: {0} degrees", 
                    antennaMixin.DishAngle.ToString("F2"))
                    .AppendLine();

            if (antennaMixin.Activated)
                info.AppendLine("Activated by default");

            /*if (MaxQ > 0)
                info.AppendLine("Snaps under high dynamic pressure");*/

            return info.ToString().TrimLines();
        }

        [KSPEvent(name = "EventToggle", 
                  guiActive = false)]
        public void EventToggle() { if (animationMixin.Animating) return; if (antennaMixin.Activated) { EventClose(); } else { EventOpen(); } }

        [KSPEvent(name = "EventTarget", category = "skip_delay",
                  guiActive = false, guiName = "Set Target")]
        public void EventTarget() { (new AntennaWindow(antennaMixin)).Show(); }

        [KSPEvent(name = "EventEditorOpen", 
                  guiActive = false, guiName = "Start deployed")]
        public void EventEditorOpen() { antennaMixin.Activated = true; }

        [KSPEvent(name = "EventEditorClose", 
                  guiActive = false, guiName = "Start retracted")]
        public void EventEditorClose() { antennaMixin.Activated = false; }

        [KSPEvent(name = "EventOpen", 
                  guiActive = false)]
        public void EventOpen() { if (!animationMixin.Animating) { antennaMixin.Activated = true; } }

        [KSPEvent(name = "EventClose",
                  guiActive = false)]
        public void EventClose() { if (!animationMixin.Animating) { antennaMixin.Activated = false; } }

        [KSPAction("ActionToggle", KSPActionGroup.None)]
        public void ActionToggle(KSPActionParam param) { EventToggle(); }

        [KSPAction("ActionOpen", KSPActionGroup.None)]
        public void ActionOpen(KSPActionParam param) { EventOpen(); }

        [KSPAction("ActionClose", KSPActionGroup.None)]
        public void ActionClose(KSPActionParam param) { EventClose(); }

        [KSPEvent(name = "OverrideTarget",   active = true, 
                  guiActiveUnfocused = true, unfocusedRange = 5,
                  externalToEVAOnly = true,  guiName = "[EVA] Set Target",
                  category = "skip_delay;skip_control")]
        public void OverrideTarget() { (new AntennaWindow((IVesselAntenna) this)).Show(); }

        [KSPEvent(name = "OverrideOpen",     active = true, 
                  guiActiveUnfocused = true, unfocusedRange = 5, 
                  externalToEVAOnly = true,  guiName = "[EVA] Force Open", 
                  category = "skip_delay;skip_control")]
        public void OverrideOpen() { EventOpen(); }

        [KSPEvent(name = "OverrideClose",    active = true, 
                  guiActiveUnfocused = true, unfocusedRange = 5, 
                  externalToEVAOnly = true,  guiName = "[EVA] Force Close", 
                  category = "skip_delay;skip_control")]
        public void OverrideClose() { EventClose(); }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            antennaMixin.Load(node);
            animationMixin.Load(node);
            transmitterMixin.Load(node);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            animationMixin = new AnimationMixin(
                getPartModule: i => part.Modules[i]
            );

            transmitterMixin = new TransmitterMixin(
                attachedTo: () => antennaMixin.Guid,
                getPart:    () => part
            );

            antennaMixin = new AntennaMixin(
                getVessel:      () => (VesselProxy) vessel,
                getName:        () => part.partInfo.name,

                requestResource: (s, d) => part.RequestResource(s, d),

                animation: animationMixin,
                transmitter: transmitterMixin
            );
        }

        public override void OnStart(StartState state)
        {
            animationMixin.Start();
            transmitterMixin.Start();
            antennaMixin.Start();

            Actions["ActionOpen"].guiName   = ActionMode1Name;
            Actions["ActionOpen"].active    = !antennaMixin.Broken;
            Actions["ActionClose"].guiName  = ActionMode0Name;
            Actions["ActionClose"].active   = !antennaMixin.Broken;
            Actions["ActionToggle"].guiName = ActionToggleName;
            Actions["ActionToggle"].active  = !antennaMixin.Broken;

            Events["EventOpen"].guiName     = ActionMode1Name;
            Events["EventClose"].guiName    = ActionMode0Name;
            Events["EventToggle"].guiName   = ActionToggleName;
            Events["EventTarget"].guiActive = (antennaMixin.ActiveDishRange > 0);
            Events["EventTarget"].active =    (antennaMixin.ActiveDishRange > 0);

            Fields["GUI_OmniRange"].guiActive = (antennaMixin.ActiveOmniRange > 0);
            Fields["GUI_DishRange"].guiActive = (antennaMixin.ActiveDishRange > 0);
            Fields["GUI_EnergyReq"].guiActive = (antennaMixin.ActiveConsumption > 0);
            Fields["GUI_Status"].guiActive = true;
        }

        private void FixedUpdate()
        {
            switch (antennaMixin.Update())
            {
                case AntennaMixin.State.Off:
                    GUI_Status = Mode0Name;
                    break;
                case AntennaMixin.State.Operational:
                    GUI_Status = Mode1Name;
                    break;
                case AntennaMixin.State.NoResources:
                    GUI_Status = "Out of power";
                    break;
                case AntennaMixin.State.Malfunction:
                    GUI_Status = "Malfunction";
                    break;
            }
            //HandleDynamicPressure();
            UpdatePartMenu();

            Events["EventOpen"].guiActive = Events["EventOpen"].active =
            Events["EventEditorOpen"].guiActiveEditor = Events["OverrideOpen"].guiActiveUnfocused
                = !antennaMixin.Activated && !antennaMixin.Broken;

            Events["EventClose"].guiActive = Events["EventClose"].active =
            Events["EventEditorClose"].guiActiveEditor = Events["OverrideClose"].guiActiveUnfocused
                = antennaMixin.Activated && !antennaMixin.Broken;

        }

        private void UpdatePartMenu()
        {
            GUI_OmniRange = RTUtil.FormatSI(antennaMixin.CurrentOmniRange, "m");
            GUI_DishRange = RTUtil.FormatSI(antennaMixin.CurrentDishRange, "m");
            GUI_EnergyReq = RTUtil.FormatConsumption(antennaMixin.CurrentConsumption);
        }

        /*private void HandleDynamicPressure()
        {
            if (vessel == null) return;
            if (!vessel.HoldPhysics && vessel.atmDensity > 0 && MaxQ > 0 && deployFxModules.Any(a => a.GetScalar > 0.9f))
            {
                if (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity / 2 > MaxQ)
                {
                    MaxQ = -1.0f;
                    part.decouple(0.0f);
                }
            }
        }*/

        private void OnDestroy()
        {
            Logger.Info("OnDestroy");
            antennaMixin.Dispose();
        }
    }
}