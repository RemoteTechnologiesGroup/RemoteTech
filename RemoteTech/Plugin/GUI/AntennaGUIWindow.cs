using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class AntennaGUIWindow : AbstractGUIWindow {
        IAntenna mAntenna;
        AntennaConfigFragment mAntennaFragment;

        public AntennaGUIWindow(IAntenna antenna)
            : base("Antenna Configuration", new Rect(100, 100, 0, 0)) {
            mAntenna = antenna;
        }

        public override void Window(int id) {
            mAntennaFragment.Draw();
            base.Window(id);
        }

        public override void Show() {
            mAntennaFragment = new AntennaConfigFragment(mAntenna, Hide, a => { });
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            mAntennaFragment.Dispose();
        }
    }

    public class AntennaConfigFragment : IGUIFragment, IDisposable {
        static String[] mHeader = { "Satellites", "Planets" };
        int mHeaderState;
        int mSatellitesState;
        int mPlanetsState;
        IAntenna mFocus;
        List<ISatellite> mSatellites;
        List<CelestialBody> mPlanets;
        String[] mSatelliteText;
        String[] mPlanetText;
        OnClick mOnClose;
        OnAntenna mOnUpdate;
        Vector2 mScrollPosition;

        public AntennaConfigFragment(IAntenna antenna, OnClick onClose, OnAntenna onUpdate) {
            mFocus = antenna;
            mOnClose = onClose;
            mOnUpdate = onUpdate;
            mHeaderState = 0;
            mScrollPosition = Vector2.zero;

            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;
            RTCore.Instance.Satellites.Registered += OnSatellite;

            RefreshPlanets();
            RefreshSatellites();
        }

        public void Draw() {
            GUILayout.BeginVertical();
            {
                RTUtil.GroupButton(2, mHeader, ref mHeaderState);
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.MinHeight(300), GUILayout.MinWidth(300));
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    if (mHeaderState == 1) {
                        RTUtil.GroupButton(1, mPlanetText, ref mPlanetsState,
                            x => { mFocus.DishTarget = mPlanets[mPlanetsState].Guid(); mOnUpdate(mFocus); });
                    } else {
                        RTUtil.GroupButton(1, mSatelliteText, ref mSatellitesState,
                            x => { mFocus.DishTarget = mSatellites[mSatellitesState].Guid; mOnUpdate(mFocus); });
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            RTCore.Instance.Satellites.Unregistered -= OnSatellite;
            RTCore.Instance.Satellites.Registered -= OnSatellite;
        }

        void RefreshPlanets() {
            mPlanets = RTCore.Instance.Network.Planets.Values.ToList();
            mPlanetsState = mPlanets.FindIndex(cb => cb.Guid() == mFocus.DishTarget);
            mPlanetText = new String[mPlanets.Count];
            for (int i = 0; i < mPlanetText.Length; i++) {
                mPlanetText[i] = mPlanets[i].name;
            }
        }

        void RefreshSatellites() {
            mSatellites = RTCore.Instance.Network.ToList();
            mSatellitesState = mSatellites.FindIndex(s => s.Guid == mFocus.DishTarget);
            mSatelliteText = new String[mSatellites.Count];
            for (int i = 0; i < mSatelliteText.Length; i++) {
                mSatelliteText[i] = mSatellites[i].Name;
            }
        }

        void OnSatellite(ISatellite satellite) {
            RefreshSatellites();
        }

        void OnAntenna(IAntenna antenna) {
            if (antenna == mFocus) {
                mOnClose.Invoke();
            }
        }
    }
}
