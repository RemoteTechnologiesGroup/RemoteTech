using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTModalAntenna : RemoteTechAntennaCore
    {
        [KSPField]
        public string
        Mode1Name = "Mode1",
        Mode0Name = "Mode0",
        ToggleName = "Toggle";

        [KSPField(isPersistant = true)]
        public bool Locked = false;

        [KSPField]
        public bool
            MasterOf0 = false,
            MasterOf1 = false,
            MasterOfLoop0 = false,
            MasterOfLoop1 = false,
            LoopLock = false,
            ModeLock = false,
            willWakeInPanic = false;

        [KSPField(isPersistant = true)]
        public int
        modeState = 0;

        [KSPField]
        public float
        MinimumDrag = 0,
        MaximumDrag = 0,
        Dragmodifier = 0,
        MaxQ = -1,
        EnergyDrain0 = 0,
        EnergyDrain1 = 0,
        Mode0EnergyCost = 0,
        Mode1EnergyCost = 0,
        antennaRange0 = 0,
        antennaRange1 = 0,
        dishRange0 = 0,
        dishRange1 = 0;

        public override string GetInfo()
        {
            string text = "";

            if (antennaRange0 != antennaRange1)
            {
                if (text.Length > 0) text += "\n";
                text += "Antenna range: " + RTUtils.length(this.antennaRange0 * 1000) + "m / " + RTUtils.length(this.antennaRange1 * 1000) + "m";
            }
            else if (antennaRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Antenna range: " + RTUtils.length(antennaRange * 1000) + "m";
            }

            if (dishRange0 != dishRange1)
            {
                if (text.Length > 0) text += "\n";
                text += "Dish range: " + RTUtils.length(dishRange0 * 1000) + "m / " + RTUtils.length(dishRange1 * 1000) + "m";
            }
            else if (dishRange > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Dish range: " + RTUtils.length(dishRange * 1000) + "m";
            }

            if (EnergyDrain0 != EnergyDrain1)
            {
                if (text.Length > 0) text += "\n";
                text += "Energy req.: " + (EnergyDrain0 * 60).ToString("0.00") + "/min. / " + (EnergyDrain1 * 60).ToString("0.00") + "/min.";
            }
            else if (this.EnergyDrain > 0)
            {
                if (text.Length > 0) text += "\n";
                text += "Energy req.: " + (EnergyDrain * 60).ToString("0.00") + "/min.";
            }

            return text;
        }

        void act0()
        {
            if (MasterOfLoop0)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode0();


            if (MasterOf0)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode0();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode0();
            }
            else
                SetMode0();

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    if (m != this)
                        m.Locked = true;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.Locked = true;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.Locked = true;

            part.SendMessage("UpdateGUI");
        }

        void act1()
        {
            if (MasterOfLoop1)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode1();

            if (MasterOf1)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode1();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode1();
            }
            else
                SetMode1();

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    if (m != this)
                        m.Locked = false;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.Locked = false;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.Locked = false;

            part.SendMessage("UpdateGUI");
        }


        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            if (Locked) return;
            if (!InControl) return;
            if (modeState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
            else
                if (modeState == 1 && RequestActPower(Mode0EnergyCost))
                {
                    act0();
                }
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && modeState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && modeState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (modeState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }
        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (modeState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "OverrideMode1Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode1Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (modeState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPEvent(name = "OverrideMode0Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode0Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (modeState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }

        public void UpdateGUI()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (Locked)
            {
                Events["Mode1Event"].active = Events["OverrideMode1Event"].active = Events["Mode0Event"].active = Events["OverrideMode0Event"].active = false;
            }
            else
            {
                if (modeState == 1)
                {
                    Events["Mode1Event"].active = Events["OverrideMode1Event"].active = false;
                    Events["Mode0Event"].active = Events["OverrideMode0Event"].active = true;
                }
                else
                {
                    Events["Mode1Event"].active = Events["OverrideMode1Event"].active = true;
                    Events["Mode0Event"].active = Events["OverrideMode0Event"].active = false;
                }
            }
        }

        public void SetMode1()
        {
            if (!RequestActPower(Mode1EnergyCost)) return;

            modeState = 1;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag + Dragmodifier;
                part.maximum_drag = this.MaximumDrag + Dragmodifier;
            }

            EnergyDrain = EnergyDrain1;
            antennaRange = antennaRange1;
            dishRange = dishRange1;

            if (HighLogic.LoadedSceneIsFlight)
            {
                RTGlobals.network = new RelayNetwork();
                try
                {
                    RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
                }
                catch { }
                UpdatePA();
            }
        }


        public void SetMode0()
        {
            if (!RequestActPower(Mode0EnergyCost)) return;

            modeState = 0;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag;
                part.maximum_drag = this.MaximumDrag;
            }

            EnergyDrain = EnergyDrain0;
            antennaRange = antennaRange0;
            dishRange = dishRange0;

            if (HighLogic.LoadedSceneIsFlight)
            {
                RTGlobals.network = new RelayNetwork();
                try
                {
                    RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
                }
                catch { }
                UpdatePA();
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            Actions["Mode1Action"].guiName = Events["Mode1Event"].guiName = Mode1Name;
            Actions["Mode0Action"].guiName = Events["Mode0Event"].guiName = Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;

            Events["OverrideMode1Event"].guiName = "Override " + Mode1Name;
            Events["OverrideMode0Event"].guiName = "Override " + Mode0Name;

            if (state == StartState.Editor) return;

            if (modeState == 1)
                act1();
            else
                act0();

            base.OnStart(state);
        }


        bool RequestActPower(float requiredAmount)
        {
            if (!HighLogic.LoadedSceneIsFlight) return true;

            if (requiredAmount == 0)
                return true;

            float amount = part.RequestResource("ElectricCharge", requiredAmount);
            if (amount == requiredAmount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void RequestPower()
        {
            if (EnergyDrain == 0)
            {
                powered = true;
            }
            else
            {
                float amount = part.RequestResource("ElectricCharge", EnergyDrain * TimeWarp.deltaTime);
                powered = amount != 0;
            }
        }

        int explodeMe = 0;
        public override void OnUpdate()
        {
            if (!flightStarted) return;

            if (vessel != null && !vessel.HoldPhysics)
            {

                if (willWakeInPanic && modeState == 0 && !InControl)
                {
                    SetMode1();
                    UpdateGUI();
                }

                if (explodeMe > 0)
                {
                    explodeMe--;
                    if (explodeMe == 0)
                        part.explode();
                }

                if (MaxQ > 0 && modeState == 1 && (vessel.srf_velocity.magnitude * vessel.srf_velocity.magnitude * vessel.atmDensity * 0.5) > MaxQ)
                {
                    part.decouple(0f);
                    explodeMe = 10;
                }
            }
            if (EnergyDrain1 > 0)
                RequestPower();
        }
    }
}
