using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteGUIWindow : AbstractGUIWindow {

        Satellite mSat;
        SatelliteConfigFragment mSatelliteFragment;
        AntennaConfigFragment mAntennaFragment;

        public SatelliteGUIWindow(Satellite sat)
            : base("Satellite Configuration", new Rect(100, 100, 0, 0)) {
            mSat = sat;
        }

        public override void Window(int id) {
            GUILayout.BeginHorizontal();
            {
                mSatelliteFragment.Draw();
                if (mAntennaFragment != null) {
                    mAntennaFragment.Draw();
                }
            }
            GUILayout.EndHorizontal();
            base.Window(id);
        }

        void AntennaClick(IAntenna antenna) {
            OnAntennaFragmentClose();
            mAntennaFragment = new AntennaConfigFragment(antenna, OnAntennaFragmentClose, x => mSatelliteFragment.RefreshAntennae());
        }

        void OnAntennaFragmentClose() {
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

    public class SatelliteConfigFragment : IGUIFragment, IDisposable {
        Satellite mFocus;
        List<IAntenna> mFocusAntennas;
        OnClick mOnClose;
        OnAntenna mOnClick;
        String[] mListCache;
        Vector2 mScrollPosition;
        String mName;
        int mScrollState;

        public SatelliteConfigFragment(Satellite sat, OnClick onClose, OnAntenna onClick) {
            mFocus = sat;
            mName = sat.Name;
            mOnClose = onClose;
            mOnClick = onClick;
            mScrollPosition = Vector2.zero;
            mListCache = new String[] { };
            mScrollState = -1;

            RTCore.Instance.Antennas.Registered += OnAntenna;
            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Registered += OnSatellite;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;
            GameEvents.onVesselRename.Add(OnVesselRename);

            RefreshAntennae();
        }

        public void Draw() {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Box(mName);
                    RTUtil.Button("Name", () => { mFocus.SignalProcessor.Vessel.RenameVessel(); },
                        GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.MinHeight(300), GUILayout.MinWidth(300));
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    RTUtil.GroupButton(1, mListCache, ref mScrollState, (x) => mOnClick.Invoke(mFocusAntennas[x]));
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("Close", mOnClose);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Registered -= OnAntenna;
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            RTCore.Instance.Satellites.Registered -= OnSatellite;
            RTCore.Instance.Satellites.Unregistered -= OnSatellite;
        }

        public void RefreshAntennae() {
            mFocusAntennas = RTCore.Instance.Antennas.For(mFocus).Where(a => a.CanTarget).ToList();
            mListCache = new String[mFocusAntennas.Count];
            for (int i = 0; i < mListCache.Length; i++) {
                mListCache[i] = (mFocusAntennas[i].Name ?? "No Name") + '\n' + (RTUtil.TargetName(mFocusAntennas[i].DishTarget));
            }
        }

        void OnVesselRename(GameEvents.FromToAction<String, String> ft) {
            mName = mFocus.Name;
        }

        void OnSatellite(ISatellite satellite) {
            if (satellite == mFocus) {
                mOnClose.Invoke();
            }
        }

        void OnAntenna(IAntenna antenna) {
            if (mFocusAntennas.Contains(antenna)) {
                RefreshAntennae();
            }
        }
    }
}
