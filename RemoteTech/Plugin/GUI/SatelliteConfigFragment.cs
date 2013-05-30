using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteFragment : IGUIFragment, IDisposable {

        ISatellite mFocus;
        List<IAntenna> mFocusAntennas;
        AntennaGUIWindow mOpenAntenna;
        OnClose mOnClose;
        String[] mListCache;
        Vector2 mScrollPosition;
        int mScrollState;

        public SatelliteFragment(ISatellite sat, OnClose onClose) {
            mFocus = sat;
            mOnClose = onClose;
            mScrollPosition = Vector2.zero;
            mListCache = new String[] { };
            mScrollState = -1;

            RTCore.Instance.Antennas.Registered += OnAntenna;
            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Registered += OnSatellite;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;

            RefreshAntennae();
        }

        public void Draw() {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(250), GUILayout.Height(300));
            RTUtil.GroupButton(230, 30, mListCache, ref mScrollState, x => {
                if (mOpenAntenna != null) {
                    mOpenAntenna.Hide();
                }
                mOpenAntenna = new AntennaGUIWindow(mFocusAntennas[x]);
                mOpenAntenna.Show();
                mScrollState = -1;
            });
            GUILayout.EndScrollView();
            RTUtil.Button(250, 30, "Close", () => mOnClose.Invoke());
            GUILayout.EndVertical();
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Registered -= OnAntenna;
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            RTCore.Instance.Satellites.Registered -= OnSatellite;
            RTCore.Instance.Satellites.Unregistered -= OnSatellite;
        }

        void RefreshAntennae() {
            mFocusAntennas = RTCore.Instance.Antennas.For(mFocus.Vessel).ToList();
            mListCache = new String[mFocusAntennas.Count];
            for (int i = 0; i < mListCache.Length; i++) {
                mListCache[i] = mFocusAntennas[i].Name ?? "No Name" + ": " + mFocusAntennas[i].Target;
            }
        }

        void OnSatellite(ISatellite satellite) {
            if (satellite == mFocus) {
                mOnClose.Invoke();
            }
        }

        void OnAntenna(IAntenna antenna) {
            if(mFocusAntennas.Contains(antenna)) {
                RefreshAntennae();
            }
        }
    }

    public class SatelliteGUIWindow : AbstractGUIWindow {

        ISatellite mSat;
        SatelliteFragment mFragment;
        Rect mWindowPosition;

        public SatelliteGUIWindow(ISatellite sat) {
            mSat = sat;
            mWindowPosition = new Rect(0, 0, 250, 400);
        }

        void Window(int id) {
            mFragment.Draw();
            GUI.DragWindow();
        }

        public override void Show() {
            mFragment = new SatelliteFragment(mSat, () => Hide());
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            mFragment.Dispose();
        }

        protected override void Draw() {
            mWindowPosition = GUILayout.Window(45, mWindowPosition, Window, "Satellite Configuration");
        }
    }
}
