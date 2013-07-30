using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class ModuleAdvGimbal : ModuleGimbal {
        [KSPField]
        public string
            FinPivotName = "",
            FinTipName = "",
            FinAnchorName = "",
            FixPointName = "";

        FinSet fins;

        bool flightStarted = false;
        public override void OnStart(PartModule.StartState state) {
            base.OnStart(state);
            if (state == StartState.Editor) return;
            flightStarted = true;

            fins = new FinSet();

            foreach (Transform gimbal in part.FindModelTransforms(gimbalTransformName))
                foreach (Transform fin in gimbal.parent)
                    if (fin.name == FinPivotName)
                        fins.Add(new VectorFin(fin, fin.FindChild(FinTipName), gimbal.FindChild(FinAnchorName)));
        }

        public override void OnFixedUpdate() {
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (!flightStarted) return;

            base.OnFixedUpdate();
            fins.Update(isEnabled);
        }
    }
}
