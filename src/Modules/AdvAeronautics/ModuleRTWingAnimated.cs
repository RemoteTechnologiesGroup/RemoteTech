using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class ModuleRTWingAnimated : PartModule {
        [KSPField]
        public bool FixAnimLayers = false;

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

        void setWingLift0() {
            if (!modifyWingDrag) return;
            part.minimum_drag = MinimumDrag0;
            part.maximum_drag = MaximumDrag0;

            if (part is Winglet) {
                (part as Winglet).dragCoeff = dragCoeff0;
                (part as Winglet).deflectionLiftCoeff = deflectionLiftCoeff0;
            } else if (part is ControlSurface) {
                (part as ControlSurface).dragCoeff = dragCoeff0;
                (part as ControlSurface).deflectionLiftCoeff = deflectionLiftCoeff0;
            }
        }
        void setWingLift1() {
            if (!modifyWingDrag) return;
            part.minimum_drag = MinimumDrag1;
            part.maximum_drag = MaximumDrag1;

            if (part is Winglet) {
                (part as Winglet).dragCoeff = dragCoeff1;
                (part as Winglet).deflectionLiftCoeff = deflectionLiftCoeff1;
            } else if (part is ControlSurface) {
                (part as ControlSurface).dragCoeff = dragCoeff1;
                (part as ControlSurface).deflectionLiftCoeff = deflectionLiftCoeff1;
            }
        }


        protected Animation anim {
            get {
                return part.FindModelAnimators()[0];
            }
        }

        protected AnimationState DependentAnim {
            get {
                if (mirrorState == 2)
                    return anim[InvertedFoldAnimationName];
                else
                    return anim[FoldAnimationName];
            }
        }

        void playAnim() {
            if (mirrorState == 2)
                anim.Play(InvertedFoldAnimationName);
            else
                anim.Play(FoldAnimationName);
        }

        private List<Transform> mirrorTransforms {
            get {
                List<Transform> tmp = new List<Transform>();
                RTUtil.findTransformsWithPrefix(part.transform, ref tmp, mirrorTransformPrefix);
                return tmp;
            }
        }


        [KSPAction("ActionToggle", KSPActionGroup.None, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param) {
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
                SetMode1();
            else
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                    SetMode0();
        }

        [KSPAction("Mode1Action", KSPActionGroup.None, guiName = "Mode1")]
        public void Mode1Action(KSPActionParam param) {
            if (animState == 0 && RequestActPower(Mode1EnergyCost))
                SetMode1();
        }

        [KSPAction("Mode0Action", KSPActionGroup.None, guiName = "Mode0")]
        public void Mode0Action(KSPActionParam param) {
            if (animState == 1 && RequestActPower(Mode0EnergyCost))
                SetMode0();
        }


        [KSPEvent(name = "Mode0Event", guiActive = true, guiName = "Mode1")]
        public void Mode0Event() {
            if (part.symmetryCounterparts.Count == 1) {
                if (animState == 1 && RequestActPower(Mode0EnergyCost * 2)) {
                    SetMode0();
                    (part.symmetryCounterparts[0].Modules["ModuleRTWingAnimated"] as ModuleRTWingAnimated).SetMode0();
                }
            } else {
                if (animState == 1 && RequestActPower(Mode0EnergyCost))
                    SetMode0();
            }

        }
        [KSPEvent(name = "Mode1Event", guiActive = true, guiName = "Mode1")]
        public void Mode1Event() {
            if (part.symmetryCounterparts.Count == 1) {
                if (animState == 0 && RequestActPower(Mode1EnergyCost * 2)) {
                    SetMode1();
                    (part.symmetryCounterparts[0].Modules["ModuleRTWingAnimated"] as ModuleRTWingAnimated).SetMode1();
                }
            } else {
                if (animState == 0 && RequestActPower(Mode1EnergyCost))
                    SetMode1();
            }
        }


        bool updateOthers = false;
        public void SetMode1() {
            DependentAnim.speed = Math.Abs(DependentAnim.speed);
            playAnim();

            animState = 1;

            updateOthers = true;

            Events["Mode1Event"].active = false;
            Events["Mode0Event"].active = true;
        }


        public void SetMode0() {
            DependentAnim.speed = -Math.Abs(DependentAnim.speed);
            playAnim();

            //Otherwise the animation whould insta-jump to the retracted state
            if (DependentAnim.normalizedTime == 0)
                DependentAnim.normalizedTime = 1;

            animState = 0;

            updateOthers = true;

            Events["Mode1Event"].active = true;
            Events["Mode0Event"].active = false;
        }

        bool RequestActPower(float requiredAmount) {
            if (requiredAmount <= 0)
                return true;

            float amount = part.RequestResource("ElectricCharge", requiredAmount);
            if (amount == requiredAmount)
                return true;
            else
                return false;
        }


        public void mirror(bool m) {
            foreach (Transform t in mirrorTransforms)
                t.localScale = new Vector3(1, 1, m ? -1 : 1);
        }


        void OnDestroy() {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count == 1) {
                (part.symmetryCounterparts[0].Modules["ModuleRTWingAnimated"] as ModuleRTWingAnimated).mirrorState = 0;
                (part.symmetryCounterparts[0].Modules["ModuleRTWingAnimated"] as ModuleRTWingAnimated).mirror(false);
            }
        }

        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state) {
            if (mirrorState == 2) {
                mirror(true);
            }

            if (state == StartState.Editor) {
                if (mirrorState == 0 && part.symmetryCounterparts.Count == 1) {
                    (part.symmetryCounterparts[0].Modules["ModuleRTWingAnimated"] as ModuleRTWingAnimated).mirrorState = 1;
                    mirrorState = 2;
                    mirror(true);
                }
            }

            Actions["Mode1Action"].guiName = Events["Mode1Event"].guiName = Mode1Name;
            Actions["Mode0Action"].guiName = Events["Mode0Event"].guiName = Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;

            if (animState == 1)
                SetMode1();
            else
                SetMode0();

            anim[FoldAnimationName].normalizedTime = anim[InvertedFoldAnimationName].normalizedTime = this.animState;

            anim[FoldAnimationName].wrapMode = anim[InvertedFoldAnimationName].wrapMode = WrapMode.Clamp;

            if (FixAnimLayers) {
                int i = 0;
                foreach (AnimationState s in anim) {
                    s.layer = i++;
                }
            }

            if (state == StartState.Editor) {
                if (animPlayStart == 1)
                    SetMode1();
                else if (animPlayStart == -1)
                    SetMode0();
            } else
                flightStarted = true;

        }

        bool animPlaying {
            get {
                return mirrorState == 2 ? anim.IsPlaying(InvertedFoldAnimationName) : anim.IsPlaying(FoldAnimationName);
            }
        }

        public override void OnUpdate() {
            if (!flightStarted) return;

            if (updateOthers && !animPlaying) {
                if (animState == 0)
                    setWingLift0();
                else
                    setWingLift1();
                updateOthers = false;
            }

        }


    }
}
