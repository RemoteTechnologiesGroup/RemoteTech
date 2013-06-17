using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteWindow : AbstractWindow {
        private readonly VesselSatellite mSat;
        private AntennaConfigFragment mAntennaFragment;
        private SatelliteConfigFragment mSatelliteFragment;

        public SatelliteWindow(VesselSatellite sat)
        : base("Satellite Configuration", new Rect(100, 100, 0, 0)) {
            mSat = sat;
        }

        public override void Window(int id) {
            base.Window(id);
            GUILayout.BeginHorizontal();
            {
                mSatelliteFragment.Draw();
                if (mAntennaFragment != null) {
                    mAntennaFragment.Draw();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void AntennaClick(IAntenna antenna) {
            OnAntennaFragmentClose();
            mAntennaFragment = new AntennaConfigFragment(antenna, mSat, OnAntennaFragmentClose,
                                                         x => mSatelliteFragment.RefreshAntennae());
        }

        private void OnAntennaFragmentClose() {
            if (mAntennaFragment != null) {
                mAntennaFragment.Dispose();
                mAntennaFragment = null;
            }
        }

        public override void Show() {
            mSatelliteFragment = new SatelliteConfigFragment(mSat, Hide, AntennaClick);
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            OnAntennaFragmentClose();
            if (mSatelliteFragment != null) {
                mSatelliteFragment.Dispose();
                mSatelliteFragment = null;
            }
        }
    }

    public class SatelliteConfigFragment : IFragment, IDisposable {
        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(200));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Box(mName);
                    RTUtil.Button("Name", () => mFocus.SignalProcessor.Vessel.RenameVessel(),
                                  GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, 
                        GUILayout.MinHeight(300));
                    {
                        RTUtil.GroupButton(1, mListCache, ref mScrollState,
                                           (x) => mOnClick.Invoke(mFocusAntennas[x]));
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private readonly VesselSatellite mFocus;
        private readonly OnAntenna mOnClick;
        private readonly OnClick mOnClose;
        private List<IAntenna> mFocusAntennas;
        private String[] mListCache;
        private String mName;
        private Vector2 mScrollPosition;
        private int mScrollState;

        public SatelliteConfigFragment(VesselSatellite sat, OnClick onClose, OnAntenna onClick) {
            mFocus = sat;
            mName = sat.Name;
            mOnClose = onClose;
            mOnClick = onClick;
            mScrollPosition = Vector2.zero;
            mListCache = new String[] {};
            mScrollState = -1;

            RTCore.Instance.Antennas.Registered += OnAntenna;
            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Registered += OnSatellite;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;
            GameEvents.onVesselRename.Add(OnVesselRename);

            RefreshAntennae();
        }

        public void RefreshAntennae() {
            mFocusAntennas = RTCore.Instance.Antennas.For(mFocus).Where(a => a.CanTarget).ToList();
            mListCache = new String[mFocusAntennas.Count];
            for (int i = 0; i < mListCache.Length; i++) {
                mListCache[i] = (mFocusAntennas[i].Name ?? "No Name") + '\n' +
                                (RTUtil.TargetName(mFocusAntennas[i].DishTarget));
            }
        }

        private void OnVesselRename(GameEvents.FromToAction<String, String> data) {
            mName = mFocus.Name;
        }

        private void OnSatellite(ISatellite satellite) {
            if (satellite == mFocus) {
                mOnClose.Invoke();
            }
        }

        private void OnAntenna(IAntenna antenna) {
            if (mFocusAntennas.Contains(antenna)) {
                RefreshAntennae();
            }
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Registered -= OnAntenna;
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            RTCore.Instance.Satellites.Registered -= OnSatellite;
            RTCore.Instance.Satellites.Unregistered -= OnSatellite;
        }
    }
}
