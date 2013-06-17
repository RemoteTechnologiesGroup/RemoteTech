using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class AntennaWindow : AbstractWindow {
        private readonly IAntenna mAntenna;
        private readonly ISatellite mSatellite;
        private AntennaConfigFragment mAntennaFragment;

        public AntennaWindow(IAntenna antenna, ISatellite sat)
        : base("Antenna Configuration", new Rect(100, 100, 0, 0)) {
            mAntenna = antenna;
            mSatellite = sat;
        }

        public override void Window(int id) {
            base.Window(id);
            mAntennaFragment.Draw();
        }

        public override void Show() {
            mAntennaFragment = new AntennaConfigFragment(mAntenna, mSatellite, Hide, a => { });
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            mAntennaFragment.Dispose();
        }
    }

    public class AntennaConfigFragment : IFragment, IDisposable {
        private static readonly String[] mHeaderStrings = {"Satellites", "Planets"};

        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(200));
            {
                RTUtil.GroupButton(2, mHeaderStrings, ref mHeaderState, OnClickHeader);

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    mScrollPosition = GUILayout.BeginScrollView(mScrollPosition,
                        GUILayout.MinHeight(300));
                    {
                        RTUtil.GroupButton(1, mHeaderState == 1 ? mPlanetText : mSatelliteText,
                                           ref mGroupState, OnClickSelection);
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private readonly IAntenna mFocusAntenna;
        private readonly ISatellite mFocusSatellite;
        private readonly OnClick mOnClose;
        private readonly OnAntenna mOnUpdate;
        private int mGroupState;
        private int mHeaderState;
        private String[] mPlanetText;
        private List<CelestialBody> mPlanets;
        private String[] mSatelliteText;
        private List<ISatellite> mSatellites;
        private Vector2 mScrollPosition;

        public AntennaConfigFragment(IAntenna antenna, ISatellite sat, OnClick onClose,
                                     OnAntenna onUpdate) {
            mFocusAntenna = antenna;
            mFocusSatellite = sat;
            mOnClose = onClose;
            mOnUpdate = onUpdate;
            mScrollPosition = Vector2.zero;
            OnClickHeader(mHeaderState = 0);
            mGroupState = 0;

            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;
            RTCore.Instance.Satellites.Registered += OnSatellite;
        }

        private void ShowPlanetView() {
            mHeaderState = 1;
            mPlanets = RTCore.Instance.Network.Planets.Values.ToList();
            mGroupState = mPlanets.FindIndex(cb => cb.Guid() == mFocusAntenna.DishTarget);
            mPlanetText = new String[mPlanets.Count];
            for (int i = 0; i < mPlanetText.Length; i++) {
                mPlanetText[i] = mPlanets[i].name;
            }
        }

        private void ShowSatelliteView() {
            mHeaderState = 0;
            mSatellites = RTCore.Instance.Network.Where(s => s != mFocusSatellite).ToList();
            mSatellites.Add(mFocusSatellite);
            mGroupState = mSatellites.FindIndex(s => s.Guid == mFocusAntenna.DishTarget);
            mSatelliteText = new String[mSatellites.Count];
            for (int i = 0; i < mSatelliteText.Length - 1; i++) {
                mSatelliteText[i] = mSatellites[i].Name;
            }
            mSatelliteText[mSatelliteText.Length - 1] = "No Target";
        }

        private void OnClickHeader(int state) {
            switch (state) {
                default:
                case 0: // Satellites
                    ShowSatelliteView();
                    break;
                case 1: // Planets
                    ShowPlanetView();
                    break;
            }
        }

        private void OnClickSelection(int state) {
            switch (mHeaderState) {
                default:
                case 0: // Satellites
                    mFocusAntenna.DishTarget = mSatellites[state] == mFocusSatellite
                                               ? Guid.Empty
                                               : mSatellites[state].Guid;
                    mOnUpdate(mFocusAntenna);
                    break;
                case 1: // Planets
                    mFocusAntenna.DishTarget = mPlanets[state].Guid();
                    mOnUpdate(mFocusAntenna);
                    break;
            }
        }

        private void OnSatellite(ISatellite satellite) {
            if (mHeaderState == 0) {
                ShowSatelliteView();
            }
        }

        private void OnAntenna(IAntenna antenna) {
            if (antenna == mFocusAntenna) {
                mOnClose.Invoke();
            }
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            RTCore.Instance.Satellites.Unregistered -= OnSatellite;
            RTCore.Instance.Satellites.Registered -= OnSatellite;
        }
    }
}
