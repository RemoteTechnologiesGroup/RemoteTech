using System;
using UnityEngine;

namespace RemoteTech {
    public class GuiManager : IDisposable {

        private readonly RTCore mCore;
        private Path.Type mIndicator;
        private String mTooltip;

        public GuiManager(RTCore core) {
            mCore = core;
            mCore.Network.ConnectionUpdated += OnConnectionUpdate;
        }

        public void Draw() {
            if (!MapView.MapIsEnabled) {
                DrawIndicator();
            }
            if (Event.current.type == EventType.KeyUp) {
                if (Event.current.keyCode == KeyCode.H) {
                    Event.current.Use();
                    OpenSettings();
                }
            }
        }

        public void OpenFlightComputer(Vessel v) {
            (new FlightComputerWindow()).Show();
        }

        public void OpenSatelliteConfig(Vessel v) {
            VesselSatellite s;
            if ((s = mCore.Satellites.For(v.id)) != null && mIndicator != Path.Type.NotConnected ) {
                (new SatelliteWindow(s)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, Vessel v) {
            ISatellite s = mCore.Satellites.For(v);
            if (s != null) {
                (new AntennaWindow(a, s)).Show();
            }
        }

        public void OpenAntennaConfig(IAntenna a, ISatellite s) {
            (new AntennaWindow(a, s)).Show();
        }

        public void OpenSettings() {
            (new SettingsWindow()).Show();
        }

        private void DrawIndicator() {
            GUI.skin = HighLogic.Skin;
            switch (mIndicator) {
                case Path.Type.Connected:
                    GUI.backgroundColor = Color.green;
                    break;
                case Path.Type.LocalControl:
                    GUI.backgroundColor = Color.yellow;
                    break;
                case Path.Type.NotConnected:
                    GUI.backgroundColor = Color.red;
                    break;
            }
            GUILayout.BeginArea(new Rect(0, 50, 200, 64));
            {
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("", () => OpenSatelliteConfig(FlightGlobals.ActiveVessel),
                                  GUILayout.Width(32), GUILayout.Height(32));
                    RTUtil.Button("", () => OpenFlightComputer(FlightGlobals.ActiveVessel),
                                  GUILayout.Width(32), GUILayout.Height(32));
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ||
                        Event.current.control
                    ? mTooltip
                    : "", GUILayout.Width(200), GUILayout.Height(32));

                if (Event.current.type == EventType.Repaint) {
                    Graphics.DrawTexture(new Rect(3, 3, 26, 26), MapView.OrbitIconsMap,
                        new Rect(0.2f, 0f, 0.2f, 0.2f), 0, 0, 0, 0, Color.grey,
                        MapView.OrbitIconsMaterial);

                    Graphics.DrawTexture(new Rect(37, 3, 26, 26), MapView.OrbitIconsMap,
                        new Rect(0.4f, 0f, 0.2f, 0.2f), 0, 0, 0, 0, Color.grey,
                        MapView.OrbitIconsMaterial);

                }
            }
            GUILayout.EndArea();

        }

        private void OnConnectionUpdate(Path<ISatellite> path) {
            if (path.Start != mCore.Satellites.For(FlightGlobals.ActiveVessel)) return;

            mIndicator = path.State;
            mTooltip = (mIndicator != Path.Type.NotConnected)
                       ? "Delay: " + path.Delay.ToString("F1") + " seconds."
                       : "No connection";
        }

        public void Dispose() {
            mCore.Network.ConnectionUpdated -= OnConnectionUpdate;
        }
    }
}
