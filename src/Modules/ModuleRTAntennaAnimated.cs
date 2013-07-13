using System;
using UnityEngine;

namespace RemoteTech {
    public class ModuleRTAntennaAnimated : ModuleRTAntenna {
        [KSPField]
        public String AnimationName = "antenna";

        [KSPField]
        public bool AnimationOneShot = true;

        [KSPField]
        public float SnappingForce = -1.0f;

        [KSPField(isPersistant = true)]
        public float AnimationState = 0.0f;

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
            if (!(AnimationState == 1.0f && AnimationOneShot)) {
                mAnimation[AnimationName].speed = state ? 1.0f : -1.0f;
                AnimationState = state ? 1.0f : 0.0f;
                mAnimation.Play(AnimationName);
            }
        }

        // Unity uses reflection to call this, so call the hidden base member too.
        public new void FixedUpdate() {
            base.FixedUpdate();
            if (SnappingForce > 0 && AnimationState == 1.0f && vessel != null) {
                if (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity * 0.5 > SnappingForce) {
                    part.decouple(0f);
                    SnappingForce = -1.0f;
                }
            }
        }
    }
}
