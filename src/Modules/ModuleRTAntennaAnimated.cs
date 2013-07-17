using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public class ModuleRTAntennaAnimated : ModuleRTAntenna
    {
        [KSPField]
        public String
            AnimationName = "antenna",
            BreakTransformName = "";

        [KSPField]
        public bool
            AnimationOneShot = true,
            FixAnimLayers = false,
            UseTransformDir = false;

        [KSPField]
        public float
            SnappingForce = -1.0f,
            ShrapnelDragCoeff = 2,
            ShrapnelDensity = 1;

        [KSPField(isPersistant = true)]
        public float AnimationState = 0.0f;

        private Animation mAnimation;

        private Transform BreakTransform;

        public override void OnStart(StartState state)
        {

            if (BreakTransformName != "")
                BreakTransform = part.FindModelTransform(BreakTransformName);

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
                RTUtil.findTransformsWithCollider(BreakTransform, ref toRemove);
                foreach (Transform t in toRemove)
                    Destroy(t.gameObject);
                toRemove.Clear();

                enabled = false;

                IsRTAntenna = false;

                SetState(false);
                return;
            }

            base.OnStart(state);

        }

        public override void SetState(bool state)
        {
            base.SetState(state);
            if (!(AnimationState == 1.0f && AnimationOneShot))
            {
                mAnimation[AnimationName].speed = state ? 1.0f : -1.0f;
                AnimationState = state ? 1.0f : 0.0f;
                mAnimation.Play(AnimationName);
            }
        }

        // Unity uses reflection to call this, so call the hidden base member too.
        public new void FixedUpdate()
        {
            if (Broken) return;
            base.FixedUpdate();
            if (SnappingForce > 0 && vessel != null && vessel.atmDensity > 0 && AnimationState == 1.0f)
            {
                if (BreakTransform == null)
                {
                    if ((Math.Pow(vessel.srf_velocity.magnitude, 2) * vessel.atmDensity * 0.5) > SnappingForce)
                    {
                        part.decouple(0f);
                        SnappingForce = -1.0f;
                    }
                }
                else
                {
                    if (UseTransformDir)
                    {
                        if ((Math.Pow(RTUtil.DirectionalSpeed(BreakTransform.up, vessel.srf_velocity), 2) * vessel.atmDensity * 0.5) > SnappingForce)
                            BreakApart();
                    }
                    else if ((Math.Pow(vessel.srf_velocity.magnitude, 2) * vessel.atmDensity * 0.5) > SnappingForce)
                        BreakApart();
                }
            }
        }

        private void BreakApart()
        {
            HashSet<Transform> toRemove = new HashSet<Transform>();
            RTUtil.findTransformsWithCollider(BreakTransform, ref toRemove);

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
