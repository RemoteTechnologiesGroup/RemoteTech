using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTTrackAntennaAnimated : ModuleRTAntenna
    {
        public enum TrackingModes
        {
            RETRACTED,
            EXTENDING,
            TRACKING,
            RESETTING,
            RETRACTING,
            BROKEN
        }

        [KSPField]
        public String
            AnimationName = "antenna",
            Pivot1Name = "",
            Pivot2Name = "";

        [KSPField]
        public bool
            AnimationOneShot = true,
            FixAnimLayers = false;

        [KSPField]
        public float
            SnappingForce = -1.0f,
            ShrapnelDragCoeff = 2,
            ShrapnelDensity = 1,
            Pivot1Speed = 1,
            Pivot2Speed = 1;

        [KSPField]
        public Vector2
            Pivot1Range = Vector2.zero,
            Pivot2Range = Vector2.zero;

        private Animation mAnimation;

        private Pivot Pivot1, Pivot2;
        Transform Pivot2Dir;

        TrackingModes TrackingMode = TrackingModes.RETRACTED;

        bool FlightStarted = false;
        public override void OnStart(StartState state)
        {
            if (Pivot1Name != "" && Pivot2Name != "")
            {
                Pivot2Dir = part.FindModelTransform(Pivot2Name);
            }
            else
                RTUtil.Log("ModuleRTAntennaAnimated: Pivot error");

            mAnimation = part.FindModelAnimators(AnimationName)[0];
            if (mAnimation == null || mAnimation[AnimationName] == null)
            {
                RTUtil.Log("ModuleRTAntennaAnimated: Animation error");
                enabled = false;
                return;
            }

            if (FixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in mAnimation)
                {
                    s.layer = i;
                    i++;
                }
            }

            mAnimation[AnimationName].speed = IsRTActive ? 1.0f : -1.0f;
            mAnimation[AnimationName].normalizedTime = IsRTActive ? 1.0f : 0.0f;
            mAnimation.Play(AnimationName);

            if (Broken)
            {
                HashSet<Transform> toRemove = new HashSet<Transform>();
                RTUtil.findTransformsWithCollider(part.FindModelTransform(Pivot1Name), ref toRemove);
                foreach (Transform t in toRemove)
                    Destroy(t.gameObject);
                toRemove.Clear();

                IsRTAntenna = false;

                SetState(false);
                TrackingMode = TrackingModes.BROKEN;
                return;
            }

            base.OnStart(state);

            if (state != StartState.Editor)
            {
                FlightStarted = true;
                Pivot1 = new Pivot(part.FindModelTransform(Pivot1Name), Pivot1Speed, Pivot1Range);
                Pivot2 = new Pivot(part.FindModelTransform(Pivot2Name), Pivot2Speed, Pivot2Range);

                if (IsRTActive)
                {
                    TrackingMode = TrackingModes.TRACKING;
                    if (!DynamicTarget.NoTarget)
                    {
                        Pivot1.SnapToTarget(DynamicTarget);
                        Pivot2.SnapToTarget(DynamicTarget);
                    }
                }
            }
        }

        public override void SetState(bool state)
        {
            base.SetState(state);
            if (!(IsRTActive && AnimationOneShot))
            {
                if (state)
                {
                    if (TrackingMode == TrackingModes.RESETTING)
                        TrackingMode = TrackingModes.TRACKING;
                    else
                    {
                        TrackingMode = TrackingModes.EXTENDING;

                        mAnimation[AnimationName].speed = Math.Abs(mAnimation[AnimationName].speed);
                        mAnimation.Play(AnimationName);
                    }
                }
                else
                {
                    if (TrackingMode == TrackingModes.TRACKING)
                        TrackingMode = TrackingModes.RESETTING;
                    else
                    {
                        TrackingMode = TrackingModes.RETRACTING;

                        mAnimation[AnimationName].speed = -Math.Abs(mAnimation[AnimationName].speed);
                        mAnimation.Play(AnimationName);
                    }
                }
            }
        }


        // Unity uses reflection to call this, so call the hidden base member too.
        public new void FixedUpdate()
        {
            if (Broken || !FlightStarted) return;
            base.FixedUpdate();


            switch (TrackingMode)
            {
                case TrackingModes.TRACKING:
                    if (IsPowered)
                    {
                        Pivot1.RotToTarget(DynamicTarget);
                        Pivot2.RotToTarget(DynamicTarget);

                        if (SnappingForce > 0 && vessel != null && vessel.atmDensity > 0 && (Math.Pow(RTUtil.DirectionalSpeed(Pivot2Dir.up, vessel.srf_velocity), 2) * vessel.atmDensity * 0.5) > SnappingForce)
                            BreakApart();
                    }
                    break;
                case TrackingModes.EXTENDING:
                    if (!mAnimation.IsPlaying(AnimationName))
                        TrackingMode = TrackingModes.TRACKING;
                    break;
                case TrackingModes.RETRACTING:
                    if (!mAnimation.IsPlaying(AnimationName))
                        TrackingMode = TrackingModes.RETRACTED;
                    break;
                case TrackingModes.RESETTING:
                    if (IsPowered && Pivot1.RotToOrigin() & Pivot2.RotToOrigin())
                    {
                        mAnimation[AnimationName].speed = -Mathf.Abs(mAnimation[AnimationName].speed);

                        mAnimation.Play(AnimationName);

                        if (mAnimation[AnimationName].normalizedTime == 0)
                            mAnimation[AnimationName].normalizedTime = 1;
                        TrackingMode = TrackingModes.RETRACTING;
                    }
                    break;
            }
        }

        private void BreakApart()
        {
            HashSet<Transform> toRemove = new HashSet<Transform>();
            RTUtil.findTransformsWithCollider(part.FindModelTransform(Pivot1Name), ref toRemove);

            foreach (Transform t in toRemove)
            {
                Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();

                rb.angularDrag = 0;
                rb.angularVelocity = part.rigidbody.angularVelocity;
                rb.drag = 0;
                rb.mass = t.collider.bounds.size.x * t.collider.bounds.size.y * t.collider.bounds.size.z * ShrapnelDensity;
                rb.velocity = part.rigidbody.velocity;
                rb.isKinematic = false;
                t.parent = null;
                rb.AddForce(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5));
                rb.AddTorque(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20));

                DragModel dm = t.gameObject.AddComponent<DragModel>();
                dm.enabled = true;
                dm.dc = ShrapnelDragCoeff;
                dm.mb = vessel.mainBody;
            }

            Events["EventOpen"].guiActive =
            Events["EventClose"].guiActive =
            Events["EventToggle"].guiActive =
            Events["EventTarget"].guiActive =
            Events["EventOpen"].active =
            Events["EventClose"].active =
            Events["EventToggle"].active =
            Events["EventTarget"].active = false;

            Fields["GUI_OmniRange"].guiActive =
            Fields["GUI_DishRange"].guiActive =
            Fields["GUI_EnergyReq"].guiActive =
            Fields["GUI_Status"].guiActive = false;

            Broken = true;
            IsRTAntenna = false;
            base.OnDestroy();
        }


    }
}
