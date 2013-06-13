using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class RTGUIManager : IDisposable {
        RTCore mCore;
        bool mIndicatorEnabled;

        public RTGUIManager(RTCore core) {
            mCore = core;
            mCore.Network.ConnectionUpdated += OnConnectionUpdate;
        }

        public void Draw() {
            if(!MapView.MapIsEnabled) {
                DrawIndicator();
            }
        }

        public void OpenSatelliteConfig(Vessel v) {
            Satellite sat;
            if ((sat = mCore.Satellites.For(v.id)) != null && mIndicatorEnabled) {
                (new SatelliteGUIWindow(sat)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a) {
            (new AntennaGUIWindow(a)).Show();
        }

        void DrawIndicator() {
            GUI.skin = HighLogic.Skin;
            GUI.backgroundColor = mIndicatorEnabled ? Color.green : Color.red;
            if (GUI.Button(new Rect(0, 100, 32, 32), "")) {
                OpenSatelliteConfig(FlightGlobals.ActiveVessel);
            }
            Graphics.DrawTexture(
                new Rect(3, 103, 26, 26),
                MapView.OrbitIconsMap,
                new Rect(0.2f, 0f, 0.2f, 0.2f), 0, 0, 0, 0, 
                Color.grey, MapView.OrbitIconsMaterial);
        }

        void OnConnectionUpdate(Path<ISatellite> path) {
            mIndicatorEnabled = path.Exists && (path.Target == mCore.Satellites.For(FlightGlobals.ActiveVessel));
        }

        public void Dispose() {
            mCore.Network.ConnectionUpdated -= OnConnectionUpdate;
        }
    }
}
