using System;
using UnityEngine;

namespace RemoteTech {
    public class MissionControlGUI {

        bool mFolded = false;

        public void Load() {
            RTUtil.Log("MissionControlGUI loaded.");
        }

        public void Draw() {
            if(FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null) {
                // We're flying!
                GUI.Button(new Rect(152, 0, 32, 32), new GUIContent(RTCore.Instance().Assets.ImgSat, "Click to Toggle"));
            }
        }
    }
}
