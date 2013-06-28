using UnityEngine;

namespace RemoteTech {
    public class MapViewSatelliteWindow : AbstractWindow {
        private VesselSatellite mSatellite;
        private AntennaConfigFragment mAntennaFragment;
        private SatelliteConfigFragment mSatelliteFragment;
        private bool mEnabled;
        private bool mRequireUplink;

        private bool ChangeUplinkAllowed {
            get { return !mRequireUplink || mSatellite.Connection.Exists; }
        }

        public MapViewSatelliteWindow(bool requireUplink)
        : base(null, new Rect(0, 0, 0, 0), WindowAlign.BottomRight) {
            MapView.OnExitMapView += Hide;
            GameEvents.onPlanetariumTargetChanged.Add(ChangeTarget);
            mRequireUplink = requireUplink;
            ChangeTarget(PlanetariumCamera.fetch.target);
        }

        private void ChangeTarget(MapObject mo) {
            if (mo != null && mo.type == MapObject.MapObjectType.VESSEL) {
                mSatellite = RTCore.Instance.Satellites.For(mo.vessel);
            } else {
                mSatellite = null;
            }
            OnSatelliteFragmentClose();
            if (mSatellite != null) {
                mSatelliteFragment = new SatelliteConfigFragment(mSatellite, Hide, OnAntennaClick);
            }
        }

        public override void Window(int id) {
            GUI.skin = null;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (mEnabled != mWindowPosition.ContainsMouse()) {
                mEnabled = mWindowPosition.ContainsMouse();
                mWindowPosition.height = 64;
                mWindowPosition.width = 64;
            }

            if (mEnabled && mSatelliteFragment != null && ChangeUplinkAllowed) {
                GUILayout.BeginHorizontal(Frame, GUILayout.Height(300));
                {
                    if (mAntennaFragment != null) {
                        mAntennaFragment.Draw();
                    }
                    mSatelliteFragment.Draw();
                }
                GUILayout.EndHorizontal();
            } else if(mSatellite != null) {
                Color push = GUI.backgroundColor;
                GUI.backgroundColor = ChangeUplinkAllowed ? push : Color.red;
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(RTCore.Instance.Settings.IconSat, HighLogic.Skin.button, 
                        GUILayout.Width(32), GUILayout.Height(32));
                }
                GUILayout.EndHorizontal();
                GUI.backgroundColor = push;
            }
            base.Window(id);
        }

        protected override void Draw() {
            base.Draw();
        }

        private void OnAntennaClick(IAntenna antenna) {
            OnAntennaFragmentClose();
            if (antenna != null) {
                mAntennaFragment = new AntennaConfigFragment(antenna, OnAntennaFragmentClose,
                    x => mSatelliteFragment.RebuildAntennaList());
            }
        }

        private void OnSatelliteFragmentClose() {
            if (mSatelliteFragment != null) {
                mSatelliteFragment.Dispose();
                mSatelliteFragment = null;
            }
        }

        private void OnAntennaFragmentClose() {
            if (mAntennaFragment != null) {
                mAntennaFragment.Dispose();
                mAntennaFragment = null;
            }
        }

        public override void Hide() {
            base.Hide();
            OnAntennaFragmentClose();
            OnSatelliteFragmentClose();
            MapView.OnExitMapView -= Hide;
            GameEvents.onPlanetariumTargetChanged.Remove(ChangeTarget);
        }
    }
}
