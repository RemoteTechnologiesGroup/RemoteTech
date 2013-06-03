using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    class RTGUIManager {

        RTCore mCore;

        public RTGUIManager(RTCore core) {
            mCore = core;
        }

        public void Draw() {
            if(!MapView.MapIsEnabled) {
                DrawOverlay();
            }
        }

        void OpenSatelliteConfig(Vessel v) {
            ISatellite sat;
            if((sat = mCore.Satellites.For(v)) != null) {
                (new SatelliteGUIWindow(sat)).Show();
            }
        }

        void OpenAntennaConfig(IAntenna a) {
            (new AntennaGUIWindow(a)).Show();
        }

        void DrawOverlay() {
            GUI.skin = HighLogic.Skin;
            if (GUI.Button(new Rect(0, 100, 32, 32), "")) {
                OpenSatelliteConfig(FlightGlobals.ActiveVessel);
            }
            Graphics.DrawTexture(
                new Rect(3, 103, 26, 26),
                MapView.OrbitIconsMap,
                new Rect(0.2f, 0f, 0.2f, 0.2f), 0, 0, 0, 0, 
                Color.grey, MapView.OrbitIconsMaterial);
        }

    }
}
