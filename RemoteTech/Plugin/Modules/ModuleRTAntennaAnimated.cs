using System;
using UnityEngine;

namespace RemoteTech {
    public class ModuleRTAntennaAnimated : ModuleRTAntenna {
        [KSPField] public String AnimationName = "antenna";

        private Animation mAnimation;

        public override void OnStart(StartState state) {
            mAnimation = part.FindModelAnimators(AnimationName)[0];
            if (mAnimation == null || mAnimation[AnimationName] == null) {
                RTUtil.Log("ModuleRTAntennaAnimated: Animation error");
                enabled = false;
                return;
            }

            mAnimation[AnimationName].speed = IsRTActive ? 1.0f : -1.0f;
            mAnimation[AnimationName].normalizedTime = IsRTActive ? 1.0f : 0.0f;
            mAnimation.Play(AnimationName);
            base.OnStart(state);
        }

        public override void SetState(bool state) {
            base.SetState(state);
            mAnimation[AnimationName].speed = state ? 1.0f : -1.0f;
            mAnimation.Play(AnimationName);
        }
    }
}
