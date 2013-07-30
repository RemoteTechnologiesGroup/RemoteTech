using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class ModuleSpoiler : PartModule {
        [KSPField]
        public string
            Animation = "",
            Mode1Name = "Mode1",
            Mode0Name = "Mode0",
            ToggleName = "Toggle";

        [KSPField]
        public float
            MinimumDrag0 = 0,
            MaximumDrag0 = 0,
            MinimumDrag1 = 0,
            MaximumDrag1 = 0;

        [KSPField(isPersistant = true)]
        public int animState = 0;

        protected Animation anim {
            get { return part.FindModelAnimators(Animation)[0]; }
        }

        [KSPAction("ActionToggle", KSPActionGroup.Brakes, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param) {
            if (param.type == KSPActionType.Activate && animState == 0)
                SetMode1();
            else
                if (param.type == KSPActionType.Deactivate && animState == 1)
                    SetMode0();
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event() {
            if (animState == 0)
                SetMode1();
        }

        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event() {
            if (animState == 1)
                SetMode0();
        }

        void SetMode1() {
            anim[Animation].speed = Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);

            if (anim[Animation].normalizedTime == 1)
                anim[Animation].normalizedTime = 0;

            animState = 1;

            Events["Mode1Event"].active = false;
            Events["Mode0Event"].active = true;
        }

        void SetMode0() {
            anim[Animation].speed = -Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);

            if (anim[Animation].normalizedTime == 0)
                anim[Animation].normalizedTime = 1;

            animState = 0;

            Events["Mode1Event"].active = true;
            Events["Mode0Event"].active = false;
        }

        void UpdateDrag() {
            if (!HighLogic.LoadedSceneIsFlight) return;

            part.minimum_drag = (MinimumDrag0 * (1 - anim[Animation].normalizedTime)) + (MinimumDrag1 * anim[Animation].normalizedTime);
            part.maximum_drag = (MaximumDrag0 * (1 - anim[Animation].normalizedTime)) + (MaximumDrag1 * anim[Animation].normalizedTime);

            if (part.Modules.Contains("ModuleLandingGear")) {
                (part.Modules["ModuleLandingGear"] as ModuleLandingGear).stowedDragMin = (part.Modules["ModuleLandingGear"] as ModuleLandingGear).deployedDragMin = part.minimum_drag;
                (part.Modules["ModuleLandingGear"] as ModuleLandingGear).stowedDragMax = (part.Modules["ModuleLandingGear"] as ModuleLandingGear).deployedDragMax = part.maximum_drag;
            }
        }


        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state) {
            Events["Mode1Event"].guiName = Mode1Name;
            Events["Mode0Event"].guiName = Mode0Name;
            Actions["ActionToggle"].guiName = ToggleName;
            if (animState == 1)
                SetMode1();
            else
                SetMode0();

            anim[Animation].normalizedTime = this.animState;

            anim[Animation].wrapMode = WrapMode.Clamp;

            if (state != StartState.Editor)
                flightStarted = true;
        }

        public override void OnUpdate() {
            if (!flightStarted) return;

            if (anim.IsPlaying(Animation))
                UpdateDrag();
        }
    }
}
