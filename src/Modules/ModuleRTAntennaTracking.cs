using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class ModuleRTAntennaTracking : ModuleRTAntennaAnimated {
        public enum TrackingModes {
            Retracted,
            Extending,
            Tracking,
            Resetting,
            Retracting,
            Broken
        }

        [KSPField]
        public String
            Pivot1Name = "",
            Pivot2Name = "";

        [KSPField]
        public float
            Pivot1Speed = 1,
            Pivot2Speed = 1;

        [KSPField]
        public Vector2
            Pivot1Range = Vector2.zero,
            Pivot2Range = Vector2.zero;

        private Pivot mPivot1, mPivot2;
        private TrackingModes TrackingMode = TrackingModes.Retracted;

        public override void OnStart(StartState state) {
            if (!String.IsNullOrEmpty(Pivot1Name) && !String.IsNullOrEmpty(Pivot2Name)) {
                ForceTransform = part.FindModelTransform(Pivot2Name);
                BreakTransform = part.FindModelTransform(Pivot1Name);
            } else {
                RTUtil.Log("ModuleRTAntennaAnimated: Pivot error");
                enabled = false;
                return;
            }

            if (IsRTBroken) {
                TrackingMode = TrackingModes.Broken;
            }

            base.OnStart(state);

            if (RTCore.Instance != null) {
                mPivot1 = new Pivot(BreakTransform, Pivot1Speed, Pivot1Range);
                mPivot2 = new Pivot(ForceTransform, Pivot2Speed, Pivot2Range);

                if (IsRTActive) {
                    TrackingMode = TrackingModes.Tracking;
                    mPivot1.SnapToTarget(new DynamicTarget(RTAntennaTargetGuid));
                    mPivot2.SnapToTarget(new DynamicTarget(RTAntennaTargetGuid));
                }
            }
        }

        public override void SetState(bool state) {
            if (IsRTActive) {
                if (AnimationOneShot) return;
                if (TrackingMode == TrackingModes.Tracking)
                    TrackingMode = TrackingModes.Resetting;
                else {
                    TrackingMode = TrackingModes.Retracting;
                    base.SetState(state);
                }
            } else {
                if (TrackingMode == TrackingModes.Resetting)
                    TrackingMode = TrackingModes.Tracking;
                else {
                    TrackingMode = TrackingModes.Extending;
                    base.SetState(state);
                }
            }
        }

        public new void FixedUpdate() {
            base.FixedUpdate();

            if (IsRTBroken || !RTCore.Instance) {
                return;
            }

            switch (TrackingMode) {
                case TrackingModes.Tracking:
                    if (IsRTPowered) {
                        mPivot1.RotToTarget(new DynamicTarget(RTAntennaTargetGuid));
                        mPivot2.RotToTarget(new DynamicTarget(RTAntennaTargetGuid));
                    }
                    break;
                case TrackingModes.Extending:
                    if (!Animation.IsPlaying(AnimationName))
                        TrackingMode = TrackingModes.Tracking;
                    break;
                case TrackingModes.Retracting:
                    if (!Animation.IsPlaying(AnimationName))
                        TrackingMode = TrackingModes.Retracted;
                    break;
                case TrackingModes.Resetting:
                    if (IsRTPowered && mPivot1.RotToOrigin() & mPivot2.RotToOrigin()) {
                        TrackingMode = TrackingModes.Retracting;
                        base.SetState(false);
                    }
                    break;
            }
        }
    }
}
