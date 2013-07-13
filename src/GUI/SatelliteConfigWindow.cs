using System;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteConfigWindow : AbstractWindow {
        private readonly VesselSatellite mSatellite;
        private SatelliteConfigFragment mSatelliteFragment;
        private AntennaConfigFragment mAntennaFragment;

        public SatelliteConfigWindow(String name, Rect pos, WindowAlign align,
                VesselSatellite satellite) : base(name, pos, align) {
            mSatellite = satellite;
        }

        public override void Window(int id) {
            GUI.skin = null;

            GUILayout.BeginHorizontal(Frame, GUILayout.Height(400));
            {
                if (mAntennaFragment != null) {
                    mAntennaFragment.Draw();
                }
                mSatelliteFragment.Draw();
            }
            GUILayout.EndHorizontal();
            base.Window(id);
        }

        public override void Show() {
            base.Show();
            mSatelliteFragment = new SatelliteConfigFragment(mSatellite, Hide, OnAntennaClick);
        }

        public override void Hide() {
            base.Hide();
            OnAntennaFragmentClose();
            OnSatelliteFragmentClose();
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
    }
}
