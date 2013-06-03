using System;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAntennaAnimated : ModuleRTAntenna {

        Animation mAnimation;

        [KSPField]
        public String AnimationName = "antenna";

        public override void OnStart(StartState state) {
            mAnimation = part.FindModelAnimators(AnimationName)[0];
            if (mAnimation == null || mAnimation[AnimationName] == null) {
                RTUtil.Log("ModuleRTAntennaAnimated: Animation error");
                enabled = false;
                return;
            } else {
                mAnimation[AnimationName].normalizedTime = IsRTActive ? 1.0f : 0.0f;
            }
            base.OnStart(state);
        }

        public override void SetState(bool state) {
            mAnimation[AnimationName].normalizedTime = IsRTActive ? 1.0f : 0.0f;
            mAnimation[AnimationName].speed = state ? 1.0f : -1.0f;
            mAnimation.Play(AnimationName);
            base.SetState(state);
        }
    }
}

