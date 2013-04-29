using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAnimatedWing : PartModule
    {
        [KSPField]
        public bool fixAnimLayers = false;

        [KSPField]
        public string
        FoldAnimationName = "",
        InvertedFoldAnimationName = "",
        mirrorTransformPrefix = "",
        Mode1Name = "Fold wings",
        Mode0Name = "Unfold wings",
        ToggleName = "Toggle wings";

        [KSPField(isPersistant = true)]
        public int mirrorState = 0, animState = 0;

        [KSPField]
        public bool modifyWingDrag = true;

        [KSPField]
        public float
        animPlayStart = 0,
        MinimumDrag0 = 0,
        MaximumDrag0 = 0,
        MinimumDrag1 = 0,
        MaximumDrag1 = 0,
        dragCoeff0 = 0,
        dragCoeff1 = 0,
        deflectionLiftCoeff0 = 0,
        deflectionLiftCoeff1 = 0,
        MaxQ = -1,
        Mode0EnergyCost = 0,
        Mode1EnergyCost = 0;

        void setWingLift0()
        {
            if (!modifyWingDrag) return;
            part.minimum_drag = MinimumDrag0;
            part.maximum_drag = MaximumDrag0;

            if (part is Winglet)
            {
                (part as Winglet).dragCoeff = dragCoeff0;
                (part as Winglet).deflectionLiftCoeff = deflectionLiftCoeff0;
            }
            else if (part is ControlSurface)
            {
                (part as ControlSurface).dragCoeff = dragCoeff0;
                (part as ControlSurface).deflectionLiftCoeff = deflectionLiftCoeff0;
            }
        }
        void setWingLift1()
        {
            if (!modifyWingDrag) return;
            part.minimum_drag = MinimumDrag1;
            part.maximum_drag = MaximumDrag1;

            if (part is Winglet)
            {
                (part as Winglet).dragCoeff = dragCoeff1;
                (part as Winglet).deflectionLiftCoeff = deflectionLiftCoeff1;
            }
            else if (part is ControlSurface)
            {
                (part as ControlSurface).dragCoeff = dragCoeff1;
                (part as ControlSurface).deflectionLiftCoeff = deflectionLiftCoeff1;
            }
        }


        protected Animation anim
        {
            get
            {
                return part.FindModelAnimators()[0];
            }
        }

        void playAnim()
        {
            if (mirrorState == 2)
                anim.Play(InvertedFoldAnimationName);
            else
                anim.Play(FoldAnimationName);
        }

        bool inControl
        {
            get
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return true;

                if (RTGlobals.coreList.ContainsKey(this.vessel))
                {
                    return (RTGlobals.coreList[this.vessel].InContact || RTGlobals.coreList[this.vessel].localControl);
                }

                return true;
            }
        }

        private List<Transform> mirrorTransforms
        {
            get
            {
                List<Transform> tmp = new List<Transform>();
                RTUtils.findTransformsWithPrefix(part.transform, ref tmp, mirrorTransformPrefix);
                return tmp;
            }
        }


        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            if (!inControl) return;
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
                SetMode1();
            else
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                    SetMode0();
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param)
        {
            if (inControl && animState == 0 && RequestActPower(Mode1EnergyCost))
                SetMode1();
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param)
        {
            if (inControl && animState == 1 && RequestActPower(Mode0EnergyCost))
                SetMode0();
        }


        [KSPEvent(name = "Mode0Event", guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (!inControl) return;

            if (part.symmetryCounterparts.Count == 1)
            {
                if (animState == 1 && RequestActPower(Mode0EnergyCost * 2))
                {
                    SetMode0();
                    (part.symmetryCounterparts[0].Modules["ModuleRTAnimatedWing"] as ModuleRTAnimatedWing).SetMode0();
                }
            }
            else
            {
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                    SetMode0();
            }

        }
        [KSPEvent(name = "Mode1Event", guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (!inControl) return;

            if (part.symmetryCounterparts.Count == 1)
            {
                    if (animState == 0 && RequestActPower(Mode1EnergyCost * 2))
                    {
                        SetMode1();
                        (part.symmetryCounterparts[0].Modules["ModuleRTAnimatedWing"] as ModuleRTAnimatedWing).SetMode1();
                    }
            }
            else
            {
                    if (animState == 0 && RequestActPower(Mode1EnergyCost))
                        SetMode1();
            }
        }


        bool updateOthers = false;
        public void SetMode1()
        {
            foreach (AnimationState s in anim)
                s.speed = Mathf.Abs(s.speed);
            playAnim();

            animState = 1;

            updateOthers = true;

            Events["Mode1Event"].active = false;
            Events["Mode0Event"].active = true;
        }


        public void SetMode0()
        {
            foreach (AnimationState s in anim)
                s.speed = -s.speed;

            playAnim();

            //Otherwise the animation whould insta-jump to the retracted state
            foreach (AnimationState s in anim)
                if (s.normalizedTime == 0)
                    s.normalizedTime = 1;

            animState = 0;

            updateOthers = true;

            Events["Mode1Event"].active = true;
            Events["Mode0Event"].active = false;
        }

        bool RequestActPower(float requiredAmount)
        {
            if (requiredAmount <= 0)
                return true;

            float amount = part.RequestResource("ElectricCharge", requiredAmount);
            if (amount == requiredAmount)
                return true;
            else
                return false;
        }


        public void mirror(bool m)
        {
            foreach (Transform t in mirrorTransforms)
                t.localScale = new Vector3(1, 1, m ? -1 : 1);
        }


        void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count == 1)
            {
                (part.symmetryCounterparts[0].Modules["ModuleRTAnimatedWing"] as ModuleRTAnimatedWing).mirrorState = 0;
                (part.symmetryCounterparts[0].Modules["ModuleRTAnimatedWing"] as ModuleRTAnimatedWing).mirror(false);
            }
        }

        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
            if (mirrorState == 2)
            {
                mirror(true);
            }

            if (state == StartState.Editor)
            {
                if (mirrorState == 0 && part.symmetryCounterparts.Count == 1)
                {
                    (part.symmetryCounterparts[0].Modules["ModuleRTAnimatedWing"] as ModuleRTAnimatedWing).mirrorState = 1;
                    mirrorState = 2;
                    mirror(true);
                }
            }

            Actions["Mode1Action"].guiName = Events["Mode1Event"].guiName = Mode1Name;
            Actions["Mode0Action"].guiName = Events["Mode0Event"].guiName= Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;        

            if (animState == 1)
                SetMode1();
            else
                SetMode0();

            anim[FoldAnimationName].normalizedTime = anim[InvertedFoldAnimationName].normalizedTime = this.animState;

            anim[FoldAnimationName].wrapMode= anim[InvertedFoldAnimationName].wrapMode = WrapMode.Clamp;

            if (fixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in anim)
                {
                    s.layer = i;
                    i++;
                }
            }

            if (state == StartState.Editor)
            {
                if (animPlayStart == 1)
                    SetMode1();
                else if (animPlayStart == -1)
                    SetMode0();
            }
            else
                flightStarted = true;

        }

        bool animPlaying
        {
            get
            {
                return mirrorState == 2 ? anim.IsPlaying(InvertedFoldAnimationName) : anim.IsPlaying(FoldAnimationName);
            }
        }

        int explodeMe = 0;
        public override void  OnUpdate()
        {
            if (!flightStarted) return;

            if (updateOthers && !animPlaying)
            {
                if (animState == 0)
                    setWingLift0();
                else
                    setWingLift1();
                updateOthers = false;
            }

            if (vessel != null && !vessel.HoldPhysics)
            {
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
