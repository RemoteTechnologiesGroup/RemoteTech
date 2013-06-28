using System;
using UnityEngine;

namespace RemoteTech {
    public class GuiManager : IDisposable, IConfigNode {
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
            MapView.OnEnterMapView += OnEnterMapView;
        }

        public void Load(ConfigNode node) {

        }

        public void Save(ConfigNode node) {

        }

        public void Dispose() {
            MapView.OnEnterMapView -= OnEnterMapView;
        }

        public void Draw() {
            if (!MapView.MapIsEnabled) {
                DrawIndicator();
            }
        }

        private void OnEnterMapView() {
            (new MapViewSatelliteWindow(true)).Show();
        }

        public void OpenFlightComputer(Vessel v) {
            VesselSatellite s;
            if ((s = mCore.Satellites.For(v.id)) != null && s.Connection.Exists) {
                (new FlightComputerWindow(s)).Show();
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
                RTUtil.Button(RTCore.Instance.Settings.IconCalc,
                    () => OpenFlightComputer(FlightGlobals.ActiveVessel),
                    GUILayout.Width(32), GUILayout.Height(32));
                GUILayout.Label(
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ||
                        Event.current.control
                    ? mTooltip
                    : "", GUILayout.Width(200), GUILayout.Height(32));
            }
            GUILayout.EndArea();
        }
    }
}
