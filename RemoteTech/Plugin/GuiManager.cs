using System;
using UnityEngine;

namespace RemoteTech {
    public class GuiManager : IDisposable {

        private enum State {
            NotConnected,
            LocalControl,
            Connected,
        }

        private State mIndicator;
        private String mTooltip;

        private readonly RTCore mCore;

        public GuiManager(RTCore core) {
            mCore = core;
        }

        public void Dispose() {

        }

        public void Draw() {
            if (!MapView.MapIsEnabled) {
                DrawIndicator();
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R && 
                    GameSettings.MODIFIER_KEY.GetKeyDown()) {
                Event.current.Use();
                OpenSettings();
            }
        }

        public void OpenFlightComputer(Vessel v) {
            VesselSatellite s;
            if ((s = mCore.Satellites.For(v.id)) != null) {
                (new FlightComputerWindow(s)).Show();
            }
            
        }

        public void OpenSatelliteConfig(Vessel v) {
            VesselSatellite s;
            if ((s = mCore.Satellites.For(v.id)) != null && mIndicator != State.NotConnected) {
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
            if (Event.current.type == EventType.Repaint) {
                VesselSatellite focus = mCore.Satellites.For(FlightGlobals.ActiveVessel);
                mIndicator = focus.Connection.Exists
                             ? State.Connected
                             : (focus.LocalControl ? State.LocalControl : State.NotConnected);
                mTooltip = (mIndicator != State.NotConnected)
                           ? "Delay: " + focus.Connection.Delay.ToString("F1") + " seconds."
                           : "No connection";
            }
            switch (mIndicator) {
                case State.Connected:
                    GUI.backgroundColor = Color.green;
                    break;
                case State.LocalControl:
                    GUI.backgroundColor = Color.yellow;
                    break;
                case State.NotConnected:
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
    }
}
