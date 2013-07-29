using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class ModuleRTAntennaAnimated : ModuleRTAntenna {
        [KSPField]
        public String
            AnimationName = "antenna",
            BreakTransformName = "",
            ForceTransformName = "";

        [KSPField]
        public bool
            AnimationOneShot = true,
            FixAnimLayers = false;

        [KSPField]
        public float
            SnappingForce = 0.0f,
            ShrapnelDragCoeff = 2,
            ShrapnelDensity = 1;

        [KSPField(isPersistant = true)]
        public float AnimationState = 0.0f;

        public Animation Animation { get; protected set; }
        public Transform BreakTransform { get; protected set; }
        public Transform ForceTransform { get; protected set; }

        public override void OnStart(StartState state) {
            if (!String.IsNullOrEmpty(BreakTransformName)) {
                BreakTransform = part.FindModelTransform(BreakTransformName);
            }
            if (!String.IsNullOrEmpty(ForceTransformName)) {
                ForceTransform = part.FindModelTransform(ForceTransformName);
            }

            Animation = part.FindModelAnimators(AnimationName)[0];
            if (Animation == null || Animation[AnimationName] == null) {
                RTUtil.Log("ModuleRTAntennaAnimated: Animation error");
                enabled = false;
                return;
            }

            if (FixAnimLayers) {
                int i = 0;
                foreach (AnimationState s in Animation) {
                    s.layer = i++;
                }
            }

            base.OnStart(state);

            if (IsRTBroken && BreakTransform != null) {
                foreach (Transform t in RTUtil.FindTransformsWithCollider(BreakTransform)) {
                    Destroy(t.gameObject);
                }
                enabled = false;
                return;
            }

            Animation[AnimationName].speed = IsRTActive ? 1.0f : -1.0f;
            Animation[AnimationName].normalizedTime = IsRTActive ? 1.0f : 0.0f;
            Animation.Play(AnimationName);
        }

        public override void SetState(bool state) {

            if (IsRTActive && AnimationOneShot)
                return;

            base.SetState(state);

            if (!IsRTActive && Animation[AnimationName].normalizedTime == 0.0f) {
                Animation[AnimationName].normalizedTime = 1.0f;
            }
            else if (IsRTActive && Animation[AnimationName].normalizedTime == 1.0f) {
                Animation[AnimationName].normalizedTime = 0.0f;
            }
            Animation[AnimationName].speed = IsRTActive ? 1.0f : -1.0f;
            AnimationState = IsRTActive ? 1.0f : 0.0f;
            Animation.Play(AnimationName);

            if (IsRTActive && AnimationOneShot)
                Events["EventClose"].active = false;
        }

        // Unity uses reflection to call this, so call the hidden base member too.
        public new void FixedUpdate() {
            base.FixedUpdate();

            // TODO: Clean this shit up.
            if (IsRTBroken || vessel == null || part == null || SnappingForce < 0) {
                return;
            }
            if (vessel.atmDensity > 0 && AnimationState > 0.0f && SnappingForce > 0 && !vessel.HoldPhysics) {
                if (ForceTransform == null) {
                    if (vessel.srf_velocity.sqrMagnitude * vessel.atmDensity / 2 > SnappingForce) {
                        if (BreakTransform == null) {
                            part.decouple(0.0f);
                            SnappingForce = -1.0f;
                        }
                        else {
                            Break();
                        }
                    }
                }
                else {
                    if (Math.Pow(Vector3d.Dot(ForceTransform.up, vessel.srf_velocity), 2) * vessel.atmDensity * 0.5 > SnappingForce) {
                        Break();
                    }
                }
            }
        }

        private void Break() {
            foreach (Transform t in RTUtil.FindTransformsWithCollider(BreakTransform)) {
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
            Fields["GUI_EnergyReq"].guiActive = false;

            IsRTBroken = true;
            base.SetState(false);
        }
    }
}
