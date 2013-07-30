using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class ModuleIntakeAnimated : PartModule {
        [KSPField]
        public string Animation = "";

        [KSPField]
        public float Speed0 = 1, Speed1 = 1;

        [KSPField]
        public bool fixAnimLayers = false;


        Animation anim {
            get {
                return part.FindModelAnimators(Animation)[0];
            }
        }

        ModuleEngines engine;

        bool EngineIgnited {
            get {
                return engine.EngineIgnited;
            }
        }

        float currentThrottle {
            get {
                return engine.currentThrottle;
            }
        }


        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) return;

            flightStarted = true;

            if (part.Modules.Contains("ModuleEngines"))
                engine = part.Modules["ModuleEngines"] as ModuleEngines;

            anim[Animation].wrapMode = WrapMode.Loop;

            if (fixAnimLayers) {
                int i = 0;
                foreach (AnimationState s in anim) {
                    s.layer = i;
                    i++;
                }
            }
        }

        float prevThrottle = 0;
        bool playing = false;
        public override void OnUpdate() {
            if (!flightStarted) return;

            if (EngineIgnited && currentThrottle != 0) {
                if (!playing) {
                    anim.Play(Animation);
                    playing = true;
                }


                if (currentThrottle != prevThrottle) {
                    prevThrottle = currentThrottle;
                    anim[Animation].speed = ((Speed0 * (1 - prevThrottle)) + (Speed1 * prevThrottle));
                }
            } else {
                if (playing) {
                    anim.Stop(Animation);
                    playing = false;
                }
            }

        }
    }
}
