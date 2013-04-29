using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRemoteTechSPU : ModuleCommand
    {
        [KSPField(isPersistant = true)]
        public bool isRemoteCommand = false;

        [KSPField]
        public float EnergyDrain = 0;


        [KSPEvent(guiActive = true, active = false, guiName = "Hide Menus")]
        public void HideMenus()
        {
            RTGlobals.show = false;
            foreach (KeyValuePair<Vessel, RemoteCore> pair in RTGlobals.coreList)
                if (pair.Key.loaded)
                    foreach (Part p in pair.Key.parts)
                        if (p.Modules.Contains("ModuleRemoteTechSPU"))
                            p.Modules["ModuleRemoteTechSPU"].Events["UPDRTmenuBool"].Invoke();
        }

        [KSPEvent(guiActive = true, active = false, guiName = "Show Menus")]
        public void ShowMenus()
        {
            RTGlobals.show = true;
            foreach (KeyValuePair<Vessel,RemoteCore> pair in RTGlobals.coreList)
                if (pair.Key.loaded)
                    foreach (Part p in pair.Key.parts)
                        if (p.Modules.Contains("ModuleRemoteTechSPU"))
                            p.Modules["ModuleRemoteTechSPU"].Events["UPDRTmenuBool"].Invoke();
        }

        [KSPEvent]
        public void UPDRTmenuBool()
        {
            Events["ShowMenus"].active = !RTGlobals.show;
            Events["HideMenus"].active = RTGlobals.show;
        }

        public override string GetInfo()
        {
            string text;

            if (isRemoteCommand)
                text = "Remote Command";
            else
                text = "Remote Control";

            text += "\nEnergy req.: " + (EnergyDrain * 60).ToString("0.00") + "/min.";

            return text;
        }

        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            if (state == StartState.Editor) return;
            flightStarted = true;
            RTGlobals.controller.createGUI();
            RTGlobals.Load();

            if (vessel.isActiveVessel)
                RTGlobals.coreList.Clear();

            UPDRTmenuBool();

            parts = vessel.parts.Count;
        }


        int parts;
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!flightStarted) return;

            if (parts != vessel.parts.Count)
            {
                try
                {
                    RTGlobals.coreList[vessel].Rnode = new RelayNode(vessel);
                }
                catch { }
                RTGlobals.coreList.recalculate();
                parts = vessel.parts.Count;
            }


            if (!RTGlobals.coreList.ContainsKey(this.vessel))
                RTGlobals.coreList.Add(this.vessel, this.EnergyDrain);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            RTGlobals.make();
        }

        [KSPEvent]
        public void UpdateRn()
        {
            RTGlobals.network = new RelayNetwork();
        }

        [KSPEvent]
        public void RTinterface(BaseEventData data)
        {
            bool success = true;
            try
            {
                data.Set<double>("controlDelay", RTGlobals.coreList[this.vessel].path.ControlDelay);
            }
            catch
            { data.Set<double>("controlDelay", 0); success = false; }

            try
            {
                data.Set<bool>("attitudeActive", RTGlobals.coreList[this.vessel].computer.AttitudeActive);
            }
            catch { data.Set<bool>("attitudeActive", false); success = false; }

            try
            {
                data.Set<bool>("localControl", RTGlobals.coreList[this.vessel].localControl);
            }
            catch { data.Set<bool>("localControl", true); success = false; }

            try
            {
                data.Set<bool>("inRadioContact", RTGlobals.coreList[this.vessel].InContact);
            }
            catch { data.Set<bool>("inRadioContact", true); success = false; }

            data.Set<bool>("success", success);
        }
    }

    public class RemoteTechAntennaCore : PartModule
    {
        [KSPField(isPersistant = true)]
        public string pointedAt = "None",
        antennaName = "Dish";
        [KSPField]
        public string
        Antenna_Range = "",
        Dish_Range = "";

        [KSPField(isPersistant = true)]
        public float
            dishRange = 0,
            antennaRange = 0,
            EnergyDrain = 0;

        [KSPField]
        public bool
            showStats = true,
            showAntennaName = false,
            showType = false;

        public bool powered = true;
        public bool InControl
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return true;
                try
                {
                    if (RTGlobals.coreList.ContainsKey(vessel))
                        return powered && RTGlobals.coreList[vessel].InControl;
                }
                catch { }
                return powered;
            }
        }

        public bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;
            flightStarted = true;
            UpdatePA();
        }

        [KSPEvent(name = "setTarget", active = false, guiActive = true, guiName = "")]
        public void setTarget()
        {
            if (!InControl) return;
            RTGlobals.controller.settings.Open(this);
        }

        [KSPEvent(name = "setTarget2", active = false, guiActive = true, guiName = "")]
        public void setTarget2()
        {
            if (!InControl) return;
            RTGlobals.controller.settings.Open(this);
        }

        [KSPEvent(name = "OverrideTarget", active = false, guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true, guiName = "")]
        public void OverrideTarget()
        {
            if (!powered) return;
            RTGlobals.controller.settings.Open(this);
        }

        [KSPEvent(name = "OverrideTarget2", active = false, guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true, guiName = "")]
        public void OverrideTarget2()
        {
            if (!powered) return;
            RTGlobals.controller.settings.Open(this);
        }


        public override string GetInfo()
        {
            string text = "";
            if (this.antennaRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Antenna range: " + RTUtils.length(this.antennaRange * 1000) + "m";
            }
            if (this.dishRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Dish range: " + RTUtils.length(this.dishRange * 1000) + "m";
            }
            if (this.EnergyDrain > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Energy req.: " + (EnergyDrain * 60).ToString("0.00") + "/min.";
            }

            //if (this.EnergyDrain0 > 0)
            //    text += "\nInactive energy req.: " + (Math.Round(this.EnergyDrain0 * 60, 1) == 0 ? Math.Round(this.EnergyDrain0 * 60, 2) : Math.Round(this.EnergyDrain0 * 60, 1)) + "/min.";
            //if (this.EnergyDrain1 > 0)
            //    text += "\nEnergy req.: " + (Math.Round(this.EnergyDrain1 * 60, 1) == 0 ? Math.Round(this.EnergyDrain1 * 60, 2) : Math.Round(this.EnergyDrain1 * 60, 1)) + "/min.";

            return text;
        }

        [KSPEvent]
        public void UpdatePA()
        {
            if (this.dishRange > 0)
            {
                if (Events["setTarget"].active)
                {
                    Events["setTarget2"].guiName = "Target: " + RTUtils.targetName(this.pointedAt);
                    Events["OverrideTarget2"].guiName = "Override " + Events["setTarget2"].guiName;
                    Events["setTarget"].active = Events["OverrideTarget"].active = false;
                    Events["setTarget2"].active = Events["OverrideTarget2"].active = true;
                }
                else
                {
                    Events["setTarget"].guiName = "Target: " + RTUtils.targetName(this.pointedAt);
                    Events["OverrideTarget"].guiName = "Override " + Events["setTarget"].guiName;
                    Events["setTarget"].active = Events["OverrideTarget"].active = true;
                    Events["setTarget2"].active = Events["OverrideTarget2"].active = false;
                }
            }
            else
            {
                Events["setTarget"].active = Events["OverrideTarget"].active = Events["setTarget2"].active = Events["OverrideTarget2"].active = false;
            }


            if (!showStats)
            {
                Fields["Dish_Range"].guiActive = Fields["Antenna_Range"].guiActive = false;
                return;
            }

            if (dishRange > 0)
            {
                Dish_Range = RTUtils.length(dishRange * 1000) + "m";

                Fields["Dish_Range"].guiName = "";

                if (showAntennaName) Fields["Dish_Range"].guiName += antennaName;
                if (showType) Fields["Dish_Range"].guiName += showAntennaName ? " dish " : "Dish ";
                Fields["Dish_Range"].guiName += (showAntennaName || showType) ? "range" : "Range";

                Fields["Dish_Range"].guiActive = true;
            }
            else
                Fields["Dish_Range"].guiActive = false;


            if (this.antennaRange > 0)
            {
                Antenna_Range = RTUtils.length(antennaRange * 1000) + "m";

                Fields["Antenna_Range"].guiName = "";

                if (showAntennaName) Fields["Antenna_Range"].guiName += antennaName;
                if (showType) Fields["Antenna_Range"].guiName += showAntennaName ? " antenna " : "Antenna ";
                Fields["Antenna_Range"].guiName += (showAntennaName || showType) ? "range" : "Range";

                Fields["Antenna_Range"].guiActive = true;
            }
            else
                Fields["Antenna_Range"].guiActive = false;

        }
    }
}