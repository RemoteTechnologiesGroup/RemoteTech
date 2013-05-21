using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class VectorFin
    {
        Transform Pivot,
            Tip,
            Anchor,
            FP;

        float Circ1Rad, TargetDist;

        Vector3 FPP
        {
            get
            {
                return Pivot.InverseTransformPoint(FP.position);
            }
        }

        Vector3 SphCenter
        {
            get
            {
                return Pivot.InverseTransformPoint(Anchor.position);
            }
        }

        Vector3 TipPos
        {
            get
            {
                return Tip.localPosition;
            }
        }


        public VectorFin(Transform PivotIn, Transform TipIn,Transform fpIn, Transform AnchorIn)
        {
            Pivot = PivotIn;
            Tip = TipIn;
            Anchor = AnchorIn;
            FP = fpIn;

            Circ1Rad = Vector3.Distance(new Vector3(0,0,0), TipPos);
            TargetDist = Vector3.Distance(Pivot.InverseTransformPoint(FP.position), SphCenter);
        }

        public void Update()
        {
            if (Vector3.Distance(TipPos, SphCenter) == TargetDist) return;

            // Project centers and coordinates within Pivot localSpace to the xy plane.
            Vector2
                tip = new Vector2(TipPos.x, TipPos.y),
                Circ1Cen = new Vector2(0, 0),
                Circ2Cen = new Vector2(SphCenter.x, SphCenter.y),
                FixPoint = new Vector2(FPP.x, FPP.y);

            // Find the radius of the circle intersection between the xy plane and a sphere with the anchor as center and target distance as radius. Using pythagoras.
            float Circ2Rad = (float)Math.Sqrt(Math.Pow(TargetDist, 2) + Math.Pow(SphCenter.z, 2)), dist = Vector2.Distance(Circ1Cen, Circ2Cen);

            // Find a and h.
            double a = (Circ1Rad * Circ1Rad - Circ2Rad * Circ2Rad + dist * dist) / (2 * dist);
            double h = Math.Sqrt(Circ1Rad * Circ1Rad - a * a);

            // Find P2.
            double cx2 = Circ1Cen.x + a * (Circ2Cen.x - Circ1Cen.x) / dist;
            double cy2 = Circ1Cen.y + a * (Circ2Cen.y - Circ1Cen.y) / dist;

            // Get the points P3.
            Vector2 intersection1 = new Vector2(
                (float)(cx2 + h * (Circ2Cen.y - Circ1Cen.y) / dist),
                (float)(cy2 - h * (Circ2Cen.x - Circ1Cen.x) / dist));
            Vector2 intersection2 = new Vector2(
                (float)(cx2 - h * (Circ2Cen.y - Circ1Cen.y) / dist),
                (float)(cy2 + h * (Circ2Cen.x - Circ1Cen.x) / dist));

            float angle = 0;

            if (Vector2.Distance(intersection1, FixPoint) < Vector2.Distance(intersection2, FixPoint))
                angle = Mathf.Deg2Rad * Vector2.Angle(tip, intersection1);
            else
                angle = Mathf.Deg2Rad * Vector2.Angle(tip, intersection2);

                Pivot.RotateAroundLocal(new Vector3(0, 0, 1), Vector2.Distance(tip, Circ2Cen) < Circ2Rad ? -angle : angle);
        }

    }

    public class FinSet : HashSet<VectorFin>
    {
        public void Update(bool Active)
        {
            if (Active)
            {                                
                foreach (VectorFin fin in this)
                    fin.Update();
            }
        }
    }

    public class ModuleAdvGimbal : ModuleGimbal
    {
        [KSPField]
        public string
            FinPivotName = "",
            FinTipName = "",
            FinAnchorName = "",
            FixPointName = "";

        FinSet fins;

        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) return;
            flightStarted = true;

            fins = new FinSet();

            foreach (Transform gimbal in part.FindModelTransforms(gimbalTransformName))
                foreach (Transform finParent in gimbal.parent)
                    foreach (Transform fin in finParent)
                        if(fin.name == FinPivotName)
                            fins.Add(new VectorFin(fin, fin.FindChild(FinTipName), finParent.FindChild(FixPointName), gimbal.FindChild(FinAnchorName)));

        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!flightStarted) return;

            base.OnFixedUpdate();
            fins.Update(isEnabled);
        }
    }

    public class ModuleAnimatedIntake : PartModule
    {
        [KSPField]
        public string Animation = "";

        [KSPField]
        public float Speed0 = 1, Speed1 = 1;

        [KSPField]
        public bool fixAnimLayers = false;


        Animation anim
        {
            get
            {
                return part.FindModelAnimators(Animation)[0];
            }
        }

        ModuleEngines engine;

        bool EngineIgnited
        {
            get
            {
                return engine.EngineIgnited;
            }
        }

        float currentThrottle
        {
            get
            {
                return engine.currentThrottle;
            }
        }


        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            flightStarted = true;

            if (part.Modules.Contains("ModuleEngines"))
                engine = part.Modules["ModuleEngines"] as ModuleEngines;

            anim[Animation].wrapMode = WrapMode.Loop;

            if (fixAnimLayers)
            {
                int i = 0;
                foreach (AnimationState s in anim)
                {
                    s.layer = i;
                    i++;
                }
            }
        }

        float prevThrottle = 0;
        bool playing = false;
        public override void OnUpdate()
        {
            if (!flightStarted) return;

            if (EngineIgnited && currentThrottle != 0)
            {
                if (!playing)
                {
                    anim.Play(Animation);
                    playing = true;
                }


                if (currentThrottle != prevThrottle)
                {
                    prevThrottle = currentThrottle;
                    anim[Animation].speed = ((Speed0 * (1 - prevThrottle)) + (Speed1 * prevThrottle));
                }
            }
            else
            {
                if (playing)
                {
                    anim.Stop(Animation);
                    playing = false;
                }
            }

        }
    }

    public class ModuleSpoiler : PartModule
    {
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

        protected Animation anim
        {
            get { return part.FindModelAnimators(Animation)[0]; }
        }

        [KSPAction("ActionToggle", KSPActionGroup.Brakes, guiName = "Toggle")]
        public void ActionToggle(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate && animState == 0)
                SetMode1();
            else
                if (param.type == KSPActionType.Deactivate && animState == 1)
                    SetMode0();
        }


        [KSPEvent(name = "Mode1Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode1Event()
        {
            if (animState == 0)
                SetMode1();
        }
        [KSPEvent(name = "Mode0Event", active = false, guiActive = true, guiName = "Mode1")]
        public void Mode0Event()
        {
            if (animState == 1)
                SetMode0();
        }

        void SetMode1()
        {
            anim[Animation].speed = Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);

            if (anim[Animation].normalizedTime == 1)
                anim[Animation].normalizedTime = 0;

            animState = 1;

            Events["Mode1Event"].active = false;
            Events["Mode0Event"].active = true;
        }

        void SetMode0()
        {
            anim[Animation].speed = -Mathf.Abs(anim[Animation].speed);

            anim.Play(Animation);

            if (anim[Animation].normalizedTime == 0)
                anim[Animation].normalizedTime = 1;

            animState = 0;

            Events["Mode1Event"].active = true;
            Events["Mode0Event"].active = false;
        }


        void UpdateDrag()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            part.minimum_drag = (MinimumDrag0 * (1 - anim[Animation].normalizedTime)) + (MinimumDrag1 * anim[Animation].normalizedTime);
            part.maximum_drag = (MaximumDrag0 * (1 - anim[Animation].normalizedTime)) + (MaximumDrag1 * anim[Animation].normalizedTime);

            if (part.Modules.Contains("ModuleLandingGear"))
            {
                (part.Modules["ModuleLandingGear"] as ModuleLandingGear).stowedDragMin = (part.Modules["ModuleLandingGear"] as ModuleLandingGear).deployedDragMin = part.minimum_drag;
                (part.Modules["ModuleLandingGear"] as ModuleLandingGear).stowedDragMax = (part.Modules["ModuleLandingGear"] as ModuleLandingGear).deployedDragMax = part.maximum_drag;
            }
        }


        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state)
        {
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

        public override void OnUpdate()
        {
            if (!flightStarted) return;

            if (anim.IsPlaying(Animation))
                UpdateDrag();
        }

    }
}
