using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace RemoteTech
{
    public class ModuleRTLoopAnimAntenna : RemoteTechAntennaCore
    {
        [KSPField]
        public string
        Animation = "",
        Mode1Name = "Mode1",
        Mode0Name = "Mode0",
        ToggleName = "Toggle";

        [KSPField(isPersistant = true)]
        public bool Locked = false;

        [KSPField]
        public bool
            waitForAnimEnd = false,
            fixAnimLayers = false,
            MasterOf0 = false,
            MasterOf1 = false,
            MasterOfLoop0 = false,
            MasterOfLoop1 = false,
            LoopLock = false,
            ModeLock = false,
            willWakeInPanic = false;

        [KSPField(isPersistant = true)]
        public int
        animState = 0;

        [KSPField]
        public float
        animPlayStart = 0,
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


        protected Animation anim
        {
            get { return part.FindModelAnimators(Animation)[0]; }
        }


        void act0()
        {
            if (MasterOfLoop0)
            {
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode0();
            }
            else
                SetMode0();


            if (MasterOf0)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode0();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode0();
            }

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.Locked = true;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                        m.Locked = true;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    if(m != this)
                    m.Locked = true;

            part.SendMessage("UpdateGUI");
        }

        void act1()
        {
            if (MasterOfLoop1)
            {
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    m.SetMode1();
            }
            else
                SetMode1();


            if (MasterOf1)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.SetMode1();

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.SetMode1();
            }

            if (ModeLock)
            {
                foreach (ModuleRTModalAntenna m in part.Modules.OfType<ModuleRTModalAntenna>())
                    m.Locked = false;

                foreach (ModuleRTAnimatedAntenna m in part.Modules.OfType<ModuleRTAnimatedAntenna>())
                    m.Locked = false;
            }

            if (LoopLock)
                foreach (ModuleRTLoopAnimAntenna m in part.Modules.OfType<ModuleRTLoopAnimAntenna>())
                    if (m != this)
                        m.Locked = false;

            part.SendMessage("UpdateGUI");
        }


        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
            else
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                {
                    act0();
                }
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param)
        {
            if (Locked) return;
            if (InControl && animState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }
        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (Locked) return;
            if (!InControl) return;
            if (animState == 1 && RequestActPower(Mode0EnergyCost))
            {
                act0();
            }
        }


        [KSPEvent(name = "OverrideMode1Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode1Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
            {
                act1();
            }
        }

        [KSPEvent(name = "OverrideMode0Event", active = false, guiName = "Mode1", guiActiveUnfocused = true, unfocusedRange = 5, externalToEVAOnly = true)]
        public void OverrideMode0Event()
        {
            if (Locked) return;
            if (!powered) return;
            if (animState == 1 && RequestActPower(Mode0EnergyCost))
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
                if (animState == 1)
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
            if (waitForAnimEnd)
                anim[Animation].wrapMode = WrapMode.Loop;

            anim.Play(Animation);

            animState = 1;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag + Dragmodifier;
                part.maximum_drag = this.MaximumDrag + Dragmodifier;
            }

            SetRange1();
        }
        void SetRange1()
        {
            EnergyDrain = EnergyDrain1;
            antennaRange = antennaRange1;
            dishRange = dishRange1;

            if (!HighLogic.LoadedSceneIsFlight) return;
            RTGlobals.network = new RelayNetwork();
            try
            {
                RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
            }
            catch { }
            UpdatePA();
        }
        public void SetMode0()
        {
            if (waitForAnimEnd)
                anim[Animation].wrapMode = WrapMode.Clamp;
            else
            anim.Stop(Animation);

            animState = 0;

            if (this.MaximumDrag > 0)
            {
                part.minimum_drag = this.MinimumDrag;
                part.maximum_drag = this.MaximumDrag;
            }

            SetRange0();
        }
        void SetRange0()
        {
            EnergyDrain = EnergyDrain0;
            antennaRange = antennaRange0;
            dishRange = dishRange0;

            if (!HighLogic.LoadedSceneIsFlight) return;
            RTGlobals.network = new RelayNetwork();
            try
            {
                RTGlobals.coreList[vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[vessel].Rnode);
            }
            catch { }
            UpdatePA();
        }

        public override void OnStart(PartModule.StartState state)
        {
            Actions["Mode1Action"].guiName = Events["Mode1Event"].guiName = Mode1Name;
            Actions["Mode0Action"].guiName = Events["Mode0Event"].guiName = Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;

            Events["OverrideMode1Event"].guiName = "Override " + Mode1Name;
            Events["OverrideMode0Event"].guiName = "Override " + Mode0Name;

            if (animState == 1)
                act1();
            else
                act0();

            if (fixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in anim)
                {
                    s.layer = i;
                    i++;
                }
            }

            anim[Animation].wrapMode = WrapMode.Loop;

            if (state == StartState.Editor)
            {
                if (animPlayStart == 1)
                    SetMode1();
                else if (animPlayStart == -1)
                    SetMode0();
            }

            base.OnStart(state);
        }


        bool RequestActPower(float requiredAmount)
        {
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


            if (EnergyDrain1 > 0)
                RequestPower();


            if (vessel != null && !vessel.HoldPhysics)
            {
                if (willWakeInPanic && animState == 0 && !InControl)
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

                if (MaxQ > 0 && animState == 1 && (vessel.srf_velocity.magnitude * vessel.srf_velocity.magnitude * vessel.atmDensity * 0.5) > MaxQ)
                {
                    part.decouple(0f);
                    explodeMe = 10;
                }
            }

        }


    }
}
